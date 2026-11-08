
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeSuKienWeb.Migrations
{
    /// <inheritdoc />
    public partial class ThemHeThongGoiYSuKien : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChuDeSuKien",
                columns: table => new
                {
                    ChuDeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    TenChuDe = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: false),

                    MoTa = table.Column<string>(
                        type: "nvarchar(500)",
                        maxLength: 500,
                        nullable: true),

                    TrangThai = table.Column<bool>(
                        type: "bit",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChuDeSuKien", x => x.ChuDeId);
                });

            migrationBuilder.CreateTable(
                name: "HoSoSoThich",
                columns: table => new
                {
                    HoSoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    NguoiDungId = table.Column<int>(
                        type: "int",
                        nullable: false),

                    DiaDiemUuTien = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: true),

                    HinhThucUuTien = table.Column<string>(
                        type: "nvarchar(30)",
                        maxLength: 30,
                        nullable: false),

                    DaHoanThanhKhaoSat = table.Column<bool>(
                        type: "bit",
                        nullable: false),

                    NgayCapNhat = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoSoSoThich", x => x.HoSoId);

                    table.ForeignKey(
                        name: "FK_HoSoSoThich_NguoiDung_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDung",
                        principalColumn: "NguoiDungId");
                });

            migrationBuilder.CreateTable(
                name: "NguoiDungSoThich",
                columns: table => new
                {
                    NguoiDungId = table.Column<int>(
                        type: "int",
                        nullable: false),

                    ChuDeId = table.Column<int>(
                        type: "int",
                        nullable: false),

                    NgayChon = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_NguoiDungSoThich",
                        x => new { x.NguoiDungId, x.ChuDeId });

                    table.ForeignKey(
                        name: "FK_NguoiDungSoThich_NguoiDung_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDung",
                        principalColumn: "NguoiDungId");

                    table.ForeignKey(
                        name: "FK_NguoiDungSoThich_ChuDeSuKien_ChuDeId",
                        column: x => x.ChuDeId,
                        principalTable: "ChuDeSuKien",
                        principalColumn: "ChuDeId");
                });

            migrationBuilder.CreateTable(
                name: "LichSuXemSuKien",
                columns: table => new
                {
                    LichSuId = table.Column<int>(
                        type: "int",
                        nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    NguoiDungId = table.Column<int>(
                        type: "int",
                        nullable: false),

                    SuKienId = table.Column<int>(
                        type: "int",
                        nullable: false),

                    ThoiGianXem = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_LichSuXemSuKien",
                        x => x.LichSuId);

                    table.ForeignKey(
                        name: "FK_LichSuXemSuKien_NguoiDung_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDung",
                        principalColumn: "NguoiDungId");

                    table.ForeignKey(
                        name: "FK_LichSuXemSuKien_SuKien_SuKienId",
                        column: x => x.SuKienId,
                        principalTable: "SuKien",
                        principalColumn: "SuKienId");
                });

            migrationBuilder.CreateTable(
                name: "SuKienChuDe",
                columns: table => new
                {
                    SuKienId = table.Column<int>(
                        type: "int",
                        nullable: false),

                    ChuDeId = table.Column<int>(
                        type: "int",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_SuKienChuDe",
                        x => new { x.SuKienId, x.ChuDeId });

                    table.ForeignKey(
                        name: "FK_SuKienChuDe_SuKien_SuKienId",
                        column: x => x.SuKienId,
                        principalTable: "SuKien",
                        principalColumn: "SuKienId");

                    table.ForeignKey(
                        name: "FK_SuKienChuDe_ChuDeSuKien_ChuDeId",
                        column: x => x.ChuDeId,
                        principalTable: "ChuDeSuKien",
                        principalColumn: "ChuDeId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChuDeSuKien_TenChuDe",
                table: "ChuDeSuKien",
                column: "TenChuDe",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HoSoSoThich_NguoiDungId",
                table: "HoSoSoThich",
                column: "NguoiDungId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDungSoThich_ChuDeId",
                table: "NguoiDungSoThich",
                column: "ChuDeId");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuXemSuKien_NguoiDungId_SuKienId",
                table: "LichSuXemSuKien",
                columns: new[] { "NguoiDungId", "SuKienId" });

            migrationBuilder.CreateIndex(
                name: "IX_LichSuXemSuKien_SuKienId",
                table: "LichSuXemSuKien",
                column: "SuKienId");

            migrationBuilder.CreateIndex(
                name: "IX_SuKienChuDe_ChuDeId",
                table: "SuKienChuDe",
                column: "ChuDeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HoSoSoThich");

            migrationBuilder.DropTable(
                name: "LichSuXemSuKien");

            migrationBuilder.DropTable(
                name: "NguoiDungSoThich");

            migrationBuilder.DropTable(
                name: "SuKienChuDe");

            migrationBuilder.DropTable(
                name: "ChuDeSuKien");
        }
    }
}
