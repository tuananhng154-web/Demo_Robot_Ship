using System.Collections.Generic;

namespace Demo_Robot_Ship
{
    internal class CvrpAssignmentPlan
    {
        public Dictionary<string, List<DeliveryOrder>> GroupsByRobotId { get; set; }
        public int TotalDistance { get; set; }

        public CvrpAssignmentPlan()
        {
            GroupsByRobotId = new Dictionary<string, List<DeliveryOrder>>();
        }
    }
}
