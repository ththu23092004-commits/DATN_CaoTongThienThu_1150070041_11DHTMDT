using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeSuKienWeb.Migrations
{
    /// <inheritdoc />
    public partial class ThemLichSuKhamPhaSuKien : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KhamPhaHoiThoai",
                columns: table => new
                {
                    HoiThoaiId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NguoiDungId = table.Column<int>(type: "int", nullable: false),
                    TieuDe = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NguCanhJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhamPhaHoiThoai", x => x.HoiThoaiId);
                    table.ForeignKey(
                        name: "FK_KhamPhaHoiThoai_NguoiDung_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDung",
                        principalColumn: "NguoiDungId");
                });

            migrationBuilder.CreateTable(
                name: "KhamPhaTinNhan",
                columns: table => new
                {
                    TinNhanId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoiThoaiId = table.Column<long>(type: "bigint", nullable: false),
                    VaiTro = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DuLieuKetQuaJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayGui = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhamPhaTinNhan", x => x.TinNhanId);
                    table.ForeignKey(
                        name: "FK_KhamPhaTinNhan_KhamPhaHoiThoai_HoiThoaiId",
                        column: x => x.HoiThoaiId,
                        principalTable: "KhamPhaHoiThoai",
                        principalColumn: "HoiThoaiId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KhamPhaHoiThoai_NguoiDungId_NgayCapNhat",
                table: "KhamPhaHoiThoai",
                columns: new[] { "NguoiDungId", "NgayCapNhat" });

            migrationBuilder.CreateIndex(
                name: "IX_KhamPhaTinNhan_HoiThoaiId_NgayGui",
                table: "KhamPhaTinNhan",
                columns: new[] { "HoiThoaiId", "NgayGui" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KhamPhaTinNhan");

            migrationBuilder.DropTable(
                name: "KhamPhaHoiThoai");
        }
    }
}
