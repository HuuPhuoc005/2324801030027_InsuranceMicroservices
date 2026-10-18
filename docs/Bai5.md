# Bài thực hành 5 — PricingService và Eureka

## Nội dung đã tích hợp

- Tích hợp DTO, command, validator, converter, unit test và integration test từ ba file RAR của giảng viên.
- Bổ sung `PricingController`, `CalculatePriceHandler`, mô hình `Tariff`, các quy tắc phí cơ bản và hệ số điều chỉnh.
- Lưu và đọc biểu phí từ PostgreSQL qua `IDataStore`/`ITariffRepository`; lần khởi động đầu tạo bảng `pricing_tariffs` và thêm các biểu phí TRI, HSI, FAI, CAR nếu chưa có. Các biểu phí đã tồn tại không bị ghi đè.
- Giữ các endpoint health của bài 4. Bổ sung `/info` cho liên kết trạng thái trên Eureka.
- Solution có 30 project, gồm project mới `PricingService.IntegrationTest`.

## Chạy bài trên Windows

1. PostgreSQL cổng 5432 cần đang chạy và có CSDL `lab_netmicro_pricing`, user `lab_user`, password thực hành `lab_pass`. Có thể thay bằng cấu hình `ConnectionStrings:DefaultConnection` hoặc biến môi trường `ConnectionStrings__DefaultConnection`.
2. Build .NET: `dotnet build InsuranceMicroservices.slnx -m:1`.
3. Build Eureka bằng Java 11: trong thư mục `eureka-server`, đặt `JAVA_HOME` vào thư mục JDK 11 rồi chạy `./gradlew.bat bootJar --no-daemon`.
4. Tại thư mục repo, chạy `powershell -ExecutionPolicy Bypass -File scripts/run-bai5.ps1`. Nếu đường dẫn Java khác máy mẫu, truyền tham số `-JavaExecutable` là đường dẫn `java.exe` của Java 11.
5. Mở http://localhost:8761 và kiểm tra `PRICINGSERVICE` có trạng thái `UP`. Pricing chạy cổng 5040; health ở http://localhost:5040/health, thông tin service ở http://localhost:5040/info.

Script chạy để lại Eureka/Pricing hoạt động phục vụ demo. Có thể dừng những tiến trình do script khởi tạo bằng `scripts/stop-bai5.ps1`. Không khởi động hai bản Pricing trên cùng cổng. Đăng ký và heartbeat Eureka do Steeltoe 3.4 quản lý qua hosted services trong `AddDiscoveryClient`; không cần lời gọi lifecycle cũ `UseDiscoveryClient`.

## Kiểm thử

Với Docker engine đang chạy (giữ cách Testcontainers của giảng viên):

```powershell
powershell -ExecutionPolicy Bypass -File scripts/test-bai5.ps1
```

Với PostgreSQL cục bộ của bài 4:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/test-bai5.ps1 -NativePostgres
```

Chế độ native đặt `PRICING_TEST_POSTGRES` làm kết nối quản trị PostgreSQL, tạo CSDL có tên ngẫu nhiên `pricing_test_<guid>`, chạy HTTP với PostgreSQL thật rồi xóa CSDL kiểm thử đó. Không dùng CSDL thực hành để chạy test. Nếu máy dùng tài khoản khác, đặt biến môi trường này trước khi chạy script. Không đặt thì fixture mặc định dùng PostgreSQL Testcontainer; không tự chuyển sang kho dữ liệu giả và không bỏ qua test khi Docker lỗi.

CI trong `.github/workflows/build.yml` chạy integration test bằng Docker/Testcontainers trên GitHub runner.

## Gọi API tính phí

`POST http://localhost:5040/api/Pricing`, Content-Type: `application/json`.

```json
{
  "productCode": "TRI",
  "policyFrom": "2026-10-23T00:00:00+07:00",
  "policyTo": "2026-10-28T00:00:00+07:00",
  "selectedCovers": ["C1", "C2", "C3"],
  "answers": [
    { "questionCode": "NUM_OF_ADULTS", "questionType": 1, "answer": 1 },
    { "questionCode": "NUM_OF_CHILDREN", "questionType": 1, "answer": 1 },
    { "questionCode": "DESTINATION", "questionType": 0, "answer": "EUR" }
  ]
}
```

Kết quả: `totalPrice=98`, `coverPrices={C1:26,C2:52,C3:20}`. QuestionType: Text=0, Number=1, Choice=2. Input không hợp lệ trả 400, mã sản phẩm không có biểu phí trả 404. Các biểu thức phí lấy từ dữ liệu biểu phí của server; câu trả lời được gắn làm biến khi tính toán.

## Minh chứng nộp bài

- Link repo: https://github.com/HuuPhuoc005/2324801030027_InsuranceMicroservices
- Log build/test và kiểm tra Eureka ở `artifacts/bai5-*.log`.
- PDF ảnh chụp thực tế được lưu cục bộ ở `output/pdf/Bai5_MinhChung_Eureka_PricingService.pdf`, để nộp riêng lên elearning.

Docker engine chưa hoạt động trên máy ở lần kiểm tra này. Build, unit test, PostgreSQL integration test và Eureka chạy trực tiếp trên Windows. Không báo kết quả Docker/Testcontainers là đã chạy khi chưa xác minh.

Tham khảo kỹ thuật: [Npgsql](https://www.npgsql.org/doc/basic-usage.html), [Dynamic Expresso](https://github.com/dynamicexpresso/DynamicExpresso). Bài 5 tập trung vào Pricing/Eureka; các nghiệp vụ của những service khác vẫn ở mức khung bài 4.
