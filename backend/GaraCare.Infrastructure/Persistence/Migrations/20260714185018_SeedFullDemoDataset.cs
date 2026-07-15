using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GaraCare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedFullDemoDataset : Migration
    {
        // Mật khẩu demo cho tài khoản Customer "annguyen@gmail.com": Customer@123 (BCrypt hash cố định).
        private const string CustomerAccountPasswordHash = "$2a$11$ZA8H2ghbvFFkK9FBRun3SOeKeyyBb58JHupQc9FAdvpKYOLihx0N.";

        // Toàn bộ Id seed dùng dải 1000+ — DB dev hiện đã có dữ liệu thật do người dùng tự đăng ký
        // test (Customer Id 3-4/User Id 6-7), offset cao để không bao giờ đụng Id thật phát sinh
        // trong lúc phát triển (kể cả những Id được cấp sau thời điểm viết migration này).

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Thứ tự seed bám theo FK: User -> Customer -> Vehicle -> Appointment -> Part
            // -> WorkOrder -> QuotationItem -> Payment -> WorkOrderStatusHistory -> Notification.
            // Toàn bộ dữ liệu gắn liền nhau qua FK thật (không tạo record mồ côi), mô phỏng đủ
            // 8 trạng thái của WorkOrderStatus và 4 trạng thái của AppointmentStatus.

            // 1 tài khoản Customer đã đăng ký (Nguyễn Văn An) — 3 tài khoản Admin/Staff/Technician
            // đã seed ở migration SeedStaffTechAdminUsers (Id 1-3).
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "EmailVerificationCode", "EmailVerificationCodeExpiresAt", "FullName", "IsEmailVerified", "PasswordHash", "PasswordResetCode", "PasswordResetCodeExpiresAt", "Phone", "Role", "Username" },
                values: new object[,]
                {
                    { 1000, "annguyen@gmail.com", null, null, "Nguyễn Văn An", true, CustomerAccountPasswordHash, null, null, "0977111222", "Customer", "annguyen@gmail.com" },
                });

            // Customer 1001 có tài khoản (UserId=1000); Customer 1002-1005 là khách vãng lai (UserId=null).
            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "FullName", "Phone", "Email", "Address", "UserId" },
                values: new object[,]
                {
                    { 1001, "Nguyễn Văn An", "0977111222", "annguyen@gmail.com", "12 Nguyễn Trãi, Quận 1, TP.HCM", 1000 },
                    { 1002, "Lê Thị Hoa", "0977222333", "hoahle@gmail.com", "45 Lý Thường Kiệt, Quận 10, TP.HCM", null },
                    { 1003, "Trần Văn Minh", "0977333444", null, "78 Cách Mạng Tháng 8, Quận 3, TP.HCM", null },
                    { 1004, "Đỗ Thị Lan", "0977444555", null, "90 Điện Biên Phủ, Bình Thạnh, TP.HCM", null },
                    { 1005, "Hoàng Văn Tùng", "0977555666", null, "15 Phan Xích Long, Phú Nhuận, TP.HCM", null },
                });

            // Customer 1001 có 2 xe (thể hiện quan hệ 1-N và cho GARA-21 xem lịch sử nhiều work order/1 xe).
            migrationBuilder.InsertData(
                table: "Vehicles",
                columns: new[] { "Id", "CustomerId", "LicensePlate", "Brand", "Model", "Year" },
                values: new object[,]
                {
                    { 1001, 1001, "51F-123.45", "Honda", "Wave Alpha", 2019 },
                    { 1002, 1001, "51F-234.56", "Yamaha", "Exciter 150", 2021 },
                    { 1003, 1002, "51G-345.67", "Honda", "SH Mode", 2020 },
                    { 1004, 1003, "51H-456.78", "Honda", "Air Blade", 2018 },
                    { 1005, 1004, "51K-567.89", "Yamaha", "Grande", 2022 },
                    { 1006, 1005, "51L-678.90", "Suzuki", "Raider R150", 2017 },
                });

            // 4 Appointment — đủ 4 trạng thái AppointmentStatus. Appointment 1002 (CheckedIn) đã được
            // check-in thành WorkOrder 1005 (gắn AppointmentId=1002 ở bảng WorkOrders bên dưới), kế
            // thừa DiscountPercent=5.
            migrationBuilder.InsertData(
                table: "Appointments",
                columns: new[] { "Id", "CustomerId", "VehicleId", "ScheduledDate", "ScheduledTimeSlot", "Status", "DiscountPercent", "CreatedAt", "IsLate", "LateNotifiedStaffAt" },
                values: new object[,]
                {
                    { 1001, 1001, 1001, new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc), "09:00-10:00", "Booked", 10.00m, new DateTime(2026, 7, 14, 20, 0, 0, DateTimeKind.Utc), false, null },
                    { 1002, 1003, 1004, new DateTime(2026, 7, 14, 0, 0, 0, DateTimeKind.Utc), "14:00-15:00", "CheckedIn", 5.00m, new DateTime(2026, 7, 12, 9, 0, 0, DateTimeKind.Utc), false, null },
                    { 1003, 1002, 1003, new DateTime(2026, 7, 8, 0, 0, 0, DateTimeKind.Utc), "08:00-09:00", "Cancelled", 10.00m, new DateTime(2026, 7, 5, 10, 0, 0, DateTimeKind.Utc), false, null },
                    { 1004, 1005, 1006, new DateTime(2026, 7, 13, 0, 0, 0, DateTimeKind.Utc), "16:00-17:00", "NoShow", 10.00m, new DateTime(2026, 7, 10, 8, 0, 0, DateTimeKind.Utc), true, new DateTime(2026, 7, 13, 16, 20, 0, DateTimeKind.Utc) },
                });

            // 9 Part — Part 1005 (Má phanh trước, tồn 3) cố tình để tồn kho thấp demo cảnh báo sắp hết hàng.
            migrationBuilder.InsertData(
                table: "Parts",
                columns: new[] { "Id", "Name", "SKU", "UnitPrice", "StockQuantity" },
                values: new object[,]
                {
                    { 1001, "Nhớt máy Castrol 1L", "NHOT-CAS-1L", 95000.00m, 40 },
                    { 1002, "Lọc nhớt Honda chính hãng", "LOC-HD-01", 65000.00m, 25 },
                    { 1003, "Bugi NGK", "BUGI-NGK-01", 45000.00m, 30 },
                    { 1004, "Nhông sên dĩa DID", "NSD-DID-01", 550000.00m, 8 },
                    { 1005, "Má phanh trước", "MP-TR-01", 120000.00m, 3 },
                    { 1006, "Lốp Michelin 90/80-17", "LOP-MICH-9080", 780000.00m, 12 },
                    { 1007, "Ắc quy GS 12V", "ACQ-GS-12V", 650000.00m, 15 },
                    { 1008, "Dây curoa (dây đai)", "DAYCUROA-01", 320000.00m, 20 },
                    { 1009, "Cảm biến oxy", "CB-OXY-01", 480000.00m, 6 },
                });

            // 8 WorkOrder — mỗi trạng thái trong WorkOrderStatus đúng 1 record (yêu cầu bắt buộc).
            // Vehicle 1001 và Vehicle 1004 mỗi xe có 2 WorkOrder (1 cũ đã xong + 1 hiện tại) để demo
            // lịch sử sửa chữa nhiều lần/1 xe (GARA-21). WorkOrder 1005 gắn Appointment 1002 (check-in).
            // CreatedByUserId=2 (Staff), các dòng lịch sử dưới dùng ChangedByUserId=3 (Technician).
            migrationBuilder.InsertData(
                table: "WorkOrders",
                columns: new[] { "Id", "VehicleId", "AppointmentId", "CreatedByUserId", "Status", "ReceivedDate", "InitialDescription", "DiagnosisNote", "TotalAmount", "DiscountPercent", "EstimatedCompletionDate", "IsDelayed", "QuoteSentAt", "ReminderSentAt", "NeedsFollowUpCall", "CompletedDate", "ApprovalToken", "ApprovalTokenExpiresAt", "ApprovalTokenUsedAt" },
                values: new object[,]
                {
                    { 1001, 1001, null, 2, "Delivered", new DateTime(2026, 6, 20, 8, 30, 0, DateTimeKind.Utc), "Bảo dưỡng định kỳ, thay nhớt", "Nhớt cũ, lọc gió bẩn, cần thay nhớt và kiểm tra lọc gió", 145000.00m, 0.00m, new DateTime(2026, 6, 20, 11, 0, 0, DateTimeKind.Utc), false, new DateTime(2026, 6, 20, 9, 0, 0, DateTimeKind.Utc), null, false, new DateTime(2026, 6, 20, 10, 30, 0, DateTimeKind.Utc), "tok-wo1001-abc123", new DateTime(2026, 6, 23, 9, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 20, 9, 15, 0, DateTimeKind.Utc) },
                    { 1002, 1001, null, 2, "QuotePending", new DateTime(2026, 7, 10, 9, 0, 0, DateTimeKind.Utc), "Xe bị rung lắc khi chạy tốc độ cao", "Nghi do nhông sên dĩa mòn, cần thay", 700000.00m, 0.00m, new DateTime(2026, 7, 12, 17, 0, 0, DateTimeKind.Utc), false, new DateTime(2026, 7, 10, 14, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 11, 14, 5, 0, DateTimeKind.Utc), true, null, "tok-wo1002-def456", new DateTime(2026, 7, 13, 14, 0, 0, DateTimeKind.Utc), null },
                    { 1003, 1002, null, 2, "InRepair", new DateTime(2026, 7, 11, 10, 0, 0, DateTimeKind.Utc), "Đèn báo lỗi động cơ sáng liên tục", "Cảm biến oxy hỏng, cần thay", 580000.00m, 0.00m, new DateTime(2026, 7, 13, 17, 0, 0, DateTimeKind.Utc), false, new DateTime(2026, 7, 11, 13, 0, 0, DateTimeKind.Utc), null, false, null, "tok-wo1003-ghi789", new DateTime(2026, 7, 14, 13, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 11, 15, 0, 0, DateTimeKind.Utc) },
                    { 1004, 1003, null, 2, "Received", new DateTime(2026, 7, 14, 8, 0, 0, DateTimeKind.Utc), "Xe có tiếng kêu lạ ở bánh trước khi phanh", null, 0.00m, 0.00m, null, false, null, null, false, null, null, null, null },
                    { 1005, 1004, 1002, 2, "Diagnosing", new DateTime(2026, 7, 14, 14, 0, 0, DateTimeKind.Utc), "Xe khó nổ máy vào buổi sáng", "Đang kiểm tra ắc quy và bugi", 0.00m, 5.00m, null, false, null, null, false, null, null, null, null },
                    { 1006, 1004, null, 2, "Cancelled", new DateTime(2026, 5, 5, 9, 0, 0, DateTimeKind.Utc), "Yêu cầu kiểm tra tổng quát trước khi đi xa", "Phát hiện lốp mòn, đề xuất thay lốp", 830000.00m, 0.00m, new DateTime(2026, 5, 5, 17, 0, 0, DateTimeKind.Utc), false, new DateTime(2026, 5, 5, 11, 0, 0, DateTimeKind.Utc), null, false, null, "tok-wo1006-jkl012", new DateTime(2026, 5, 8, 11, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 5, 16, 0, 0, DateTimeKind.Utc) },
                    { 1007, 1005, null, 2, "WaitingParts", new DateTime(2026, 7, 8, 9, 30, 0, DateTimeKind.Utc), "Phanh trước ăn không đều, có tiếng kêu", "Má phanh mòn, cần thay nhưng kho sắp hết hàng", 320000.00m, 0.00m, new DateTime(2026, 7, 14, 17, 0, 0, DateTimeKind.Utc), true, new DateTime(2026, 7, 8, 13, 0, 0, DateTimeKind.Utc), null, false, null, "tok-wo1007-mno345", new DateTime(2026, 7, 11, 13, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 8, 15, 0, 0, DateTimeKind.Utc) },
                    { 1008, 1006, null, 2, "Completed", new DateTime(2026, 7, 9, 10, 0, 0, DateTimeKind.Utc), "Bảo dưỡng 10.000km", "Thay nhớt, lọc nhớt, bugi", 370000.00m, 0.00m, new DateTime(2026, 7, 9, 16, 0, 0, DateTimeKind.Utc), false, new DateTime(2026, 7, 9, 11, 0, 0, DateTimeKind.Utc), null, false, new DateTime(2026, 7, 9, 15, 30, 0, DateTimeKind.Utc), "tok-wo1008-pqr678", new DateTime(2026, 7, 12, 11, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 9, 11, 30, 0, DateTimeKind.Utc) },
                });

            // QuotationItem — mỗi WorkOrder đã gửi báo giá có ít nhất 1 Part + 1 Labor. WorkOrder
            // 1004/1005 (Received/Diagnosing) chưa lập báo giá nên không có item. IsUsed=true chỉ ở
            // WorkOrder Completed/Delivered (đã thực sự lắp/thực hiện).
            migrationBuilder.InsertData(
                table: "QuotationItems",
                columns: new[] { "Id", "WorkOrderId", "PartId", "Type", "Description", "Quantity", "UnitPrice", "IsApproved", "IsUsed" },
                values: new object[,]
                {
                    { 1001, 1001, 1001, "Part", "Nhớt máy Castrol 1L", 1, 95000.00m, true, true },
                    { 1002, 1001, null, "Labor", "Công thay nhớt", 1, 50000.00m, true, true },
                    { 1003, 1002, 1004, "Part", "Nhông sên dĩa DID", 1, 550000.00m, false, false },
                    { 1004, 1002, null, "Labor", "Công thay nhông sên dĩa", 1, 150000.00m, false, false },
                    { 1005, 1003, 1009, "Part", "Cảm biến oxy", 1, 480000.00m, true, false },
                    { 1006, 1003, null, "Labor", "Công thay cảm biến oxy", 1, 100000.00m, true, false },
                    { 1007, 1006, 1006, "Part", "Lốp Michelin 90/80-17", 1, 780000.00m, false, false },
                    { 1008, 1006, null, "Labor", "Công thay lốp", 1, 50000.00m, false, false },
                    { 1009, 1007, 1005, "Part", "Má phanh trước", 2, 120000.00m, true, false },
                    { 1010, 1007, null, "Labor", "Công thay má phanh", 1, 80000.00m, true, false },
                    { 1011, 1008, 1001, "Part", "Nhớt máy Castrol 1L", 1, 95000.00m, true, true },
                    { 1012, 1008, 1002, "Part", "Lọc nhớt Honda chính hãng", 1, 65000.00m, true, true },
                    { 1013, 1008, 1003, "Part", "Bugi NGK", 2, 45000.00m, true, true },
                    { 1014, 1008, null, "Labor", "Công bảo dưỡng định kỳ", 1, 120000.00m, true, true },
                });

            // Payment — chỉ WorkOrder Delivered (Id=1001) có Payment, đúng ràng buộc "không tạo
            // Payment khi WorkOrder chưa Completed" (docs/01-business-spec.md §5).
            migrationBuilder.InsertData(
                table: "Payments",
                columns: new[] { "Id", "WorkOrderId", "Amount", "Method", "ConfirmedByUserId", "TransactionRef", "GatewayStatus", "PaidDate" },
                values: new object[,]
                {
                    { 1001, 1001, 145000.00m, "Cash", 2, null, null, new DateTime(2026, 6, 20, 10, 45, 0, DateTimeKind.Utc) },
                });

            // WorkOrderStatusHistory — audit log đầy đủ chuỗi transition dẫn tới trạng thái hiện tại
            // của mỗi WorkOrder (không có dòng cho lúc mới tạo ở Received, vì đó là trạng thái khởi
            // tạo, chưa qua transition nào). ApprovedViaToken=true mô phỏng khách duyệt/từ chối qua
            // magic link; ChangedByUserId=null cùng lúc vì hành động đó không qua tài khoản đăng nhập.
            migrationBuilder.InsertData(
                table: "WorkOrderStatusHistories",
                columns: new[] { "Id", "WorkOrderId", "FromStatus", "ToStatus", "ChangedByUserId", "ApprovedViaToken", "ChangedAt" },
                values: new object[,]
                {
                    { 1001, 1001, "Received", "Diagnosing", 3, false, new DateTime(2026, 6, 20, 8, 45, 0, DateTimeKind.Utc) },
                    { 1002, 1001, "Diagnosing", "QuotePending", 2, false, new DateTime(2026, 6, 20, 9, 0, 0, DateTimeKind.Utc) },
                    { 1003, 1001, "QuotePending", "InRepair", null, true, new DateTime(2026, 6, 20, 9, 15, 0, DateTimeKind.Utc) },
                    { 1004, 1001, "InRepair", "Completed", 3, false, new DateTime(2026, 6, 20, 10, 30, 0, DateTimeKind.Utc) },
                    { 1005, 1001, "Completed", "Delivered", 2, false, new DateTime(2026, 6, 20, 10, 45, 0, DateTimeKind.Utc) },
                    { 1006, 1002, "Received", "Diagnosing", 3, false, new DateTime(2026, 7, 10, 10, 0, 0, DateTimeKind.Utc) },
                    { 1007, 1002, "Diagnosing", "QuotePending", 2, false, new DateTime(2026, 7, 10, 14, 0, 0, DateTimeKind.Utc) },
                    { 1008, 1003, "Received", "Diagnosing", 3, false, new DateTime(2026, 7, 11, 11, 0, 0, DateTimeKind.Utc) },
                    { 1009, 1003, "Diagnosing", "QuotePending", 2, false, new DateTime(2026, 7, 11, 13, 0, 0, DateTimeKind.Utc) },
                    { 1010, 1003, "QuotePending", "InRepair", 1000, false, new DateTime(2026, 7, 11, 15, 0, 0, DateTimeKind.Utc) },
                    { 1011, 1005, "Received", "Diagnosing", 3, false, new DateTime(2026, 7, 14, 14, 30, 0, DateTimeKind.Utc) },
                    { 1012, 1006, "Received", "Diagnosing", 3, false, new DateTime(2026, 5, 5, 9, 30, 0, DateTimeKind.Utc) },
                    { 1013, 1006, "Diagnosing", "QuotePending", 2, false, new DateTime(2026, 5, 5, 11, 0, 0, DateTimeKind.Utc) },
                    { 1014, 1006, "QuotePending", "Cancelled", null, true, new DateTime(2026, 5, 5, 16, 0, 0, DateTimeKind.Utc) },
                    { 1015, 1007, "Received", "Diagnosing", 3, false, new DateTime(2026, 7, 8, 10, 0, 0, DateTimeKind.Utc) },
                    { 1016, 1007, "Diagnosing", "QuotePending", 2, false, new DateTime(2026, 7, 8, 13, 0, 0, DateTimeKind.Utc) },
                    { 1017, 1007, "QuotePending", "InRepair", null, true, new DateTime(2026, 7, 8, 15, 0, 0, DateTimeKind.Utc) },
                    { 1018, 1007, "InRepair", "WaitingParts", 3, false, new DateTime(2026, 7, 10, 9, 0, 0, DateTimeKind.Utc) },
                    { 1019, 1008, "Received", "Diagnosing", 3, false, new DateTime(2026, 7, 9, 10, 30, 0, DateTimeKind.Utc) },
                    { 1020, 1008, "Diagnosing", "QuotePending", 2, false, new DateTime(2026, 7, 9, 11, 0, 0, DateTimeKind.Utc) },
                    { 1021, 1008, "QuotePending", "InRepair", null, true, new DateTime(2026, 7, 9, 11, 30, 0, DateTimeKind.Utc) },
                    { 1022, 1008, "InRepair", "Completed", 3, false, new DateTime(2026, 7, 9, 15, 30, 0, DateTimeKind.Utc) },
                });

            // Notification — 1 record cho mỗi giá trị NotificationType (QuoteReady/Delayed/
            // StatusChanged/AppointmentConfirmed), gắn đúng Customer/WorkOrder/Appointment liên quan.
            migrationBuilder.InsertData(
                table: "Notifications",
                columns: new[] { "Id", "CustomerId", "WorkOrderId", "AppointmentId", "Type", "Message", "EmailSentSuccessfully", "IsRead", "CreatedAt" },
                values: new object[,]
                {
                    { 1001, 1001, 1002, null, "QuoteReady", "Báo giá sửa chữa xe 51F-123.45 đã sẵn sàng, vui lòng xem và duyệt.", true, false, new DateTime(2026, 7, 10, 14, 0, 0, DateTimeKind.Utc) },
                    { 1002, 1004, 1007, null, "Delayed", "Xe 51K-567.89 của quý khách bị trễ tiến độ do chờ phụ tùng, gara xin lỗi vì sự bất tiện này.", true, false, new DateTime(2026, 7, 14, 17, 5, 0, DateTimeKind.Utc) },
                    { 1003, 1001, 1001, null, "StatusChanged", "Xe 51F-123.45 đã hoàn tất sửa chữa và giao xe thành công.", true, true, new DateTime(2026, 6, 20, 10, 45, 0, DateTimeKind.Utc) },
                    { 1004, 1001, null, 1001, "AppointmentConfirmed", "Lịch hẹn ngày 20/07/2026 (09:00-10:00) đã được xác nhận.", true, false, new DateTime(2026, 7, 14, 20, 1, 0, DateTimeKind.Utc) },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Xoá theo thứ tự ngược lại để không vi phạm FK.
            for (var id = 1001; id <= 1004; id++)
            {
                migrationBuilder.DeleteData(table: "Notifications", keyColumn: "Id", keyValue: id);
            }

            for (var id = 1001; id <= 1022; id++)
            {
                migrationBuilder.DeleteData(table: "WorkOrderStatusHistories", keyColumn: "Id", keyValue: id);
            }

            migrationBuilder.DeleteData(table: "Payments", keyColumn: "Id", keyValue: 1001);

            for (var id = 1001; id <= 1014; id++)
            {
                migrationBuilder.DeleteData(table: "QuotationItems", keyColumn: "Id", keyValue: id);
            }

            for (var id = 1001; id <= 1008; id++)
            {
                migrationBuilder.DeleteData(table: "WorkOrders", keyColumn: "Id", keyValue: id);
            }

            for (var id = 1001; id <= 1009; id++)
            {
                migrationBuilder.DeleteData(table: "Parts", keyColumn: "Id", keyValue: id);
            }

            for (var id = 1001; id <= 1004; id++)
            {
                migrationBuilder.DeleteData(table: "Appointments", keyColumn: "Id", keyValue: id);
            }

            for (var id = 1001; id <= 1006; id++)
            {
                migrationBuilder.DeleteData(table: "Vehicles", keyColumn: "Id", keyValue: id);
            }

            for (var id = 1001; id <= 1005; id++)
            {
                migrationBuilder.DeleteData(table: "Customers", keyColumn: "Id", keyValue: id);
            }

            migrationBuilder.DeleteData(table: "Users", keyColumn: "Id", keyValue: 1000);
        }
    }
}
