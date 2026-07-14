using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GaraCare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedStaffTechAdminUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tài khoản demo cho Staff/Technician/Admin — Customer không seed vì tự đăng ký qua UC đăng ký.
            // IsEmailVerified=true vì đây là tài khoản nội bộ do Admin tạo, không qua luồng xác minh email.
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "EmailVerificationCode", "EmailVerificationCodeExpiresAt", "FullName", "IsEmailVerified", "PasswordHash", "PasswordResetCode", "PasswordResetCodeExpiresAt", "Phone", "Role", "Username" },
                values: new object[,]
                {
                    { 1, "admin@garacare.vn", null, null, "Đặng Quốc Bảo", true, "$2a$11$BTIuhwoLyAel4niJg0MTN.17.lRsya9Uq02FXycMdkHmeI./P21N6", null, null, "0901000001", "Admin", "admin@garacare.vn" },
                    { 2, "staff@garacare.vn", null, null, "Ngô Thị Mai", true, "$2a$11$a/dyqjVqfpbGE2O4dswdZOOlC4WS5.SG4OsFK8Om3AKKG2K96.Ufe", null, null, "0901000002", "Staff", "staff@garacare.vn" },
                    { 3, "technician@garacare.vn", null, null, "Bùi Văn Hùng", true, "$2a$11$F.cOWXGMpfZn5bK/K2qu.uOc39rcaz8z4QG8A6bnyiGlIhgQHqERq", null, null, "0901000003", "Technician", "technician@garacare.vn" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "Users", keyColumn: "Id", keyValue: 1);
            migrationBuilder.DeleteData(table: "Users", keyColumn: "Id", keyValue: 2);
            migrationBuilder.DeleteData(table: "Users", keyColumn: "Id", keyValue: 3);
        }
    }
}
