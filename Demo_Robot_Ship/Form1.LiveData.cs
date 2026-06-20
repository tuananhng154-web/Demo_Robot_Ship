using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Demo_Robot_Ship
{
    public partial class Form1
    {
        private void RefreshUi()
        {
            RefreshRobotCards();
            RefreshOrderGrids();
            UpdateCurrentComparisonRow(GetCurrentRunStatus());
        }

        private void RefreshRobotCards()
        {
            foreach (Robot robot in fleet)
            {
                if (!robotCards.ContainsKey(robot.Id)) continue;

                RobotCard card = robotCards[robot.Id];
                int battery = (int)Math.Max(0, Math.Min(100, robot.Battery));
                card.Title.Text = string.Format("{0} - {1}", robot.Id, ToVietnameseStatus(robot.Status));
                card.Title.ForeColor = GetStatusColor(robot.Status);
                int batchCount = robot.CurrentOrders == null ? 0 : robot.CurrentOrders.Count;
                double effectiveBattery = BatteryModel.GetEffectiveBattery(robot.Battery, robot.BatteryHealth);
                string healthLevel = BatteryModel.GetHealthLevel(robot.BatteryHealth);
                card.Detail.Text = string.Format("Vị trí: ({0},{1}) | Tải: {2:0.0}/{3:0.0}kg | Đơn: {4}\r\nQĐ: {5} ô | Pin hiệu dụng: {6:0.0}% | Pin khỏe: {7:0.0}% ({8}) | Chai: {9:0.0}% | Chờ: {10}", robot.GridX, robot.GridY, robot.Payload, robot.MaxPayloadKg, batchCount, robot.TotalDistance, effectiveBattery, robot.BatteryHealth, healthLevel, robot.BatteryDegradation, robot.TotalWaitSteps);
                card.Battery.Value = battery;
                card.BatteryText.Text = string.Format("Pin {0}% | SoH {1:0.0}%", battery, robot.BatteryHealth);
                card.BatteryText.ForeColor = GetBatteryColor(robot.Battery);
            }
        }

        private void RefreshOrderGrids()
        {
            var pendingView = allOrders
                .Where(o => o.Status == "WAITING" || o.Status == "ASSIGNED" || o.Status == "DELIVERING")
                .Select(o => new OrderView(o))
                .ToList();

            var completedView = allOrders
                .Where(o => o.Status == "DELIVERED" || o.Status == "FAILED")
                .Select(o => new OrderView(o))
                .ToList();

            dgvPending.DataSource = null;
            dgvPending.DataSource = pendingView;

            dgvCompleted.DataSource = null;
            dgvCompleted.DataSource = completedView;

            tabPage1.Text = "Đang chờ (" + pendingView.Count + ")";
            tabPage2.Text = "Đã giao (" + completedView.Count + ")";

            dgvPending.ClearSelection();
            dgvCompleted.ClearSelection();
        }

        private string ToVietnameseStatus(string status)
        {
            if (status == "IDLE") return "RẢNH";
            if (status == "WAITING") return "ĐANG CHỜ";
            if (status == "ASSIGNED") return "ĐÃ GÁN";
            if (status == "DELIVERING") return "ĐANG GIAO";
            if (status == "RETURNING") return "VỀ TRẠM";
            if (status == "CHARGING") return "ĐANG SẠC";
            if (status == "LOW_BATTERY") return "PIN YẾU";
            if (status == "MAINTENANCE_REQUIRED") return "CẦN BẢO DƯỠNG";
            if (status == "MAINTENANCE") return "BẢO DƯỠNG";
            if (status == "DELIVERED") return "ĐÃ GIAO";
            if (status == "FAILED") return "LỖI";
            return status;
        }

        private void WriteLog(string message)
        {
            if (rtbLog == null)
            {
                txtLog.AppendText(DateTime.Now.ToString("HH:mm:ss") + " - " + message + Environment.NewLine);
                return;
            }

            Color color = ColorTranslator.FromHtml("#93C5FD");
            string prefix = "INFO";

            string lower = message.ToLower();
            if (lower.Contains("đơn mới")) { color = ColorTranslator.FromHtml("#C4B5FD"); prefix = "ORDER"; }
            else if (lower.Contains("điều phối")) { color = ColorTranslator.FromHtml("#FBBF24"); prefix = "DISPATCH"; }
            else if (lower.Contains("hoàn tất") || lower.Contains("đã giao")) { color = ColorTranslator.FromHtml("#86EFAC"); prefix = "DONE"; }
            else if (lower.Contains("kẹt") || lower.Contains("không tìm") || lower.Contains("quá tải")) { color = ColorTranslator.FromHtml("#FCA5A5"); prefix = "WARN"; }
            else if (lower.Contains("tạm dừng") || lower.Contains("làm mới")) { color = ColorTranslator.FromHtml("#E5E7EB"); prefix = "SYSTEM"; }

            string line = string.Format("[{0}] [{1,-8}] {2}\r\n", DateTime.Now.ToString("HH:mm:ss"), prefix, message);

            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.SelectionLength = 0;
            rtbLog.SelectionColor = color;
            rtbLog.AppendText(line);
            rtbLog.SelectionColor = rtbLog.ForeColor;

            TrimLogLines();
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.ScrollToCaret();
        }


        private void ClearScoreTestData()
        {
            scoreAllRows.Clear();
            foreach (string robotId in new string[] { "R1", "R3", "R2" })
            {
                if (!scoreRowsByRobot.ContainsKey(robotId)) scoreRowsByRobot[robotId] = new List<ScoreCandidateRow>();
                scoreRowsByRobot[robotId].Clear();
            }
            RefreshScoreTables();
            if (lblScoreSummary != null)
            {
                lblScoreSummary.Text = "Đã xóa. Chờ lần điều phối tiếp theo.";
            }
        }

        private void BeginScoreCycle(int waitingCount, int oldestWait, int remainingWait, bool isTimedOut, int robotCandidateCount)
        {
            scoreAllRows.Clear();
            foreach (string robotId in new string[] { "R1", "R3", "R2" })
            {
                if (!scoreRowsByRobot.ContainsKey(robotId)) scoreRowsByRobot[robotId] = new List<ScoreCandidateRow>();
                scoreRowsByRobot[robotId].Clear();
            }

            if (lblScoreSummary != null)
            {
                lblScoreSummary.Text = string.Format(
                    "{0} | Tick {1} | Waiting {2} | Wait {3}/{4} | Còn {5} | {6} | Robot {7}",
                    GetStrategyName(currentDispatchStrategy),
                    simulationTick,
                    waitingCount,
                    oldestWait,
                    MaxBatchWaitTicks,
                    remainingWait,
                    isTimedOut ? "Hết giờ" : "Đang chờ",
                    robotCandidateCount);
            }

            RefreshScoreTables();
        }

        private void UpsertCandidateScoreRow(string tag, AssignmentCandidate candidate, string reason)
        {
            if (candidate == null || candidate.ScoreBreakdown == null || candidate.Robot == null) return;

            ScoreCandidateRow row = BuildScoreCandidateRow(tag, candidate, reason);
            string robotId = row.Robot;
            if (!scoreRowsByRobot.ContainsKey(robotId)) scoreRowsByRobot[robotId] = new List<ScoreCandidateRow>();

            ScoreCandidateRow existing = scoreAllRows.FirstOrDefault(r => r.CandidateKey == row.CandidateKey);
            if (existing == null)
            {
                scoreAllRows.Add(row);
                scoreRowsByRobot[robotId].Add(row);
            }
            else
            {
                existing.Decision = row.Decision;
                existing.Reason = row.Reason;
                existing.FinalScoreText = row.FinalScoreText;
            }

            RefreshScoreTables();
        }

        private ScoreCandidateRow BuildScoreCandidateRow(string tag, AssignmentCandidate candidate, string reason)
        {
            string route = candidate.RoutePlan == null ? "N/A" : candidate.RoutePlan.RouteText;
            int moveSteps = candidate.RoutePlan == null ? 0 : candidate.RoutePlan.MoveSteps;
            int waitSteps = candidate.RoutePlan == null ? 0 : candidate.RoutePlan.WaitSteps;
            int estimatedTime = candidate.RoutePlan == null ? 0 : candidate.RoutePlan.EstimatedTimeTicks;
            string decision = "Ứng viên";
            if (string.Equals(tag, "BEST", StringComparison.OrdinalIgnoreCase)) decision = "TỐT NHẤT";
            else if (string.Equals(tag, "NO-DISPATCH", StringComparison.OrdinalIgnoreCase)) decision = "CHỜ THÊM";
            else if (string.Equals(tag, "ASSIGN", StringComparison.OrdinalIgnoreCase)) decision = "CHỌN";

            ScoreBreakdown score = candidate.ScoreBreakdown;
            return new ScoreCandidateRow
            {
                CandidateKey = MakeCandidateKey(candidate),
                Tick = simulationTick,
                Strategy = GetStrategyName(currentDispatchStrategy),
                Robot = candidate.Robot.Id,
                Decision = decision,
                Reason = reason,
                Orders = candidate.OrderText,
                Route = route,
                Load = string.Format("{0:0.0}/{1:0.0}kg ({2:0}%)", candidate.TotalWeight, candidate.Robot.MaxPayloadKg, candidate.LoadRatio * 100.0),
                Steps = string.Format("Move {0} | Wait {1} | Time {2}", moveSteps, waitSteps, estimatedTime),
                Battery = string.Format("Eff {0:0.0}% | Cost {1:0.0}% | After {2:0.0}%", candidate.EffectiveBattery, candidate.EstimatedEnergyCost, candidate.BatteryAfterMission),
                Health = string.Format("SoH {0:0.0}%", candidate.Robot.BatteryHealth),
                Delay = candidate.AvailableDelay.ToString() + " tick",
                LoadScore = score.LoadScore,
                DistanceScore = score.DistanceScore,
                TimeScore = score.TimeScore,
                BatteryScore = score.BatteryScore,
                HealthScore = score.HealthScore,
                WaitScore = score.WaitScore,
                DelayPenalty = score.RobotDelayPenalty,
                FinalScore = score.FinalScore,
                FinalScoreText = string.Format("{0:0.0}", score.FinalScore)
            };
        }

        private string MakeCandidateKey(AssignmentCandidate candidate)
        {
            string route = candidate.RoutePlan == null ? "N/A" : candidate.RoutePlan.RouteText;
            return string.Format("{0}|{1}|{2}|{3}|{4}|{5:0.000}", GetStrategyName(currentDispatchStrategy), candidate.Robot.Id, candidate.OrderText, route, candidate.AvailableDelay, candidate.Score);
        }

        private void RefreshScoreTables()
        {
            ScoreCandidateRow bestOverall = scoreAllRows.Count > 0
                ? scoreAllRows.OrderByDescending(r => r.FinalScore).First()
                : null;

            foreach (string robotId in new string[] { "R1", "R3", "R2" })
            {
                ScoreCandidateRow bestOfRobot = null;
                if (scoreRowsByRobot.ContainsKey(robotId) && scoreRowsByRobot[robotId].Count > 0)
                {
                    bestOfRobot = scoreRowsByRobot[robotId].OrderByDescending(r => r.FinalScore).First();
                }

                bool isChosenRobot = bestOverall != null && bestOfRobot != null &&
                                     string.Equals(bestOverall.CandidateKey, bestOfRobot.CandidateKey, StringComparison.Ordinal);
                string decision = isChosenRobot
                    ? (bestOverall.Decision == "CHỜ THÊM" ? "TẠM CHỜ" : "CHỌN")
                    : (bestOfRobot == null ? "" : "Ứng viên");

                if (scoreDetailGrids.ContainsKey(robotId))
                {
                    SetDetailGridData(scoreDetailGrids[robotId], BuildScoreDetailItems(bestOfRobot, decision, robotId));
                }

                if (scoreRobotBoxes.ContainsKey(robotId))
                {
                    scoreRobotBoxes[robotId].ForeColor = isChosenRobot
                        ? ColorTranslator.FromHtml("#16A34A")
                        : ColorTranslator.FromHtml("#111827");
                }
            }

            if (dgvBestScore != null)
            {
                string decision = bestOverall == null
                    ? ""
                    : (bestOverall.Decision == "CHỜ THÊM" ? "TẠM CHỜ" : "CHỌN");
                SetDetailGridData(dgvBestScore, BuildScoreDetailItems(bestOverall, decision, "BEST"));
            }

            if (bestScoreBox != null)
            {
                bestScoreBox.ForeColor = bestOverall == null
                    ? ColorTranslator.FromHtml("#111827")
                    : ColorTranslator.FromHtml("#16A34A");
            }
        }

        private List<ScoreDetailItem> BuildScoreDetailItems(ScoreCandidateRow row, string decision, string fallbackRobot)
        {
            List<ScoreDetailItem> items = new List<ScoreDetailItem>();

            if (row == null)
            {
                items.Add(new ScoreDetailItem { Field = "Kết quả", Value = "Chưa có dữ liệu" });
                items.Add(new ScoreDetailItem { Field = "Robot", Value = fallbackRobot });
                items.Add(new ScoreDetailItem { Field = "Score", Value = "-" });
                items.Add(new ScoreDetailItem { Field = "Đơn", Value = "-" });
                items.Add(new ScoreDetailItem { Field = "Tuyến", Value = "-" });
                items.Add(new ScoreDetailItem { Field = "Tải", Value = "-" });
                items.Add(new ScoreDetailItem { Field = "Di chuyển", Value = "-" });
                items.Add(new ScoreDetailItem { Field = "Pin", Value = "-" });
                items.Add(new ScoreDetailItem { Field = "Score TP", Value = "-" });
                return items;
            }

            string result = string.IsNullOrEmpty(decision) ? row.Decision : decision;
            string scoreParts = string.Format(
                "Load {0:0.00} | Dist {1:0.00} | Time {2:0.00}\nBat {3:0.00} | Health {4:0.00} | Wait {5:0.00} | Delay {6:0.00}",
                row.LoadScore,
                row.DistanceScore,
                row.TimeScore,
                row.BatteryScore,
                row.HealthScore,
                row.WaitScore,
                row.DelayPenalty);

            items.Add(new ScoreDetailItem { Field = "Kết quả", Value = result });
            items.Add(new ScoreDetailItem { Field = "Robot", Value = row.Robot });
            items.Add(new ScoreDetailItem { Field = "Score", Value = row.FinalScoreText });
            items.Add(new ScoreDetailItem { Field = "Đơn", Value = row.Orders });
            items.Add(new ScoreDetailItem { Field = "Tuyến", Value = row.Route });
            items.Add(new ScoreDetailItem { Field = "Tải", Value = row.Load });
            items.Add(new ScoreDetailItem { Field = "Di chuyển", Value = row.Steps });
            items.Add(new ScoreDetailItem { Field = "Pin", Value = row.Battery });
            items.Add(new ScoreDetailItem { Field = "SoH", Value = row.Health });
            items.Add(new ScoreDetailItem { Field = "Delay", Value = row.Delay });
            items.Add(new ScoreDetailItem { Field = "Score TP", Value = scoreParts });
            items.Add(new ScoreDetailItem { Field = "Lý do", Value = row.Reason });
            return items;
        }

        private void SetDetailGridData(DataGridView grid, List<ScoreDetailItem> rows)
        {
            if (grid == null) return;
            grid.DataSource = null;
            grid.DataSource = rows;
            grid.ClearSelection();
        }

        private ScoreCandidateRow CloneScoreRow(ScoreCandidateRow source, string decision)
        {
            return new ScoreCandidateRow
            {
                CandidateKey = source.CandidateKey,
                Tick = source.Tick,
                Strategy = source.Strategy,
                Robot = source.Robot,
                Decision = decision,
                Reason = source.Reason,
                Orders = source.Orders,
                Route = source.Route,
                Load = source.Load,
                Steps = source.Steps,
                Battery = source.Battery,
                Health = source.Health,
                Delay = source.Delay,
                LoadScore = source.LoadScore,
                DistanceScore = source.DistanceScore,
                TimeScore = source.TimeScore,
                BatteryScore = source.BatteryScore,
                HealthScore = source.HealthScore,
                WaitScore = source.WaitScore,
                DelayPenalty = source.DelayPenalty,
                FinalScore = source.FinalScore,
                FinalScoreText = source.FinalScoreText
            };
        }

        private void SetGridData(DataGridView grid, List<ScoreCandidateRow> rows)
        {
            if (grid == null) return;
            grid.DataSource = null;
            grid.DataSource = rows;
            grid.ClearSelection();
        }

        private void WriteTestLog(string message)
        {
            if (rtbTestLog == null) return;

            Color color = ColorTranslator.FromHtml("#E5E7EB");
            string lower = message.ToLower();
            if (lower.Contains("[score]") || lower.Contains("[best]")) color = ColorTranslator.FromHtml("#93C5FD");
            else if (lower.Contains("[assign]")) color = ColorTranslator.FromHtml("#86EFAC");
            else if (lower.Contains("[reject]")) color = ColorTranslator.FromHtml("#FCA5A5");
            else if (lower.Contains("[wait-best]") || lower.Contains("[no-dispatch]")) color = ColorTranslator.FromHtml("#FBBF24");
            else if (lower.Contains("[system]") || lower.Contains("[reset]")) color = ColorTranslator.FromHtml("#C4B5FD");

            string line = string.Format("[{0}] T{1:0000} {2}\r\n", DateTime.Now.ToString("HH:mm:ss"), simulationTick, message);

            rtbTestLog.SelectionStart = rtbTestLog.TextLength;
            rtbTestLog.SelectionLength = 0;
            rtbTestLog.SelectionColor = color;
            rtbTestLog.AppendText(line);
            rtbTestLog.SelectionColor = rtbTestLog.ForeColor;

            TrimTestLogLines();
            rtbTestLog.SelectionStart = rtbTestLog.TextLength;
            rtbTestLog.ScrollToCaret();
        }

        private void TrimTestLogLines()
        {
            if (rtbTestLog == null) return;
            if (rtbTestLog.Lines.Length <= MaxTestLogLines) return;

            string[] lines = rtbTestLog.Lines.Skip(rtbTestLog.Lines.Length - MaxTestLogLines).ToArray();
            rtbTestLog.Text = string.Join(Environment.NewLine, lines);
            if (!rtbTestLog.Text.EndsWith(Environment.NewLine)) rtbTestLog.AppendText(Environment.NewLine);
        }

        private void WriteCandidateScoreLog(string tag, AssignmentCandidate candidate, string reason)
        {
            if (candidate == null || candidate.ScoreBreakdown == null) return;

            UpsertCandidateScoreRow(tag, candidate, reason);

            string orders = candidate.OrderText;
            string route = candidate.RoutePlan == null ? "N/A" : candidate.RoutePlan.RouteText;
            int moveSteps = candidate.RoutePlan == null ? 0 : candidate.RoutePlan.MoveSteps;
            int waitSteps = candidate.RoutePlan == null ? 0 : candidate.RoutePlan.WaitSteps;
            int estimatedTime = candidate.RoutePlan == null ? 0 : candidate.RoutePlan.EstimatedTimeTicks;

            WriteTestLog(string.Format(
                "[{0}] {1} | {2} | Orders: {3} | Route: {4} | Load {5:0.0}/{6:0.0}kg ({7:0}%) | Move {8} | Wait {9} | Time {10} | EffPin {11:0.0}% | Cost {12:0.0}% | After {13:0.0}% | SoH {14:0.0}% | Delay {15} | Score: {16}",
                tag,
                candidate.Robot.Id,
                reason,
                orders,
                route,
                candidate.TotalWeight,
                candidate.Robot.MaxPayloadKg,
                candidate.LoadRatio * 100.0,
                moveSteps,
                waitSteps,
                estimatedTime,
                candidate.EffectiveBattery,
                candidate.EstimatedEnergyCost,
                candidate.BatteryAfterMission,
                candidate.Robot.BatteryHealth,
                candidate.AvailableDelay,
                candidate.ScoreBreakdown.ToLogString()));
        }

        private void TrimLogLines()
        {
            if (rtbLog == null) return;
            if (rtbLog.Lines.Length <= MaxLogLines) return;

            string[] lines = rtbLog.Lines.Skip(rtbLog.Lines.Length - MaxLogLines).ToArray();
            rtbLog.Text = string.Join(Environment.NewLine, lines);
            if (!rtbLog.Text.EndsWith(Environment.NewLine)) rtbLog.AppendText(Environment.NewLine);
        }
    }
}
