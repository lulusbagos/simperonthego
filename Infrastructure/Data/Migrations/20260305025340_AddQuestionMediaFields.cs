using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimperSecureOnlineTestSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionMediaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "tbl_m_question",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "video_url",
                table: "tbl_m_question",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_url",
                table: "tbl_m_question");

            migrationBuilder.DropColumn(
                name: "video_url",
                table: "tbl_m_question");
        }
    }
}
