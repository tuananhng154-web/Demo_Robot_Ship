using System;
using System.Collections.Generic;
using System.Linq;

namespace Demo_Robot_Ship
{
    public partial class Form1
    {
        private void DispatchOrdersCvrp()
        {
            if (pendingOrders.Count == 0) return;

            FailOrdersThatNoRobotCanCarry();
            if (pendingOrders.Count == 0) return;

            List<DeliveryOrder> waitingOrders = pendingOrders
                .Where(o => o.Status == "WAITING")
                .OrderBy(o => o.CreatedTick)
                .ThenBy(o => o.Id)
                .ToList();

            if (waitingOrders.Count == 0) return;

            int oldestWait = waitingOrders.Max(o => o.GetWaitTicks(simulationTick));
            List<RobotAvailability> robotCandidates = BuildRobotAvailability(0, true)
                .Where(a => a.AvailableDelay == 0)
                .OrderBy(a => a.Robot.Id)
                .ToList();

            BeginScoreCycle(waitingOrders.Count, oldestWait, 0, true, robotCandidates.Count);
            WriteTestLog(string.Format("[SYSTEM] CVRP: {0} đơn WAITING, robot IDLE ứng viên={1}. Tối ưu phân chia theo tải trọng và tổng quãng đường.", waitingOrders.Count, robotCandidates.Count));

            if (robotCandidates.Count == 0)
            {
                WriteTestLog("[NO-DISPATCH] CVRP: Không có robot IDLE đủ điều kiện nhận nhiệm vụ.");
                return;
            }

            CvrpAssignmentPlan plan = FindBestCvrpPlan(waitingOrders, robotCandidates);
            if (plan == null || plan.GroupsByRobotId.Count == 0)
            {
                WriteTestLog("[NO-DISPATCH] CVRP: Không tìm được cách phân chia đơn hợp lệ theo tải trọng robot.");
                return;
            }

            WriteTestLog(string.Format("[CVRP] Chọn phương án có tổng quãng đường ước lượng {0} bước, dùng {1} robot.", plan.TotalDistance, plan.GroupsByRobotId.Count));

            int assignedRobots = 0;
            foreach (RobotAvailability availability in robotCandidates)
            {
                if (!plan.GroupsByRobotId.ContainsKey(availability.Robot.Id)) continue;

                List<DeliveryOrder> group = plan.GroupsByRobotId[availability.Robot.Id]
                    .Where(o => o.Status == "WAITING")
                    .OrderBy(o => o.CreatedTick)
                    .ThenBy(o => o.Id)
                    .ToList();

                if (group.Count == 0) continue;

                AssignmentCandidate candidate = BuildCandidateForOrderSet(availability, group, oldestWait, "CVRP");
                if (candidate == null) continue;

                WriteCandidateScoreLog("ASSIGN", candidate, string.Format("CVRP: nhóm {0} đơn trong phương án tổng {1} bước", group.Count, plan.TotalDistance));
                RemoveOrdersFromPending(candidate.Orders);
                AssignBatchToRobot(ToBatchAssignment(candidate));
                assignedRobots++;
            }

            if (assignedRobots == 0)
            {
                WriteTestLog("[NO-DISPATCH] CVRP: Đã có phương án phân chia nhưng không gán được do pin/đường đi/va chạm.");
            }
        }

        private CvrpAssignmentPlan FindBestCvrpPlan(List<DeliveryOrder> waitingOrders, List<RobotAvailability> robotCandidates)
        {
            if (waitingOrders == null || waitingOrders.Count == 0 || robotCandidates == null || robotCandidates.Count == 0) return null;

            List<DeliveryOrder> candidateOrders = waitingOrders
                .Where(o => robotCandidates.Any(a => o.WeightKg <= a.Robot.MaxPayloadKg))
                .OrderBy(o => o.CreatedTick)
                .ThenBy(o => o.Id)
                .Take(MaxCvrpOrdersPerOptimization)
                .ToList();

            if (candidateOrders.Count == 0) return null;

            // Nếu tổng đơn vượt tổng tải của các robot rảnh, giảm dần số đơn xét theo thứ tự tạo để vẫn có phương án khả thi.
            for (int count = candidateOrders.Count; count >= 1; count--)
            {
                List<DeliveryOrder> subset = candidateOrders.Take(count).ToList();
                if (subset.Sum(o => o.WeightKg) > robotCandidates.Sum(a => a.Robot.MaxPayloadKg) + 0.0001) continue;

                CvrpAssignmentPlan plan = SolveCvrpExact(subset, robotCandidates);
                if (plan != null) return plan;
            }

            return null;
        }

        private CvrpAssignmentPlan SolveCvrpExact(List<DeliveryOrder> orders, List<RobotAvailability> robotCandidates)
        {
            int robotCount = robotCandidates.Count;
            List<DeliveryOrder>[] buckets = new List<DeliveryOrder>[robotCount];
            double[] loads = new double[robotCount];
            for (int i = 0; i < robotCount; i++) buckets[i] = new List<DeliveryOrder>();

            int bestCost = int.MaxValue;
            List<DeliveryOrder>[] bestBuckets = null;

            Action<int> search = null;
            search = delegate (int orderIndex)
            {
                if (orderIndex >= orders.Count)
                {
                    int cost = 0;
                    for (int r = 0; r < robotCount; r++)
                    {
                        if (buckets[r].Count == 0) continue;
                        RobotAvailability availability = robotCandidates[r];
                        int routeDistance = EstimatePlainRouteDistance(
                            availability.StartX,
                            availability.StartY,
                            availability.Robot.HomeX,
                            availability.Robot.HomeY,
                            buckets[r]);

                        if (routeDistance >= int.MaxValue / 8) return;
                        cost += routeDistance;
                        if (cost >= bestCost) return;
                    }

                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        bestBuckets = buckets.Select(b => b.ToList()).ToArray();
                    }
                    return;
                }

                DeliveryOrder order = orders[orderIndex];
                for (int r = 0; r < robotCount; r++)
                {
                    RobotAvailability availability = robotCandidates[r];
                    if (loads[r] + order.WeightKg > availability.Robot.MaxPayloadKg + 0.0001) continue;

                    buckets[r].Add(order);
                    loads[r] += order.WeightKg;
                    search(orderIndex + 1);
                    loads[r] -= order.WeightKg;
                    buckets[r].RemoveAt(buckets[r].Count - 1);
                }
            };

            search(0);

            if (bestBuckets == null) return null;

            CvrpAssignmentPlan plan = new CvrpAssignmentPlan();
            plan.TotalDistance = bestCost;
            for (int r = 0; r < robotCount; r++)
            {
                if (bestBuckets[r].Count == 0) continue;
                plan.GroupsByRobotId[robotCandidates[r].Robot.Id] = bestBuckets[r];
            }

            return plan;
        }
    }
}
