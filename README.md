# AILA Backend

REST API cho nền tảng học AI **AILA**, xây dựng bằng ASP.NET Core 8 theo kiến trúc Clean Architecture kết hợp CQRS.

---

## 1. Tổng quan

Hệ thống phục vụ ba vai trò người dùng: **Learner** (người học), **Expert** (chuyên gia tạo khóa học) và **Admin** (quản trị viên).

Các nhóm chức năng chính:

- **Xác thực và tài khoản** — đăng ký, đăng nhập cho từng vai trò, đăng nhập bằng Google, làm mới phiên, đăng xuất, quên mật khẩu qua mã OTP gửi email.
- **Khóa học và nội dung** — danh mục, khóa học, học phần, học liệu (video, tài liệu, bài trắc nghiệm, bài luyện tập AI), ghi danh và theo dõi tiến độ học.
- **Trắc nghiệm** — ngân hàng câu hỏi, nhập câu hỏi từ file Excel, làm bài và chấm điểm.
- **Luyện tập với AI** — người học tương tác với AI theo tình huống do chuyên gia thiết kế, hệ thống chấm điểm và đưa nhận xét; chuyên gia có thể chạy thử trước khi xuất bản.
- **Trợ lý hỏi đáp** — nội dung học liệu được đưa vào kho tri thức để người học đặt câu hỏi và nhận câu trả lời kèm trích dẫn nguồn.
- **Chuyên gia đánh giá** — người học gửi bài luyện tập nhờ chuyên gia chấm và nhận nhận xét.
- **Gói đăng ký và thanh toán** — quản lý gói dịch vụ, xử lý giao dịch qua cổng thanh toán SePay.
- **Quản trị** — quản lý người dùng, thẻ, danh mục, bài viết, báo cáo vi phạm, hạn mức tài nguyên AI và nhật ký hoạt động.

---

## 2. Công nghệ sử dụng

| Hạng mục | Công nghệ |
|---|---|
| Ngôn ngữ / nền tảng | C# trên .NET 8 |
| Framework | ASP.NET Core Web API |
| Cơ sở dữ liệu | PostgreSQL 16 (bản có sẵn tiện ích `pgvector`) |
| Truy xuất dữ liệu | Entity Framework Core, Npgsql, Dapper |
| Bộ nhớ đệm | Redis |
| Điều phối nghiệp vụ | MediatR |
| Kiểm tra dữ liệu đầu vào | FluentValidation |
| Xác thực | JWT Bearer, BCrypt, Google Auth |
| Tài liệu API | Swagger (Swashbuckle) |
| Tích hợp AI | Microsoft Semantic Kernel với connector OpenAI |
| Dịch vụ ngoài | MailKit (gửi email), Cloudinary (lưu tệp), ClosedXML (Excel) |
| Kiểm thử | xUnit, Moq |

---

## 3. Cấu trúc dự án

```text
aila-backend/
├── AILA.slnx                # Solution
├── Dockerfile               # Đóng gói AILA.Api
├── docker-compose.yml       # Dựng PostgreSQL, Redis và API
├── postgres/init.sql        # Khởi tạo tiện ích vector cho database
├── AILA.Api/                # Tầng trình bày: controller, middleware, cấu hình khởi động
├── AILA.Application/        # Tầng nghiệp vụ: các use case, interface, quy tắc kiểm tra dữ liệu
├── AILA.Domain/             # Tầng lõi: thực thể, enum, quy tắc nghiệp vụ
├── AILA.Infrastructure/     # Tầng hạ tầng: database, repository, dịch vụ ngoài
├── Shared/                  # Kiểu dữ liệu và lớp cấu hình dùng chung
└── AILA.Application.Tests/  # Unit test
```

Bốn tầng phụ thuộc một chiều từ ngoài vào trong: tầng trình bày gọi tầng nghiệp vụ, tầng nghiệp vụ chỉ biết tầng lõi, còn tầng hạ tầng cung cấp phần hiện thực cho các interface mà tầng nghiệp vụ khai báo.

---

## 4. Yêu cầu môi trường

- .NET SDK 8.0
- PostgreSQL 16 và Redis 7
- Docker cùng Docker Compose, nếu chạy bằng container
- Công cụ `dotnet-ef`, nếu cần thao tác migration thủ công

---

## 5. Cấu hình

Các giá trị nhạy cảm trong `appsettings.json` đang để dạng placeholder nên **bắt buộc phải khai báo lại** trước khi chạy. Khi chạy trên máy cá nhân, tạo file `appsettings.Development.json` trong thư mục `AILA.Api` (file này đã được loại khỏi Git) hoặc dùng User Secrets. Khi chạy bằng Docker, tạo file `.env` đặt cạnh `docker-compose.yml`; danh sách đầy đủ tên biến có thể xem trực tiếp trong `docker-compose.yml`.

Những cấu hình bắt buộc:

| Cấu hình | Ghi chú |
|---|---|
| Chuỗi kết nối PostgreSQL | Thiếu thì không kết nối được cơ sở dữ liệu |
| Chuỗi kết nối Redis | Thiếu thì ứng dụng dừng ngay khi khởi động |
| Khóa ký JWT | Thiếu thì ứng dụng dừng ngay khi khởi động |
| Tài khoản Admin | Dùng để tạo tài khoản quản trị đầu tiên |
| Khóa mã hóa OTP | Cần cho chức năng quên mật khẩu |
| Thông tin SMTP | Thiếu thì hệ thống chỉ ghi log, không gửi email thật |
| Google, Cloudinary, OpenAI, SePay | Cần cho từng tính năng tương ứng |

> Không đưa khóa bí mật thật vào mã nguồn hay commit lên repository.

---

## 6. Cài đặt và chạy

**Chạy bằng Docker Compose (khuyến nghị):**

```bash
git clone https://github.com/Duc854/aila-backend.git
cd aila-backend
# tạo file .env theo mục 5
docker compose up -d --build
```

API chạy ở cổng 8080, chỉ mở trên localhost nên cần reverse proxy nếu muốn truy cập từ bên ngoài.

**Chạy trực tiếp trên máy:**

```bash
docker compose up -d postgres redis
# tạo AILA.Api/appsettings.Development.json
dotnet restore AILA.slnx
dotnet run --project AILA.Api
```

Ứng dụng lắng nghe ở `http://localhost:5084` và `https://localhost:7124`.

---

## 7. Cơ sở dữ liệu

Ứng dụng tự động áp dụng migration và tạo dữ liệu khởi tạo mỗi lần khởi động, gồm tài khoản quản trị, chính sách hạn mức tài nguyên và bộ thẻ hệ thống. Tiện ích `vector` của PostgreSQL được tạo sẵn qua `postgres/init.sql`, và script này chỉ chạy khi database còn trống.

Nếu cần thao tác thủ công:

```bash
dotnet ef migrations add <TenMigration> --project AILA.Infrastructure --startup-project AILA.Api
dotnet ef database update --project AILA.Infrastructure --startup-project AILA.Api
```

---

## 8. Tài liệu API

Swagger chỉ bật ở môi trường Development, truy cập tại `https://localhost:7124/swagger`. Với các API yêu cầu đăng nhập, bấm nút **Authorize** rồi dán access token vào ô nhập.

Các nhóm endpoint chính: xác thực (`/api/auth`), khóa học và học liệu, trắc nghiệm, luyện tập AI, trợ lý hỏi đáp, khu vực dành riêng cho người học, chuyên gia, quản trị viên, và endpoint nhận thông báo thanh toán từ SePay.

---

## 9. Xác thực và phân quyền

Hệ thống dùng JWT làm access token, kèm refresh token lưu trong cookie an toàn để gia hạn phiên. Khi người dùng đăng xuất, access token bị thu hồi và không dùng lại được. Mật khẩu được băm trước khi lưu. Quyền truy cập được phân theo ba vai trò Learner, Expert và Admin; mỗi endpoint khai báo rõ vai trò được phép gọi.

---

## 10. Kiểm thử

Dự án hiện chỉ có unit test, đặt trong `AILA.Application.Tests`, viết bằng xUnit và Moq, bao phủ các quy tắc nghiệp vụ, use case và dịch vụ. Chưa có integration test hay end-to-end test.

```bash
dotnet test AILA.slnx
```
---

## 11. Đóng gói và triển khai

```bash
dotnet publish AILA.Api/AILA.Api.csproj -c Release -o ./publish
docker compose up -d --build
```

Docker Compose dựng đủ ba thành phần: cơ sở dữ liệu, Redis và API. Danh sách tên miền được phép gọi API (CORS) khai báo trong mã nguồn khởi động, hiện gồm địa chỉ frontend chạy local và hai tên miền chính thức. Repository chưa có pipeline CI/CD.
