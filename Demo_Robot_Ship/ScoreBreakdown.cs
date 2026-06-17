namespace Demo_Robot_Ship
{
    internal class ScoreBreakdown
    {
        public double LoadScore { get; set; }
        public double DistanceScore { get; set; }
        public double TimeScore { get; set; }
        public double BatteryScore { get; set; }
        public double HealthScore { get; set; }
        public double WaitScore { get; set; }
        public double RobotDelayPenalty { get; set; }
        public double FinalScore { get; set; }

        public string ToLogString()
        {
            return string.Format(
                "Load={0:0.00}, Dist={1:0.00}, Time={2:0.00}, Battery={3:0.00}, Health={4:0.00}, Wait={5:0.00}, Delay={6:0.00}, Score={7:0.0}",
                LoadScore, DistanceScore, TimeScore, BatteryScore, HealthScore, WaitScore, RobotDelayPenalty, FinalScore);
        }
    }
}
