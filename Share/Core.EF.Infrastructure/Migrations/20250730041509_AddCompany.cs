using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.EF.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyID",
                table: "SysUserRole",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyID",
                table: "SysUser",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyID",
                table: "SysRoleMenuAction",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyID",
                table: "SysRole",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyID",
                table: "SysRefreshToken",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyID",
                table: "SysMenuAction",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyID",
                table: "SysMenu",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyID",
                table: "SysCounter",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyID",
                table: "SysUserRole");

            migrationBuilder.DropColumn(
                name: "CompanyID",
                table: "SysUser");

            migrationBuilder.DropColumn(
                name: "CompanyID",
                table: "SysRoleMenuAction");

            migrationBuilder.DropColumn(
                name: "CompanyID",
                table: "SysRole");

            migrationBuilder.DropColumn(
                name: "CompanyID",
                table: "SysRefreshToken");

            migrationBuilder.DropColumn(
                name: "CompanyID",
                table: "SysMenuAction");

            migrationBuilder.DropColumn(
                name: "CompanyID",
                table: "SysMenu");

            migrationBuilder.DropColumn(
                name: "CompanyID",
                table: "SysCounter");
        }
    }
}
