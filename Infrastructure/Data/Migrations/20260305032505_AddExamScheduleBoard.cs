using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SimperSecureOnlineTestSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExamScheduleBoard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_t_exam_schedule",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<long>(type: "bigint", nullable: false),
                    vehicle_id = table.Column<long>(type: "bigint", nullable: false),
                    scheduled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_t_exam_schedule", x => x.id);
                    table.ForeignKey(
                        name: "FK_tbl_t_exam_schedule_tbl_m_employee_employee_id",
                        column: x => x.employee_id,
                        principalTable: "tbl_m_employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_t_exam_schedule_tbl_m_user_login_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "tbl_m_user_login",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_tbl_t_exam_schedule_tbl_m_vehicle_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "tbl_m_vehicle",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_t_exam_schedule_created_by_user_id",
                table: "tbl_t_exam_schedule",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_t_exam_schedule_employee_id_vehicle_id_scheduled_at",
                table: "tbl_t_exam_schedule",
                columns: new[] { "employee_id", "vehicle_id", "scheduled_at" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_t_exam_schedule_vehicle_id_scheduled_at",
                table: "tbl_t_exam_schedule",
                columns: new[] { "vehicle_id", "scheduled_at" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_t_exam_schedule");
        }
    }
}
