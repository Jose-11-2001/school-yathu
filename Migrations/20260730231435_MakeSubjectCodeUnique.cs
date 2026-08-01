using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace school_yathu.Migrations
{
    /// <inheritdoc />
    public partial class MakeSubjectCodeUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SecondaryRoles",
                table: "Users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecondaryRoles",
                table: "Users");
        }
    }
}
