using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations.Billing
{
    /// <inheritdoc />
    public partial class AddReportingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Refunds_TenantId_CreationDate",
                schema: "billing",
                table: "Refunds",
                columns: new[] { "TenantId", "CreationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_OnlinePaymentAttempts_Status_CreationDate",
                schema: "billing",
                table: "OnlinePaymentAttempts",
                columns: new[] { "Status", "CreationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId_IssuedAt",
                schema: "billing",
                table: "Invoices",
                columns: new[] { "TenantId", "IssuedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_TenantId_CreationDate",
                schema: "billing",
                table: "CreditNotes",
                columns: new[] { "TenantId", "CreationDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Refunds_TenantId_CreationDate",
                schema: "billing",
                table: "Refunds");

            migrationBuilder.DropIndex(
                name: "IX_OnlinePaymentAttempts_Status_CreationDate",
                schema: "billing",
                table: "OnlinePaymentAttempts");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_TenantId_IssuedAt",
                schema: "billing",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_CreditNotes_TenantId_CreationDate",
                schema: "billing",
                table: "CreditNotes");
        }
    }
}
