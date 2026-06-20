using System;
using System.Collections.Generic;
using System.Linq;

namespace Demo_Robot_Ship
{
    public partial class Form1
    {
        private void timerSimulation_Tick(object sender, EventArgs e)
        {
            simulationTick++;
            pathDashOffset -= 1.2f;
            if (pathDashOffset < -1000) pathDashOffset = 0f;

            ChargeRobots();
            DispatchOrders();
            MoveRobotsWithCollisionAvoidance();
            RefreshUi();
            picMap.Invalidate();
        }

        private void ChargeRobots()
        {
            foreach (Robot robot in fleet)
            {
                if (robot.BatteryHealth < MaintenanceHealthThreshold && robot.Status != "DELIVERING" && robot.Status != "RETURNING")
                {
                    robot.Status = IsChargingCell(robot.GridX, robot.GridY) ? "MAINTENANCE" : "MAINTENANCE_REQUIRED";
                }

                if (IsChargingCell(robot.GridX, robot.GridY) &&
                    (robot.Status == "CHARGING" || robot.Status == "IDLE" || robot.Status == "LOW_BATTERY" || robot.Status == "MAINTENANCE"))
                {
                    BatteryModel.CloseDischargeCycle(robot, DegradationPerCycle);

                    if (robot.Battery < 100)
                    {
                        robot.Battery = Math.Min(100, robot.Battery + RechargeRatePerTick);
                        if (robot.Status == "IDLE" && pendingOrders.Count == 0) robot.Status = "CHARGING";
                    }

                    if (robot.Battery > 30) robot.DeepDischargeRecorded = false;

                    if (robot.Status == "MAINTENANCE")
                    {
                        robot.BatteryHealth = Math.Min(92.0, robot.BatteryHealth + 0.4);
                        if (robot.BatteryHealth >= 90.0 && robot.Battery >= ReadyBatteryThreshold)
                        {
                            robot.ChargeCycles = 0;
                            robot.DeepDischargeCount = 0;
                            robot.DeepDischargeRecorded = false;
                            robot.Status = "IDLE";
                            WriteLog(string.Format("{0}: Hoàn tất bảo dưỡng/thay pin, BatteryHealth phục hồi {1:0.0}%.", robot.Id, robot.BatteryHealth));
                        }
                    }
                    else if (robot.BatteryHealth >= MaintenanceHealthThreshold)
                    {
                        if ((robot.Status == "CHARGING" || robot.Status == "LOW_BATTERY") && robot.Battery >= ReadyBatteryThreshold)
                        {
                            robot.Status = "IDLE";
                        }
                    }
                }
            }
        }

        private void DispatchOrders()
        {
            if (currentDispatchStrategy == DispatchStrategy.Cvrp)
            {
                DispatchOrdersCvrp();
            }
            else
            {
                DispatchOrdersGreedy();
            }
        }

        private void DispatchOrdersGreedy()
        {
            if (pendingOrders.Count == 0) return;

            FailOrdersThatNoRobotCanCarry();
            if (pendingOrders.Count == 0) return;

            int maxDispatchThisTick = Math.Max(1, fleet.Count(r => r.Status == "IDLE"));
            while (pendingOrders.Count > 0 && maxDispatchThisTick > 0)
            {
                BatchAssignment assignment = FindBestBatchAssignment();
                if (assignment == null) break;

                RemoveOrdersFromPending(assignment.Orders);
                AssignBatchToRobot(assignment);
                maxDispatchThisTick--;
            }
        }

        private void FailOrdersThatNoRobotCanCarry()
        {
            double maxFleetPayload = fleet.Max(r => r.MaxPayloadKg);
            List<DeliveryOrder> impossibleOrders = pendingOrders
                .Where(o => o.Status == "WAITING" && o.WeightKg > maxFleetPayload)
                .ToList();

            foreach (DeliveryOrder order in impossibleOrders)
            {
                order.Status = "FAILED";
                WriteLog(string.Format("Đơn #{0:000}: Quá tải {1:0.0}kg. Robot lớn nhất chỉ chở {2:0.0}kg.", order.Id, order.WeightKg, maxFleetPayload));
            }

            if (impossibleOrders.Count > 0)
            {
                RemoveOrdersFromPending(impossibleOrders);
            }
        }

        private BatchAssignment FindBestBatchAssignment()
        {
            List<DeliveryOrder> waitingOrders = pendingOrders
                .Where(o => o.Status == "WAITING")
                .OrderBy(o => o.CreatedTick)
                .ThenBy(o => o.Id)
                .ToList();

            if (waitingOrders.Count == 0) return null;

            int oldestWait = waitingOrders.Max(o => o.GetWaitTicks(simulationTick));
            bool isTimedOut = oldestWait >= MaxBatchWaitTicks;
            int remainingWait = Math.Max(0, MaxBatchWaitTicks - oldestWait);

            List<RobotAvailability> robotCandidates = BuildRobotAvailability(remainingWait, isTimedOut);
            if (robotCandidates.Count == 0) return null;

            List<AssignmentCandidate> candidates = new List<AssignmentCandidate>();

            BeginScoreCycle(waitingOrders.Count, oldestWait, remainingWait, isTimedOut, robotCandidates.Count);
            WriteTestLog(string.Format("[SYSTEM] Kiểm thử điều phối: {0} đơn WAITING, oldestWait={1}/{2}, remainingWait={3}, timedOut={4}, robot ứng viên={5}.", waitingOrders.Count, oldestWait, MaxBatchWaitTicks, remainingWait, isTimedOut, robotCandidates.Count));

            foreach (RobotAvailability availability in robotCandidates)
            {
                foreach (DeliveryOrder seed in waitingOrders)
                {
                    AssignmentCandidate candidate = BuildCandidateForRobot(availability, seed, waitingOrders, oldestWait);
                    if (candidate != null)
                    {
                        candidates.Add(candidate);
                        WriteCandidateScoreLog("SCORE", candidate, availability.Reason);
                    }
                }
            }

            if (candidates.Count == 0)
            {
                WriteTestLog("[NO-DISPATCH] Không có phương án hợp lệ sau khi kiểm tra tải, pin, đường đi và bảo dưỡng.");
                return null;
            }

            List<AssignmentCandidate> dispatchable = candidates
                .Where(c => c.LoadRatio >= LoadDispatchThreshold || isTimedOut)
                .OrderByDescending(c => c.Score)
                .ToList();

            if (dispatchable.Count == 0)
            {
                AssignmentCandidate topWaiting = candidates.OrderByDescending(c => c.Score).FirstOrDefault();
                if (topWaiting != null)
                {
                    WriteCandidateScoreLog("NO-DISPATCH", topWaiting, "Tải chưa đạt 70% và chưa hết thời gian chờ");
                }
                return null;
            }

            AssignmentCandidate best = dispatchable[0];
            WriteCandidateScoreLog("BEST", best, isTimedOut ? "Hết thời gian chờ" : "Đủ điều kiện xuất phát");

            // Nếu robot tốt nhất chưa sẵn sàng nhưng vẫn còn thời gian gom đơn, tiếp tục chờ thay vì gán đơn cho robot khác kém hơn.
            if (best.AvailableDelay > 0 && !isTimedOut)
            {
                if (simulationTick - lastDelayedRobotLogTick >= 5)
                {
                    WriteLog(string.Format(
                        "Gom đơn: phương án tốt nhất là chờ {0} thêm {1} tick. Route {2}. Score {3:0.0}.",
                        best.Robot.Id, best.AvailableDelay, best.RoutePlan.RouteText, best.Score));
                    lastDelayedRobotLogTick = simulationTick;
                }
                return null;
            }

            return ToBatchAssignment(best);
        }

        private List<RobotAvailability> BuildRobotAvailability(int remainingWait, bool isTimedOut)
        {
            List<RobotAvailability> result = new List<RobotAvailability>();

            foreach (Robot robot in fleet)
            {
                if (robot.BatteryHealth < MaintenanceHealthThreshold)
                {
                    if (robot.Status != "DELIVERING" && robot.Status != "RETURNING") robot.Status = "MAINTENANCE_REQUIRED";
                    continue;
                }

                if (robot.Status == "IDLE")
                {
                    if (robot.Battery < LowBatteryThreshold)
                    {
                        robot.Status = "LOW_BATTERY";
                        SendRobotHome(robot);
                        continue;
                    }

                    result.Add(new RobotAvailability
                    {
                        Robot = robot,
                        AvailableDelay = 0,
                        StartX = robot.GridX,
                        StartY = robot.GridY,
                        EstimatedBatteryAtStart = robot.Battery,
                        Reason = "IDLE"
                    });
                }
                else if (!isTimedOut && robot.Status == "RETURNING" && robot.CurrentPath != null)
                {
                    int delay = robot.CurrentPath.Count;
                    if (delay <= remainingWait)
                    {
                        double estimatedBattery = Math.Max(0, robot.Battery - EstimateDisplayedDrainForPath(robot, robot.CurrentPath, 0));
                        result.Add(new RobotAvailability
                        {
                            Robot = robot,
                            AvailableDelay = delay,
                            StartX = robot.HomeX,
                            StartY = robot.HomeY,
                            EstimatedBatteryAtStart = estimatedBattery,
                            Reason = "RETURNING"
                        });
                    }
                }
                else if (!isTimedOut && robot.Status == "CHARGING")
                {
                    int delay = (int)Math.Ceiling(Math.Max(0, ReadyBatteryThreshold - robot.Battery) / RechargeRatePerTick);
                    if (delay <= remainingWait)
                    {
                        result.Add(new RobotAvailability
                        {
                            Robot = robot,
                            AvailableDelay = delay,
                            StartX = robot.GridX,
                            StartY = robot.GridY,
                            EstimatedBatteryAtStart = Math.Min(100, robot.Battery + delay * RechargeRatePerTick),
                            Reason = "CHARGING"
                        });
                    }
                }
            }

            return result;
        }

        private AssignmentCandidate BuildCandidateForRobot(RobotAvailability availability, DeliveryOrder seed, List<DeliveryOrder> waitingOrders, int oldestWait)
        {
            if (seed == null || seed.Status != "WAITING") return null;
            Robot robot = availability.Robot;

            List<DeliveryOrder> orders = BuildRouteBatchForRobot(availability, seed, waitingOrders);
            if (orders.Count == 0)
            {
                WriteTestLog(string.Format("[REJECT] {0} | seed #{1:000}-{2}: không tạo được nhóm đơn.", robot.Id, seed.Id, seed.Room));
                return null;
            }

            return BuildCandidateForOrderSet(availability, orders, oldestWait, "GREEDY");
        }

        private AssignmentCandidate BuildCandidateForOrderSet(RobotAvailability availability, List<DeliveryOrder> orders, int oldestWait, string source)
        {
            if (availability == null || availability.Robot == null) return null;
            Robot robot = availability.Robot;
            if (orders == null || orders.Count == 0)
            {
                WriteTestLog(string.Format("[REJECT] {0} | {1}: không có đơn hợp lệ.", robot.Id, source));
                return null;
            }

            orders = orders
                .Where(o => o != null && o.Status == "WAITING")
                .OrderBy(o => o.CreatedTick)
                .ThenBy(o => o.Id)
                .ToList();

            if (orders.Count == 0)
            {
                WriteTestLog(string.Format("[REJECT] {0} | {1}: các đơn không còn ở trạng thái WAITING.", robot.Id, source));
                return null;
            }

            double totalWeight = orders.Sum(o => o.WeightKg);
            if (totalWeight > robot.MaxPayloadKg)
            {
                WriteTestLog(string.Format("[REJECT] {0} | {1} | Orders {2}: vượt tải {3:0.0}/{4:0.0}kg.", robot.Id, source, string.Join(", ", orders.Select(o => "#" + o.Id.ToString("000") + "-" + o.Room)), totalWeight, robot.MaxPayloadKg));
                return null;
            }

            List<Node> stops = OptimizeStopOrder(availability.StartX, availability.StartY, orders);
            if (stops.Count == 0)
            {
                WriteTestLog(string.Format("[REJECT] {0} | {1} | Orders {2}: không xác định được điểm giao.", robot.Id, source, string.Join(", ", orders.Select(o => "#" + o.Id.ToString("000") + "-" + o.Room))));
                return null;
            }

            RoutePlan route = PlanCooperativeRoute(
                availability.StartX,
                availability.StartY,
                robot.HomeX,
                robot.HomeY,
                stops,
                robot,
                availability.AvailableDelay);

            if (route == null)
            {
                WriteTestLog(string.Format("[REJECT] {0} | {1} | Orders {2}: không tìm được tuyến an toàn bằng Time-Space A*.", robot.Id, source, string.Join(", ", orders.Select(o => "#" + o.Id.ToString("000") + "-" + o.Room))));
                return null;
            }

            double loadRatio = totalWeight / robot.MaxPayloadKg;
            double effectiveBattery = BatteryModel.GetEffectiveBattery(availability.EstimatedBatteryAtStart, robot.BatteryHealth);
            double energyCost = BatteryModel.EstimateEnergyCost(route.MoveSteps, route.WaitSteps, loadRatio, BatteryDrainBase, BaseWaitCost, LoadFactor);
            double batteryAfterMission = effectiveBattery - energyCost;

            if (batteryAfterMission < BatteryReservePercent)
            {
                WriteTestLog(string.Format("[REJECT] {0} | {1} | Orders {2}: pin không đủ. Effective={3:0.0}%, Cost={4:0.0}%, After={5:0.0}%, Reserve={6:0.0}%.", robot.Id, source, string.Join(", ", orders.Select(o => "#" + o.Id.ToString("000") + "-" + o.Room)), effectiveBattery, energyCost, batteryAfterMission, BatteryReservePercent));
                return null;
            }

            AssignmentCandidate candidate = new AssignmentCandidate
            {
                Robot = robot,
                Orders = orders,
                RoutePlan = route,
                TotalWeight = totalWeight,
                LoadRatio = loadRatio,
                EffectiveBattery = effectiveBattery,
                EstimatedEnergyCost = energyCost,
                BatteryAfterMission = batteryAfterMission,
                OldestWaitTicks = oldestWait,
                AvailableDelay = availability.AvailableDelay
            };

            candidate.ScoreBreakdown = AssignmentScorer.Calculate(candidate, new AssignmentScoringSettings
            {
                MaxDistance = Math.Max(80, mapGrid.GetLength(0) * mapGrid.GetLength(1) / 2),
                MaxTime = PlanningHorizon,
                MaxBatchWait = MaxBatchWaitTicks
            });

            return candidate;
        }

        private List<DeliveryOrder> BuildRouteBatchForRobot(RobotAvailability availability, DeliveryOrder seed, List<DeliveryOrder> waitingOrders)
        {
            Robot robot = availability.Robot;
            List<DeliveryOrder> selected = new List<DeliveryOrder>();
            selected.Add(seed);
            double totalWeight = seed.WeightKg;

            while (selected.Count < MaxOrdersPerTrip)
            {
                DeliveryOrder bestOrder = null;
                int bestDistance = int.MaxValue;

                foreach (DeliveryOrder order in waitingOrders)
                {
                    if (selected.Any(o => o.Id == order.Id)) continue;
                    if (order.Status != "WAITING") continue;
                    if (totalWeight + order.WeightKg > robot.MaxPayloadKg) continue;

                    List<DeliveryOrder> trial = selected.Concat(new[] { order }).ToList();
                    int distance = EstimatePlainRouteDistance(availability.StartX, availability.StartY, robot.HomeX, robot.HomeY, trial);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestOrder = order;
                    }
                }

                if (bestOrder == null) break;

                selected.Add(bestOrder);
                totalWeight += bestOrder.WeightKg;

                if (totalWeight / robot.MaxPayloadKg >= LoadDispatchThreshold) break;
            }

            return selected;
        }

        private int EstimatePlainRouteDistance(int startX, int startY, int homeX, int homeY, List<DeliveryOrder> orders)
        {
            List<Node> stops = OptimizeStopOrder(startX, startY, orders);
            int currentX = startX;
            int currentY = startY;
            int total = 0;

            foreach (Node stop in stops)
            {
                List<Node> path = FindPath_AStar(currentX, currentY, stop.X, stop.Y);
                if (path == null) return int.MaxValue / 4;
                total += CountTravelSteps(currentX, currentY, path);
                currentX = stop.X;
                currentY = stop.Y;
            }

            List<Node> homePath = FindPath_AStar(currentX, currentY, homeX, homeY);
            if (homePath == null) return int.MaxValue / 4;
            total += CountTravelSteps(currentX, currentY, homePath);
            return total;
        }

        private List<Node> OptimizeStopOrder(int startX, int startY, IEnumerable<DeliveryOrder> orders)
        {
            List<Node> remaining = orders
                .Where(o => o.Target != null)
                .GroupBy(o => o.TargetKey)
                .Select(g => new Node(g.First().Target.X, g.First().Target.Y))
                .ToList();

            List<Node> result = new List<Node>();
            int currentX = startX;
            int currentY = startY;

            while (remaining.Count > 0)
            {
                Node best = null;
                int bestDistance = int.MaxValue;

                foreach (Node target in remaining)
                {
                    List<Node> path = FindPath_AStar(currentX, currentY, target.X, target.Y);
                    int distance = path == null ? GetDistance(new Node(currentX, currentY), target) + 1000 : CountTravelSteps(currentX, currentY, path);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = target;
                    }
                }

                if (best == null) break;
                result.Add(best);
                remaining.Remove(best);
                currentX = best.X;
                currentY = best.Y;
            }

            return result;
        }

        private BatchAssignment ToBatchAssignment(AssignmentCandidate candidate)
        {
            return new BatchAssignment
            {
                Robot = candidate.Robot,
                Orders = candidate.Orders,
                Target = candidate.RoutePlan.Stops.FirstOrDefault(),
                Path = candidate.RoutePlan.DeliveryPath,
                RoutePlan = candidate.RoutePlan,
                TotalWeight = candidate.TotalWeight,
                LoadRatio = candidate.LoadRatio,
                EffectiveBattery = candidate.EffectiveBattery,
                EstimatedBatteryCost = candidate.EstimatedEnergyCost,
                BatteryAfterMission = candidate.BatteryAfterMission,
                EstimatedDistance = candidate.RoutePlan.MoveSteps,
                EstimatedTime = candidate.RoutePlan.EstimatedTimeTicks,
                OldestWaitTicks = candidate.OldestWaitTicks,
                AvailableDelay = candidate.AvailableDelay,
                Score = candidate.Score,
                ScoreBreakdown = candidate.ScoreBreakdown,
                RouteText = candidate.RoutePlan.RouteText
            };
        }

        private void RemoveOrdersFromPending(IEnumerable<DeliveryOrder> ordersToRemove)
        {
            HashSet<int> ids = new HashSet<int>(ordersToRemove.Select(o => o.Id));
            pendingOrders = new Queue<DeliveryOrder>(pendingOrders.Where(o => !ids.Contains(o.Id)));
        }

        private void AssignBatchToRobot(BatchAssignment assignment)
        {
            Robot robot = assignment.Robot;
            List<DeliveryOrder> orders = assignment.Orders;
            if (orders == null || orders.Count == 0) return;

            string batchId = string.Format("B{0:HHmmss}-{1}", DateTime.Now, robot.Id);
            double totalWeight = orders.Sum(o => o.WeightKg);

            foreach (DeliveryOrder order in orders)
            {
                order.Status = "DELIVERING";
                order.AssignedRobotId = robot.Id;
                order.BatchId = batchId;
                order.AssignedTick = simulationTick;
            }

            robot.CurrentOrders = orders.ToList();
            robot.CurrentOrder = orders[0];
            robot.Payload = totalWeight;
            robot.CurrentPath = assignment.Path ?? new List<Node>();
            robot.CurrentRouteText = assignment.RouteText;
            robot.Status = "DELIVERING";
            robot.WaitTicks = 0;
            RegisterAssignmentMetrics(assignment);

            string orderList = string.Join(", ", orders.Select(o => "#" + o.Id.ToString("000") + "-" + o.Room));

            WriteLog(string.Format(
                "Điều phối: {0} nhận {1} đơn ({2}), route {3}, tải {4:0.0}/{5:0.0}kg ({6:0}%), pin hiệu dụng {7:0.0}%, hao {8:0.0}%, còn {9:0.0}%, Score {10:0.0}.",
                robot.Id,
                orders.Count,
                orderList,
                assignment.RouteText,
                totalWeight,
                robot.MaxPayloadKg,
                assignment.LoadRatio * 100.0,
                assignment.EffectiveBattery,
                assignment.EstimatedBatteryCost,
                assignment.BatteryAfterMission,
                assignment.Score));

            if (assignment.ScoreBreakdown != null)
            {
                WriteLog(string.Format("[SCORING] {0}: {1}", robot.Id, assignment.ScoreBreakdown.ToLogString()));
                WriteTestLog(string.Format("[ASSIGN] Chọn {0} nhận {1} đơn ({2}) | route {3} | FinalScore={4:0.0} | {5}", robot.Id, orders.Count, orderList, assignment.RouteText, assignment.Score, assignment.ScoreBreakdown.ToLogString()));
            }

            DeliverReachedOrders(robot);
            if (robot.CurrentOrders.Count == 0) SendRobotHome(robot);
        }

        private void MoveRobotsWithCollisionAvoidance()
        {
            HashSet<string> reservedCellsThisTick = new HashSet<string>();
            HashSet<string> reservedMovesThisTick = new HashSet<string>();

            foreach (Robot robot in fleet)
            {
                if ((robot.Status == "DELIVERING" || robot.Status == "RETURNING") && robot.CurrentPath != null && robot.CurrentPath.Count > 0)
                {
                    Node nextStep = robot.CurrentPath[0];
                    string nextKey = nextStep.Key();
                    bool isPlannedWait = nextStep.X == robot.GridX && nextStep.Y == robot.GridY;
                    string moveKey = BuildMoveKey(robot.GridX, robot.GridY, nextStep.X, nextStep.Y);
                    string reverseMoveKey = BuildMoveKey(nextStep.X, nextStep.Y, robot.GridX, robot.GridY);

                    bool canMove = (isPlannedWait || IsCellFreeForRobot(nextStep.X, nextStep.Y, robot))
                                   && !reservedCellsThisTick.Contains(nextKey)
                                   && (isPlannedWait || !reservedMovesThisTick.Contains(reverseMoveKey));

                    if (canMove)
                    {
                        robot.CurrentPath.RemoveAt(0);
                        reservedCellsThisTick.Add(nextKey);
                        if (!isPlannedWait) reservedMovesThisTick.Add(moveKey);
                        robot.WaitTicks = 0;

                        if (!isPlannedWait)
                        {
                            robot.GridX = nextStep.X;
                            robot.GridY = nextStep.Y;
                            ConsumeMoveBattery(robot);
                        }
                        else
                        {
                            ConsumeWaitBattery(robot);
                        }

                        if (robot.Status == "DELIVERING") DeliverReachedOrders(robot);
                    }
                    else
                    {
                        robot.WaitTicks++;
                        ConsumeWaitBattery(robot);
                        if (robot.WaitTicks >= 2) ReplanRobot(robot);
                    }
                }

                if ((robot.Status == "DELIVERING" || robot.Status == "RETURNING") && (robot.CurrentPath == null || robot.CurrentPath.Count == 0))
                {
                    OnRobotArrived(robot);
                }
            }
        }

        private string BuildMoveKey(int fromX, int fromY, int toX, int toY)
        {
            return fromX + "," + fromY + ">" + toX + "," + toY;
        }

        private bool IsCellFreeForRobot(int x, int y, Robot currentRobot)
        {
            foreach (Robot other in fleet)
            {
                if (other == currentRobot) continue;
                if (other.GridX == x && other.GridY == y) return false;
            }
            return true;
        }

        private void ReplanRobot(Robot robot)
        {
            robot.ReplanCount++;
            RegisterReplanMetric();

            if (robot.Status == "DELIVERING" && robot.CurrentOrders != null && robot.CurrentOrders.Count > 0)
            {
                List<Node> stops = OptimizeStopOrder(robot.GridX, robot.GridY, robot.CurrentOrders);
                RoutePlan newRoute = PlanCooperativeRoute(robot.GridX, robot.GridY, robot.HomeX, robot.HomeY, stops, robot, 0);
                if (newRoute != null && newRoute.DeliveryPath.Count > 0)
                {
                    robot.CurrentPath = newRoute.DeliveryPath;
                    robot.CurrentRouteText = newRoute.RouteText;
                    robot.WaitTicks = 0;
                    WriteLog(string.Format("{0}: REPLAN giao hàng, route {1}, {2} bước, chờ {3} nhịp.", robot.Id, newRoute.RouteText, newRoute.DeliveryPath.Count, newRoute.DeliveryWaitSteps));
                    return;
                }
            }
            else if (robot.Status == "RETURNING")
            {
                List<Node> pathHome = PlanCooperativePath(robot, new Node(robot.HomeX, robot.HomeY));
                if (pathHome != null && pathHome.Count > 0)
                {
                    robot.CurrentPath = pathHome;
                    robot.WaitTicks = 0;
                    WriteLog(string.Format("{0}: REPLAN về trạm, {1} bước, chờ {2} nhịp.", robot.Id, pathHome.Count, CountWaitSteps(robot.GridX, robot.GridY, pathHome)));
                    return;
                }
            }

            robot.WaitTicks = 0;
            robot.CurrentPath = new List<Node> { new Node(robot.GridX, robot.GridY) };
            WriteLog(robot.Id + ": Chưa tìm được đường an toàn, tạm chờ 1 nhịp.");
        }

        private void ConsumeMoveBattery(Robot robot)
        {
            robot.TotalDistance++;
            RegisterMoveMetric();
            double loadRatio = robot.MaxPayloadKg <= 0 ? 0 : robot.Payload / robot.MaxPayloadKg;
            double effectiveCost = BatteryModel.EstimateEnergyCost(1, 0, loadRatio, BatteryDrainBase, BaseWaitCost, LoadFactor);
            double displayedDrain = BatteryModel.ConvertEffectiveCostToDisplayedBatteryDrain(effectiveCost, robot.BatteryHealth);
            ApplyBatteryDrain(robot, displayedDrain);
        }

        private void ConsumeWaitBattery(Robot robot)
        {
            double loadRatio = robot.MaxPayloadKg <= 0 ? 0 : robot.Payload / robot.MaxPayloadKg;
            double effectiveCost = BatteryModel.EstimateEnergyCost(0, 1, loadRatio, BatteryDrainBase, BaseWaitCost, LoadFactor);
            double displayedDrain = BatteryModel.ConvertEffectiveCostToDisplayedBatteryDrain(effectiveCost, robot.BatteryHealth);
            ApplyBatteryDrain(robot, displayedDrain);
            robot.TotalWaitSteps++;
            RegisterWaitMetric();
        }

        private double EstimateDisplayedDrainForPath(Robot robot, List<Node> path, double payloadKg)
        {
            int moveSteps = CountTravelSteps(robot.GridX, robot.GridY, path);
            int waitSteps = CountWaitSteps(robot.GridX, robot.GridY, path);
            double loadRatio = robot.MaxPayloadKg <= 0 ? 0 : payloadKg / robot.MaxPayloadKg;
            double effectiveCost = BatteryModel.EstimateEnergyCost(moveSteps, waitSteps, loadRatio, BatteryDrainBase, BaseWaitCost, LoadFactor);
            return BatteryModel.ConvertEffectiveCostToDisplayedBatteryDrain(effectiveCost, robot.BatteryHealth);
        }

        private void ApplyBatteryDrain(Robot robot, double displayedDrain)
        {
            robot.Battery -= displayedDrain;
            if (robot.Battery < 0) robot.Battery = 0;
            BatteryModel.RegisterBatteryUse(robot, displayedDrain, DegradationPerCycle);
            RegisterBatteryMetric(displayedDrain);

            if (robot.Battery < LowBatteryThreshold && robot.Payload <= 0 && robot.Status == "IDLE")
            {
                robot.Status = "LOW_BATTERY";
                SendRobotHome(robot);
            }
        }

        private void DeliverReachedOrders(Robot robot)
        {
            if (robot.CurrentOrders == null || robot.CurrentOrders.Count == 0) return;

            List<DeliveryOrder> deliveredNow = robot.CurrentOrders
                .Where(o => o.Target != null && o.Target.X == robot.GridX && o.Target.Y == robot.GridY)
                .ToList();

            if (deliveredNow.Count == 0) return;

            foreach (DeliveryOrder order in deliveredNow)
            {
                order.Status = "DELIVERED";
                order.DeliveredAt = DateTime.Now;
                order.DeliveredTick = simulationTick;
                RegisterDeliveredMetric(order.DeliveredTick);
                robot.CurrentOrders.Remove(order);
                robot.TotalDelivered++;
            }

            robot.Payload = robot.CurrentOrders.Sum(o => o.WeightKg);
            robot.CurrentOrder = robot.CurrentOrders.FirstOrDefault();

            string deliveredList = string.Join(", ", deliveredNow.Select(o => "#" + o.Id.ToString("000") + "-" + o.Room));
            WriteLog(string.Format("Hoàn tất điểm giao: {0} đã giao {1} đơn ({2}) tại ({3},{4}).", robot.Id, deliveredNow.Count, deliveredList, robot.GridX, robot.GridY));

            if (robot.CurrentOrders.Count == 0)
            {
                robot.Payload = 0;
                robot.CurrentOrder = null;
                SendRobotHome(robot);
            }
        }

        private void OnRobotArrived(Robot robot)
        {
            if (robot.Status == "DELIVERING")
            {
                DeliverReachedOrders(robot);

                if (robot.CurrentOrders != null && robot.CurrentOrders.Count > 0)
                {
                    ReplanRobot(robot);
                }
                else
                {
                    SendRobotHome(robot);
                }
            }
            else if (robot.Status == "RETURNING")
            {
                BatteryModel.CloseDischargeCycle(robot, DegradationPerCycle);
                robot.CurrentPath = new List<Node>();
                robot.CurrentRouteText = "";

                if (robot.BatteryHealth < MaintenanceHealthThreshold)
                {
                    robot.Status = "MAINTENANCE_REQUIRED";
                    WriteLog(string.Format("{0}: Đã về trạm nhưng BatteryHealth chỉ còn {1:0.0}%, cần bảo dưỡng.", robot.Id, robot.BatteryHealth));
                }
                else
                {
                    robot.Status = robot.Battery < ReadyBatteryThreshold ? "CHARGING" : "IDLE";
                    WriteLog(string.Format("{0}: Đã về trạm sạc. Pin {1:0.0}%, sức khỏe pin {2:0.0}%, chu kỳ {3:0.00}.", robot.Id, robot.Battery, robot.BatteryHealth, robot.ChargeCycles));
                }
            }
        }

        private void SendRobotHome(Robot robot)
        {
            if (robot.GridX == robot.HomeX && robot.GridY == robot.HomeY)
            {
                robot.CurrentPath = new List<Node>();
                robot.CurrentRouteText = "";
                if (robot.BatteryHealth < MaintenanceHealthThreshold) robot.Status = "MAINTENANCE_REQUIRED";
                else robot.Status = robot.Battery < ReadyBatteryThreshold ? "CHARGING" : "IDLE";
                return;
            }

            List<Node> pathHome = PlanCooperativePath(robot, new Node(robot.HomeX, robot.HomeY));
            if (pathHome != null && pathHome.Count > 0)
            {
                robot.CurrentPath = pathHome;
                robot.CurrentRouteText = "Home";
                robot.Status = "RETURNING";
                WriteLog(string.Format("{0}: Lập lộ trình về trạm, {1} bước, chờ {2} nhịp.", robot.Id, pathHome.Count, CountWaitSteps(robot.GridX, robot.GridY, pathHome)));
            }
            else
            {
                robot.CurrentPath = new List<Node>();
                robot.Status = IsChargingCell(robot.GridX, robot.GridY) ? "CHARGING" : "IDLE";
            }
        }
    }
}
