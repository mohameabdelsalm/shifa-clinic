using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations.Billing
{
    /// <inheritdoc />
    public partial class HardenRefunds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_VisitAppointment",
                schema: "billing",
                table: "Invoices");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "billing",
                table: "Refunds",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Completed");   // every refund made before ADR-017 was completed at once

            migrationBuilder.AddColumn<decimal>(
                name: "CapturedAmount",
                schema: "billing",
                table: "OnlinePaymentAttempts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GatewayOrderId",
                schema: "billing",
                table: "OnlinePaymentAttempts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_VisitAppointment",
                schema: "billing",
                table: "Invoices",
                columns: new[] { "TenantId", "AppointmentId" },
                unique: true,
                filter: "[Kind] = 'Visit' AND [Status] <> 'Void' AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_VisitAppointment",
                schema: "billing",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "billing",
                table: "Refunds");

            migrationBuilder.DropColumn(
                name: "CapturedAmount",
                schema: "billing",
                table: "OnlinePaymentAttempts");

            migrationBuilder.DropColumn(
                name: "GatewayOrderId",
                schema: "billing",
                table: "OnlinePaymentAttempts");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_VisitAppointment",
                schema: "billing",
                table: "Invoices",
                columns: new[] { "TenantId", "AppointmentId" },
                unique: true,
                filter: "[Kind] = 'Visit' AND [IsDeleted] = 0");
        }
    }
}
