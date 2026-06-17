using System;

namespace Demo_Robot_Ship
{
    internal static class BatteryModel
    {
        public static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static double GetEffectiveBattery(double currentBattery, double batteryHealth)
        {
            return Clamp(currentBattery, 0, 100) * Clamp(batteryHealth, 0, 100) / 100.0;
        }

        public static double EstimateEnergyCost(int moveSteps, int waitSteps, double loadRatio, double baseMoveCost, double baseWaitCost, double loadFactor)
        {
            loadRatio = Clamp(loadRatio, 0, 1);
            return moveSteps * baseMoveCost * (1.0 + loadFactor * loadRatio) + waitSteps * baseWaitCost;
        }

        public static double ConvertEffectiveCostToDisplayedBatteryDrain(double effectiveCost, double batteryHealth)
        {
            double healthFactor = Math.Max(0.40, Clamp(batteryHealth, 0, 100) / 100.0);
            return effectiveCost / healthFactor;
        }

        public static bool IsMaintenanceRequired(double batteryHealth, double minimumHealth)
        {
            return batteryHealth < minimumHealth;
        }

        public static string GetHealthLevel(double batteryHealth)
        {
            if (batteryHealth >= 85) return "Tốt";
            if (batteryHealth >= 70) return "Khá";
            if (batteryHealth >= 55) return "Yếu";
            if (batteryHealth >= 40) return "Rất yếu";
            return "Cần bảo dưỡng";
        }

        public static void RegisterBatteryUse(Robot robot, double displayedDrain)
        {
            // Giữ overload cũ để tránh lỗi nếu còn chỗ nào gọi theo dạng cũ.
            RegisterBatteryUse(robot, displayedDrain, 0.03);
        }

        public static void RegisterBatteryUse(Robot robot, double displayedDrain, double degradationPerCycle)
        {
            if (robot == null) return;

            displayedDrain = Math.Max(0, displayedDrain);

            // Lưu lượng pin đã dùng trong chu kỳ hiện tại để hiển thị/thống kê.
            robot.BatterySpentSinceLastCharge += displayedDrain;

            // Cập nhật chu kỳ sạc-xả theo lượng pin thực tế đã tiêu hao.
            // Ví dụ dùng 25% pin thì cộng 0.25 chu kỳ, không phải cứ cắm sạc là cộng 1 chu kỳ.
            robot.ChargeCycles += displayedDrain / 100.0;

            if (robot.Battery < 15 && !robot.DeepDischargeRecorded)
            {
                robot.DeepDischargeCount++;
                robot.DeepDischargeRecorded = true;
            }

            // Cập nhật BatteryHealth ngay sau khi robot tiêu hao pin để giao diện thấy độ chai thay đổi.
            robot.BatteryHealth = CalculateBatteryHealth(robot.ChargeCycles, robot.DeepDischargeCount, degradationPerCycle);
        }

        public static void CloseDischargeCycle(Robot robot, double degradationPerCycle)
        {
            if (robot == null) return;
            if (robot.BatterySpentSinceLastCharge <= 0) return;

            // Chu kỳ đã được cộng dồn dần trong RegisterBatteryUse().
            // Khi về trạm/sạc, chỉ đóng chu kỳ đang theo dõi và đồng bộ lại BatteryHealth.
            robot.BatterySpentSinceLastCharge = 0;
            robot.BatteryHealth = CalculateBatteryHealth(robot.ChargeCycles, robot.DeepDischargeCount, degradationPerCycle);
        }

        public static double CalculateBatteryHealth(double chargeCycles, int deepDischargeCount, double degradationPerCycle)
        {
            double health = 100.0 - chargeCycles * degradationPerCycle - deepDischargeCount * 0.5;
            return Clamp(health, 0, 100);
        }
    }
}
