using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shifa.Scheduling.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "scheduling");

            migrationBuilder.CreateSequence(
                name: "EntityFrameworkHiLoSequence",
                schema: "scheduling",
                incrementBy: 10);

            migrationBuilder.CreateTable(
                name: "Appointments",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    ServiceId = table.Column<long>(type: "bigint", nullable: false),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    StartAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    QueueNumber = table.Column<int>(type: "int", nullable: true),
                    CheckedInAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModificationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.UniqueConstraint("AK_Appointments_TenantId_Id", x => new { x.TenantId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "IdempotencyRecords",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ResultId = table.Column<long>(type: "bigint", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModificationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecords", x => x.Id);
                    table.UniqueConstraint("AK_IdempotencyRecords_TenantId_Id", x => new { x.TenantId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "AppointmentSlots",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    AppointmentId = table.Column<long>(type: "bigint", nullable: false),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    SlotStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModificationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentSlots", x => x.Id);
                    table.UniqueConstraint("AK_AppointmentSlots_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_AppointmentSlots_Appointments_TenantId_AppointmentId",
                        columns: x => new { x.TenantId, x.AppointmentId },
                        principalSchema: "scheduling",
                        principalTable: "Appointments",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentStatusHistory",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    AppointmentId = table.Column<long>(type: "bigint", nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ToStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModificationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentStatusHistory", x => x.Id);
                    table.UniqueConstraint("AK_AppointmentStatusHistory_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_AppointmentStatusHistory_Appointments_TenantId_AppointmentId",
                        columns: x => new { x.TenantId, x.AppointmentId },
                        principalSchema: "scheduling",
                        principalTable: "Appointments",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PatientDoctorDay",
                schema: "scheduling",
                table: "Appointments",
                columns: new[] { "TenantId", "PatientId", "DoctorId", "LocalDate" },
                unique: true,
                filter: "[Status] <> 'Cancelled' AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_Queue",
                schema: "scheduling",
                table: "Appointments",
                columns: new[] { "TenantId", "BranchId", "DoctorId", "LocalDate", "QueueNumber" },
                unique: true,
                filter: "[QueueNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_TenantId_BranchId_StartAt",
                schema: "scheduling",
                table: "Appointments",
                columns: new[] { "TenantId", "BranchId", "StartAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_TenantId_DoctorId_StartAt",
                schema: "scheduling",
                table: "Appointments",
                columns: new[] { "TenantId", "DoctorId", "StartAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_TenantId_PatientId_StartAt",
                schema: "scheduling",
                table: "Appointments",
                columns: new[] { "TenantId", "PatientId", "StartAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentSlots_TenantId_AppointmentId",
                schema: "scheduling",
                table: "AppointmentSlots",
                columns: new[] { "TenantId", "AppointmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentSlots_Tick",
                schema: "scheduling",
                table: "AppointmentSlots",
                columns: new[] { "TenantId", "DoctorId", "SlotStart" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentStatusHistory_TenantId_AppointmentId",
                schema: "scheduling",
                table: "AppointmentStatusHistory",
                columns: new[] { "TenantId", "AppointmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_Key",
                schema: "scheduling",
                table: "IdempotencyRecords",
                columns: new[] { "TenantId", "Operation", "Key" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentSlots",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "AppointmentStatusHistory",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "IdempotencyRecords",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "Appointments",
                schema: "scheduling");

            migrationBuilder.DropSequence(
                name: "EntityFrameworkHiLoSequence",
                schema: "scheduling");
        }
    }
}
