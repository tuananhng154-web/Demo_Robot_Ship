using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Demo_Robot_Ship
{
    public partial class Form1 : Form
    {
        private int cellSize = 35;

        // 0 = Đường đi, 1 = Tường bao, 2 = Cổng giao/trạm sạc, 3 = lõi tòa nhà, 4 = tường tòa nhà
        private int[,] mapGrid = new int[,]
        {
            { 1, 1,1,1,1, 1, 1,1,1,1, 1, 1,1,1,1, 1, 1,1,1,1, 1 },
            { 1, 4,4,4,4, 0, 4,4,4,4, 0, 4,4,4,4, 0, 4,4,4,4, 1 },
            { 1, 4,3,3,2, 0, 2,3,3,4, 0, 4,3,3,2, 0, 2,3,3,4, 1 },
            { 1, 4,2,2,4, 0, 4,2,2,4, 0, 4,2,2,4, 0, 4,2,2,4, 1 },
            { 1, 0,0,0,0, 0, 0,0,0,0, 0, 0,0,0,0, 0, 0,0,0,0, 1 },
            { 1, 4,2,2,4, 0, 4,4,4,4, 0, 4,2,2,4, 0, 4,4,2,4, 1 },
            { 1, 4,3,3,4, 0, 4,3,3,2, 0, 2,3,3,4, 0, 4,3,3,4, 1 },
            { 1, 4,2,4,4, 0, 4,2,2,4, 0, 4,4,4,4, 0, 4,2,2,4, 1 },
            { 1, 0,0,0,0, 0, 0,0,0,0, 0, 0,0,0,0, 0, 0,0,0,0, 1 },
            { 1, 4,2,2,4, 0, 4,4,4,4, 0, 4,4,2,4, 0, 4,2,2,4, 1 },
            { 1, 4,3,3,2, 0, 2,3,3,4, 0, 4,3,3,4, 0, 4,3,3,4, 1 },
            { 1, 4,4,4,4, 0, 4,2,2,4, 0, 4,2,2,4, 0, 4,2,4,4, 1 },
            { 1, 0,0,0,0, 0, 0,0,0,0, 0, 0,0,0,0, 0, 0,0,0,0, 1 },
            { 1, 1,1,1,1, 1, 1,1,1,2, 2, 2,1,1,1, 1, 1,1,1,1, 1 }
        };

        private List<Robot> fleet = new List<Robot>();
        private PathPlanner pathPlanner;
        private Queue<DeliveryOrder> pendingOrders = new Queue<DeliveryOrder>();
        private List<DeliveryOrder> allOrders = new List<DeliveryOrder>();
        private Dictionary<string, Node> buildingTargets = new Dictionary<string, Node>();
        private Dictionary<string, RobotCard> robotCards = new Dictionary<string, RobotCard>();
        private RichTextBox rtbLog;
        private RichTextBox rtbTestLog;
        private Label lblScoreSummary;
        private TabControl tabScoreRobots;
        private DataGridView dgvScoreR1;
        private DataGridView dgvScoreR2;
        private DataGridView dgvScoreR3;
        private DataGridView dgvBestScore;
        private Dictionary<string, DataGridView> scoreRobotGrids = new Dictionary<string, DataGridView>();
        private Dictionary<string, List<ScoreCandidateRow>> scoreRowsByRobot = new Dictionary<string, List<ScoreCandidateRow>>();
        private List<ScoreCandidateRow> scoreAllRows = new List<ScoreCandidateRow>();
        private int orderCounter = 0;
        private int simulationTick = 0;
        private int lastDelayedRobotLogTick = -1000;

        // MODERN UI / MAP THEME
        private readonly Color CBackground = ColorTranslator.FromHtml("#F4F7FB");
        private readonly Color CSidebar = ColorTranslator.FromHtml("#0F172A");
        private readonly Color CCard = Color.White;
        private readonly Color CBorder = ColorTranslator.FromHtml("#E5E7EB");
        private readonly Color CText = ColorTranslator.FromHtml("#111827");
        private readonly Color CMuted = ColorTranslator.FromHtml("#6B7280");
        private readonly Color CPrimary = ColorTranslator.FromHtml("#2563EB");
        private readonly Color CPrimaryHover = ColorTranslator.FromHtml("#1D4ED8");
        private readonly Color CSuccess = ColorTranslator.FromHtml("#16A34A");
        private readonly Color CSuccessHover = ColorTranslator.FromHtml("#15803D");
        private readonly Color CWarning = ColorTranslator.FromHtml("#F59E0B");
        private readonly Color CWarningHover = ColorTranslator.FromHtml("#D97706");
        private readonly Color CDanger = ColorTranslator.FromHtml("#DC2626");
        private readonly Color CCharging = ColorTranslator.FromHtml("#7C3AED");
        private readonly Color CGrayButton = ColorTranslator.FromHtml("#475569");
        private readonly Color CGrayButtonHover = ColorTranslator.FromHtml("#334155");

        private float pathDashOffset = 0f;
        private bool showDebugGrid = false;
        private List<BuildingView> buildings = new List<BuildingView>();

        private const int MaxLogLines = 250;
        private const int MaxTestLogLines = 1000;

        // Cửa sổ gom đơn theo tick mô phỏng. Đạt 70% tải thì đi sớm, hết thời gian chờ thì bắt buộc giao.
        private const int MaxBatchWaitTicks = 15;
        private const double LoadDispatchThreshold = 0.70;
        private const int MaxOrdersPerTrip = 5;

        // Mô hình pin: CurrentBattery là pin hiện tại, BatteryHealth là sức khỏe pin.
        private const double BatteryReservePercent = 15.0;
        private const double RechargeRatePerTick = 1.2;
        private const double LowBatteryThreshold = 20.0;
        private const double MaintenanceHealthThreshold = 40.0;
        private const double ReadyBatteryThreshold = 80.0;
        // Hệ số mô phỏng chai pin. Để demo dễ quan sát, đặt 2% cho mỗi chu kỳ xả-sạc tương đương.
        // Nếu muốn gần thực tế hơn, có thể giảm về 0.03.
        private const double DegradationPerCycle = 2.0;

        // Tiêu hao năng lượng theo công thức: move*base*(1 + loadFactor*loadRatio) + wait*baseWait.
        private const double BatteryDrainBase = 0.35;
        private const double BatteryDrainPerKg = 0.02;
        private const double BaseWaitCost = 0.05;
        private const double LoadFactor = 0.40;

        // COOPERATIVE A*: lập kế hoạch theo thời gian để tránh robot đụng nhau.
        private const int PlanningHorizon = 120;
        private const int TargetReserveTicks = 8;
        private const int MoveCost = 10;
        private const int WaitCost = 11;

        public Form1()
        {
            InitializeComponent();
            pathPlanner = new PathPlanner(mapGrid, PlanningHorizon, TargetReserveTicks, MoveCost, WaitCost, BatteryDrainBase, BatteryDrainPerKg);

            InitBuildingTargets();
            InitBuildingViews();
            InitFleet();
            SetupDashboardUi();
            RefreshUi();
        }

        private void InitFleet()
        {
            fleet.Clear();

            // Mỗi robot có tải tối đa khác nhau để bộ điều phối chọn robot tiết kiệm nhất.
            fleet.Add(new Robot("R1", Color.Red, 9, 13, 5.0));         // Robot nhỏ: phù hợp đơn nhẹ/gần
            fleet.Add(new Robot("R3", Color.LimeGreen, 10, 13, 8.0));  // Robot trung bình
            fleet.Add(new Robot("R2", Color.DodgerBlue, 11, 13, 12.0)); // Robot tải lớn
        }

        private void InitBuildingTargets()
        {
            // Chọn 1 cổng chính cho từng tòa. Nếu muốn đổi cổng, sửa tọa độ ở đây.
            buildingTargets.Clear();
            buildingTargets["I"] = new Node(2, 3);
            buildingTargets["J"] = new Node(7, 3);
            buildingTargets["K"] = new Node(12, 3);
            buildingTargets["L"] = new Node(17, 3);
            buildingTargets["E"] = new Node(2, 5);
            buildingTargets["F"] = new Node(7, 7);
            buildingTargets["G"] = new Node(12, 5);
            buildingTargets["H"] = new Node(17, 7);
            buildingTargets["A"] = new Node(2, 9);
            buildingTargets["B"] = new Node(7, 11);
            buildingTargets["C"] = new Node(12, 11);
            buildingTargets["D"] = new Node(17, 9);
        }

        private void InitBuildingViews()
        {
            buildings.Clear();
            buildings.Add(new BuildingView("TÒA I", new Rectangle(1, 1, 4, 3), new Point(2, 3)));
            buildings.Add(new BuildingView("TÒA J", new Rectangle(6, 1, 4, 3), new Point(7, 3)));
            buildings.Add(new BuildingView("TÒA K", new Rectangle(11, 1, 4, 3), new Point(12, 3)));
            buildings.Add(new BuildingView("TÒA L", new Rectangle(16, 1, 4, 3), new Point(17, 3)));

            buildings.Add(new BuildingView("TÒA E", new Rectangle(1, 5, 4, 3), new Point(2, 5)));
            buildings.Add(new BuildingView("TÒA F", new Rectangle(6, 5, 4, 3), new Point(7, 7)));
            buildings.Add(new BuildingView("TÒA G", new Rectangle(11, 5, 4, 3), new Point(12, 5)));
            buildings.Add(new BuildingView("TÒA H", new Rectangle(16, 5, 4, 3), new Point(17, 7)));

            buildings.Add(new BuildingView("TÒA A", new Rectangle(1, 9, 4, 3), new Point(2, 9)));
            buildings.Add(new BuildingView("TÒA B", new Rectangle(6, 9, 4, 3), new Point(7, 11)));
            buildings.Add(new BuildingView("TÒA C", new Rectangle(11, 9, 4, 3), new Point(12, 11)));
            buildings.Add(new BuildingView("TÒA D", new Rectangle(16, 9, 4, 3), new Point(17, 9)));
        }
    }
}
