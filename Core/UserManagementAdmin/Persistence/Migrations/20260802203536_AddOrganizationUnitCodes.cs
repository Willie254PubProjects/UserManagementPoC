using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserManagementAdmin.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationUnitCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "OrganizationUnits",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UnitCode",
                table: "OrganizationUnits",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "OrganizationUnits");

            migrationBuilder.DropColumn(
                name: "UnitCode",
                table: "OrganizationUnits");
        }
    }
}
