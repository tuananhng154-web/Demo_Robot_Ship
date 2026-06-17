namespace Demo_Robot_Ship
{
    internal class AssignmentScoringSettings
    {
        public int MaxDistance { get; set; }
        public int MaxTime { get; set; }
        public int MaxBatchWait { get; set; }

        public AssignmentScoringSettings()
        {
            MaxDistance = 120;
            MaxTime = 140;
            MaxBatchWait = 15;
        }
    }

    internal static class AssignmentScorer
    {
        public static ScoreBreakdown Calculate(AssignmentCandidate candidate, AssignmentScoringSettings settings)
        {
            if (settings == null) settings = new AssignmentScoringSettings();

            int distance = candidate.RoutePlan == null ? settings.MaxDistance : candidate.RoutePlan.MoveSteps;
            int time = candidate.RoutePlan == null ? settings.MaxTime : candidate.RoutePlan.EstimatedTimeTicks;

            ScoreBreakdown score = new ScoreBreakdown();
            score.LoadScore = BatteryModel.Clamp(candidate.LoadRatio, 0, 1);
            score.DistanceScore = BatteryModel.Clamp(1.0 - (double)distance / settings.MaxDistance, 0, 1);
            score.TimeScore = BatteryModel.Clamp(1.0 - (double)time / settings.MaxTime, 0, 1);
            score.BatteryScore = BatteryModel.Clamp(candidate.BatteryAfterMission / 100.0, 0, 1);
            score.HealthScore = BatteryModel.Clamp(candidate.Robot.BatteryHealth / 100.0, 0, 1);
            score.WaitScore = BatteryModel.Clamp((double)candidate.OldestWaitTicks / settings.MaxBatchWait, 0, 1);
            score.RobotDelayPenalty = BatteryModel.Clamp((double)candidate.AvailableDelay / settings.MaxBatchWait, 0, 1);

            score.FinalScore =
                20.0 * score.LoadScore +
                20.0 * score.DistanceScore +
                15.0 * score.TimeScore +
                20.0 * score.BatteryScore +
                15.0 * score.HealthScore +
                10.0 * score.WaitScore -
                10.0 * score.RobotDelayPenalty;

            return score;
        }
    }
}
