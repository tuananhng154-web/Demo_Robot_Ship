using System.Collections.Generic;

namespace Demo_Robot_Ship
{
    public partial class Form1
    {
        private bool IsWalkable(int x, int y)
        {
            return pathPlanner.IsWalkable(x, y);
        }

        private bool IsChargingCell(int x, int y)
        {
            return pathPlanner.IsChargingCell(x, y);
        }

        private int GetDistance(Node nodeA, Node nodeB)
        {
            return pathPlanner.GetDistance(nodeA, nodeB);
        }

        private List<Node> GetNeighbors(Node node)
        {
            return pathPlanner.GetNeighbors(node);
        }

        private List<Node> FindPath_AStar(int startX, int startY, int targetX, int targetY)
        {
            return pathPlanner.FindPathAStar(startX, startY, targetX, targetY);
        }

        private List<Node> PlanCooperativePath(Robot robot, Node target)
        {
            return pathPlanner.PlanCooperativePath(robot, target, fleet);
        }


        private RoutePlan PlanCooperativeRoute(int startX, int startY, int homeX, int homeY, IList<Node> stops, Robot robot, int startDelayTicks)
        {
            return pathPlanner.PlanCooperativeRoute(startX, startY, homeX, homeY, stops, fleet, robot, startDelayTicks);
        }

        private int CountWaitSteps(int startX, int startY, List<Node> path)
        {
            return pathPlanner.CountWaitSteps(startX, startY, path);
        }

        private int CountTravelSteps(int startX, int startY, List<Node> path)
        {
            return pathPlanner.CountTravelSteps(startX, startY, path);
        }

        private double EstimateMissionBatteryPercent(Robot robot, double payloadKg, int distanceToTarget, int distanceHome)
        {
            return pathPlanner.EstimateMissionBatteryPercent(robot, payloadKg, distanceToTarget, distanceHome);
        }
    }
}
