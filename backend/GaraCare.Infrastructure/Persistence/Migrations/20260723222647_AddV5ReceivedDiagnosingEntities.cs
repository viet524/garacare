using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GaraCare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddV5ReceivedDiagnosingEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EstimatedCompletionDate",
                table: "WorkOrders",
                newName: "SystemSuggestedDate");

            migrationBuilder.AddColumn<DateTime>(
                name: "FinalEstimatedDate",
                table: "WorkOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHeavyRepair",
                table: "WorkOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TechnicianStatus",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Bays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CurrentWorkOrderId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bays_WorkOrders_CurrentWorkOrderId",
                        column: x => x.CurrentWorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DiagnosisRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkOrderId = table.Column<int>(type: "int", nullable: false),
                    TechnicianId = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EstimatedLaborHours = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    SignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiagnosisRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiagnosisRecords_Users_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiagnosisRecords_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceCatalogItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EstimatedDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    RequiredBayType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsMasterTechRequired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCatalogItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkOrderId = table.Column<int>(type: "int", nullable: false),
                    TechnicianId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StageAtStart = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StageAtEnd = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HandoffReason = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LaborHoursLogged = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    CommissionSplitPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderAssignments_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrderAssignments_Users_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrderAssignments_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bays_CurrentWorkOrderId",
                table: "Bays",
                column: "CurrentWorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosisRecords_TechnicianId",
                table: "DiagnosisRecords",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosisRecords_WorkOrderId",
                table: "DiagnosisRecords",
                column: "WorkOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderAssignments_ApprovedByUserId",
                table: "WorkOrderAssignments",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderAssignments_TechnicianId",
                table: "WorkOrderAssignments",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderAssignments_WorkOrderId",
                table: "WorkOrderAssignments",
                column: "WorkOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bays");

            migrationBuilder.DropTable(
                name: "DiagnosisRecords");

            migrationBuilder.DropTable(
                name: "ServiceCatalogItems");

            migrationBuilder.DropTable(
                name: "WorkOrderAssignments");

            migrationBuilder.DropColumn(
                name: "FinalEstimatedDate",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "IsHeavyRepair",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "TechnicianStatus",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "SystemSuggestedDate",
                table: "WorkOrders",
                newName: "EstimatedCompletionDate");
        }
    }
}
