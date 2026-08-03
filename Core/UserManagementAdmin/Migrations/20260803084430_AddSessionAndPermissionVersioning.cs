using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserManagementAdmin.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionAndPermissionVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "UserSessions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdleTimeoutMinutes",
                table: "UserSessions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PermissionVersion",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "IdleTimeoutMinutes",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "PermissionVersion",
                table: "AspNetUsers");
        }
    }
}
