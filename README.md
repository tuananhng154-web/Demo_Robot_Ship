# Demo Robot Ship — AI Campus Delivery Simulation

Hệ thống mô phỏng đội robot tự hành giao hàng trên khuôn viên trường đại học, ứng dụng thuật toán Time-Space A* kết hợp Reservation Table để tránh va chạm và thuật toán Tham lam để phân bổ đơn hàng tự động.

## Mục lục

- [Yêu cầu hệ thống](#yêu-cầu-hệ-thống)
- [Gói phần mềm](#gói-phần-mềm)
- [Cài đặt](#cài-đặt)
- [Biên dịch](#biên-dịch)
- [Chạy chương trình](#chạy-chương-trình)
- [Hướng dẫn sử dụng](#hướng-dẫn-sử-dụng)
- [Cấu trúc thư mục](#cấu-trúc-thư-mục)

---

## Yêu cầu hệ thống

- Windows 10 / Windows 11 (64-bit) — bắt buộc, WinForms chỉ chạy trên Windows
- RAM tối thiểu 4 GB (khuyến nghị 8 GB)
- Ổ cứng trống khoảng 5 GB (cho Visual Studio và DevExpress)
- Visual Studio 2022 Community Edition
- .NET Framework 4.7.2 (thường đã có sẵn trên Windows 10/11)
- DevExpress v24.2.5

---

## Gói phần mềm

### Thư viện .NET Framework (tích hợp sẵn, không cần cài thêm)

| Assembly | Mô tả |
|----------|-------|
| `System.Windows.Forms` | Giao diện: Form, Button, DataGridView, Timer |
| `System.Drawing` | Vẽ bản đồ, robot, đường đi trên PictureBox |
| `System.Drawing.Drawing2D` | Gradient, đường đứt nét animated |
| `System.Collections.Generic` | List, Queue, Dictionary, HashSet dùng trong A* |
| `System.Linq` | Truy vấn danh sách đơn hàng, robot, candidate |
| `System.Data` | DataGridView binding cho bảng Score |
| `System.ComponentModel` | Data binding |
| `Microsoft.CSharp` | Dynamic code support |

### Thư viện bên ngoài — DevExpress v24.2.5 (cần cài đặt riêng)

| DLL | Mô tả |
|-----|-------|
| `DevExpress.XtraEditors.v24.2` | LabelControl — tiêu đề 3 cột giao diện |
| `DevExpress.Utils.v24.2` | Utilities hỗ trợ XtraEditors |
| `DevExpress.Data.v24.2` | Data layer |
| `DevExpress.Data.Desktop.v24.2` | Desktop extensions |
| `DevExpress.Drawing.v24.2` | Drawing primitives |
| `DevExpress.Printing.v24.2.Core` | Printing core (dependency) |
| `DevExpress.Sparkline.v24.2.Core` | Sparkline core (dependency) |

> **Lưu ý:** DevExpress là thư viện thương mại. Có thể dùng bản **Trial 30 ngày miễn phí** tại https://www.devexpress.com/products/try/ 

---

## Cài đặt

### Bước 1: Cài Visual Studio 2022 Community

1. Truy cập https://visualstudio.microsoft.com/vs/community/ và tải file cài đặt
2. Trong màn hình **Workloads**, tích chọn:
   - ✅ **.NET desktop development**
3. Trong tab **Individual components**, đảm bảo có:
   - ✅ **.NET Framework 4.7.2 targeting pack**
4. Nhấn **Install** và chờ hoàn tất (khoảng 15–30 phút)

### Bước 2: Cài .NET Framework 4.7.2 (nếu chưa có)

Windows 10 version 1803 trở lên thường đã có sẵn. Nếu thiếu, tải **Developer Pack** tại:

```
https://dotnet.microsoft.com/download/dotnet-framework/net472
```

### Bước 3: Cài DevExpress v24.2

**Cách A — Cài bản Trial 30 ngày (khuyến nghị):**

1. Đăng ký tài khoản miễn phí tại https://www.devexpress.com/products/try/
2. Tải **DevExpress Unified Installer** (~1.5 GB)
3. Chạy installer → chọn version **v24.2** → chọn **WinForms** → Install

**Cách B — Copy DLL thủ công (dùng khi chỉ cần chạy file .exe):**

Copy 7 file DLL sau vào cùng thư mục với `Demo_Robot_Ship.exe`:

```
DevExpress.XtraEditors.v24.2.dll
DevExpress.Utils.v24.2.dll
DevExpress.Data.v24.2.dll
DevExpress.Data.Desktop.v24.2.dll
DevExpress.Drawing.v24.2.dll
DevExpress.Printing.v24.2.Core.dll
DevExpress.Sparkline.v24.2.Core.dll
```

Các file này nằm tại máy đã cài DevExpress, thường ở:
```
C:\Program Files (x86)\DevExpress 24.2\Components\Bin\Framework\
```

**Cách C — Đang có DevExpress phiên bản khác (ví dụ v23.2):**

Mở file `Demo_Robot_Ship.csproj` bằng Notepad, thay toàn bộ chuỗi `v24.2` thành phiên bản hiện có, sau đó mở lại project trong Visual Studio.

---

## Biên dịch

### Bước 1: Mở Solution

1. Mở **Visual Studio 2022**
2. Chọn **File → Open → Project/Solution**
3. Duyệt đến thư mục project, chọn file `Demo_Robot_Ship.sln`
4. Chờ Visual Studio load xong (Solution Explorer hiển thị ~28 file `.cs`)

### Bước 2: Kiểm tra References

1. Trong **Solution Explorer**, mở rộng mục **References**
2. Các dòng `DevExpress.*` không có biểu tượng cảnh báo `(!)` là đã sẵn sàng
3. Nếu có `(!)`: chuột phải **References** → **Add Reference** → **Browse**
   - Duyệt đến: `C:\Program Files (x86)\DevExpress 24.2\Components\Bin\Framework\`
   - Chọn các file `DevExpress.*.v24.2.dll` tương ứng

### Bước 3: Build Solution

```
Ctrl + Shift + B   hoặc   menu Build → Build Solution
```

Kết quả thành công:
```
========== Build: 1 succeeded, 0 failed, 0 up-to-date, 0 skipped ==========
```

File thực thi được tạo tại:
```
Demo_Robot_Ship\bin\Debug\Demo_Robot_Ship.exe      (Debug mode)
Demo_Robot_Ship\bin\Release\Demo_Robot_Ship.exe    (Release mode)
```

---

## Chạy chương trình

### Chạy từ Visual Studio

```
F5          — chạy ở chế độ Debug (có thể đặt breakpoint)
Ctrl + F5   — chạy không debug (nhanh hơn, dùng khi demo)
```

### Chạy file .exe trực tiếp

Vào thư mục `bin\Release\` và double-click `Demo_Robot_Ship.exe`.

Yêu cầu máy chạy phải có:
- .NET Framework 4.7.2 Runtime
- 7 file DLL DevExpress (đã cài sẵn hoặc copy vào cùng thư mục với file `.exe`)

---

## Hướng dẫn sử dụng

Giao diện chia 3 cột: **DASHBOARD** (trái) — **AI SIMULATION MAP** (giữa) — **LIVE DATA** (phải)

### Các bước cơ bản

**Bước 1:** Nhập số phòng (ví dụ: `A101`, `B203`, `I305`) và khối lượng (kg) → nhấn **Thêm đơn hàng**

> Hoặc click trực tiếp vào cổng giao hàng trên bản đồ để tạo đơn nhanh (mặc định 1kg)

**Bước 2:** Nhấn **Bắt đầu giao** để chạy mô phỏng

**Bước 3:** Quan sát robot di chuyển trên bản đồ và theo dõi **Log hệ thống** (góc trái dưới)

**Bước 4:** Chuyển sang tab **Bảng kiểm thử Score** để xem điểm đánh giá từng robot theo từng tick

**Bước 5:** Kéo thanh **Tốc độ (1–10)** để điều chỉnh tốc độ mô phỏng

**Bước 6:** Nhấn **Tạm dừng** để dừng hoặc **Làm mới bản đồ** để reset toàn bộ

### Định dạng số phòng hợp lệ

Ký tự đầu là tên tòa nhà (A–L), theo sau là số phòng bất kỳ:

```
A101, B203, C305, D101, E202, F303, G101, H202, I305, J101, K202, L303, ...
```

---

## Cấu trúc thư mục

```
Demo_Robot_Ship/
├── Demo_Robot_Ship.sln              # Mở file này trong Visual Studio
├── README.md                        # Hướng dẫn này
│
└── Demo_Robot_Ship/
    ├── Demo_Robot_Ship.csproj       # Cấu hình project và danh sách file
    ├── App.config                   # Cấu hình runtime .NET
    │
    ├── Program.cs                   # Điểm khởi chạy ứng dụng
    │
    ├── [Giao diện — Form1 partial classes]
    │   ├── Form1.cs                 # Constants, fields, khởi tạo chung
    │   ├── Form1.Designer.cs        # Layout UI (tự sinh bởi VS Designer)
    │   ├── Form1.UiSetup.cs         # Thiết lập theme, màu sắc, controls
    │   ├── Form1.EventHandlers.cs   # Xử lý sự kiện: button, trackbar, mouse
    │   ├── Form1.MapDrawing.cs      # Vẽ bản đồ, robot, đường đi
    │   ├── Form1.LiveData.cs        # Cập nhật robot cards, bảng Score, log
    │   ├── Form1.PathFinding.cs     # Wrapper gọi PathPlanner
    │   └── Form1.Simulation.cs      # Vòng lặp tick, dispatch, di chuyển
    │
    ├── [Thuật toán]
    │   ├── PathPlanner.cs           # A*, Time-Space A*, Reservation Table
    │   ├── AssignmentScorer.cs      # Công thức tính điểm 7 thành phần
    │   └── BatteryModel.cs          # Mô hình pin SoC/SoH/ChargeCycles
    │
    ├── [Data Models]
    │   ├── Robot.cs                 # Class Robot + Node (node A*)
    │   ├── DeliveryOrder.cs         # Class đơn hàng
    │   ├── TimedNode.cs             # Node cho Time-Space A*
    │   ├── RoutePlan.cs             # Kế hoạch tuyến đường robot
    │   ├── RobotAvailability.cs     # Trạng thái sẵn sàng của robot
    │   ├── AssignmentCandidate.cs   # Ứng viên trong quá trình dispatch
    │   ├── BatchAssignment.cs       # Kết quả gán đơn cho robot
    │   └── ScoreBreakdown.cs        # Chi tiết điểm từng thành phần
    │
    ├── [View Models]
    │   ├── OrderView.cs             # Hiển thị đơn hàng trong bảng
    │   ├── RobotCard.cs             # Thẻ thông tin robot
    │   ├── ScoreCandidateRow.cs     # Hàng dữ liệu bảng kiểm thử Score
    │   └── BuildingView.cs          # Dữ liệu vẽ tòa nhà
    │
    ├── [Utilities]
    │   └── ButtonTheme.cs           # Màu sắc hover/normal cho nút bấm
    │
    └── Properties/
        ├── AssemblyInfo.cs
        ├── Resources.Designer.cs
        └── Settings.Designer.cs
```
