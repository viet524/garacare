using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GaraCare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedDemoUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "EmailVerificationCode", "EmailVerificationCodeExpiresAt", "FullName", "IsEmailVerified", "PasswordHash", "PasswordResetCode", "PasswordResetCodeExpiresAt", "Phone", "Role", "Username" },
                values: new object[,]
                {
                    { 1, "admin@garacare.vn", null, null, "Đặng Quốc Bảo", true, "$2a$11$EH94CA4UW7yi9dQvxkyBvu3LQiFusGTsJXbPimwHeAil86qX4P9lq", null, null, "0901000001", "Admin", "admin@garacare.vn" },
                    { 2, "staff@garacare.vn", null, null, "Ngô Thị Mai", true, "$2a$11$EH94CA4UW7yi9dQvxkyBvu3LQiFusGTsJXbPimwHeAil86qX4P9lq", null, null, "0901000002", "Staff", "staff@garacare.vn" },
                    { 3, "technician@garacare.vn", null, null, "Bùi Văn Hùng", true, "$2a$11$EH94CA4UW7yi9dQvxkyBvu3LQiFusGTsJXbPimwHeAil86qX4P9lq", null, null, "0901000003", "Technician", "technician@garacare.vn" },
                    { 4, "customer@garacare.vn", null, null, "Nguyễn Văn An", true, "$2a$11$EH94CA4UW7yi9dQvxkyBvu3LQiFusGTsJXbPimwHeAil86qX4P9lq", null, null, "0901000004", "Customer", "customer@garacare.vn" }
                });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "Address", "Email", "FullName", "Phone", "UserId" },
                values: new object[] { 1, null, "customer@garacare.vn", "Nguyễn Văn An", "0901000004", 4 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4);
        }
    }
}
