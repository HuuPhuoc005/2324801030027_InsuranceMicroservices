# InsuranceMicroservices — Bài thực hành 4

Khung cài đặt kiến trúc microservices cho đại lý bảo hiểm. Solution gồm 29 project: 9 dịch vụ × 3 project (API definition, implementation, test), API Gateway và Blazor WebAssembly.

## Phạm vi hiện tại

- Đã tạo cấu trúc cho Auth, Chat, Pricing, Policy, PolicySearch, Payment, Product, Document và Dashboard.
- Các project `.Api` là thư viện chứa hợp đồng/DTO; project implementation chạy HTTP và đăng ký Eureka, MediatR.
- Mỗi service có endpoint `/health`; Payment có thêm `/api/payments/health` để kiểm tra qua Gateway.
- DTO của Product, Policy, PolicySearch và Dashboard đáp ứng các tham chiếu của Blazor được cung cấp.
- Nghiệp vụ đăng nhập JWT, chat SignalR, tính phí, bán hợp đồng, thanh toán, tìm kiếm Elasticsearch và tạo tài liệu chưa được triển khai. Các endpoint health chỉ xác nhận tiến trình ứng dụng, không xác nhận kết nối CSDL hoặc RabbitMQ.
- `eureka-server`, `postgres`, `scripts`, `Web` được đưa vào solution dưới dạng Solution Folder.

## Build và test trên Windows

Cài .NET SDK 10 (đọc định dạng `.slnx`) và .NET/ASP.NET Core Runtime 8.

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

Log được lưu ở `artifacts/build.log`, `artifacts/test.log`; kết quả TRX ở `artifacts/test-results`. Có thể chạy `scripts/build.bat` hoặc `scripts/build-without-tests.bat` từ bất kỳ thư mục nào.

Kiểm tra HTTP cho 9 service (tạm tắt đăng ký Eureka, tự dừng các tiến trình sau kiểm tra):

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\smoke-test.ps1
```

## Hạ tầng và PostgreSQL

Cài và khởi động Docker Desktop với Linux containers, sau đó:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\start-infra.ps1
docker compose -f scripts/infra.yml ps
```

| Thành phần | Địa chỉ / cổng |
|---|---|
| Eureka | http://localhost:8761 |
| PostgreSQL | localhost:5432; user `postgres`, password `lab_pass` |
| RabbitMQ | http://localhost:15672; user `lab_user`, password `lab_pass` |
| Elasticsearch | http://localhost:9200 |

Mật khẩu trên chỉ dùng cho môi trường thực hành cục bộ. PostgreSQL tạo bốn CSDL khi volume khởi tạo lần đầu: `lab_netmicro_payments`, `lab_netmicro_jobs`, `lab_netmicro_policy`, `lab_netmicro_pricing`, do `lab_user` sở hữu. Script SQL không tự chạy lại trên volume đã có dữ liệu.

Trên máy đã kiểm tra, PostgreSQL 16.15, RabbitMQ 4.3.6 (Erlang 28.5.0.6) và Elasticsearch 8.9.2 đã được cài và chạy trực tiếp dưới dạng Windows Service vì Docker bị chặn bởi ảo hóa. Tên service: `postgresql-x64-16`, `RabbitMQ`, `elasticsearch-service-x64`. RabbitMQ native dùng tài khoản mặc định `guest`/`guest` trên localhost; tài khoản `lab_user`/`lab_pass` trong bảng là của cấu hình Docker.

RabbitMQ nằm trong `.local/rabbitmq`, dữ liệu ở `.local/rabbitmq-data`; Elasticsearch nằm trong `.local/elasticsearch-8.9.2`. Các thư mục này không được commit. Không di chuyển thư mục repo khi các service còn dùng những đường dẫn này. Khi chuyển sang Docker, dừng các service native trước để tránh trùng cổng.

## Backup và restore để nộp bài

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\backup-databases.ps1
```

Bốn file PostgreSQL custom-format được lưu trong `backups/`; script kiểm tra bằng `pg_restore --list`. Nộp các file này riêng lên elearning. `createdatabases.sql` chỉ là script khởi tạo, không thay thế backup. Không đưa dữ liệu CSDL lên GitHub.

Nếu Docker báo không có virtualization, cần bật Virtual Machine Platform và ảo hóa trong BIOS/UEFI rồi khởi động lại. Có thể cài PostgreSQL trực tiếp để tạo CSDL và backup độc lập với Docker. Với PostgreSQL 16 cài ở đường dẫn mặc định:

```powershell
$env:PGPASSWORD='lab_pass'
& 'C:\Program Files\PostgreSQL\16\bin\psql.exe' -h localhost -U postgres -v ON_ERROR_STOP=1 -f .\postgres\createdatabases.sql
Remove-Item Env:PGPASSWORD
powershell -ExecutionPolicy Bypass -File .\scripts\backup-databases.ps1 -Native
```

Lệnh tạo CSDL trên chỉ chạy một lần trên PostgreSQL mới. Không chạy lại nếu user/CSDL đã tồn tại.

Restore vào một CSDL rỗng đã tạo:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\restore-database.ps1 -BackupFile .\backups\lab_netmicro_payments.backup -Database lab_netmicro_payments
```

## Chạy ứng dụng

```powershell
docker compose -f scripts/infra.yml -f scripts/app.yml up -d --build
```

Hoặc chạy riêng `dotnet run --project PaymentService` và `dotnet run --project AgentPortalApiGateway`. Payment dùng cổng 5100, Gateway 8099. Sau khi Payment đăng ký Eureka, `http://localhost:8099/api/payments/health` trả về trạng thái service.

Kiểm tra chuỗi Eureka → Gateway → Payment trên Windows, sau khi build .NET và Eureka (script tự khởi động rồi dừng các tiến trình kiểm tra, cần các cổng 8761/5100/8099 đang trống):

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\smoke-gateway.ps1
```

Blazor: `dotnet run --project BlazorWasmClient`. Giao diện biên dịch được nhưng các luồng nghiệp vụ chưa hoạt động vì backend hiện là khung triển khai.

Vue:

```powershell
cd Web
npm ci --legacy-peer-deps
$env:NODE_OPTIONS='--openssl-legacy-provider'
npm run build
```

Vue dùng bộ mã Vue 2 được cung cấp trong đề. Đã thay `node-sass` bằng Dart Sass để tránh phụ thuộc trình biên dịch native cũ. Dockerfile dùng Node 22.

Eureka chạy bằng Java 11: trong `eureka-server`, chạy `./gradlew.bat build --no-daemon` hoặc `./gradlew.bat bootRun`.

## Nộp bài

1. Đưa source lên repository GitHub. Nếu private, mời giảng viên qua email trong đề.
2. Nộp bốn file backup PostgreSQL lên elearning.
3. Chụp màn hình terminal/Visual Studio có kết quả build thành công và nộp ảnh đó. Log trong `artifacts` là bằng chứng dạng văn bản, không thay thế ảnh chụp màn hình.

Thư mục `artifacts/original` giữ các file mẫu Payment đã chuyển ra ngoài project; không được commit. Git bỏ qua `.vs`, `bin`, `obj`, `node_modules` và dữ liệu backup.

Hướng dẫn kỹ thuật đối chiếu: [RabbitMQ trên Windows](https://www.rabbitmq.com/docs/install-windows), [tương thích Erlang](https://www.rabbitmq.com/docs/which-erlang), [Elasticsearch ZIP trên Windows](https://www.elastic.co/guide/en/elasticsearch/reference/8.9/zip-windows.html), [service discovery Ocelot 23.3](https://ocelot.readthedocs.io/en/23.3.6/features/servicediscovery.html).
