using System.Collections.Generic;

namespace Demo_Robot_Ship
{
    internal class BatchAssignment
    {
        public Robot Robot { get; set; }
        public List<DeliveryOrder> Orders { get; set; }
        public Node Target { get; set; }
        public List<Node> Path { get; set; }
        public RoutePlan RoutePlan { get; set; }
        public double TotalWeight { get; set; }
        public double LoadRatio { get; set; }
        public double EffectiveBattery { get; set; }
        public double EstimatedBatteryCost { get; set; }
        public double BatteryAfterMission { get; set; }
        public int EstimatedDistance { get; set; }
        public int EstimatedTime { get; set; }
        public int OldestWaitTicks { get; set; }
        public int AvailableDelay { get; set; }
        public double Score { get; set; }
        public ScoreBreakdown ScoreBreakdown { get; set; }
        public string RouteText { get; set; }
    }
}
