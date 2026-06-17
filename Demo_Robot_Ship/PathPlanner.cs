using System;
using System.Collections.Generic;
using System.Linq;

namespace Demo_Robot_Ship
{
    internal class PathPlanner
    {
        private readonly int[,] mapGrid;
        private readonly int planningHorizon;
        private readonly int targetReserveTicks;
        private readonly int moveCost;
        private readonly int waitCost;
        private readonly double batteryDrainBase;
        private readonly double batteryDrainPerKg;

        public PathPlanner(
            int[,] mapGrid,
            int planningHorizon,
            int targetReserveTicks,
            int moveCost,
            int waitCost,
            double batteryDrainBase,
            double batteryDrainPerKg)
        {
            this.mapGrid = mapGrid;
            this.planningHorizon = planningHorizon;
            this.targetReserveTicks = targetReserveTicks;
            this.moveCost = moveCost;
            this.waitCost = waitCost;
            this.batteryDrainBase = batteryDrainBase;
            this.batteryDrainPerKg = batteryDrainPerKg;
        }

        public bool IsWalkable(int x, int y)
        {
            if (x < 0 || x >= mapGrid.GetLength(1) || y < 0 || y >= mapGrid.GetLength(0)) return false;
            return mapGrid[y, x] == 0 || mapGrid[y, x] == 2;
        }

        public bool IsChargingCell(int x, int y)
        {
            return y == 13 && (x == 9 || x == 10 || x == 11);
        }

        public int GetDistance(Node nodeA, Node nodeB)
        {
            return Math.Abs(nodeA.X - nodeB.X) + Math.Abs(nodeA.Y - nodeB.Y);
        }

        public List<Node> GetNeighbors(Node node)
        {
            List<Node> neighbors = new List<Node>();
            int[] dx = { 0, 0, -1, 1 };
            int[] dy = { -1, 1, 0, 0 };

            for (int i = 0; i < 4; i++)
            {
                int checkX = node.X + dx[i];
                int checkY = node.Y + dy[i];
                if (IsWalkable(checkX, checkY)) neighbors.Add(new Node(checkX, checkY));
            }
            return neighbors;
        }

        public List<Node> FindPathAStar(int startX, int startY, int targetX, int targetY)
        {
            Node startNode = new Node(startX, startY);
            Node targetNode = new Node(targetX, targetY);

            List<Node> openList = new List<Node>();
            List<Node> closedList = new List<Node>();
            openList.Add(startNode);

            while (openList.Count > 0)
            {
                Node currentNode = openList[0];
                for (int i = 1; i < openList.Count; i++)
                {
                    if (openList[i].F < currentNode.F || (openList[i].F == currentNode.F && openList[i].H < currentNode.H))
                    {
                        currentNode = openList[i];
                    }
                }

                openList.Remove(currentNode);
                closedList.Add(currentNode);

                if (currentNode.X == targetNode.X && currentNode.Y == targetNode.Y)
                {
                    List<Node> path = new List<Node>();
                    Node traceNode = currentNode;
                    while (traceNode.X != startNode.X || traceNode.Y != startNode.Y)
                    {
                        path.Add(traceNode);
                        traceNode = traceNode.Parent;
                    }
                    path.Reverse();
                    return path;
                }

                foreach (Node neighbor in GetNeighbors(currentNode))
                {
                    if (closedList.Any(n => n.X == neighbor.X && n.Y == neighbor.Y)) continue;

                    int newCostToNeighbor = currentNode.G + 1;
                    Node existingNeighbor = openList.FirstOrDefault(n => n.X == neighbor.X && n.Y == neighbor.Y);

                    if (existingNeighbor == null || newCostToNeighbor < existingNeighbor.G)
                    {
                        if (existingNeighbor == null) existingNeighbor = neighbor;
                        existingNeighbor.G = newCostToNeighbor;
                        existingNeighbor.H = GetDistance(existingNeighbor, targetNode);
                        existingNeighbor.Parent = currentNode;

                        if (!openList.Contains(existingNeighbor)) openList.Add(existingNeighbor);
                    }
                }
            }
            return null;
        }

        public List<Node> PlanCooperativePath(Robot robot, Node target, IEnumerable<Robot> fleet)
        {
            HashSet<string> reservedCells;
            HashSet<string> reservedMoves;
            BuildReservationTable(robot, fleet, out reservedCells, out reservedMoves);

            return FindPathCooperativeAStar(robot.GridX, robot.GridY, target.X, target.Y, reservedCells, reservedMoves, 0, true);
        }

        public RoutePlan PlanCooperativeRoute(
            int startX,
            int startY,
            int homeX,
            int homeY,
            IList<Node> stops,
            IEnumerable<Robot> fleet,
            Robot ignoreRobot,
            int startDelayTicks)
        {
            if (stops == null || stops.Count == 0) return null;

            HashSet<string> reservedCells;
            HashSet<string> reservedMoves;
            BuildReservationTable(ignoreRobot, fleet, out reservedCells, out reservedMoves);

            RoutePlan route = new RoutePlan();
            route.Stops = stops.Select(s => new Node(s.X, s.Y)).ToList();

            int currentX = startX;
            int currentY = startY;
            int currentTime = Math.Max(0, startDelayTicks);

            foreach (Node stop in stops)
            {
                List<Node> leg = FindPathCooperativeAStar(currentX, currentY, stop.X, stop.Y, reservedCells, reservedMoves, currentTime, false);
                if (leg == null) return null;

                route.DeliveryWaitSteps += CountWaitSteps(currentX, currentY, leg);
                route.DeliveryMoveSteps += CountTravelSteps(currentX, currentY, leg);
                route.DeliveryPath.AddRange(leg);

                ReservePlannedPath(reservedCells, reservedMoves, currentX, currentY, leg, currentTime);

                currentTime += leg.Count;
                currentX = stop.X;
                currentY = stop.Y;
            }

            List<Node> returnLeg = FindPathCooperativeAStar(currentX, currentY, homeX, homeY, reservedCells, reservedMoves, currentTime, true);
            if (returnLeg == null) return null;

            route.ReturnWaitSteps = CountWaitSteps(currentX, currentY, returnLeg);
            route.ReturnMoveSteps = CountTravelSteps(currentX, currentY, returnLeg);
            route.ReturnHomePath.AddRange(returnLeg);

            return route;
        }

        private void BuildReservationTable(Robot ignoreRobot, IEnumerable<Robot> fleet, out HashSet<string> reservedCells, out HashSet<string> reservedMoves)
        {
            reservedCells = new HashSet<string>();
            reservedMoves = new HashSet<string>();

            foreach (Robot other in fleet)
            {
                if (other == ignoreRobot) continue;

                int time = 0;
                int currentX = other.GridX;
                int currentY = other.GridY;
                ReserveCell(reservedCells, currentX, currentY, time);

                bool hasPath = other.CurrentPath != null && other.CurrentPath.Count > 0;

                if (hasPath)
                {
                    foreach (Node step in other.CurrentPath)
                    {
                        time++;
                        ReserveCell(reservedCells, step.X, step.Y, time);
                        ReserveMove(reservedMoves, currentX, currentY, step.X, step.Y, time);
                        currentX = step.X;
                        currentY = step.Y;
                    }
                }

                int reserveUntil = hasPath ? Math.Min(planningHorizon, time + targetReserveTicks) : planningHorizon;
                for (int t = time + 1; t <= reserveUntil; t++)
                {
                    ReserveCell(reservedCells, currentX, currentY, t);
                }
            }
        }

        private List<Node> FindPathCooperativeAStar(
            int startX,
            int startY,
            int targetX,
            int targetY,
            HashSet<string> reservedCells,
            HashSet<string> reservedMoves,
            int startTime,
            bool reserveTargetAfterArrival)
        {
            TimedNode startNode = new TimedNode(startX, startY, startTime);
            startNode.G = 0;
            startNode.H = GetManhattanCost(startX, startY, targetX, targetY);

            List<TimedNode> openList = new List<TimedNode>();
            HashSet<string> closedSet = new HashSet<string>();
            openList.Add(startNode);

            int[] dx = { 0, 0, -1, 1, 0 };
            int[] dy = { -1, 1, 0, 0, 0 };

            while (openList.Count > 0)
            {
                TimedNode currentNode = openList[0];
                for (int i = 1; i < openList.Count; i++)
                {
                    TimedNode candidate = openList[i];
                    if (candidate.F < currentNode.F ||
                        (candidate.F == currentNode.F && candidate.H < currentNode.H) ||
                        (candidate.F == currentNode.F && candidate.H == currentNode.H && candidate.Time < currentNode.Time))
                    {
                        currentNode = candidate;
                    }
                }

                openList.Remove(currentNode);
                closedSet.Add(TimeCellKey(currentNode.X, currentNode.Y, currentNode.Time));

                if (currentNode.X == targetX && currentNode.Y == targetY)
                {
                    if (!reserveTargetAfterArrival || IsTargetFreeAfterArrival(targetX, targetY, currentNode.Time, reservedCells))
                    {
                        return ReconstructTimedPath(currentNode, startX, startY, startTime);
                    }
                }

                if (currentNode.Time >= planningHorizon) continue;

                for (int i = 0; i < dx.Length; i++)
                {
                    int nextX = currentNode.X + dx[i];
                    int nextY = currentNode.Y + dy[i];
                    int nextTime = currentNode.Time + 1;
                    bool isWait = dx[i] == 0 && dy[i] == 0;

                    if (!isWait && !IsWalkable(nextX, nextY)) continue;
                    if (isWait && !IsWalkable(currentNode.X, currentNode.Y)) continue;

                    if (reservedCells.Contains(TimeCellKey(nextX, nextY, nextTime))) continue;
                    if (reservedMoves.Contains(TimeMoveKey(nextX, nextY, currentNode.X, currentNode.Y, nextTime))) continue;

                    string closedKey = TimeCellKey(nextX, nextY, nextTime);
                    if (closedSet.Contains(closedKey)) continue;

                    int stepCost = isWait ? waitCost : moveCost;
                    int newG = currentNode.G + stepCost;

                    TimedNode existing = openList.FirstOrDefault(n => n.X == nextX && n.Y == nextY && n.Time == nextTime);
                    if (existing == null)
                    {
                        existing = new TimedNode(nextX, nextY, nextTime);
                        openList.Add(existing);
                    }

                    if (existing.Parent == null || newG < existing.G)
                    {
                        existing.G = newG;
                        existing.H = GetManhattanCost(nextX, nextY, targetX, targetY);
                        existing.Parent = currentNode;
                    }
                }
            }

            return null;
        }

        private void ReservePlannedPath(HashSet<string> reservedCells, HashSet<string> reservedMoves, int startX, int startY, List<Node> path, int startTime)
        {
            int currentX = startX;
            int currentY = startY;
            int time = startTime;

            foreach (Node step in path)
            {
                time++;
                ReserveCell(reservedCells, step.X, step.Y, time);
                ReserveMove(reservedMoves, currentX, currentY, step.X, step.Y, time);
                currentX = step.X;
                currentY = step.Y;
            }
        }

        private bool IsTargetFreeAfterArrival(int targetX, int targetY, int arrivalTime, HashSet<string> reservedCells)
        {
            int safeUntil = Math.Min(planningHorizon, arrivalTime + targetReserveTicks);
            for (int t = arrivalTime; t <= safeUntil; t++)
            {
                if (reservedCells.Contains(TimeCellKey(targetX, targetY, t))) return false;
            }
            return true;
        }

        private List<Node> ReconstructTimedPath(TimedNode endNode, int startX, int startY, int startTime)
        {
            List<Node> path = new List<Node>();
            TimedNode traceNode = endNode;

            while (traceNode != null && !(traceNode.X == startX && traceNode.Y == startY && traceNode.Time == startTime))
            {
                path.Add(new Node(traceNode.X, traceNode.Y));
                traceNode = traceNode.Parent;
            }

            path.Reverse();
            return path;
        }

        private int GetManhattanCost(int x1, int y1, int x2, int y2)
        {
            return (Math.Abs(x1 - x2) + Math.Abs(y1 - y2)) * moveCost;
        }

        public int CountWaitSteps(int startX, int startY, List<Node> path)
        {
            if (path == null || path.Count == 0) return 0;

            int waitCount = 0;
            int currentX = startX;
            int currentY = startY;

            foreach (Node step in path)
            {
                if (step.X == currentX && step.Y == currentY) waitCount++;
                currentX = step.X;
                currentY = step.Y;
            }
            return waitCount;
        }

        public int CountTravelSteps(int startX, int startY, List<Node> path)
        {
            if (path == null || path.Count == 0) return 0;

            int travelCount = 0;
            int currentX = startX;
            int currentY = startY;

            foreach (Node step in path)
            {
                if (step.X != currentX || step.Y != currentY) travelCount++;
                currentX = step.X;
                currentY = step.Y;
            }

            return travelCount;
        }

        public double EstimateMissionBatteryPercent(Robot robot, double payloadKg, int distanceToTarget, int distanceHome)
        {
            double healthFactor = Math.Max(0.50, robot.BatteryHealth / 100.0);
            double goCost = distanceToTarget * (batteryDrainBase + payloadKg * batteryDrainPerKg);
            double homeCost = distanceHome * batteryDrainBase;
            return (goCost + homeCost) / healthFactor;
        }

        private void ReserveCell(HashSet<string> reservedCells, int x, int y, int time)
        {
            reservedCells.Add(TimeCellKey(x, y, time));
        }

        private void ReserveMove(HashSet<string> reservedMoves, int fromX, int fromY, int toX, int toY, int time)
        {
            reservedMoves.Add(TimeMoveKey(fromX, fromY, toX, toY, time));
        }

        private string TimeCellKey(int x, int y, int time)
        {
            return x + "," + y + "@" + time;
        }

        private string TimeMoveKey(int fromX, int fromY, int toX, int toY, int time)
        {
            return fromX + "," + fromY + ">" + toX + "," + toY + "@" + time;
        }
    }
}
