using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimperSecureOnlineTestSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExamAccessPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "access_password_hash",
                table: "tbl_t_exam_session",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "access_password_hash",
                table: "tbl_t_exam_session");
        }
    }
}
