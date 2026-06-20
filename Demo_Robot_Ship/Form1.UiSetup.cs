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
    public partial class Form1
    {
        private void SetupDashboardUi()
        {
            this.Text = "RobotShip - AI Campus Delivery Simulation";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1280, 720);
            this.BackColor = CBackground;

            // Cân lại layout: không để panel phải quá rộng vì sẽ ép bản đồ ở giữa bị cắt.
            pnlLeft.Width = 330;
            pnlRight.Width = 470;

            // Dùng % thay vì chiều cao tuyệt đối để khi phóng to/thu nhỏ form không bị đè layout.
            tableLayoutPanel2.RowStyles.Clear();
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));

            tableLayoutPanel1.RowStyles.Clear();
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 220F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 310F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Input đơn hàng
            textBox1.Name = "txtRoom";
            textBox1.CharacterCasing = CharacterCasing.Upper;
            textBox1.Text = "A101";

            numericUpDown1.Name = "numWeight";
            numericUpDown1.DecimalPlaces = 1;
            numericUpDown1.Minimum = 0;
            numericUpDown1.Maximum = 30; // Cho phép nhập đơn quá tải để demo hệ thống từ chối nếu vượt tải robot.
            numericUpDown1.Increment = 0.5M;
            if (numericUpDown1.Value == 0) numericUpDown1.Value = 1;

            btnAddOrder.Click -= btnAddOrder_Click;
            btnAddOrder.Click += btnAddOrder_Click;

            btnReset.Click -= btnReset_Click;
            btnReset.Click += btnReset_Click;

            trackBar1.Minimum = 1;
            trackBar1.Maximum = 10;
            trackBar1.TickFrequency = 1;
            if (trackBar1.Value < 1 || trackBar1.Value > 10) trackBar1.Value = 5;
            trackBar1.ValueChanged -= trackBar1_ValueChanged;
            trackBar1.ValueChanged += trackBar1_ValueChanged;
            trackBar1_ValueChanged(trackBar1, EventArgs.Empty);
            SetupDispatchStrategySelector();

            SetupLogBox();
            SetupTestLogTab();
            SetupComparisonTab();
            StartNewComparisonRun();

            SetupOrderGrid(dgvPending, false);
            SetupOrderGrid(dgvCompleted, true);

            flowLayoutPanel1.Resize -= flowLayoutPanel1_Resize;
            flowLayoutPanel1.Resize += flowLayoutPanel1_Resize;

            picMap.Resize -= picMap_Resize;
            picMap.Resize += picMap_Resize;

            BuildRobotCards();
            ApplyModernTheme();
            WriteLog("Hệ thống sẵn sàng. Nhập phòng dạng A101/B203 hoặc click vào cổng giao hàng trên bản đồ.");
        }

        private void SetupDispatchStrategySelector()
        {
            btnStart.Top = 28;
            btnPause.Top = 76;
            btnReset.Top = 124;

            Label lblStrategy = new Label();
            lblStrategy.AutoSize = true;
            lblStrategy.Left = 13;
            lblStrategy.Top = 174;
            lblStrategy.Text = "Cơ chế điều phối";
            lblStrategy.ForeColor = ColorTranslator.FromHtml("#E5E7EB");
            lblStrategy.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            cboDispatchStrategy = new ComboBox();
            cboDispatchStrategy.Left = 13;
            cboDispatchStrategy.Top = 199;
            cboDispatchStrategy.Width = 276;
            cboDispatchStrategy.DropDownStyle = ComboBoxStyle.DropDownList;
            cboDispatchStrategy.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            cboDispatchStrategy.Items.Clear();
            cboDispatchStrategy.Items.Add("Greedy - hiện tại");
            cboDispatchStrategy.Items.Add("CVRP - giới hạn tải");
            cboDispatchStrategy.SelectedIndex = currentDispatchStrategy == DispatchStrategy.Cvrp ? 1 : 0;
            cboDispatchStrategy.SelectedIndexChanged -= cboDispatchStrategy_SelectedIndexChanged;
            cboDispatchStrategy.SelectedIndexChanged += cboDispatchStrategy_SelectedIndexChanged;

            lblDispatchStrategyInfo = new Label();
            lblDispatchStrategyInfo.Left = 13;
            lblDispatchStrategyInfo.Top = 232;
            lblDispatchStrategyInfo.Width = 276;
            lblDispatchStrategyInfo.Height = 36;
            lblDispatchStrategyInfo.ForeColor = ColorTranslator.FromHtml("#CBD5E1");
            lblDispatchStrategyInfo.Font = new Font("Segoe UI", 7.8F, FontStyle.Regular);
            lblDispatchStrategyInfo.Text = GetStrategyDescription(currentDispatchStrategy);

            label3.Left = 13;
            label3.Top = 274;
            trackBar1.Left = 70;
            trackBar1.Top = 263;
            trackBar1.Width = 219;

            groupBox2.Controls.Add(lblStrategy);
            groupBox2.Controls.Add(cboDispatchStrategy);
            groupBox2.Controls.Add(lblDispatchStrategyInfo);
        }

        private void cboDispatchStrategy_SelectedIndexChanged(object sender, EventArgs e)
        {
            DispatchStrategy selected = cboDispatchStrategy != null && cboDispatchStrategy.SelectedIndex == 1
                ? DispatchStrategy.Cvrp
                : DispatchStrategy.Greedy;

            if (selected == currentDispatchStrategy)
            {
                if (lblDispatchStrategyInfo != null) lblDispatchStrategyInfo.Text = GetStrategyDescription(currentDispatchStrategy);
                return;
            }

            FinalizeCurrentComparisonRun("Đã chốt do đổi cơ chế");
            currentDispatchStrategy = selected;
            if (lblDispatchStrategyInfo != null) lblDispatchStrategyInfo.Text = GetStrategyDescription(currentDispatchStrategy);
            StartNewComparisonRun();
            WriteLog("Hệ thống: Đổi cơ chế điều phối sang " + GetStrategyName(currentDispatchStrategy) + ".");
            RefreshComparisonGrid();
        }

        private void SetupLogBox()
        {
            groupBox3.Controls.Clear();

            rtbLog = new RichTextBox();
            rtbLog.Dock = DockStyle.Fill;
            rtbLog.ReadOnly = true;
            rtbLog.BorderStyle = BorderStyle.None;
            rtbLog.BackColor = ColorTranslator.FromHtml("#0B1220");
            rtbLog.ForeColor = ColorTranslator.FromHtml("#E5E7EB");
            rtbLog.Font = new Font("Consolas", 9F);
            rtbLog.WordWrap = false;
            rtbLog.ScrollBars = RichTextBoxScrollBars.Vertical;

            groupBox3.Controls.Add(rtbLog);
        }


        private void SetupTestLogTab()
        {
            tabPage4.Text = "Bảng kiểm thử Score";
            tabPage4.Controls.Clear();
            tabPage4.BackColor = ColorTranslator.FromHtml("#F8FAFC");

            scoreRowsByRobot.Clear();
            scoreAllRows.Clear();
            scoreRobotGrids.Clear();
            scoreDetailGrids.Clear();
            scoreRobotBoxes.Clear();

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 3;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.BackColor = ColorTranslator.FromHtml("#F8FAFC");
            layout.Padding = new Padding(8);

            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.Padding = new Padding(12, 6, 12, 6);
            header.BackColor = ColorTranslator.FromHtml("#111827");

            Label title = new Label();
            title.AutoSize = false;
            title.Dock = DockStyle.Fill;
            title.ForeColor = ColorTranslator.FromHtml("#F9FAFB");
            title.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            title.Text = "KIỂM THỬ SCORE";
            title.TextAlign = ContentAlignment.MiddleLeft;

            Button btnClearTestLog = new Button();
            btnClearTestLog.Dock = DockStyle.Right;
            btnClearTestLog.Width = 100;
            btnClearTestLog.Text = "Xóa";
            btnClearTestLog.FlatStyle = FlatStyle.Flat;
            btnClearTestLog.FlatAppearance.BorderSize = 0;
            btnClearTestLog.BackColor = ColorTranslator.FromHtml("#374151");
            btnClearTestLog.ForeColor = Color.White;
            btnClearTestLog.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            btnClearTestLog.Click += delegate
            {
                ClearScoreTestData();
                if (rtbTestLog != null) rtbTestLog.Clear();
                WriteTestLog("[RESET] Score table cleared.");
            };

            header.Controls.Add(btnClearTestLog);
            header.Controls.Add(title);

            lblScoreSummary = new Label();
            lblScoreSummary.Dock = DockStyle.Fill;
            lblScoreSummary.Padding = new Padding(12, 0, 12, 0);
            lblScoreSummary.TextAlign = ContentAlignment.MiddleLeft;
            lblScoreSummary.BackColor = ColorTranslator.FromHtml("#EFF6FF");
            lblScoreSummary.ForeColor = ColorTranslator.FromHtml("#1E3A8A");
            lblScoreSummary.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblScoreSummary.Text = "Chưa có dữ liệu.";

            TableLayoutPanel body = new TableLayoutPanel();
            body.Dock = DockStyle.Fill;
            body.ColumnCount = 2;
            body.RowCount = 1;
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            body.BackColor = ColorTranslator.FromHtml("#F8FAFC");
            body.Padding = new Padding(0, 8, 0, 0);

            GroupBox robotsBox = new GroupBox();
            robotsBox.Text = "Robot ứng viên";
            robotsBox.Dock = DockStyle.Fill;
            robotsBox.Padding = new Padding(10, 24, 10, 10);
            robotsBox.BackColor = ColorTranslator.FromHtml("#F8FAFC");
            robotsBox.ForeColor = ColorTranslator.FromHtml("#111827");
            robotsBox.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            TableLayoutPanel robotLayout = new TableLayoutPanel();
            robotLayout.Dock = DockStyle.Fill;
            robotLayout.ColumnCount = 1;
            robotLayout.RowCount = 3;
            robotLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            robotLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            robotLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34F));
            robotLayout.BackColor = ColorTranslator.FromHtml("#F8FAFC");

            dgvScoreR1 = CreateScoreDetailGrid();
            dgvScoreR3 = CreateScoreDetailGrid();
            dgvScoreR2 = CreateScoreDetailGrid();

            scoreDetailGrids["R1"] = dgvScoreR1;
            scoreDetailGrids["R3"] = dgvScoreR3;
            scoreDetailGrids["R2"] = dgvScoreR2;
            scoreRowsByRobot["R1"] = new List<ScoreCandidateRow>();
            scoreRowsByRobot["R3"] = new List<ScoreCandidateRow>();
            scoreRowsByRobot["R2"] = new List<ScoreCandidateRow>();

            robotLayout.Controls.Add(CreateRobotScoreCard("R1", "R1 - Robot nhỏ", dgvScoreR1), 0, 0);
            robotLayout.Controls.Add(CreateRobotScoreCard("R3", "R3 - Robot vừa", dgvScoreR3), 0, 1);
            robotLayout.Controls.Add(CreateRobotScoreCard("R2", "R2 - Robot tải lớn", dgvScoreR2), 0, 2);
            robotsBox.Controls.Add(robotLayout);

            bestScoreBox = new GroupBox();
            bestScoreBox.Text = "Phương án được chọn";
            bestScoreBox.Dock = DockStyle.Fill;
            bestScoreBox.Padding = new Padding(10, 24, 10, 10);
            bestScoreBox.BackColor = ColorTranslator.FromHtml("#F8FAFC");
            bestScoreBox.ForeColor = ColorTranslator.FromHtml("#111827");
            bestScoreBox.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            dgvBestScore = CreateScoreDetailGrid();
            bestScoreBox.Controls.Add(dgvBestScore);

            body.Controls.Add(robotsBox, 0, 0);
            body.Controls.Add(bestScoreBox, 1, 0);

            // Giữ log ẩn để không làm rối giao diện nhưng vẫn không ảnh hưởng các hàm ghi log hiện có.
            rtbTestLog = new RichTextBox();
            rtbTestLog.Visible = false;

            layout.Controls.Add(header, 0, 0);
            layout.Controls.Add(lblScoreSummary, 0, 1);
            layout.Controls.Add(body, 0, 2);
            tabPage4.Controls.Add(layout);

            WriteTestLog("[SYSTEM] Score view ready.");
        }

        private GroupBox CreateRobotScoreCard(string robotId, string title, DataGridView grid)
        {
            GroupBox box = new GroupBox();
            box.Text = title;
            box.Dock = DockStyle.Fill;
            box.Padding = new Padding(8, 22, 8, 8);
            box.Margin = new Padding(0, 0, 0, 8);
            box.BackColor = Color.White;
            box.ForeColor = ColorTranslator.FromHtml("#111827");
            box.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            grid.Dock = DockStyle.Fill;
            box.Controls.Add(grid);
            scoreRobotBoxes[robotId] = box;
            return box;
        }

        private DataGridView CreateScoreDetailGrid()
        {
            DataGridView grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.AutoGenerateColumns = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.RowHeadersVisible = false;
            grid.ColumnHeadersVisible = false;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.FixedSingle;
            grid.EnableHeadersVisualStyles = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells;
            grid.ScrollBars = ScrollBars.Vertical;
            grid.RowTemplate.Height = 28;
            grid.Columns.Clear();

            DataGridViewTextBoxColumn fieldCol = new DataGridViewTextBoxColumn();
            fieldCol.DataPropertyName = "Field";
            fieldCol.HeaderText = "Mục";
            fieldCol.FillWeight = 34;
            fieldCol.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            fieldCol.DefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#475569");
            grid.Columns.Add(fieldCol);

            DataGridViewTextBoxColumn valueCol = new DataGridViewTextBoxColumn();
            valueCol.DataPropertyName = "Value";
            valueCol.HeaderText = "Giá trị";
            valueCol.FillWeight = 66;
            valueCol.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.Columns.Add(valueCol);

            grid.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);
            grid.DefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#111827");
            grid.DefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#DBEAFE");
            grid.DefaultCellStyle.SelectionForeColor = ColorTranslator.FromHtml("#111827");
            grid.GridColor = ColorTranslator.FromHtml("#E5E7EB");
            grid.AlternatingRowsDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#F8FAFC");
            grid.CellFormatting -= ScoreDetailGrid_CellFormatting;
            grid.CellFormatting += ScoreDetailGrid_CellFormatting;
            return grid;
        }

        private void ScoreDetailGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            DataGridView grid = sender as DataGridView;
            if (grid == null || e.RowIndex < 0 || e.ColumnIndex < 0) return;
            ScoreDetailItem item = grid.Rows[e.RowIndex].DataBoundItem as ScoreDetailItem;
            if (item == null) return;

            if (item.Field == "Kết quả")
            {
                string value = item.Value == null ? "" : item.Value;
                if (value.Contains("CHỌN"))
                {
                    grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = ColorTranslator.FromHtml("#DCFCE7");
                    grid.Rows[e.RowIndex].DefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#14532D");
                    grid.Rows[e.RowIndex].DefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                }
                else if (value.Contains("CHỜ"))
                {
                    grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = ColorTranslator.FromHtml("#FEF3C7");
                    grid.Rows[e.RowIndex].DefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#92400E");
                }
            }
            else if (item.Field == "Score")
            {
                grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = ColorTranslator.FromHtml("#EFF6FF");
                grid.Rows[e.RowIndex].DefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#1D4ED8");
                grid.Rows[e.RowIndex].DefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            }
        }

        private void SetupComparisonTab()

        {
            tabComparison = new TabPage("So sánh cơ chế");
            tabComparison.BackColor = ColorTranslator.FromHtml("#F8FAFC");
            tabComparison.Padding = new Padding(8);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 3;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.BackColor = ColorTranslator.FromHtml("#F8FAFC");

            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.Padding = new Padding(10, 6, 10, 6);
            header.BackColor = ColorTranslator.FromHtml("#111827");

            Label title = new Label();
            title.Dock = DockStyle.Fill;
            title.TextAlign = ContentAlignment.MiddleLeft;
            title.ForeColor = ColorTranslator.FromHtml("#E5E7EB");
            title.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            title.Text = "SO SÁNH GREEDY / CVRP - chạy cùng tập đơn, chốt kết quả rồi reset để chạy cơ chế còn lại";

            Button btnClear = new Button();
            btnClear.Dock = DockStyle.Right;
            btnClear.Width = 112;
            btnClear.Text = "Xóa so sánh";
            btnClear.FlatStyle = FlatStyle.Flat;
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.BackColor = ColorTranslator.FromHtml("#991B1B");
            btnClear.ForeColor = Color.White;
            btnClear.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnClear.Click += delegate
            {
                comparisonRows.Clear();
                currentRunMetrics = null;
                strategyRunCounter = 0;
                StartNewComparisonRun();
                WriteLog("Hệ thống: Đã xóa bảng so sánh cơ chế.");
            };

            Button btnSnapshot = new Button();
            btnSnapshot.Dock = DockStyle.Right;
            btnSnapshot.Width = 112;
            btnSnapshot.Text = "Chốt kết quả";
            btnSnapshot.FlatStyle = FlatStyle.Flat;
            btnSnapshot.FlatAppearance.BorderSize = 0;
            btnSnapshot.BackColor = ColorTranslator.FromHtml("#2563EB");
            btnSnapshot.ForeColor = Color.White;
            btnSnapshot.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnSnapshot.Click += delegate
            {
                FinalizeCurrentComparisonRun("Đã chốt thủ công");
                StartNewComparisonRun();
                WriteLog("Hệ thống: Đã chốt kết quả so sánh cho lượt chạy hiện tại.");
            };

            header.Controls.Add(btnClear);
            header.Controls.Add(btnSnapshot);
            header.Controls.Add(title);

            lblComparisonSummary = new Label();
            lblComparisonSummary.Dock = DockStyle.Fill;
            lblComparisonSummary.Padding = new Padding(10, 0, 10, 0);
            lblComparisonSummary.TextAlign = ContentAlignment.MiddleLeft;
            lblComparisonSummary.BackColor = ColorTranslator.FromHtml("#ECFDF5");
            lblComparisonSummary.ForeColor = ColorTranslator.FromHtml("#064E3B");
            lblComparisonSummary.Font = new Font("Segoe UI", 8.4F, FontStyle.Bold);
            lblComparisonSummary.Text = "Chọn cơ chế ở panel trái, thêm đơn và chạy mô phỏng. Bấm Chốt kết quả sau mỗi lượt test.";

            dgvComparison = new DataGridView();
            dgvComparison.Dock = DockStyle.Fill;
            dgvComparison.AutoGenerateColumns = false;
            dgvComparison.AllowUserToAddRows = false;
            dgvComparison.AllowUserToDeleteRows = false;
            dgvComparison.AllowUserToResizeRows = false;
            dgvComparison.ReadOnly = true;
            dgvComparison.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvComparison.MultiSelect = false;
            dgvComparison.RowHeadersVisible = false;
            dgvComparison.BackgroundColor = Color.White;
            dgvComparison.BorderStyle = BorderStyle.None;
            dgvComparison.EnableHeadersVisualStyles = false;
            dgvComparison.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvComparison.RowTemplate.Height = 34;
            dgvComparison.Columns.Clear();

            AddComparisonColumn("RunName", "Lượt", 65, 6);
            AddComparisonColumn("StrategyName", "Cơ chế", 80, 8);
            AddComparisonColumn("Status", "Trạng thái", 90, 9);
            AddComparisonColumn("TotalDistance", "Tổng QĐ", 80, 8);
            AddComparisonColumn("CompletionTime", "TG hoàn thành", 95, 9);
            AddComparisonColumn("TotalBattery", "Pin tiêu thụ", 90, 9);
            AddComparisonColumn("WaitReplan", "Chờ / Replan", 90, 9);
            AddComparisonColumn("RobotsUsed", "Robot dùng", 75, 7);
            AddComparisonColumn("AvgLoad", "Tải TB", 75, 7);
            AddComparisonColumn("AvgOrderWait", "Chờ đơn TB", 85, 8);
            AddComparisonColumn("Orders", "Đơn", 145, 14);
            AddComparisonColumn("Note", "Ghi chú", 220, 20);

            StyleGrid(dgvComparison);
            dgvComparison.DefaultCellStyle.Font = new Font("Segoe UI", 8F);
            dgvComparison.ColumnHeadersDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#059669");
            dgvComparison.CellFormatting -= ComparisonGrid_CellFormatting;
            dgvComparison.CellFormatting += ComparisonGrid_CellFormatting;

            layout.Controls.Add(header, 0, 0);
            layout.Controls.Add(lblComparisonSummary, 0, 1);
            layout.Controls.Add(dgvComparison, 0, 2);
            tabComparison.Controls.Add(layout);
            tabControl2.TabPages.Add(tabComparison);
        }

        private void AddComparisonColumn(string property, string header, int width, float fillWeight)
        {
            DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
            col.DataPropertyName = property;
            col.HeaderText = header;
            col.Width = width;
            col.FillWeight = fillWeight;
            dgvComparison.Columns.Add(col);
        }

        private void AddRobotScoreTab(string title, DataGridView grid)
        {
            TabPage page = new TabPage(title);
            page.BackColor = Color.White;
            page.Padding = new Padding(4);
            grid.Dock = DockStyle.Fill;
            page.Controls.Add(grid);
            tabScoreRobots.TabPages.Add(page);
        }

        private DataGridView CreateScoreGrid(bool bestGrid)
        {
            DataGridView grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.AutoGenerateColumns = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.RowHeadersVisible = false;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.EnableHeadersVisualStyles = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.ScrollBars = ScrollBars.Both;
            grid.RowTemplate.Height = bestGrid ? 32 : 30;
            grid.Columns.Clear();

            AddScoreGridColumn(grid, "Tick", "Tick", 44);
            AddScoreGridColumn(grid, "Strategy", "Cơ chế", 70);
            AddScoreGridColumn(grid, "Decision", bestGrid ? "Kết luận" : "Trạng thái", 95);
            AddScoreGridColumn(grid, "Robot", "Robot", 55);
            AddScoreGridColumn(grid, "FinalScoreText", "Score", 65);
            AddScoreGridColumn(grid, "Orders", "Nhóm đơn", 160);
            AddScoreGridColumn(grid, "Route", "Tuyến giao", 160);
            AddScoreGridColumn(grid, "Load", "Tải trọng", 95);
            AddScoreGridColumn(grid, "Steps", "Move/Wait/Time", 130);
            AddScoreGridColumn(grid, "Battery", "Pin", 155);
            AddScoreGridColumn(grid, "Health", "SoH", 75);
            AddScoreGridColumn(grid, "LoadScore", "Load", 58, "0.00");
            AddScoreGridColumn(grid, "DistanceScore", "Dist", 58, "0.00");
            AddScoreGridColumn(grid, "TimeScore", "Time", 58, "0.00");
            AddScoreGridColumn(grid, "BatteryScore", "Bat", 58, "0.00");
            AddScoreGridColumn(grid, "HealthScore", "Health", 62, "0.00");
            AddScoreGridColumn(grid, "WaitScore", "Wait", 58, "0.00");
            AddScoreGridColumn(grid, "DelayPenalty", "Delay-", 62, "0.00");
            AddScoreGridColumn(grid, "Reason", "Lý do", 180);

            StyleGrid(grid);
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.ColumnHeadersDefaultCellStyle.BackColor = bestGrid ? ColorTranslator.FromHtml("#16A34A") : ColorTranslator.FromHtml("#2563EB");
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 8F);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            if (grid.Columns.Count > 4) grid.Columns[4].DefaultCellStyle.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            grid.CellFormatting -= ScoreGrid_CellFormatting;
            grid.CellFormatting += ScoreGrid_CellFormatting;
            return grid;
        }

        private void ScoreGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            DataGridView grid = sender as DataGridView;
            if (grid == null || e.RowIndex < 0) return;
            ScoreCandidateRow row = grid.Rows[e.RowIndex].DataBoundItem as ScoreCandidateRow;
            if (row == null) return;

            string value = row.Decision == null ? "" : row.Decision;
            if (value.Contains("CHỌN"))
            {
                grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = ColorTranslator.FromHtml("#DCFCE7");
                grid.Rows[e.RowIndex].DefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#14532D");
                grid.Rows[e.RowIndex].DefaultCellStyle.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            }
            else if (value.Contains("TẠM CHỜ") || value.Contains("CHỜ"))
            {
                grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = ColorTranslator.FromHtml("#FEF3C7");
                grid.Rows[e.RowIndex].DefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#92400E");
            }
            else if (value.Contains("BEST") || value.Contains("TỐT"))
            {
                grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = ColorTranslator.FromHtml("#DBEAFE");
                grid.Rows[e.RowIndex].DefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#1D4ED8");
            }
        }

        private void AddScoreGridColumn(DataGridView grid, string property, string header, int width)
        {
            AddScoreGridColumn(grid, property, header, width, null);
        }

        private void AddScoreGridColumn(DataGridView grid, string property, string header, int width, string format)
        {
            DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
            col.DataPropertyName = property;
            col.HeaderText = header;
            col.Width = width;
            if (!string.IsNullOrEmpty(format)) col.DefaultCellStyle.Format = format;
            grid.Columns.Add(col);
        }

        private void SetupOrderGrid(DataGridView grid, bool completedGrid)
        {
            grid.AutoGenerateColumns = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.RowHeadersVisible = false;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.FixedSingle;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 8F);
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            grid.RowTemplate.Height = 26;
            grid.Columns.Clear();

            AddGridColumn(grid, "IdText", "Mã", 70);
            AddGridColumn(grid, "Room", "Phòng/Cổng", 105);
            AddGridColumn(grid, "WeightText", "Kg", 55);
            AddGridColumn(grid, "RobotText", "Robot", 70);
            AddGridColumn(grid, "StatusText", "Trạng thái", 95);
            AddGridColumn(grid, completedGrid ? "DeliveredText" : "CreatedText", completedGrid ? "Hoàn tất" : "Tạo lúc", 85);
            StyleGrid(grid);
        }

        private void AddGridColumn(DataGridView grid, string dataPropertyName, string headerText, int width)
        {
            DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
            col.DataPropertyName = dataPropertyName;
            col.HeaderText = headerText;
            col.Width = width;
            grid.Columns.Add(col);
        }

        private void BuildRobotCards()
        {
            robotCards.Clear();
            flowLayoutPanel1.Controls.Clear();
            flowLayoutPanel1.WrapContents = false;
            flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;
            flowLayoutPanel1.AutoScroll = true;
            flowLayoutPanel1.BackColor = Color.WhiteSmoke;

            foreach (Robot robot in fleet)
            {
                RobotCard card = CreateRobotCard(robot);
                robotCards[robot.Id] = card;
                flowLayoutPanel1.Controls.Add(card.Container);
            }

            ResizeRobotCards();
        }

        private RobotCard CreateRobotCard(Robot robot)
        {
            int cardWidth = Math.Max(280, flowLayoutPanel1.ClientSize.Width - 24);

            Panel panel = new Panel();
            panel.Width = cardWidth;
            panel.Height = 96;
            panel.Margin = new Padding(6, 5, 6, 5);
            panel.Padding = new Padding(8);
            panel.BackColor = Color.White;
            panel.BorderStyle = BorderStyle.FixedSingle;

            Panel colorDot = new Panel();
            colorDot.Left = 12;
            colorDot.Top = 14;
            colorDot.Width = 20;
            colorDot.Height = 20;
            colorDot.BackColor = robot.RobotColor;

            Label title = new Label();
            title.Left = 44;
            title.Top = 8;
            title.Width = cardWidth - 58;
            title.Height = 22;
            title.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);

            Label detail = new Label();
            detail.Left = 44;
            detail.Top = 31;
            detail.Width = cardWidth - 58;
            detail.Height = 39;
            detail.Font = new Font("Segoe UI", 8.2F);

            ProgressBar battery = new ProgressBar();
            battery.Left = 44;
            battery.Top = 74;
            battery.Width = Math.Max(120, cardWidth - 155);
            battery.Height = 12;
            battery.Minimum = 0;
            battery.Maximum = 100;

            Label batteryText = new Label();
            batteryText.Left = battery.Right + 6;
            batteryText.Top = 70;
            batteryText.Width = 72;
            batteryText.Height = 20;
            batteryText.Font = new Font("Segoe UI", 8.1F, FontStyle.Bold);

            panel.Controls.Add(colorDot);
            panel.Controls.Add(title);
            panel.Controls.Add(detail);
            panel.Controls.Add(battery);
            panel.Controls.Add(batteryText);

            return new RobotCard
            {
                Container = panel,
                Title = title,
                Detail = detail,
                Battery = battery,
                BatteryText = batteryText
            };
        }

        private void flowLayoutPanel1_Resize(object sender, EventArgs e)
        {
            ResizeRobotCards();
        }

        private void picMap_Resize(object sender, EventArgs e)
        {
            picMap.Invalidate();
        }

        private void ResizeRobotCards()
        {
            int cardWidth = Math.Max(280, flowLayoutPanel1.ClientSize.Width - 24);

            foreach (RobotCard card in robotCards.Values)
            {
                card.Container.Width = cardWidth;
                card.Title.Width = cardWidth - 58;
                card.Detail.Width = cardWidth - 58;
                card.Battery.Width = Math.Max(120, cardWidth - 155);
                card.BatteryText.Left = card.Battery.Right + 6;
            }
        }
        // Thiết kế môi trường bản đồ dạng lưới 2D; ứng dụng thuật toán Tham lam để phân bổ đơn hàng tự động.
        // Sử dụng thuật toán Time-Space A* kết hợp Bảng đặt chỗ (Reservation Table) để tìm đường đi ngắn nhất
        // và xử lý triệt để xung đột (né tránh va chạm) giữa các robot.
        // Bao gồm giao diện trực quan hóa quá trình di chuyển theo thời gian thực và thống kê hiệu suất (quãng đường, thời gian).
        private void ApplyModernTheme()
        {
            pnlLeft.BackColor = CSidebar;
            pnlRight.BackColor = CBackground;
            pnlCenter.BackColor = CBackground;
            pnlLeft.Padding = new Padding(10, 0, 10, 10);
            pnlRight.Padding = new Padding(10, 0, 10, 10);
            pnlCenter.Padding = new Padding(10, 0, 10, 10);

            SetDevLabel(labelControl1, "DASHBOARD", Color.White);
            SetDevLabel(labelControl2, "LIVE DATA", CText);
            SetDevLabel(labelControl3, "AI SIMULATION MAP", CText);

            StyleSidebarGroup(groupBox1);
            StyleSidebarGroup(groupBox2);
            StyleSidebarGroup(groupBox3);
            StyleLightGroup(groupBox4);
            StyleLightGroup(groupBox5);

            StyleTextBox(textBox1);
            StyleNumeric(numericUpDown1);
            StyleButton(btnAddOrder, CPrimary, CPrimaryHover, Color.White);
            StyleButton(btnStart, CSuccess, CSuccessHover, Color.White);
            StyleButton(btnPause, CWarning, CWarningHover, Color.White);
            StyleButton(btnReset, CGrayButton, CGrayButtonHover, Color.White);
            if (cboDispatchStrategy != null)
            {
                cboDispatchStrategy.BackColor = Color.White;
                cboDispatchStrategy.ForeColor = CText;
            }

            trackBar1.BackColor = CSidebar;
            SetSidebarLabels(groupBox1);
            SetSidebarLabels(groupBox2);
            SetSidebarLabels(groupBox3);

            StyleTabControl(tabControl1);
            StyleTabControl(tabControl2);

            if (rtbLog != null) StyleLogBox(rtbLog);
            if (rtbTestLog != null) StyleLogBox(rtbTestLog);
            picMap.BackColor = CBackground;
            flowLayoutPanel1.BackColor = CBackground;
        }

        private void SetDevLabel(DevExpress.XtraEditors.LabelControl label, string text, Color foreColor)
        {
            label.Text = text;
            label.Appearance.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            label.Appearance.ForeColor = foreColor;
            label.Appearance.Options.UseFont = true;
            label.Appearance.Options.UseForeColor = true;
            label.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            label.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        }

        private void SetSidebarLabels(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                Label lbl = c as Label;
                if (lbl != null)
                {
                    lbl.ForeColor = ColorTranslator.FromHtml("#E5E7EB");
                    lbl.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                }
                if (c.HasChildren) SetSidebarLabels(c);
            }
        }

        private void StyleSidebarGroup(GroupBox gb)
        {
            gb.ForeColor = Color.White;
            gb.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            gb.BackColor = CSidebar;
            gb.Padding = new Padding(10);
        }

        private void StyleLightGroup(GroupBox gb)
        {
            gb.ForeColor = CText;
            gb.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            gb.BackColor = CBackground;
            gb.Padding = new Padding(8);
        }

        private void StyleTextBox(TextBox tb)
        {
            tb.BorderStyle = BorderStyle.FixedSingle;
            tb.BackColor = Color.White;
            tb.ForeColor = CText;
            tb.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular);
        }

        private void StyleNumeric(NumericUpDown nud)
        {
            nud.BorderStyle = BorderStyle.FixedSingle;
            nud.BackColor = Color.White;
            nud.ForeColor = CText;
            nud.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular);
        }

        private void StyleButton(Button btn, Color normalColor, Color hoverColor, Color textColor)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = normalColor;
            btn.ForeColor = textColor;
            btn.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;

            btn.MouseEnter -= ButtonHoverEnter;
            btn.MouseLeave -= ButtonHoverLeave;
            btn.MouseDown -= ButtonMouseDown;
            btn.MouseUp -= ButtonMouseUp;

            btn.Tag = new ButtonTheme(normalColor, hoverColor);
            btn.MouseEnter += ButtonHoverEnter;
            btn.MouseLeave += ButtonHoverLeave;
            btn.MouseDown += ButtonMouseDown;
            btn.MouseUp += ButtonMouseUp;
        }

        private void ButtonHoverEnter(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            ButtonTheme theme = btn == null ? null : btn.Tag as ButtonTheme;
            if (btn != null && theme != null) btn.BackColor = theme.Hover;
        }

        private void ButtonHoverLeave(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            ButtonTheme theme = btn == null ? null : btn.Tag as ButtonTheme;
            if (btn != null && theme != null) btn.BackColor = theme.Normal;
        }

        private void ButtonMouseDown(object sender, MouseEventArgs e)
        {
            Button btn = sender as Button;
            ButtonTheme theme = btn == null ? null : btn.Tag as ButtonTheme;
            if (btn != null && theme != null) btn.BackColor = ControlPaint.Dark(theme.Hover);
        }

        private void ButtonMouseUp(object sender, MouseEventArgs e)
        {
            Button btn = sender as Button;
            ButtonTheme theme = btn == null ? null : btn.Tag as ButtonTheme;
            if (btn != null && theme != null) btn.BackColor = theme.Hover;
        }

        private void StyleLogBox(RichTextBox rtb)
        {
            rtb.BackColor = ColorTranslator.FromHtml("#0B1220");
            rtb.ForeColor = ColorTranslator.FromHtml("#E5E7EB");
            rtb.BorderStyle = BorderStyle.None;
            rtb.Font = new Font("Consolas", 9F, FontStyle.Regular);
            rtb.ReadOnly = true;
        }

        private void StyleGrid(DataGridView dgv)
        {
            dgv.BackgroundColor = Color.White;
            dgv.BorderStyle = BorderStyle.None;
            dgv.EnableHeadersVisualStyles = false;
            dgv.RowHeadersVisible = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.ReadOnly = true;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.GridColor = CBorder;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = CPrimary;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.ColumnHeadersHeight = 34;
            dgv.DefaultCellStyle.BackColor = Color.White;
            dgv.DefaultCellStyle.ForeColor = CText;
            dgv.DefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#DBEAFE");
            dgv.DefaultCellStyle.SelectionForeColor = CText;
            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);
            dgv.RowTemplate.Height = 30;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#F8FAFC");
        }

        private void StyleTabControl(TabControl tab)
        {
            tab.DrawMode = TabDrawMode.OwnerDrawFixed;
            tab.SizeMode = TabSizeMode.Fixed;
            tab.ItemSize = new Size(130, 30);
            tab.DrawItem -= Tab_DrawItem;
            tab.DrawItem += Tab_DrawItem;
        }

        private void Tab_DrawItem(object sender, DrawItemEventArgs e)
        {
            TabControl tab = sender as TabControl;
            if (tab == null || e.Index < 0) return;

            Rectangle rect = e.Bounds;
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color back = selected ? Color.White : ColorTranslator.FromHtml("#E5E7EB");
            Color text = selected ? CPrimary : CMuted;

            using (SolidBrush b = new SolidBrush(back)) e.Graphics.FillRectangle(b, rect);
            using (Pen p = new Pen(CBorder)) e.Graphics.DrawRectangle(p, rect);

            TextRenderer.DrawText(
                e.Graphics,
                tab.TabPages[e.Index].Text,
                new Font("Segoe UI", 8.8F, FontStyle.Bold),
                rect,
                text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}
