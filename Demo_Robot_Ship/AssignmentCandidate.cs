using System.Collections.Generic;
using System.Linq;

namespace Demo_Robot_Ship
{
    internal class AssignmentCandidate
    {
        public Robot Robot { get; set; }
        public List<DeliveryOrder> Orders { get; set; }
        public RoutePlan RoutePlan { get; set; }
        public double TotalWeight { get; set; }
        public double LoadRatio { get; set; }
        public double EffectiveBattery { get; set; }
        public double EstimatedEnergyCost { get; set; }
        public double BatteryAfterMission { get; set; }
        public int OldestWaitTicks { get; set; }
        public int AvailableDelay { get; set; }
        public ScoreBreakdown ScoreBreakdown { get; set; }

        public AssignmentCandidate()
        {
            Orders = new List<DeliveryOrder>();
        }

        public double Score
        {
            get { return ScoreBreakdown == null ? 0 : ScoreBreakdown.FinalScore; }
        }

        public string OrderText
        {
            get { return Orders == null ? "" : string.Join(", ", Orders.Select(o => "#" + o.Id.ToString("000") + "-" + o.Room)); }
        }
    }
}
