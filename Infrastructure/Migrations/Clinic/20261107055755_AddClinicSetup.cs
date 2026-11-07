using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations.Clinic
{
    /// <inheritdoc />
    public partial class AddClinicSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Doctors",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    FullNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FullNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModificationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doctors", x => x.Id);
                    table.UniqueConstraint("AK_Doctors_TenantId_Id", x => new { x.TenantId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "Specialties",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModificationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specialties", x => x.Id);
                    table.UniqueConstraint("AK_Specialties_TenantId_Id", x => new { x.TenantId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "DoctorBranches",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    CreationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModificationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorBranches", x => x.Id);
                    table.UniqueConstraint("AK_DoctorBranches_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_DoctorBranches_Branches_TenantId_BranchId",
                        columns: x => new { x.TenantId, x.BranchId },
                        principalSchema: "clinic",
                        principalTable: "Branches",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DoctorBranches_Doctors_TenantId_DoctorId",
                        columns: x => new { x.TenantId, x.DoctorId },
                        principalSchema: "clinic",
                        principalTable: "Doctors",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DoctorTimeOffs",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    FromAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ToAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
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
                    table.PrimaryKey("PK_DoctorTimeOffs", x => x.Id);
                    table.UniqueConstraint("AK_DoctorTimeOffs_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_DoctorTimeOffs_Doctors_TenantId_DoctorId",
                        columns: x => new { x.TenantId, x.DoctorId },
                        principalSchema: "clinic",
                        principalTable: "Doctors",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DoctorSpecialties",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    SpecialtyId = table.Column<long>(type: "bigint", nullable: false),
                    CreationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModificationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorSpecialties", x => x.Id);
                    table.UniqueConstraint("AK_DoctorSpecialties_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_DoctorSpecialties_Doctors_TenantId_DoctorId",
                        columns: x => new { x.TenantId, x.DoctorId },
                        principalSchema: "clinic",
                        principalTable: "Doctors",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DoctorSpecialties_Specialties_TenantId_SpecialtyId",
                        columns: x => new { x.TenantId, x.SpecialtyId },
                        principalSchema: "clinic",
                        principalTable: "Specialties",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    SpecialtyId = table.Column<long>(type: "bigint", nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModificationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                    table.UniqueConstraint("AK_Services_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Services_Specialties_TenantId_SpecialtyId",
                        columns: x => new { x.TenantId, x.SpecialtyId },
                        principalSchema: "clinic",
                        principalTable: "Specialties",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DoctorSchedules",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    DoctorBranchId = table.Column<long>(type: "bigint", nullable: false),
                    DayOfWeek = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    SlotMinutes = table.Column<int>(type: "int", nullable: false),
                    CreationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModificationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorSchedules", x => x.Id);
                    table.UniqueConstraint("AK_DoctorSchedules_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_DoctorSchedules_DoctorBranches_TenantId_DoctorBranchId",
                        columns: x => new { x.TenantId, x.DoctorBranchId },
                        principalSchema: "clinic",
                        principalTable: "DoctorBranches",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServicePrices",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ServiceId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModificationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePrices", x => x.Id);
                    table.UniqueConstraint("AK_ServicePrices_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ServicePrices_Branches_TenantId_BranchId",
                        columns: x => new { x.TenantId, x.BranchId },
                        principalSchema: "clinic",
                        principalTable: "Branches",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServicePrices_Services_TenantId_ServiceId",
                        columns: x => new { x.TenantId, x.ServiceId },
                        principalSchema: "clinic",
                        principalTable: "Services",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorBranches_TenantId_BranchId",
                schema: "clinic",
                table: "DoctorBranches",
                columns: new[] { "TenantId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorBranches_TenantId_DoctorId_BranchId",
                schema: "clinic",
                table: "DoctorBranches",
                columns: new[] { "TenantId", "DoctorId", "BranchId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorSchedules_TenantId_DoctorBranchId_DayOfWeek",
                schema: "clinic",
                table: "DoctorSchedules",
                columns: new[] { "TenantId", "DoctorBranchId", "DayOfWeek" });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorSpecialties_TenantId_DoctorId_SpecialtyId",
                schema: "clinic",
                table: "DoctorSpecialties",
                columns: new[] { "TenantId", "DoctorId", "SpecialtyId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorSpecialties_TenantId_SpecialtyId",
                schema: "clinic",
                table: "DoctorSpecialties",
                columns: new[] { "TenantId", "SpecialtyId" });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorTimeOffs_TenantId_DoctorId_FromAt",
                schema: "clinic",
                table: "DoctorTimeOffs",
                columns: new[] { "TenantId", "DoctorId", "FromAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ServicePrices_TenantId_BranchId",
                schema: "clinic",
                table: "ServicePrices",
                columns: new[] { "TenantId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_ServicePrices_TenantId_ServiceId_BranchId",
                schema: "clinic",
                table: "ServicePrices",
                columns: new[] { "TenantId", "ServiceId", "BranchId" },
                unique: true,
                filter: "[EffectiveTo] IS NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Services_TenantId_SpecialtyId_NameEn",
                schema: "clinic",
                table: "Services",
                columns: new[] { "TenantId", "SpecialtyId", "NameEn" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Specialties_TenantId_NameEn",
                schema: "clinic",
                table: "Specialties",
                columns: new[] { "TenantId", "NameEn" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoctorSchedules",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "DoctorSpecialties",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "DoctorTimeOffs",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "ServicePrices",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "DoctorBranches",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "Services",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "Doctors",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "Specialties",
                schema: "clinic");
        }
    }
}
