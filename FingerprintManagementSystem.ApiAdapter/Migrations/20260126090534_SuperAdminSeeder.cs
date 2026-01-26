using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FingerprintManagementSystem.ApiAdapter.Migrations
{
    /// <inheritdoc />
    public partial class SuperAdminSeeder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AllowedUsers",
                columns: new[] { "Id", "Department", "Email", "EmployeeId", "FullName", "IsActive", "IsAdmin", "ValidUntil" },
                values: new object[] { 1, "", "admin@admin.com", 123456789, "System Admin", true, true, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AllowedUsers",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
