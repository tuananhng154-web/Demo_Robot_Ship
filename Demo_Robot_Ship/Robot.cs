using System.Collections.Generic;
using System.Drawing;

namespace Demo_Robot_Ship
{
    public class Robot
    {
        public string Id { get; set; }
        public Color RobotColor { get; set; }
        public int GridX { get; set; }
        public int GridY { get; set; }
        public int HomeX { get; set; }
        public int HomeY { get; set; }

        // Pin hiện tại (State of Charge - SoC): robot đang còn bao nhiêu % trong lần sạc hiện tại.
        public double Battery { get; set; } = 100.0;

        // Sức khỏe pin (State of Health - SoH). Chai pin = 100% - BatteryHealth.
        public double BatteryHealth { get; set; } = 100.0;
        public double ChargeCycles { get; set; } = 0.0;
        public double BatterySpentSinceLastCharge { get; set; } = 0.0;
        public int DeepDischargeCount { get; set; } = 0;
        public bool DeepDischargeRecorded { get; set; } = false;

        // Tải hiện tại robot đang mang.
        public double Payload { get; set; } = 0.0;

        // Tải tối đa robot có thể mang trong một chuyến.
        public double MaxPayloadKg { get; set; } = 5.0;

        public int TotalDistance { get; set; } = 0;
        public int TotalDelivered { get; set; } = 0;
        public int WaitTicks { get; set; } = 0;
        public int TotalWaitSteps { get; set; } = 0;
        public int ReplanCount { get; set; } = 0;
        public int AvailableDelay { get; set; } = 0;
        public string Status { get; set; } = "IDLE"; // IDLE, DELIVERING, RETURNING, CHARGING, LOW_BATTERY, MAINTENANCE_REQUIRED, MAINTENANCE

        // Giữ lại CurrentOrder để tương thích code cũ, nhưng phiên bản mới dùng CurrentOrders để gom nhiều đơn theo tuyến.
        public DeliveryOrder CurrentOrder { get; set; }
        public List<DeliveryOrder> CurrentOrders { get; set; } = new List<DeliveryOrder>();
        public List<Node> CurrentPath { get; set; } = new List<Node>();
        public string CurrentRouteText { get; set; } = "";

        public double BatteryDegradation
        {
            get { return 100.0 - BatteryHealth; }
        }

        public Robot(string id, Color color, int x, int y, double maxPayloadKg)
        {
            Id = id;
            RobotColor = color;
            GridX = x;
            GridY = y;
            HomeX = x;
            HomeY = y;
            MaxPayloadKg = maxPayloadKg;
        }

        public Robot(string id, Color color, int x, int y) : this(id, color, x, y, 5.0)
        {
        }
    }

    public class Node
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int G { get; set; }
        public int H { get; set; }
        public int F { get { return G + H; } }
        public Node Parent { get; set; }

        public Node(int x, int y)
        {
            X = x;
            Y = y;
        }

        public string Key()
        {
            return X + "," + Y;
        }
    }
}
