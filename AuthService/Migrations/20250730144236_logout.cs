using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.Migrations
{
    /// <inheritdoc />
    public partial class logout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RefreshToken",
                table: "Users",
                newName: "RefreshTokenHash");

            migrationBuilder.AddColumn<DateTime>(
                name: "RevokedOn",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RevokedOn",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "RefreshTokenHash",
                table: "Users",
                newName: "RefreshToken");
        }
    }
}
