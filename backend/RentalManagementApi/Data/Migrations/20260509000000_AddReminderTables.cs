using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalManagementApi.Data.Migrations
{
    public partial class AddReminderTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReminderSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    DaysBeforeDue = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    DaysAfterDue = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReminderSettings", x => x.Id);
                    table.ForeignKey(name: "FK_ReminderSettings_Leases_LeaseId",
                        column: x => x.LeaseId, principalTable: "Leases",
                        principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReminderLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReminderType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReminderLogs", x => x.Id);
                    table.ForeignKey(name: "FK_ReminderLogs_Leases_LeaseId",
                        column: x => x.LeaseId, principalTable: "Leases",
                        principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_ReminderSettings_LeaseId",
                table: "ReminderSettings", column: "LeaseId", unique: true);

            migrationBuilder.CreateIndex(name: "IX_ReminderLogs_LeaseId_DueDate_ReminderType_SentDate",
                table: "ReminderLogs", columns: ["LeaseId", "DueDate", "ReminderType", "SentDate"], unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ReminderLogs");
            migrationBuilder.DropTable(name: "ReminderSettings");
        }
    }
}
