using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations.Clinic
{
    /// <inheritdoc />
    public partial class AddDoctorMembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "MembershipId",
                schema: "clinic",
                table: "Doctors",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_TenantId_MembershipId",
                schema: "clinic",
                table: "Doctors",
                columns: new[] { "TenantId", "MembershipId" },
                unique: true,
                filter: "[MembershipId] IS NOT NULL AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Doctors_TenantId_MembershipId",
                schema: "clinic",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "MembershipId",
                schema: "clinic",
                table: "Doctors");
        }
    }
}
