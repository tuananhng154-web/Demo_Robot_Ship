namespace Demo_Robot_Ship
{
    internal class ScoreCandidateRow
    {
        public string CandidateKey { get; set; }
        public int Tick { get; set; }
        public string Strategy { get; set; }
        public string Robot { get; set; }
        public string Decision { get; set; }
        public string Reason { get; set; }
        public string Orders { get; set; }
        public string Route { get; set; }
        public string Load { get; set; }
        public string Steps { get; set; }
        public string Battery { get; set; }
        public string Health { get; set; }
        public string Delay { get; set; }
        public double LoadScore { get; set; }
        public double DistanceScore { get; set; }
        public double TimeScore { get; set; }
        public double BatteryScore { get; set; }
        public double HealthScore { get; set; }
        public double WaitScore { get; set; }
        public double DelayPenalty { get; set; }
        public double FinalScore { get; set; }
        public string FinalScoreText { get; set; }
    }
}
