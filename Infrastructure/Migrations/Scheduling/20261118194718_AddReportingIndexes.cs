using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations.Scheduling
{
    /// <inheritdoc />
    public partial class AddReportingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Appointments_Status_StartAt",
                schema: "scheduling",
                table: "Appointments",
                columns: new[] { "Status", "StartAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_TenantId_LocalDate_Stats",
                schema: "scheduling",
                table: "Appointments",
                columns: new[] { "TenantId", "LocalDate" })
                .Annotation("SqlServer:Include", new[] { "BranchId", "DoctorId", "Status", "StartAt", "EndAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Appointments_Status_StartAt",
                schema: "scheduling",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_TenantId_LocalDate_Stats",
                schema: "scheduling",
                table: "Appointments");
        }
    }
}
