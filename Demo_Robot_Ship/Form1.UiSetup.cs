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
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 230F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 220F));
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

            SetupLogBox();
            SetupTestLogTab();

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

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 5;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90F));
            layout.BackColor = ColorTranslator.FromHtml("#F8FAFC");

            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.Padding = new Padding(10, 6, 10, 6);
            header.BackColor = ColorTranslator.FromHtml("#111827");

            Label title = new Label();
            title.AutoSize = false;
            title.Dock = DockStyle.Fill;
            title.ForeColor = ColorTranslator.FromHtml("#E5E7EB");
            title.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            title.Text = "BẢNG KIỂM THỬ SCORE - tách riêng từng robot, xem điểm thành phần và phương án tốt nhất";
            title.TextAlign = ContentAlignment.MiddleLeft;

            Button btnClearTestLog = new Button();
            btnClearTestLog.Dock = DockStyle.Right;
            btnClearTestLog.Width = 118;
            btnClearTestLog.Text = "Xóa bảng";
            btnClearTestLog.FlatStyle = FlatStyle.Flat;
            btnClearTestLog.FlatAppearance.BorderSize = 0;
            btnClearTestLog.BackColor = ColorTranslator.FromHtml("#374151");
            btnClearTestLog.ForeColor = Color.White;
            btnClearTestLog.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnClearTestLog.Click += delegate
            {
                ClearScoreTestData();
                if (rtbTestLog != null) rtbTestLog.Clear();
                WriteTestLog("[RESET] Đã xóa bảng kiểm thử Score.");
            };

            header.Controls.Add(btnClearTestLog);
            header.Controls.Add(title);

            lblScoreSummary = new Label();
            lblScoreSummary.Dock = DockStyle.Fill;
            lblScoreSummary.Padding = new Padding(10, 0, 10, 0);
            lblScoreSummary.TextAlign = ContentAlignment.MiddleLeft;
            lblScoreSummary.BackColor = ColorTranslator.FromHtml("#E0F2FE");
            lblScoreSummary.ForeColor = ColorTranslator.FromHtml("#0F172A");
            lblScoreSummary.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblScoreSummary.Text = "Chưa có dữ liệu kiểm thử. Thêm đơn rồi bấm Start để hệ thống tính Score.";

            tabScoreRobots = new TabControl();
            tabScoreRobots.Dock = DockStyle.Fill;
            tabScoreRobots.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);

            dgvScoreR1 = CreateScoreGrid(false);
            dgvScoreR3 = CreateScoreGrid(false);
            dgvScoreR2 = CreateScoreGrid(false);
            scoreRobotGrids["R1"] = dgvScoreR1;
            scoreRobotGrids["R3"] = dgvScoreR3;
            scoreRobotGrids["R2"] = dgvScoreR2;
            scoreRowsByRobot["R1"] = new List<ScoreCandidateRow>();
            scoreRowsByRobot["R3"] = new List<ScoreCandidateRow>();
            scoreRowsByRobot["R2"] = new List<ScoreCandidateRow>();

            AddRobotScoreTab("R1 - Robot nhỏ", dgvScoreR1);
            AddRobotScoreTab("R3 - Robot vừa", dgvScoreR3);
            AddRobotScoreTab("R2 - Robot tải lớn", dgvScoreR2);

            GroupBox bestBox = new GroupBox();
            bestBox.Text = "Kết quả tốt nhất theo từng robot và phương án được chọn";
            bestBox.Dock = DockStyle.Fill;
            bestBox.Padding = new Padding(8, 24, 8, 8);
            bestBox.BackColor = ColorTranslator.FromHtml("#F8FAFC");
            bestBox.ForeColor = ColorTranslator.FromHtml("#111827");
            bestBox.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);

            dgvBestScore = CreateScoreGrid(true);
            bestBox.Controls.Add(dgvBestScore);

            GroupBox rawBox = new GroupBox();
            rawBox.Text = "Log thô ngắn gọn";
            rawBox.Dock = DockStyle.Fill;
            rawBox.Padding = new Padding(8, 22, 8, 8);
            rawBox.BackColor = ColorTranslator.FromHtml("#F8FAFC");
            rawBox.ForeColor = ColorTranslator.FromHtml("#111827");
            rawBox.Font = new Font("Segoe UI", 8F, FontStyle.Bold);

            rtbTestLog = new RichTextBox();
            rtbTestLog.Dock = DockStyle.Fill;
            rtbTestLog.ReadOnly = true;
            rtbTestLog.BorderStyle = BorderStyle.None;
            rtbTestLog.BackColor = ColorTranslator.FromHtml("#0B1220");
            rtbTestLog.ForeColor = ColorTranslator.FromHtml("#E5E7EB");
            rtbTestLog.Font = new Font("Consolas", 8F);
            rtbTestLog.WordWrap = false;
            rtbTestLog.ScrollBars = RichTextBoxScrollBars.Both;
            rawBox.Controls.Add(rtbTestLog);

            layout.Controls.Add(header, 0, 0);
            layout.Controls.Add(lblScoreSummary, 0, 1);
            layout.Controls.Add(tabScoreRobots, 0, 2);
            layout.Controls.Add(bestBox, 0, 3);
            layout.Controls.Add(rawBox, 0, 4);
            tabPage4.Controls.Add(layout);

            StyleTabControl(tabScoreRobots);
            WriteTestLog("[SYSTEM] Dùng các bảng R1/R3/R2 để kiểm tra Score. Dòng CHỌN ở bảng dưới là phương án tốt nhất hiện tại.");
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
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            grid.ScrollBars = ScrollBars.Both;
            grid.RowTemplate.Height = 26;
            grid.Columns.Clear();

            AddScoreGridColumn(grid, "Tick", "Tick", 48);
            AddScoreGridColumn(grid, "Decision", bestGrid ? "Kết luận" : "Loại", 96);
            AddScoreGridColumn(grid, "Robot", "Robot", 55);
            AddScoreGridColumn(grid, "FinalScoreText", "Score", 70);
            AddScoreGridColumn(grid, "Orders", "Đơn", 150);
            AddScoreGridColumn(grid, "Route", "Tuyến", 170);
            AddScoreGridColumn(grid, "Load", "Tải", 95);
            AddScoreGridColumn(grid, "Steps", "Bước/Thời gian", 120);
            AddScoreGridColumn(grid, "Battery", "Pin", 155);
            AddScoreGridColumn(grid, "Health", "Pin khỏe", 100);
            AddScoreGridColumn(grid, "Delay", "Delay", 70);
            AddScoreGridColumn(grid, "LoadScore", "Load", 60, "0.00");
            AddScoreGridColumn(grid, "DistanceScore", "Dist", 60, "0.00");
            AddScoreGridColumn(grid, "TimeScore", "Time", 60, "0.00");
            AddScoreGridColumn(grid, "BatteryScore", "Battery", 70, "0.00");
            AddScoreGridColumn(grid, "HealthScore", "Health", 70, "0.00");
            AddScoreGridColumn(grid, "WaitScore", "Wait", 60, "0.00");
            AddScoreGridColumn(grid, "DelayPenalty", "Delay-", 65, "0.00");
            AddScoreGridColumn(grid, "Reason", "Lý do", 180);

            StyleGrid(grid);
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            grid.ColumnHeadersDefaultCellStyle.BackColor = bestGrid ? ColorTranslator.FromHtml("#16A34A") : ColorTranslator.FromHtml("#2563EB");
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 8F);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            grid.CellFormatting -= ScoreGrid_CellFormatting;
            grid.CellFormatting += ScoreGrid_CellFormatting;
            return grid;
        }

        private void ScoreGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            DataGridView grid = sender as DataGridView;
            if (grid == null || e.RowIndex < 0) return;
            if (grid.Columns[e.ColumnIndex].DataPropertyName != "Decision") return;

            string value = e.Value == null ? "" : e.Value.ToString();
            if (value.Contains("CHỌN"))
            {
                e.CellStyle.BackColor = ColorTranslator.FromHtml("#DCFCE7");
                e.CellStyle.ForeColor = ColorTranslator.FromHtml("#166534");
                e.CellStyle.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            }
            else if (value.Contains("TẠM CHỜ") || value.Contains("CHỜ"))
            {
                e.CellStyle.BackColor = ColorTranslator.FromHtml("#FEF3C7");
                e.CellStyle.ForeColor = ColorTranslator.FromHtml("#92400E");
                e.CellStyle.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            }
            else if (value.Contains("BEST") || value.Contains("TỐT"))
            {
                e.CellStyle.BackColor = ColorTranslator.FromHtml("#DBEAFE");
                e.CellStyle.ForeColor = ColorTranslator.FromHtml("#1D4ED8");
                e.CellStyle.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
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
