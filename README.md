# 🎮 Game Account Marketplace

Một nền tảng thương mại điện tử chuyên biệt dành cho mua bán account game trực tuyến. Trang web cung cấp các tính năng quản lý account, thanh toán và hỗ trợ người dùng.

## 📋 Mục lục
- [Giới thiệu](#giới-thiệu)
- [Các chức năng](#các-chức-năng)
- [Yêu cầu hệ thống](#yêu-cầu-hệ-thống)
- [Cài đặt & Chạy](#cài-đặt--chạy)
- [Cấu trúc dự án](#cấu-trúc-dự-án)
- [Công nghệ sử dụng](#công-nghệ-sử-dụng)

## 🎯 Giới thiệu

Game Account Marketplace là một ứng dụng web ASP.NET Core cho phép:
- **Người mua** tìm kiếm và mua account game từ các seller
- **Seller** đăng bán account game của họ
- **Admin** quản lý hệ thống, xử lý báo cáo, quản lý người dùng

Trang web sử dụng công nghệ:
- **.NET 8** - Framework backend
- **ASP.NET Core MVC** - Kiến trúc ứng dụng
- **Entity Framework Core** - ORM cho database
- **SQL Server** - Cơ sở dữ liệu
- **Bootstrap 5** - Giao diện người dùng

## ✨ Các chức năng

### 👥 Chức năng người dùng
- ✅ Đăng ký / Đăng nhập
- ✅ Xem hồ sơ cá nhân
- ✅ Quản lý lịch sử mua hàng
- ✅ Thêm account vào giỏ hàng
- ✅ Tìm kiếm account theo game
- ✅ Đăng ký trở thành seller

### 📦 Chức năng Seller
- ✅ Đăng bán account game với mô tả & hình ảnh
- ✅ Quản lý danh sách account đang bán
- ✅ Sửa thông tin account
- ✅ Xóa account khỏi danh sách
- ✅ Upload nhiều hình ảnh cho mỗi account
- ✅ Quản lý đơn hàng nhận được

### 🛒 Chức năng mua hàng
- ✅ Thêm account vào giỏ hàng
- ✅ Xem chi tiết giỏ hàng
- ✅ Thanh toán và xác nhận đơn hàng
- ✅ Xem lịch sử mua hàng
- ✅ Báo cáo đơn hàng nếu seller từ chối sau khi nhận tiền

### 👨‍💼 Chức năng Admin
- ✅ Quản lý người dùng (User, Seller, Admin)
- ✅ Duyệt account đăng bán trước khi hiển thị
- ✅ Quản lý danh sách game
- ✅ Xem & xử lý báo cáo từ người mua
- ✅ Xem thống kê, quản lý seller
- ✅ Thêm admin mới

### 🚩 Chức năng báo cáo
- ✅ Người mua có thể báo cáo seller nếu:
  - Seller đã nhận tiền nhưng từ chối
  - Account không như mô tả
  - Account bị khóa
- ✅ Admin xem & xử lý báo cáo
- ✅ Liên hệ seller để yêu cầu hoàn tiền

## 💻 Yêu cầu hệ thống

### Phần mềm cần thiết
- **Visual Studio 2022+** hoặc **Visual Studio Code**
- **.NET 8 SDK**
- **SQL Server 2019+** hoặc **SQL Server Express**
- **Git**

### Cấu hình tối thiểu
- CPU: Intel i5 hoặc tương đương
- RAM: 8GB
- Ổ cứng: 5GB trống

## 🚀 Cài đặt & Chạy

### Bước 1: Clone Repository
git clone https://github.com/HoangLongg17/CuoiKyCNNET_Nhom2_MarketPlaceAccountGame.git

### Bước 2: Cấu hình Database

Mở file `appsettings.json` và cập nhật connection string để kết nối với SQL Server của bạn

### Bước 4: Tạo Database (Migration)
Tools-> NuGet Package Manager -> Package Manage Console

### Bước 5: Chạy ứng dụng

**Với Visual Studio:**
- Mở `CKCNNET.sln`
- Nhấn `F5` hoặc `Ctrl+F5`

**Với CLI:**
dotnet run

Ứng dụng sẽ chạy tại: `https://localhost:7224` hoặc `http://localhost:5083`

### Bước 6: Đăng nhập

**Tài khoản Admin mặc định:**
- Username: `Admin`
- Password: `Admin@123`
- Email: `admin@gmail.com`

## 📁 Cấu trúc dự án

## 🛠️ Công nghệ sử dụng

| Công nghệ             | Phiên bản       | Mục đích        |
|-----------------------|-----------------|-----------------|
| .NET		            | 8.0             | Framework chính |
| ASP.NET Core MVC      | 8.0             | Web framework   |
| Entity Framework Core | 8.0             | ORM             |
| SQL Server            | 2019+           | Database        |
| Bootstrap             | 5.3             | CSS Framework   |
| jQuery                | 3.6+            | JavaScript      |

## 📝 Các tính năng nổi bật

### 🖼️ Upload hình ảnh
- Hỗ trợ upload **nhiều ảnh** cho mỗi account
- Giới hạn: Tối đa 10 ảnh, mỗi ảnh dưới 5MB
- Lưu trữ ảnh trong thư mục `wwwroot/uploads/game-accounts/`

### 💬 Hệ thống báo cáo
- Người mua có thể báo cáo seller lừa đảo
- Admin xem xét & xử lý báo cáo
- Thông tin liên hệ seller hiển thị cho admin

### 🔐 Bảo mật
- Hash password với SHA256
- Session-based authentication
- Role-based authorization (User, Seller, Admin)
- CSRF protection

### 📱 Responsive Design
- Giao diện thân thiện trên desktop, tablet, mobile
- Bootstrap 5 responsive grid

## 🐛 Troubleshooting

**Lỗi: "Database không kết nối được"**
- Kiểm tra connection string trong `appsettings.json`

## 📞 Hỗ trợ

- **Email:** nglong1708@gmail.com
- **Issues:** Tạo issue trên GitHub

## 👥 Tác giả

- **Nhóm 2** - Dự án cuối kỳ Công nghệ NET
- Repository: [GitHub](https://github.com/HoangLongg17/CuoiKyCNNET_Nhom2_MarketPlaceAccountGame)

## 🔄 Cập nhật gần đây

- ✅ Thêm chức năng upload hình ảnh cho account
- ✅ Hệ thống báo cáo từ người mua
- ✅ Quản lý báo cáo cho admin
- ✅ Hiển thị hình ảnh trong chi tiết account
- ✅ Cải thiện giao diện admin

---

**Made by Nhóm 2**