using System.Windows.Forms;

namespace Demo_Robot_Ship
{
    internal class RobotCard
    {
        public Panel Container { get; set; }
        public Label Title { get; set; }
        public Label Detail { get; set; }
        public ProgressBar Battery { get; set; }
        public Label BatteryText { get; set; }
    }
}
