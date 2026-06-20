using System.Collections.Generic;
using System.Linq;

namespace Demo_Robot_Ship
{
    internal class StrategyRunMetrics
    {
        public int RunId { get; set; }
        public DispatchStrategy Strategy { get; set; }
        public int StartTick { get; set; }
        public int MoveSteps { get; set; }
        public int WaitSteps { get; set; }
        public int ReplanCount { get; set; }
        public double BatteryConsumed { get; set; }
        public int LastDeliveredTick { get; set; }
        public int AssignedOrderCount { get; set; }
        public HashSet<string> RobotsUsed { get; private set; }
        public List<double> LoadRatios { get; private set; }
        public List<int> OrderWaitTicks { get; private set; }

        public StrategyRunMetrics()
        {
            RobotsUsed = new HashSet<string>();
            LoadRatios = new List<double>();
            OrderWaitTicks = new List<int>();
            LastDeliveredTick = -1;
        }

        public bool HasActivity
        {
            get
            {
                return AssignedOrderCount > 0 || MoveSteps > 0 || WaitSteps > 0 || ReplanCount > 0 || BatteryConsumed > 0.0001;
            }
        }

        public double AverageLoadRatio
        {
            get { return LoadRatios.Count == 0 ? 0 : LoadRatios.Average(); }
        }

        public double AverageOrderWait
        {
            get { return OrderWaitTicks.Count == 0 ? 0 : OrderWaitTicks.Average(); }
        }
    }
}
