using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FingerprintManagementSystem.ApiAdapter.Migrations
{
    /// <inheritdoc />
    public partial class AddWasAssignedBeforeToDelegationTerminals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WasAssignedBefore",
                table: "DelegationTerminals",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WasAssignedBefore",
                table: "DelegationTerminals");
        }
    }
}
