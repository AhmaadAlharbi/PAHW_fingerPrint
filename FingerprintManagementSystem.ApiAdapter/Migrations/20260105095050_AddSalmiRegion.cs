using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FingerprintManagementSystem.ApiAdapter.Migrations
{
    /// <inheritdoc />
    public partial class AddSalmiRegion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Regions",
                columns: new[] { "Id", "Name" },
                values: new object[] { 15, "السالمي" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Regions",
                keyColumn: "Id",
                keyValue: 15);
        }
    }
}
