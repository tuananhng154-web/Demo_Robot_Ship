namespace Demo_Robot_Ship
{
    internal class StrategyComparisonRow
    {
        public int RunId { get; set; }
        public string RunName { get; set; }
        public string StrategyName { get; set; }
        public string Status { get; set; }
        public int TotalDistance { get; set; }
        public int CompletionTime { get; set; }
        public string TotalBattery { get; set; }
        public string WaitReplan { get; set; }
        public int RobotsUsed { get; set; }
        public string AvgLoad { get; set; }
        public string AvgOrderWait { get; set; }
        public string Orders { get; set; }
        public string Note { get; set; }
        public bool Locked { get; set; }
    }
}
