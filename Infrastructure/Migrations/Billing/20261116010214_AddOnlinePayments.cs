using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations.Billing
{
    /// <inheritdoc />
    public partial class AddOnlinePayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OnlinePaymentAttempts",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    InvoiceId = table.Column<long>(type: "bigint", nullable: false),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GatewayIntentionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GatewayTransactionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PaymentId = table.Column<long>(type: "bigint", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModificationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlinePaymentAttempts", x => x.Id);
                    table.UniqueConstraint("AK_OnlinePaymentAttempts_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_OnlinePaymentAttempts_Invoices_TenantId_InvoiceId",
                        columns: x => new { x.TenantId, x.InvoiceId },
                        principalSchema: "billing",
                        principalTable: "Invoices",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnlinePaymentAttempts_Payments_TenantId_PaymentId",
                        columns: x => new { x.TenantId, x.PaymentId },
                        principalSchema: "billing",
                        principalTable: "Payments",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentGatewaySettings",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PublicKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SecretKeyProtected = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    HmacSecretProtected = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IntegrationIds = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModificationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentGatewaySettings", x => x.Id);
                    table.UniqueConstraint("AK_PaymentGatewaySettings_TenantId_Id", x => new { x.TenantId, x.Id });
                });

            migrationBuilder.CreateIndex(
                name: "IX_OnlinePaymentAttempts_TenantId_InvoiceId",
                schema: "billing",
                table: "OnlinePaymentAttempts",
                columns: new[] { "TenantId", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_OnlinePaymentAttempts_TenantId_PaymentId",
                schema: "billing",
                table: "OnlinePaymentAttempts",
                columns: new[] { "TenantId", "PaymentId" });

            migrationBuilder.CreateIndex(
                name: "IX_OnlinePaymentAttempts_TenantId_Reference",
                schema: "billing",
                table: "OnlinePaymentAttempts",
                columns: new[] { "TenantId", "Reference" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_OnlinePaymentAttempts_TenantId_Status",
                schema: "billing",
                table: "OnlinePaymentAttempts",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentGatewaySettings_TenantId",
                schema: "billing",
                table: "PaymentGatewaySettings",
                column: "TenantId",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OnlinePaymentAttempts",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "PaymentGatewaySettings",
                schema: "billing");
        }
    }
}
