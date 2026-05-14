using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimperSecureOnlineTestSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExamSessionRefId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ref_id",
                table: "tbl_t_exam_session",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE tbl_t_exam_session
                SET ref_id = CONCAT('REF', UPPER(SUBSTRING(MD5(token) FROM 1 FOR 12)))
                WHERE ref_id IS NULL OR ref_id = '';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ref_id",
                table: "tbl_t_exam_session",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_t_exam_session_ref_id",
                table: "tbl_t_exam_session",
                column: "ref_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tbl_t_exam_session_ref_id",
                table: "tbl_t_exam_session");

            migrationBuilder.DropColumn(
                name: "ref_id",
                table: "tbl_t_exam_session");
        }
    }
}
