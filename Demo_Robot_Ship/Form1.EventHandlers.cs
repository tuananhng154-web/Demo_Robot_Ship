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
        private void btnStart_Click(object sender, EventArgs e)
        {
            EnsureComparisonRun();
            timerSimulation.Start();
            WriteLog("Hệ thống: Đã bắt đầu mô phỏng với cơ chế " + GetStrategyName(currentDispatchStrategy) + ".");
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            timerSimulation.Stop();
            WriteLog("Hệ thống: Đã tạm dừng mô phỏng.");
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            timerSimulation.Stop();
            FinalizeCurrentComparisonRun("Đã chốt trước khi reset");
            pendingOrders.Clear();
            allOrders.Clear();
            orderCounter = 0;
            simulationTick = 0;
            lastDelayedRobotLogTick = -1000;
            InitFleet();
            BuildRobotCards();
            ClearScoreTestData();
            if (rtbTestLog != null)
            {
                rtbTestLog.Clear();
                WriteTestLog("[RESET] Đã xóa bảng kiểm thử và khởi tạo lại dữ liệu test.");
            }
            StartNewComparisonRun();
            WriteLog("Hệ thống: Đã làm mới bản đồ, danh sách đơn và đội robot.");
            RefreshUi();
            picMap.Invalidate();
        }

        private void btnAddOrder_Click(object sender, EventArgs e)
        {
            string room = textBox1.Text.Trim().ToUpper();
            double weight = (double)numericUpDown1.Value;

            if (string.IsNullOrWhiteSpace(room))
            {
                MessageBox.Show("Nhập số phòng theo dạng A101, B203, C305... hoặc click trực tiếp vào cổng giao hàng trên bản đồ.", "Thiếu số phòng");
                textBox1.Focus();
                return;
            }

            string buildingKey = ExtractBuildingKey(room);
            if (!buildingTargets.ContainsKey(buildingKey))
            {
                MessageBox.Show("Không nhận ra tòa nhà từ số phòng. Ví dụ hợp lệ: A101, B203, I305, L402.", "Sai số phòng");
                textBox1.Focus();
                return;
            }

            AddOrder(room, weight, buildingTargets[buildingKey]);
            textBox1.Clear();
            textBox1.Focus();
            numericUpDown1.Value = 1;
        }

        private void picMap_MouseClick(object sender, MouseEventArgs e)
        {
            UpdateMapCellSize();
            int offsetX = GetMapOffsetX();
            int offsetY = GetMapOffsetY();
            int clickX = (e.X - offsetX) / cellSize;
            int clickY = (e.Y - offsetY) / cellSize;

            if (clickX >= 0 && clickX < mapGrid.GetLength(1) && clickY >= 0 && clickY < mapGrid.GetLength(0))
            {
                if (mapGrid[clickY, clickX] == 2)
                {
                    AddOrder("PORT-" + clickX + "-" + clickY, 1.0, new Node(clickX, clickY));
                }
            }
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            int value = Math.Max(1, trackBar1.Value);
            timerSimulation.Interval = Math.Max(40, 650 - value * 60);
            label3.Text = "Tốc độ: " + value + "/10";
        }

        private void AddOrder(string room, double weight, Node target)
        {
            DeliveryOrder order = new DeliveryOrder(++orderCounter, room, weight, target, simulationTick);
            pendingOrders.Enqueue(order);
            allOrders.Add(order);
            WriteLog(string.Format("Đơn mới: #{0:000} -> {1}, {2:0.0}kg, tọa độ ({3},{4}), tick {5}.", order.Id, order.Room, order.WeightKg, target.X, target.Y, simulationTick));
            RefreshUi();
            picMap.Invalidate();
        }

        private string ExtractBuildingKey(string room)
        {
            if (room == null) return "";
            string upper = room.Trim().ToUpper();
            foreach (char ch in upper)
            {
                string key = ch.ToString();
                if (buildingTargets.ContainsKey(key)) return key;
            }
            return "";
        }
    }
}
