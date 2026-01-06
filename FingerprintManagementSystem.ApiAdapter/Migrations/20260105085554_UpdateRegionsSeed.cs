using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FingerprintManagementSystem.ApiAdapter.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRegionsSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Regions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Regions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TerminalRegionMaps",
                columns: table => new
                {
                    TerminalId = table.Column<string>(type: "TEXT", nullable: false),
                    RegionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TerminalRegionMaps", x => x.TerminalId);
                    table.ForeignKey(
                        name: "FK_TerminalRegionMaps_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Regions",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "المبنى الرئيسي" },
                    { 2, "المطلاع" },
                    { 3, "برج التحرير" },
                    { 4, "صباح السالم" },
                    { 5, "الجهراء - حكومة مول" },
                    { 6, "الجهراء - تيماء" },
                    { 7, "جابر الأحمد" },
                    { 8, "سعد العبدالله" },
                    { 9, "الصليبية" },
                    { 10, "القرين - حكومة مول" },
                    { 11, "مبارك الكبير" },
                    { 12, "النهضة" },
                    { 13, "غرب الجليب" },
                    { 14, "مواقع أخرى" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TerminalRegionMaps_RegionId",
                table: "TerminalRegionMaps",
                column: "RegionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TerminalRegionMaps");

            migrationBuilder.DropTable(
                name: "Regions");
        }
    }
}
