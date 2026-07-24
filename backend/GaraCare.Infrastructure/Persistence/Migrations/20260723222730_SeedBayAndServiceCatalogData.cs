using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GaraCare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedBayAndServiceCatalogData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Số lượng Bay theo ví dụ đã chốt ở docs/01-business-spec.md §12 (3 LiftBay + 2 TireBay)
            // + 2 GeneralBay cho các việc không cần Bay chuyên dụng (chẩn đoán ban đầu, thay dầu...).
            migrationBuilder.InsertData(
                table: "Bays",
                columns: new[] { "Id", "Type", "Status", "CurrentWorkOrderId" },
                values: new object[,]
                {
                    { 1, "LiftBay", "Free", null },
                    { 2, "LiftBay", "Free", null },
                    { 3, "LiftBay", "Free", null },
                    { 4, "TireBay", "Free", null },
                    { 5, "TireBay", "Free", null },
                    { 6, "GeneralBay", "Free", null },
                    { 7, "GeneralBay", "Free", null },
                });

            // Catalog khởi điểm đã chốt với người dùng — sửa lại sau khi có danh sách dịch vụ
            // thật của gara (docs/03-data-model.md — entity ServiceCatalogItem).
            migrationBuilder.InsertData(
                table: "ServiceCatalogItems",
                columns: new[] { "Id", "Name", "Description", "UnitPrice", "EstimatedDurationMinutes", "RequiredBayType", "IsMasterTechRequired" },
                values: new object[,]
                {
                    { 1, "Thay dầu động cơ", "Thay dầu + lọc dầu định kỳ", 250000m, 30, "GeneralBay", false },
                    { 2, "Kiểm tra tổng quát", "Kiểm tra tổng thể xe, không cần tháo lắp lớn", 100000m, 20, "GeneralBay", false },
                    { 3, "Đảo lốp", "Đảo vị trí 4 lốp để mòn đều", 80000m, 30, "TireBay", false },
                    { 4, "Thay lốp", "Tháo lốp cũ, lắp lốp mới, cân bằng động", 150000m, 45, "TireBay", false },
                    { 5, "Sửa phanh (má phanh, đĩa phanh)", "Kiểm tra/thay má phanh, đĩa phanh", 350000m, 60, "LiftBay", false },
                    { 6, "Kiểm tra/sửa hộp số", "Cần tháo gầm, đòi hỏi thợ chuyên biệt", 1200000m, 180, "LiftBay", true },
                    { 7, "Kiểm tra/sửa gầm", "Kiểm tra hệ thống treo, gầm xe", 450000m, 90, "LiftBay", false },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "ServiceCatalogItems", keyColumn: "Id", keyValues: new object[] { 1, 2, 3, 4, 5, 6, 7 });
            migrationBuilder.DeleteData(table: "Bays", keyColumn: "Id", keyValues: new object[] { 1, 2, 3, 4, 5, 6, 7 });
        }
    }
}
