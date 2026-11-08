
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeSuKienWeb.Migrations
{
    public partial class ThemThongTinHoanTienDonHang : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NgayHoan",
                table: "DonHang",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SoTienHoan",
                table: "DonHang",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TrangThaiHoan",
                table: "DonHang",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NgayHoan",
                table: "DonHang");

            migrationBuilder.DropColumn(
                name: "SoTienHoan",
                table: "DonHang");

            migrationBuilder.DropColumn(
                name: "TrangThaiHoan",
                table: "DonHang");
        }
    }
}
