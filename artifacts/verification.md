# Kết quả kiểm tra ngày 11/10/2026

- `dotnet build InsuranceMicroservices.slnx --no-restore -m:1 -nr:false`: thành công, 0 lỗi, 0 cảnh báo; gồm 29 project, có Blazor.
- `dotnet test InsuranceMicroservices.slnx --no-build --no-restore -m:1`: 9 test pass, 0 fail.
- `scripts/smoke-test.ps1`: cả 9 service trả HTTP `/health` với đúng tên service và trạng thái `UP`; thử nghiệm này tắt đăng ký Eureka.
- Vue: build production thành công; còn 2 cảnh báo kích thước bundle của bộ mã Vue 2.
- Eureka: Gradle build thành công với Java 11.
- Docker Compose: `docker compose -f scripts/infra.yml -f scripts/app.yml config --quiet` thành công. Docker engine chưa chạy do máy không bật ảo hóa; chưa kiểm tra build/start container toàn hệ thống.
- PostgreSQL 16.15 cài trực tiếp: đã tạo bốn CSDL theo SQL, owner là `lab_user`; xuất custom-format backup và restore thành công từng file vào CSDL kiểm tra tạm, sau đó dọn các CSDL kiểm tra.
- RabbitMQ 4.3.6 với Erlang 28.5.0.6 và Elasticsearch 8.9.2 đã cài chạy trực tiếp dưới dạng Windows Service. RabbitMQ management HTTP API đã trả đúng phiên bản; Elasticsearch HTTP đã trả đúng phiên bản.
- Chuỗi Eureka → Gateway → PaymentService đã trả HTTP health `UP` qua cổng 8099; kết quả ở `gateway-smoke-test.log`.
- CSDL mới khởi tạo chưa có bảng/dữ liệu nghiệp vụ. Backup được nộp riêng, không commit vào GitHub.
- Ảnh Visual Studio trong thư mục này là ảnh chụp thực tế. Project Blazor đã được nạp lại trong Visual Studio; kết quả build đầy đủ bao gồm Blazor cũng được xác nhận trong `build.log` bằng .NET CLI.

Phần nghiệp vụ vẫn là khung ban đầu; không dùng kết quả health/build này để kết luận các luồng đăng nhập, bán hợp đồng, tính phí hoặc thanh toán đã hoàn thiện.
