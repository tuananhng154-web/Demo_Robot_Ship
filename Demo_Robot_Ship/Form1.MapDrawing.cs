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
        private int GetEffectiveCellSize()
        {
            int cols = mapGrid.GetLength(1);
            int rows = mapGrid.GetLength(0);

            // Trừ lề 24px để bản đồ không chạm sát tab/picturebox.
            int sizeByWidth = Math.Max(1, (picMap.ClientSize.Width - 24) / cols);
            int sizeByHeight = Math.Max(1, (picMap.ClientSize.Height - 24) / rows);

            // Giữ tối đa 35 như thiết kế cũ, nhưng tự co lại khi cửa sổ hẹp.
            return Math.Max(18, Math.Min(35, Math.Min(sizeByWidth, sizeByHeight)));
        }

        private void UpdateMapCellSize()
        {
            cellSize = GetEffectiveCellSize();
        }
        private void picMap_Paint(object sender, PaintEventArgs e)
        {
            UpdateMapCellSize();
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int rows = mapGrid.GetLength(0);
            int cols = mapGrid.GetLength(1);
            int offsetX = GetMapOffsetX();
            int offsetY = GetMapOffsetY();

            DrawCampusBackground(g, offsetX, offsetY, cols, rows);
            DrawModernRoads(g, offsetX, offsetY);
            DrawModernBuildings(g, offsetX, offsetY);
            DrawOrderTargets(g, offsetX, offsetY);
            DrawRobotPaths(g, offsetX, offsetY);
            DrawRobots(g, offsetX, offsetY);
        }

        private GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            int d = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private Rectangle CellRect(int gridX, int gridY, int offsetX, int offsetY)
        {
            return new Rectangle(offsetX + gridX * cellSize, offsetY + gridY * cellSize, cellSize, cellSize);
        }

        private void DrawCampusBackground(Graphics g, int offsetX, int offsetY, int cols, int rows)
        {
            Rectangle mapRect = new Rectangle(offsetX, offsetY, cols * cellSize, rows * cellSize);

            using (GraphicsPath shadow = RoundedRect(new Rectangle(mapRect.X + 5, mapRect.Y + 6, mapRect.Width, mapRect.Height), 18))
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(30, 15, 23, 42)))
            {
                g.FillPath(shadowBrush, shadow);
            }

            using (GraphicsPath bgPath = RoundedRect(mapRect, 18))
            using (LinearGradientBrush bgBrush = new LinearGradientBrush(mapRect, ColorTranslator.FromHtml("#F8FAFC"), ColorTranslator.FromHtml("#EEF6FF"), LinearGradientMode.Vertical))
            using (Pen borderPen = new Pen(ColorTranslator.FromHtml("#CBD5E1"), 2))
            {
                g.FillPath(bgBrush, bgPath);
                g.DrawPath(borderPen, bgPath);
            }
        }

        private void DrawModernRoads(Graphics g, int offsetX, int offsetY)
        {
            using (SolidBrush roadBrush = new SolidBrush(ColorTranslator.FromHtml("#E2E8F0")))
            using (SolidBrush portBrush = new SolidBrush(ColorTranslator.FromHtml("#FEF3C7")))
            using (Pen portPen = new Pen(ColorTranslator.FromHtml("#F59E0B"), 1.5F))
            {
                for (int r = 0; r < mapGrid.GetLength(0); r++)
                {
                    for (int c = 0; c < mapGrid.GetLength(1); c++)
                    {
                        if (mapGrid[r, c] == 0 || mapGrid[r, c] == 2)
                        {
                            Rectangle rect = CellRect(c, r, offsetX, offsetY);
                            Brush roadCellBrush = mapGrid[r, c] == 2 ? (Brush)portBrush : roadBrush;
                            g.FillRectangle(roadCellBrush, rect);

                            if (mapGrid[r, c] == 2)
                            {
                                Rectangle dot = new Rectangle(rect.X + cellSize / 2 - 4, rect.Y + cellSize / 2 - 4, 8, 8);
                                g.FillEllipse(Brushes.White, dot);
                                g.DrawEllipse(portPen, dot);
                            }
                        }
                    }
                }

                if (showDebugGrid)
                {
                    using (Pen gridPen = new Pen(ColorTranslator.FromHtml("#CBD5E1")))
                    {
                        for (int r = 0; r < mapGrid.GetLength(0); r++)
                        {
                            for (int c = 0; c < mapGrid.GetLength(1); c++)
                            {
                                g.DrawRectangle(gridPen, CellRect(c, r, offsetX, offsetY));
                            }
                        }
                    }
                }
            }

            Rectangle station = new Rectangle(offsetX + 9 * cellSize, offsetY + 13 * cellSize, 3 * cellSize, cellSize);
            station.Inflate(-2, -2);
            using (GraphicsPath stationPath = RoundedRect(station, 10))
            using (SolidBrush stationBrush = new SolidBrush(ColorTranslator.FromHtml("#EDE9FE")))
            using (Pen stationPen = new Pen(CCharging, 2))
            {
                g.FillPath(stationBrush, stationPath);
                g.DrawPath(stationPen, stationPath);
            }

            using (Font font = new Font("Segoe UI", Math.Max(6F, cellSize * 0.20F), FontStyle.Bold))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (SolidBrush textBrush = new SolidBrush(ColorTranslator.FromHtml("#5B21B6")))
            {
                g.DrawString("CHARGE", font, textBrush, station, sf);
            }
        }

        private void DrawModernBuildings(Graphics g, int offsetX, int offsetY)
        {
            foreach (BuildingView building in buildings)
            {
                DrawModernBuilding(g, building, offsetX, offsetY);
            }
        }

        private void DrawModernBuilding(Graphics g, BuildingView building, int offsetX, int offsetY)
        {
            Rectangle rect = new Rectangle(
                offsetX + building.GridRect.X * cellSize + 4,
                offsetY + building.GridRect.Y * cellSize + 4,
                building.GridRect.Width * cellSize - 8,
                building.GridRect.Height * cellSize - 8);

            using (GraphicsPath shadowPath = RoundedRect(new Rectangle(rect.X + 4, rect.Y + 5, rect.Width, rect.Height), 12))
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(35, 15, 23, 42)))
            {
                g.FillPath(shadowBrush, shadowPath);
            }

            using (GraphicsPath bodyPath = RoundedRect(rect, 12))
            using (LinearGradientBrush buildingBrush = new LinearGradientBrush(rect, ColorTranslator.FromHtml("#60A5FA"), ColorTranslator.FromHtml("#2563EB"), LinearGradientMode.Vertical))
            using (Pen borderPen = new Pen(ColorTranslator.FromHtml("#1E40AF"), 1.5F))
            {
                g.FillPath(buildingBrush, bodyPath);
                g.DrawPath(borderPen, bodyPath);
            }

            Rectangle inner = new Rectangle(rect.X + 10, rect.Y + 10, rect.Width - 20, rect.Height - 20);
            if (inner.Width > 10 && inner.Height > 10)
            {
                using (GraphicsPath innerPath = RoundedRect(inner, 8))
                using (SolidBrush innerBrush = new SolidBrush(Color.FromArgb(235, 255, 255, 255)))
                {
                    g.FillPath(innerBrush, innerPath);
                }
            }

            using (Font font = new Font("Segoe UI", Math.Max(7F, cellSize * 0.28F), FontStyle.Bold))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (SolidBrush textBrush = new SolidBrush(ColorTranslator.FromHtml("#0F172A")))
            {
                g.DrawString(building.Name, font, textBrush, inner, sf);
            }

            Point door = GetCellCenter(offsetX, offsetY, building.Door.X, building.Door.Y);
            Rectangle doorRect = new Rectangle(door.X - 6, door.Y - 6, 12, 12);
            using (SolidBrush doorBrush = new SolidBrush(ColorTranslator.FromHtml("#FACC15")))
            using (Pen doorPen = new Pen(ColorTranslator.FromHtml("#92400E"), 1.5F))
            {
                g.FillEllipse(doorBrush, doorRect);
                g.DrawEllipse(doorPen, doorRect);
            }
        }

        private void DrawOrderTargets(Graphics g, int offsetX, int offsetY)
        {
            foreach (DeliveryOrder order in allOrders.Where(o => o.Status == "WAITING" || o.Status == "ASSIGNED" || o.Status == "DELIVERING"))
            {
                Point center = GetCellCenter(offsetX, offsetY, order.Target.X, order.Target.Y);
                int radius = order.Status == "WAITING" ? 8 : 10;
                Rectangle halo = new Rectangle(center.X - radius, center.Y - radius, radius * 2, radius * 2);
                using (SolidBrush haloBrush = new SolidBrush(order.Status == "WAITING" ? Color.FromArgb(130, 245, 158, 11) : Color.FromArgb(150, 37, 99, 235)))
                using (Pen border = new Pen(order.Status == "WAITING" ? CWarning : CPrimary, 2))
                {
                    g.FillEllipse(haloBrush, halo);
                    g.DrawEllipse(border, halo);
                }
            }
        }

        private void DrawRobotPaths(Graphics g, int offsetX, int offsetY)
        {
            foreach (Robot robot in fleet)
            {
                if (robot.CurrentPath == null || robot.CurrentPath.Count == 0) continue;

                List<Point> points = new List<Point>();
                points.Add(GetCellCenter(offsetX, offsetY, robot.GridX, robot.GridY));
                foreach (Node node in robot.CurrentPath)
                {
                    points.Add(GetCellCenter(offsetX, offsetY, node.X, node.Y));
                }

                if (points.Count < 2) continue;

                using (Pen glowPen = new Pen(Color.FromArgb(70, robot.RobotColor), 8))
                using (Pen pathPen = new Pen(robot.RobotColor, 3))
                {
                    glowPen.StartCap = LineCap.Round;
                    glowPen.EndCap = LineCap.Round;
                    glowPen.LineJoin = LineJoin.Round;

                    pathPen.DashStyle = DashStyle.Dash;
                    pathPen.DashOffset = pathDashOffset;
                    pathPen.StartCap = LineCap.Round;
                    pathPen.EndCap = LineCap.Round;
                    pathPen.LineJoin = LineJoin.Round;

                    g.DrawLines(glowPen, points.ToArray());
                    g.DrawLines(pathPen, points.ToArray());
                }
            }
        }

        private void DrawRobots(Graphics g, int offsetX, int offsetY)
        {
            foreach (Robot robot in fleet)
            {
                DrawDeliveryRobot(g, robot, offsetX, offsetY);
            }
        }

        private void DrawDeliveryRobot(Graphics g, Robot robot, int offsetX, int offsetY)
        {
            Point center = GetCellCenter(offsetX, offsetY, robot.GridX, robot.GridY);
            int bodyW = Math.Max(22, (int)(cellSize * 0.72));
            int bodyH = Math.Max(18, (int)(cellSize * 0.58));

            Rectangle shadow = new Rectangle(center.X - bodyW / 2 + 3, center.Y - bodyH / 2 + 5, bodyW, bodyH);
            using (GraphicsPath shadowPath = RoundedRect(shadow, 8))
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
            {
                g.FillPath(shadowBrush, shadowPath);
            }

            Rectangle body = new Rectangle(center.X - bodyW / 2, center.Y - bodyH / 2, bodyW, bodyH);
            using (GraphicsPath bodyPath = RoundedRect(body, 8))
            using (SolidBrush bodyBrush = new SolidBrush(robot.RobotColor))
            using (Pen glowPen = new Pen(Color.FromArgb(140, robot.RobotColor), 3))
            using (Pen borderPen = new Pen(Color.White, 2))
            {
                g.DrawPath(glowPen, bodyPath);
                g.FillPath(bodyBrush, bodyPath);
                g.DrawPath(borderPen, bodyPath);
            }

            using (SolidBrush wheelBrush = new SolidBrush(ColorTranslator.FromHtml("#111827")))
            {
                int wheel = Math.Max(5, cellSize / 5);
                g.FillEllipse(wheelBrush, body.Left - wheel / 2, body.Top + 3, wheel, wheel);
                g.FillEllipse(wheelBrush, body.Left - wheel / 2, body.Bottom - wheel - 3, wheel, wheel);
                g.FillEllipse(wheelBrush, body.Right - wheel / 2, body.Top + 3, wheel, wheel);
                g.FillEllipse(wheelBrush, body.Right - wheel / 2, body.Bottom - wheel - 3, wheel, wheel);
            }

            string shortId = robot.Id.Replace("R", "");
            using (Font idFont = new Font("Segoe UI", Math.Max(7F, cellSize * 0.25F), FontStyle.Bold))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (SolidBrush textBrush = new SolidBrush(Color.White))
            {
                g.DrawString(shortId, idFont, textBrush, body, sf);
            }

            if (robot.Payload > 0)
            {
                Rectangle package = new Rectangle(body.Right - 9, body.Top - 8, 13, 11);
                using (GraphicsPath packPath = RoundedRect(package, 3))
                using (SolidBrush packBrush = new SolidBrush(CWarning))
                using (Pen packPen = new Pen(Color.White, 1))
                {
                    g.FillPath(packBrush, packPath);
                    g.DrawPath(packPen, packPath);
                }
            }

            int barWidth = Math.Max(18, bodyW);
            int batteryWidth = (int)(barWidth * Math.Max(0, Math.Min(100, robot.Battery)) / 100.0);
            Rectangle batteryBack = new Rectangle(center.X - barWidth / 2, body.Bottom + 4, barWidth, 4);
            Rectangle batteryFill = new Rectangle(batteryBack.X, batteryBack.Y, batteryWidth, batteryBack.Height);
            using (SolidBrush backBrush = new SolidBrush(ColorTranslator.FromHtml("#E5E7EB")))
            using (SolidBrush fillBrush = new SolidBrush(GetBatteryColor(robot.Battery)))
            {
                g.FillRectangle(backBrush, batteryBack);
                g.FillRectangle(fillBrush, batteryFill);
            }
        }

        private Color GetBatteryColor(double battery)
        {
            if (battery < 30) return CDanger;
            if (battery < 60) return CWarning;
            return CSuccess;
        }

        private Color GetStatusColor(string status)
        {
            if (status == "IDLE") return CPrimary;
            if (status == "DELIVERING") return CSuccess;
            if (status == "RETURNING") return CWarning;
            if (status == "CHARGING") return CCharging;
            if (status == "LOW_BATTERY") return CDanger;
            if (status == "MAINTENANCE_REQUIRED" || status == "MAINTENANCE") return CMuted;
            return CMuted;
        }

        private Point GetCellCenter(int offsetX, int offsetY, int gridX, int gridY)
        {
            return new Point(offsetX + gridX * cellSize + cellSize / 2, offsetY + gridY * cellSize + cellSize / 2);
        }

        private int GetMapOffsetX()
        {
            int mapWidth = mapGrid.GetLength(1) * cellSize;
            int offsetX = (picMap.Width - mapWidth) / 2;
            return offsetX < 0 ? 0 : offsetX;
        }

        private int GetMapOffsetY()
        {
            int mapHeight = mapGrid.GetLength(0) * cellSize;
            int offsetY = (picMap.Height - mapHeight) / 2;
            return offsetY < 0 ? 0 : offsetY;
        }

        private void tabPage4_SizeChanged(object sender, EventArgs e)
        {
            picMap.Invalidate();
        }
    }
}
