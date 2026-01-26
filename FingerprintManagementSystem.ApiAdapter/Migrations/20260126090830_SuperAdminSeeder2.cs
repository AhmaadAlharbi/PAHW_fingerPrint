using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FingerprintManagementSystem.ApiAdapter.Migrations
{
    /// <inheritdoc />
    public partial class SuperAdminSeeder2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AllowedUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EmployeeId", "FullName" },
                values: new object[] { 7300, "أحمد زيد الحربي" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AllowedUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EmployeeId", "FullName" },
                values: new object[] { 123456789, "System Admin" });
        }
    }
}
