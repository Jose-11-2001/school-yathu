using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace school_yathu.Migrations
{
    /// <inheritdoc />
    public partial class YourMigrationName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Users_TeacherId",
                table: "Classes");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Users_UserId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentSubjects_Users_TeacherId",
                table: "StudentSubjects");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherSubjects_Users_TeacherId",
                table: "TeacherSubjects");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_TeacherId",
                table: "Classes",
                column: "TeacherId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_UserId",
                table: "Notifications",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentSubjects_TeacherId",
                table: "StudentSubjects",
                column: "TeacherId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherSubjects_TeacherId",
                table: "TeacherSubjects",
                column: "TeacherId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_TeacherId",
                table: "Classes");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_UserId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentSubjects_TeacherId",
                table: "StudentSubjects");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherSubjects_TeacherId",
                table: "TeacherSubjects");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Users_TeacherId",
                table: "Classes",
                column: "TeacherId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Users_UserId",
                table: "Notifications",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentSubjects_Users_TeacherId",
                table: "StudentSubjects",
                column: "TeacherId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherSubjects_Users_TeacherId",
                table: "TeacherSubjects",
                column: "TeacherId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
