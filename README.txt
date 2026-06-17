================================================================================
  DEMO_ROBOT_SHIP — AI Campus Delivery Simulation
  Hệ thống mô phỏng robot giao hàng tự hành trên khuôn viên trường đại học
================================================================================

MỤC LỤC
--------
  1. Giới thiệu chương trình
  2. Yêu cầu hệ thống
  3. Các gói phần mềm sử dụng
  4. Hướng dẫn cài đặt môi trường
  5. Hướng dẫn biên dịch (Build)
  6. Hướng dẫn chạy chương trình
  7. Cấu trúc thư mục
  8. Xử lý lỗi thường gặp


================================================================================
1. GIỚI THIỆU CHƯƠNG TRÌNH
================================================================================

Chương trình mô phỏng đội 3 robot tự hành giao hàng trên khuôn viên 12 tòa
nhà, không cần người điều khiển. Các thuật toán được áp dụng:

  - Bản đồ lưới 2D (14 x 21 ô): môi trường di chuyển của robot
  - Thuật toán Tham lam (Greedy):  phân bổ đơn hàng tự động, gom đơn cùng tòa
  - Time-Space A*:  tìm đường ngắn nhất trong không gian-thời gian
  - Reservation Table (Bảng Đặt Chỗ):  tránh va chạm giữa các robot
  - Mô hình pin SoC/SoH:  quản lý pin và mô phỏng chai pin theo thời gian
  - Bảng kiểm thử Score:  trực quan hóa điểm đánh giá từng robot mỗi tick

Ngôn ngữ    : C# (.NET Framework 4.7.2)
Loại ứng dụng: Windows Forms Application (WinForms)
IDE          : Visual Studio 2022


================================================================================
2. YÊU CẦU HỆ THỐNG
================================================================================

  - Hệ điều hành : Windows 10 / Windows 11 (64-bit)
                   [Bắt buộc — WinForms chỉ chạy trên Windows]
  - RAM           : Tối thiểu 4 GB (khuyến nghị 8 GB)
  - Ổ cứng        : ~5 GB trống (cho Visual Studio + DevExpress)
  - Visual Studio : 2022 Community Edition (miễn phí)
  - .NET Framework: 4.7.2 (thường đã có sẵn trên Windows 10/11)
  - DevExpress    : v24.2.5 (thư viện UI — xem mục 3 và 4)


================================================================================
3. CÁC GÓI PHẦN MỀM SỬ DỤNG
================================================================================

A. THƯ VIỆN .NET FRAMEWORK (tích hợp sẵn, không cần cài thêm)
---------------------------------------------------------------
  System.Windows.Forms          — Giao diện: Form, Button, DataGridView, Timer
  System.Drawing                — Vẽ bản đồ, robot, đường đi trên PictureBox
  System.Drawing.Drawing2D      — Gradient, đường đứt nét animated
  System.Collections.Generic    — List, Queue, Dictionary, HashSet (dùng trong A*)
  System.Linq                   — Truy vấn danh sách đơn hàng, robot, candidate
  System.Data                   — DataGridView binding cho bảng Score
  System.ComponentModel         — Data binding, PropertyDescriptor
  Microsoft.CSharp              — Dynamic code support

B. THƯ VIỆN BÊN NGOÀI — DevExpress v24.2.5 (cần cài đặt)
----------------------------------------------------------
  Các DLL DevExpress được tham chiếu trong project:

  DevExpress.XtraEditors.v24.2.dll      — LabelControl (tiêu đề 3 cột giao diện)
  DevExpress.Utils.v24.2.dll            — Utilities hỗ trợ XtraEditors
  DevExpress.Data.v24.2.dll             — Data layer của DevExpress
  DevExpress.Data.Desktop.v24.2.dll     — Desktop extensions
  DevExpress.Drawing.v24.2.dll          — Drawing primitives
  DevExpress.Printing.v24.2.Core.dll    — Printing core (dependency bắt buộc)
  DevExpress.Sparkline.v24.2.Core.dll   — Sparkline core (dependency bắt buộc)

  LƯU Ý: DevExpress là thư viện thương mại. Có thể dùng bản Trial 30 ngày
  miễn phí tại: https://www.devexpress.com/products/try/
  Bản trial đầy đủ chức năng, đủ dùng cho mục đích demo và báo cáo.


================================================================================
4. HƯỚNG DẪN CÀI ĐẶT MÔI TRƯỜNG
================================================================================

---- BƯỚC 1: Cài Visual Studio 2022 Community ----

  1. Truy cập: https://visualstudio.microsoft.com/vs/community/
  2. Tải file VisualStudioSetup.exe và chạy với quyền Administrator
  3. Trong màn hình "Workloads", tích chọn:
       [x] .NET desktop development       <-- BẮT BUỘC
  4. Trong tab "Individual components", đảm bảo có:
       [x] .NET Framework 4.7.2 targeting pack
  5. Nhấn Install — quá trình cài mất khoảng 15–30 phút (~5–10 GB)


---- BƯỚC 2: Cài .NET Framework 4.7.2 (nếu chưa có) ----

  Windows 10 version 1803 trở lên thường đã có sẵn.
  Nếu thiếu, tải tại:
    https://dotnet.microsoft.com/download/dotnet-framework/net472

  Chọn ".NET Framework 4.7.2 Developer Pack" → cài → restart máy.


---- BƯỚC 3: Cài DevExpress v24.2 ----

  CÁCH A — Cài bản Trial 30 ngày (đầy đủ chức năng):
    1. Truy cập: https://www.devexpress.com/products/try/
    2. Đăng ký tài khoản miễn phí → xác nhận email
    3. Tải DevExpress Unified Installer (file .exe, khoảng 1.5 GB)
    4. Chạy installer → chọn version v24.2 → chọn WinForms → Install
    5. Sau cài đặt, các DLL tự động đăng ký vào hệ thống

  CÁCH B — Dùng DLL trực tiếp (không cần cài toàn bộ):
    Nếu chỉ cần chạy file .exe (không build từ source):
    Copy 7 file DLL sau vào cùng thư mục với Demo_Robot_Ship.exe:
      - DevExpress.XtraEditors.v24.2.dll
      - DevExpress.Utils.v24.2.dll
      - DevExpress.Data.v24.2.dll
      - DevExpress.Data.Desktop.v24.2.dll
      - DevExpress.Drawing.v24.2.dll
      - DevExpress.Printing.v24.2.Core.dll
      - DevExpress.Sparkline.v24.2.Core.dll
    Các file này lấy từ máy đã cài DevExpress, thường tại:
      C:\Program Files (x86)\DevExpress 24.2\Components\Bin\Framework\

  CÁCH C — Nếu đã có DevExpress phiên bản khác (ví dụ v23.2):
    Mở file Demo_Robot_Ship.csproj bằng Notepad, thay toàn bộ
    "v24.2" thành phiên bản hiện có, ví dụ "v23.2".
    Sau đó mở lại project trong Visual Studio.


================================================================================
5. HƯỚNG DẪN BIÊN DỊCH (BUILD)
================================================================================

  1. Giải nén file zip vào thư mục tùy chọn (ví dụ: D:\Projects\)

  2. Mở Visual Studio 2022

  3. Chọn File → Open → Project/Solution
     Duyệt đến thư mục vừa giải nén, chọn file:
       Demo_Robot_Ship.sln

  4. Chờ Visual Studio load xong
     (Solution Explorer sẽ hiển thị ~28 file .cs)

  5. Kiểm tra References:
     - Trong Solution Explorer, mở rộng mục "References"
     - Các dòng DevExpress.* không có biểu tượng cảnh báo (!) là OK
     - Nếu có (!): chuột phải References → Add Reference → Browse
       Duyệt đến: C:\Program Files (x86)\DevExpress 24.2\Components\Bin\Framework\
       Chọn các file DevExpress.*.v24.2.dll cần thiết

  6. Chọn cấu hình build:
     - Debug   : phát triển, kiểm tra lỗi (chậm hơn)
     - Release : trình bày demo (nhanh hơn, tối ưu hơn)

  7. Build Solution:
     Nhấn Ctrl + Shift + B  hoặc  menu Build → Build Solution

  8. Kết quả thành công:
     ========== Build: 1 succeeded, 0 failed ==========

  9. File thực thi được tạo tại:
     Debug  : Demo_Robot_Ship\bin\Debug\Demo_Robot_Ship.exe
     Release: Demo_Robot_Ship\bin\Release\Demo_Robot_Ship.exe


================================================================================
6. HƯỚNG DẪN CHẠY CHƯƠNG TRÌNH
================================================================================

---- CÁCH 1: Chạy từ Visual Studio ----
  - Nhấn F5          : chạy ở chế độ Debug (có thể đặt breakpoint)
  - Nhấn Ctrl + F5   : chạy không debug (nhanh hơn, dùng khi demo)

---- CÁCH 2: Chạy file .exe trực tiếp ----
  Vào thư mục bin\Release\ và double-click Demo_Robot_Ship.exe
  Yêu cầu máy chạy phải có:
    - .NET Framework 4.7.2 Runtime
    - 7 file DLL DevExpress (cài sẵn hoặc copy vào cùng thư mục .exe)

---- HƯỚNG DẪN SỬ DỤNG NHANH ----

  Giao diện chia 3 cột:
    DASHBOARD (trái)    : Tạo đơn hàng + điều khiển mô phỏng + log
    AI SIMULATION MAP   : Bản đồ khuôn viên, robot di chuyển trực quan
    LIVE DATA (phải)    : Trạng thái robot + danh sách đơn + bảng Score

  Các bước cơ bản:
    Bước 1 : Nhập số phòng (ví dụ: A101, B203, I305) và khối lượng (kg)
             → Nhấn "Thêm đơn hàng"
             Hoặc: Click trực tiếp vào cổng giao hàng trên bản đồ
    Bước 2 : Nhấn "Bắt đầu giao" để chạy mô phỏng
    Bước 3 : Quan sát robot di chuyển trên bản đồ
    Bước 4 : Xem Log hệ thống (góc trái dưới) để theo dõi quá trình
    Bước 5 : Chuyển tab "Bảng kiểm thử Score" để xem điểm đánh giá
    Bước 6 : Kéo thanh Tốc độ (1-10) để điều chỉnh tốc độ mô phỏng
    Bước 7 : Nhấn "Tạm dừng" để dừng, "Làm mới bản đồ" để reset

  Định dạng số phòng hợp lệ:
    A101, A205, B101, B303, C202, D101, E205, F303,
    G101, H202, I305, J101, K202, L303, ...
    (Ký tự đầu = tên tòa nhà A-L, các số sau = số phòng)


================================================================================
7. CẤU TRÚC THƯ MỤC
================================================================================

  Demo_Robot_Ship_Refactored/
  │
  ├── Demo_Robot_Ship.sln              <- Mở file này trong Visual Studio
  │
  └── Demo_Robot_Ship/
      ├── Demo_Robot_Ship.csproj       <- Cấu hình project và danh sách file
      ├── App.config                   <- Cấu hình runtime .NET
      │
      ├── [ĐIỂM KHỞI CHẠY]
      │   └── Program.cs               <- Main entry point
      │
      ├── [GIAO DIỆN - Form1 partial classes]
      │   ├── Form1.cs                 <- Constants, fields, khởi tạo chung
      │   ├── Form1.Designer.cs        <- Layout UI (tự sinh bởi VS Designer)
      │   ├── Form1.UiSetup.cs         <- Thiết lập theme, màu sắc, controls
      │   ├── Form1.EventHandlers.cs   <- Xử lý sự kiện: button, trackbar, mouse
      │   ├── Form1.MapDrawing.cs      <- Vẽ bản đồ, robot, đường đi
      │   ├── Form1.LiveData.cs        <- Cập nhật robot cards, bảng Score, log
      │   ├── Form1.PathFinding.cs     <- Wrapper gọi PathPlanner
      │   └── Form1.Simulation.cs      <- Vòng lặp tick, dispatch, di chuyển
      │
      ├── [THUẬT TOÁN]
      │   ├── PathPlanner.cs           <- A*, Time-Space A*, Reservation Table
      │   ├── AssignmentScorer.cs      <- Công thức tính điểm 7 thành phần
      │   └── BatteryModel.cs          <- Mô hình pin SoC/SoH/ChargeCycles
      │
      ├── [DATA MODELS]
      │   ├── Robot.cs                 <- Class Robot + Node (node A*)
      │   ├── DeliveryOrder.cs         <- Class đơn hàng
      │   ├── TimedNode.cs             <- Node cho Time-Space A*
      │   ├── RoutePlan.cs             <- Kế hoạch tuyến đường robot
      │   ├── RobotAvailability.cs     <- Trạng thái sẵn sàng của robot
      │   ├── AssignmentCandidate.cs   <- Ứng viên trong quá trình dispatch
      │   ├── BatchAssignment.cs       <- Kết quả gán đơn cho robot
      │   └── ScoreBreakdown.cs        <- Chi tiết điểm từng thành phần
      │
      ├── [VIEW MODELS - dùng cho DataGridView]
      │   ├── OrderView.cs             <- Hiển thị đơn hàng trong bảng
      │   ├── RobotCard.cs             <- Thẻ thông tin robot
      │   ├── ScoreCandidateRow.cs     <- Hàng dữ liệu bảng kiểm thử Score
      │   └── BuildingView.cs          <- Dữ liệu vẽ tòa nhà
      │
      ├── [UTILITIES]
      │   └── ButtonTheme.cs           <- Màu sắc hover/normal cho nút bấm
      │
      └── Properties/
          ├── AssemblyInfo.cs
          ├── Resources.Designer.cs
          └── Settings.Designer.cs


================================================================================
8. XỬ LÝ LỖI THƯỜNG GẶP
================================================================================

  LỖI: Could not load file or assembly 'DevExpress.XtraEditors.v24.2'
  NGUYÊN NHÂN: DevExpress chưa cài hoặc sai phiên bản
  CÁCH SỬA  : Cài DevExpress v24.2 (xem mục 4 - Bước 3)
               Hoặc sửa số phiên bản trong .csproj cho khớp với bản đang có

  LỖI: The type or namespace 'DevExpress' could not be found
  NGUYÊN NHÂN: References bị mất sau khi copy project sang máy khác
  CÁCH SỬA  : Solution Explorer → References → chuột phải → Add Reference
               Browse đến thư mục cài DevExpress, chọn các DLL tương ứng

  LỖI: TargetFrameworkVersion v4.7.2 is not installed
  NGUYÊN NHÂN: Thiếu .NET Framework 4.7.2 Developer Pack
  CÁCH SỬA  : Tải và cài từ https://dotnet.microsoft.com/download/dotnet-framework/net472

  LỖI: Build succeeded nhưng robot không di chuyển khi nhấn Start
  NGUYÊN NHÂN: Chưa có đơn hàng trong queue
  CÁCH SỬA  : Thêm ít nhất 1 đơn hàng trước khi nhấn "Bắt đầu giao"

  LỖI: Build warning "Assembly binding redirection"
  NGUYÊN NHÂN: Bình thường với .NET Framework 4.7.2
  CÁCH SỬA  : Bỏ qua, không ảnh hưởng đến việc chạy chương trình


================================================================================
  Phiên bản: 1.0  |  Ngôn ngữ: C# .NET Framework 4.7.2  |  IDE: VS 2022
================================================================================
