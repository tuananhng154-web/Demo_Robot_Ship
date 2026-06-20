using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Demo_Robot_Ship
{
    public partial class Form1
    {
        private string GetStrategyName(DispatchStrategy strategy)
        {
            return strategy == DispatchStrategy.Cvrp ? "CVRP" : "Greedy";
        }

        private string GetStrategyDescription(DispatchStrategy strategy)
        {
            if (strategy == DispatchStrategy.Cvrp)
            {
                return "CVRP: chia đồng thời đơn cho robot theo giới hạn tải, tối thiểu hóa tổng quãng đường ước lượng.";
            }
            return "Greedy: giữ logic hiện tại - chọn phương án có score tốt nhất theo từng lượt điều phối.";
        }

        private void StartNewComparisonRun()
        {
            currentRunMetrics = new StrategyRunMetrics
            {
                RunId = ++strategyRunCounter,
                Strategy = currentDispatchStrategy,
                StartTick = simulationTick
            };

            comparisonRows.Add(new StrategyComparisonRow
            {
                RunId = currentRunMetrics.RunId,
                RunName = string.Format("Run {0:00}", currentRunMetrics.RunId),
                StrategyName = GetStrategyName(currentDispatchStrategy),
                Status = "Chưa chạy",
                TotalBattery = "0.0%",
                WaitReplan = "0 / 0",
                AvgLoad = "0.0%",
                AvgOrderWait = "0.0 tick",
                Orders = "0 gán | 0 giao",
                Note = GetStrategyDescription(currentDispatchStrategy)
            });

            RefreshComparisonGrid();
        }

        private void EnsureComparisonRun()
        {
            if (currentRunMetrics == null) StartNewComparisonRun();
        }

        private void FinalizeCurrentComparisonRun(string reason)
        {
            if (currentRunMetrics == null) return;
            StrategyComparisonRow row = comparisonRows.FirstOrDefault(r => r.RunId == currentRunMetrics.RunId);
            if (!currentRunMetrics.HasActivity && allOrders.Count == 0)
            {
                if (row != null)
                {
                    comparisonRows.Remove(row);
                    if (row.RunId == strategyRunCounter) strategyRunCounter = Math.Max(0, strategyRunCounter - 1);
                }
                currentRunMetrics = null;
                RefreshComparisonGrid();
                return;
            }

            UpdateCurrentComparisonRow(reason);
            row = comparisonRows.FirstOrDefault(r => r.RunId == currentRunMetrics.RunId);
            if (row != null) row.Locked = true;
            currentRunMetrics = null;
            RefreshComparisonGrid();
        }

        private void RegisterAssignmentMetrics(BatchAssignment assignment)
        {
            if (assignment == null || assignment.Robot == null || assignment.Orders == null) return;
            EnsureComparisonRun();

            currentRunMetrics.RobotsUsed.Add(assignment.Robot.Id);
            currentRunMetrics.LoadRatios.Add(assignment.LoadRatio);
            currentRunMetrics.AssignedOrderCount += assignment.Orders.Count;

            foreach (DeliveryOrder order in assignment.Orders)
            {
                currentRunMetrics.OrderWaitTicks.Add(Math.Max(0, simulationTick - order.CreatedTick));
            }

            UpdateCurrentComparisonRow("Đang chạy");
        }

        private void RegisterMoveMetric()
        {
            EnsureComparisonRun();
            currentRunMetrics.MoveSteps++;
        }

        private void RegisterWaitMetric()
        {
            EnsureComparisonRun();
            currentRunMetrics.WaitSteps++;
        }

        private void RegisterReplanMetric()
        {
            EnsureComparisonRun();
            currentRunMetrics.ReplanCount++;
        }

        private void RegisterBatteryMetric(double displayedDrain)
        {
            EnsureComparisonRun();
            currentRunMetrics.BatteryConsumed += Math.Max(0, displayedDrain);
        }

        private void RegisterDeliveredMetric(int deliveredTick)
        {
            EnsureComparisonRun();
            currentRunMetrics.LastDeliveredTick = Math.Max(currentRunMetrics.LastDeliveredTick, deliveredTick);
            UpdateCurrentComparisonRow(GetCurrentRunStatus());
        }

        private string GetCurrentRunStatus()
        {
            if (currentRunMetrics == null || !currentRunMetrics.HasActivity) return "Chưa chạy";
            bool hasActiveOrders = allOrders.Any(o => o.Status == "WAITING" || o.Status == "ASSIGNED" || o.Status == "DELIVERING");
            if (!hasActiveOrders && allOrders.Count > 0) return "Hoàn tất";
            return "Đang chạy";
        }

        private void UpdateCurrentComparisonRow(string status)
        {
            if (currentRunMetrics == null) return;
            StrategyComparisonRow row = comparisonRows.FirstOrDefault(r => r.RunId == currentRunMetrics.RunId);
            if (row == null) return;

            int delivered = allOrders.Count(o => o.Status == "DELIVERED");
            int failed = allOrders.Count(o => o.Status == "FAILED");
            int waitingOrRunning = allOrders.Count(o => o.Status == "WAITING" || o.Status == "ASSIGNED" || o.Status == "DELIVERING");
            int completionTime = currentRunMetrics.LastDeliveredTick >= 0
                ? Math.Max(0, currentRunMetrics.LastDeliveredTick - currentRunMetrics.StartTick)
                : 0;

            row.StrategyName = GetStrategyName(currentRunMetrics.Strategy);
            row.Status = status;
            row.TotalDistance = currentRunMetrics.MoveSteps;
            row.CompletionTime = completionTime;
            row.TotalBattery = string.Format("{0:0.0}%", currentRunMetrics.BatteryConsumed);
            row.WaitReplan = string.Format("{0} / {1}", currentRunMetrics.WaitSteps, currentRunMetrics.ReplanCount);
            row.RobotsUsed = currentRunMetrics.RobotsUsed.Count;
            row.AvgLoad = string.Format("{0:0.0}%", currentRunMetrics.AverageLoadRatio * 100.0);
            row.AvgOrderWait = string.Format("{0:0.0} tick", currentRunMetrics.AverageOrderWait);
            row.Orders = string.Format("{0} gán | {1} giao | {2} lỗi | {3} chờ", currentRunMetrics.AssignedOrderCount, delivered, failed, waitingOrRunning);
            row.Note = GetStrategyDescription(currentRunMetrics.Strategy);

            RefreshComparisonGrid();
        }

        private void RefreshComparisonGrid()
        {
            if (dgvComparison != null)
            {
                dgvComparison.DataSource = null;
                dgvComparison.DataSource = comparisonRows.OrderBy(r => r.RunId).ToList();
                dgvComparison.ClearSelection();
            }

            if (lblComparisonSummary != null)
            {
                StrategyComparisonRow bestDistance = comparisonRows
                    .Where(r => r.Locked && r.TotalDistance > 0)
                    .OrderBy(r => r.TotalDistance)
                    .FirstOrDefault();

                StrategyComparisonRow bestTime = comparisonRows
                    .Where(r => r.Locked && r.CompletionTime > 0)
                    .OrderBy(r => r.CompletionTime)
                    .FirstOrDefault();

                string text = string.Format("Cơ chế hiện tại: {0} | {1}", GetStrategyName(currentDispatchStrategy), GetStrategyDescription(currentDispatchStrategy));
                if (bestDistance != null)
                {
                    text += string.Format(" | QĐ thấp nhất: {0} ({1} bước)", bestDistance.StrategyName, bestDistance.TotalDistance);
                }
                if (bestTime != null)
                {
                    text += string.Format(" | Nhanh nhất: {0} ({1} tick)", bestTime.StrategyName, bestTime.CompletionTime);
                }
                lblComparisonSummary.Text = text;
            }
        }

        private void ComparisonGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            DataGridView grid = sender as DataGridView;
            if (grid == null || e.RowIndex < 0) return;
            StrategyComparisonRow row = grid.Rows[e.RowIndex].DataBoundItem as StrategyComparisonRow;
            if (row == null) return;

            if (row.Status.Contains("Hoàn tất") || row.Locked)
            {
                grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = ColorTranslator.FromHtml("#DCFCE7");
                grid.Rows[e.RowIndex].DefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#14532D");
            }
            else if (row.Status.Contains("Đang"))
            {
                grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = ColorTranslator.FromHtml("#DBEAFE");
                grid.Rows[e.RowIndex].DefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#1E3A8A");
            }
        }
    }
}
