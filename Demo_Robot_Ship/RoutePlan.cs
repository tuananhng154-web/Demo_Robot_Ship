using System.Collections.Generic;
using System.Linq;

namespace Demo_Robot_Ship
{
    internal class RoutePlan
    {
        public List<Node> Stops { get; set; }
        public List<Node> DeliveryPath { get; set; }
        public List<Node> ReturnHomePath { get; set; }
        public int DeliveryMoveSteps { get; set; }
        public int DeliveryWaitSteps { get; set; }
        public int ReturnMoveSteps { get; set; }
        public int ReturnWaitSteps { get; set; }

        public RoutePlan()
        {
            Stops = new List<Node>();
            DeliveryPath = new List<Node>();
            ReturnHomePath = new List<Node>();
        }

        public List<Node> FullPath
        {
            get
            {
                List<Node> result = new List<Node>();
                result.AddRange(DeliveryPath);
                result.AddRange(ReturnHomePath);
                return result;
            }
        }

        public int MoveSteps
        {
            get { return DeliveryMoveSteps + ReturnMoveSteps; }
        }

        public int WaitSteps
        {
            get { return DeliveryWaitSteps + ReturnWaitSteps; }
        }

        public int EstimatedTimeTicks
        {
            get { return DeliveryPath.Count + ReturnHomePath.Count; }
        }

        public string RouteText
        {
            get
            {
                if (Stops == null || Stops.Count == 0) return "Home";
                return string.Join(" -> ", Stops.Select(s => string.Format("({0},{1})", s.X, s.Y))) + " -> Home";
            }
        }
    }
}
