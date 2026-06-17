namespace Demo_Robot_Ship
{
    internal class RobotAvailability
    {
        public Robot Robot { get; set; }
        public int AvailableDelay { get; set; }
        public int StartX { get; set; }
        public int StartY { get; set; }
        public double EstimatedBatteryAtStart { get; set; }
        public string Reason { get; set; }
    }
}
