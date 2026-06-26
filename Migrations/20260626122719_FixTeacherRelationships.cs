using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace school_yathu.Migrations
{
    /// <inheritdoc />
    public partial class FixTeacherRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassSubjects_Users_TeacherId",
                table: "ClassSubjects");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentSubjects_TeacherId",
                table: "StudentSubjects");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSubjects_Users_TeacherId",
                table: "ClassSubjects",
                column: "TeacherId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentSubjects_Users_TeacherId",
                table: "StudentSubjects",
                column: "TeacherId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassSubjects_Users_TeacherId",
                table: "ClassSubjects");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentSubjects_Users_TeacherId",
                table: "StudentSubjects");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSubjects_Users_TeacherId",
                table: "ClassSubjects",
                column: "TeacherId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentSubjects_TeacherId",
                table: "StudentSubjects",
                column: "TeacherId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
