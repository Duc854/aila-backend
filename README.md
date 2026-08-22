# AILA Backend

REST API cho nền tảng học AI **AILA**, xây dựng bằng ASP.NET Core 8 theo kiến trúc Clean Architecture + CQRS.

---

## 1. Tổng quan

Backend phục vụ 3 vai trò: **Learner**, **Expert**, **Admin**. Các nhóm chức năng chính (theo controller thực tế trong source):

- **Authentication** — đăng ký/đăng nhập Learner, Expert, Admin; Google OAuth; refresh token; logout; quên mật khẩu bằng OTP qua email.
- **Course & Content** — Category, Course, Module, Material (Video / Document / Quiz / AI Practice), Enrollment, tiến độ học.
- **Quiz** — ngân hàng câu hỏi, import câu hỏi từ Excel, làm bài và chấm điểm.
- **AI Practice** — luyện tập prompt với AI, chấm điểm bằng LLM, Expert chạy thử (simulation).
- **RAG Chat** — index học liệu thành knowledge base, hỏi đáp theo khóa học kèm trích dẫn.
- **Expert Evaluation** — Learner nhờ chuyên gia đánh giá bài luyện tập.
- **Subscription & Payment** — gói đăng ký, thanh toán qua webhook SePay.
- **Quota & Admin** — giới hạn tài nguyên AI, quản lý người dùng, tag, danh mục, blog, báo cáo, nhật ký hoạt động.

---

## 2. Công nghệ sử dụng

| Hạng mục | Công nghệ | Version |
|---|---|---|
| Language / Runtime | C# / .NET | `net8.0` |
| Framework | ASP.NET Core Web API | 8.0 |
| Database | PostgreSQL (image `pgvector/pgvector:pg16`) | 16 |
| ORM | EF Core + Npgsql | 8.0.11 / 8.0.4 |
| Cache | Redis (`StackExchange.Redis`) | Redis 7 |
| CQRS | MediatR | 12.4.1 |
| Validation | FluentValidation | 11.9.2 |
| Auth | JWT Bearer + BCrypt.Net-Next + Google.Apis.Auth | 8.0.11 |
| API Docs | Swashbuckle (Swagger) | 6.6.2 |
| AI / LLM | Microsoft.SemanticKernel + Connectors.OpenAI | 1.78.0 |
| Khác | MailKit (SMTP), CloudinaryDotNet, ClosedXML (Excel), Dapper | — |
| Testing | xUnit + Moq + coverlet | xUnit 2.5.3 |

---

## 3. Kiến trúc & cấu trúc thư mục

```
AILA.Api  ──►  AILA.Application  ──►  AILA.Domain
    │                  ▲                   ▲
    └──► AILA.Infrastructure ──────────────┘   (đều tham chiếu Shared)
```

```text
aila-backend/
├── AILA.slnx                    # Solution
├── Dockerfile                   # Build & publish AILA.Api
├── docker-compose.yml           # postgres + redis + api
├── postgres/init.sql            # CREATE EXTENSION vector
├── AILA.Api/                    # Presentation: Controllers, Middlewares, Program.cs
├── AILA.Application/            # Use case: Features/ (CQRS), Common/ (Interfaces, Behaviours)
├── AILA.Domain/                 # Entities, Enums, ValueObjects (không phụ thuộc gì)
├── AILA.Infrastructure/         # DbContext, Migrations, Repositories, Services (AI, Email...)
├── Shared/                      # ResponseDto, PageResult, các class Settings
└── AILA.Application.Tests/      # Unit test (xUnit)
```

- **Domain** — entity nghiệp vụ với business rule, không tham chiếu project nào khác.
- **Application** — Command/Query + Handler (MediatR), tổ chức theo `Features/<Tên nghiệp vụ>/`; khai báo interface cho Infrastructure; `ValidationBehavior` chạy FluentValidation trước mọi handler.
- **Infrastructure** — implement các interface: EF Core, Repository/UnitOfWork, Redis, JWT, SMTP, Cloudinary, Semantic Kernel.
- **Api** — Controller mỏng, chỉ nhận request → `ISender.Send()` → map sang HTTP status.

---

## 4. Yêu cầu môi trường

- **.NET SDK 8.0**
- **PostgreSQL 16** (cần extension `vector`) và **Redis 7**
- **Docker + Docker Compose** (nếu chạy bằng container)
- **dotnet-ef** (nếu cần chạy migration thủ công)

---

## 5. Cấu hình

Giá trị nhạy cảm trong `appsettings.json` đang để placeholder `"[data]"` → **bắt buộc override**:

- Chạy local: tạo `AILA.Api/appsettings.Development.json` (đã gitignore) hoặc dùng User Secrets.
- Chạy Docker: tạo file `.env` cạnh `docker-compose.yml` (xem danh sách biến trong `docker-compose.yml`).

| Cấu hình | Ghi chú |
|---|---|
| `ConnectionStrings:PostgreSQL` | Thiếu → không kết nối được DB |
| `ConnectionStrings:Redis` | Thiếu → app **ném exception khi khởi động** |
| `JwtSettings:Key` | Thiếu → app **ném exception khi khởi động** |
| `AdminAccount:*` | Seed tài khoản Admin đầu tiên |
| `PasswordReset:OtpHashSecret` | Cần cho luồng quên mật khẩu |
| `Smtp:*` | Thiếu → chỉ ghi log, **không gửi mail thật** |
| `GoogleSettings:*`, `Cloudinary:*`, `OpenAI:*`, `SePay:*` | Cần cho từng tính năng tương ứng |

Mẫu `.env` cho Docker (dùng cú pháp `Section__Key`):

```env
POSTGRES_DB=<db-name>
POSTGRES_USER=<db-user>
POSTGRES_PASSWORD=<db-password>
REDIS_PASSWORD=<redis-password>
JWT_KEY=<jwt-signing-key>
JWT_ISSUER=AILAServer
JWT_AUDIENCE=AILAClients
JWT_EXPIRE_MINUTES=60
OTP_HASH_SECRET=<otp-hmac-secret>
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=<smtp-username>
SMTP_PASSWORD=<smtp-app-password>
SMTP_FROM_EMAIL=<from-email>
SMTP_FROM_NAME=AILA
GOOGLE_CLIENT_ID=<google-client-id>
GOOGLE_CLIENT_SECRET=<google-client-secret>
GOOGLE_REDIRECT_URI=<api-url>/api/auth/google/callback
ADMIN_EMAIL=<admin-email>
ADMIN_PASSWORD=<admin-password>
ADMIN_FULLNAME=<admin-fullname>
CLOUDINARY_CLOUD_NAME=<cloud-name>
CLOUDINARY_API_KEY=<api-key>
CLOUDINARY_API_SECRET=<api-secret>
OPENAI_API_KEY=<llm-api-key>
OPENAI_MODEL_ID=<model-id>
OPENAI_BASE_URL=<openai-compatible-base-url>
SEPAY_API_KEY=<sepay-api-key>
SEPAY_WEBHOOK_SECRET=<sepay-webhook-secret>
SEPAY_BANK_ACCOUNT_NUMBER=<bank-account-number>
SEPAY_BANK_ACCOUNT_NAME=<bank-account-name>
SEPAY_BANK_CODE=<bank-code>
```

> ⚠️ Không commit secret thật vào repo.

---

## 6. Cài đặt & chạy

**Cách 1 — Docker Compose (khuyến nghị):**

```bash
git clone https://github.com/Duc854/aila-backend.git
cd aila-backend
# tạo file .env theo mẫu ở mục 5
docker compose up -d --build
```

API chạy tại `http://127.0.0.1:8080` với `ASPNETCORE_ENVIRONMENT=Production`.

**Cách 2 — Chạy local:**

```bash
docker compose up -d postgres redis      # dựng DB + Redis
# tạo AILA.Api/appsettings.Development.json
dotnet restore AILA.slnx
dotnet run --project AILA.Api
```

Profile `http` → http://localhost:5084 · Profile `https` (mặc định) → https://localhost:7124

---

## 7. Database

**Migration và seed chạy tự động khi khởi động app** (`InitializeDatabaseAsync()` trong `Program.cs`): apply migration → seed Admin, ResourceLimitPolicy, SystemTag. Extension `vector` được tạo bởi `postgres/init.sql` (chỉ chạy khi volume Postgres còn trống).

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add <TenMigration> --project AILA.Infrastructure --startup-project AILA.Api
dotnet ef database update --project AILA.Infrastructure --startup-project AILA.Api
```

---

## 8. API Documentation

Swagger **chỉ bật ở môi trường Development** — UI tại `https://localhost:7124/swagger`, JSON tại `/swagger/v1/swagger.json`. Bấm **Authorize** rồi dán access token thuần (không cần gõ `Bearer `).

---

## 9. Authentication & Authorization

- **JWT Bearer**, ký HMAC bằng `JwtSettings:Key`, `ClockSkew = 0`.
- **Refresh token** lưu trong cookie `HttpOnly` tên `refreshToken` (hạn 7 ngày).
- **Logout** đưa `jti` của access token vào **blacklist trên Redis**.
- Mật khẩu hash bằng **BCrypt**; phân quyền theo **role** `Learner` / `Expert` / `Admin` qua `[Authorize(Roles = "...")]` (không có custom policy).

| Endpoint | Method | Mô tả |
|---|---|---|
| `/api/auth/register` | POST | Đăng ký Learner |
| `/api/learner/login` | POST | Đăng nhập Learner |
| `/api/auth/expert/login` \| `/admin/login` | POST | Đăng nhập Expert / Admin |
| `/api/auth/google/url` \| `/google/callback` | GET | Google OAuth |
| `/api/auth/refresh` \| `/logout` | POST | Cấp lại token / đăng xuất |
| `/api/auth/password-reset/request` \| `/verify` \| `/confirm` | POST | Quên mật khẩu bằng OTP |

Các nhóm API còn lại (xem đầy đủ trong Swagger): `/api/courses`, `/api/modules`, `/api/quiz-materials`, `/api/practice`, `/api/rag`, `/api/learner/*`, `/api/experts/*`, `/api/admin/*`, `/api/webhooks/sepay`.

---

## 10. Response & Error format

Mọi response dùng envelope `ResponseDto<T>`:

```jsonc
{ "success": true,  "data": { }, "errorCode": null, "errorMessage": null }
{ "success": false, "data": null, "errorCode": "VALIDATION_ERROR", "errorMessage": "..." }
```

`ExceptionMiddleware` map lỗi toàn cục: `ValidationException` → **400** `VALIDATION_ERROR`; exception khác → **500** `INTERNAL_SERVER_ERROR`. Tầng auth trả **401** `UNAUTHORIZED` / `TOKEN_REVOKED` và **403** `FORBIDDEN`.

Logging dùng `Microsoft.Extensions.Logging` mặc định của ASP.NET Core (không có Serilog/APM).

---

## 11. Testing

Chỉ có **Unit Test** trong `AILA.Application.Tests` (xUnit + Moq), 36 file test cho Domain entity, Command Handler và Service. Không có Integration Test / E2E Test.

```bash
dotnet test AILA.slnx
```

## 12. Build & Deployment

```bash
dotnet publish AILA.Api/AILA.Api.csproj -c Release -o ./publish
docker compose up -d --build && docker compose logs -f api
```

- Docker build từ `sdk:8.0`, chạy trên `aspnet:8.0`, expose port `8080`.
- Port map `127.0.0.1:8080:8080` → cần reverse proxy để expose ra ngoài.
- CORS whitelist trong `Program.cs`: `http://localhost:5173`, `https://aila.io.vn`, `https://www.aila.io.vn`.
- Không có CI/CD pipeline trong repo (`.github/` chỉ có `CODEOWNERS`).

---

## 13. Lỗi thường gặp

| Lỗi | Cách xử lý |
|---|---|
| `ConnectionStrings:Redis chưa được cấu hình.` | Cấu hình connection string Redis (`appsettings.json` đang để `"[data]"`) |
| `JWT secret key (JwtSettings:Key) chưa được cấu hình.` | Đặt `JwtSettings:Key` |
| `Đã xảy ra lỗi khi migrate hoặc seed dữ liệu.` | Kiểm tra Postgres đang chạy, connection string và quyền user |
| Lỗi extension `vector` khi migrate | Chạy `postgres/init.sql`; với Docker cần `docker compose down -v` rồi dựng lại |
| `/swagger` trả 404 | Swagger chỉ bật khi `ASPNETCORE_ENVIRONMENT=Development` |
| 401 `TOKEN_REVOKED` | Token đã bị blacklist sau logout → đăng nhập lại |
| Frontend bị chặn CORS | Thêm origin vào `AddCors` trong `Program.cs` |
| Không nhận được email OTP | Cấu hình đủ `Smtp:*`, nếu không hệ thống chỉ ghi log |

---

## 14. Quy ước phát triển

- **Thêm API mới:** tạo slice trong `AILA.Application/Features/<TenFeature>/Commands|Queries/` gồm `Command.cs` + `CommandHandler.cs` + `CommandValidator.cs` (nếu cần) — handler và validator **được auto-scan**, không cần đăng ký DI. Sau đó tạo Controller chỉ gọi `_sender.Send()`, trả `ResponseDto<T>` và khai báo `[Authorize(Roles = "...")]` rõ ràng.
- **Thêm entity:** class trong `Domain/Entities/` (private setter + method hành vi) → `Configuration` trong `Infrastructure/Persistence/Configurations/` → `DbSet` trong `ApplicationDbContext` → tạo migration.
- **Thêm repository/service:** interface ở `Application/Common/Interfaces/`, implement ở `Infrastructure/`, **đăng ký thủ công** trong `Infrastructure/DependencyInjection.cs`.
- **Viết test:** đặt trong `AILA.Application.Tests/UnitTests/`, tên file `<Đối tượng>_<Hành vi>Tests.cs`.
- Comment và message lỗi dùng tiếng Việt; `errorCode` là hợp đồng ổn định với frontend; không đưa secret vào source.
