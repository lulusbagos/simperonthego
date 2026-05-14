using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SimperSecureOnlineTestSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPracticalAssessmentModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tbl_t_exam_schedule_vehicle_id_scheduled_at",
                table: "tbl_t_exam_schedule");

            migrationBuilder.CreateTable(
                name: "tbl_m_practical_template",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    vehicle_id = table.Column<long>(type: "bigint", nullable: false),
                    template_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    scoring_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    passing_score = table.Column<decimal>(type: "numeric", nullable: true),
                    passing_grade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    grade_options = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_m_practical_template", x => x.id);
                    table.ForeignKey(
                        name: "FK_tbl_m_practical_template_tbl_m_company_company_id",
                        column: x => x.company_id,
                        principalTable: "tbl_m_company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_m_practical_template_tbl_m_vehicle_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "tbl_m_vehicle",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_m_practical_template_item",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    template_id = table.Column<long>(type: "bigint", nullable: false),
                    section_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    item_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    weight = table.Column<decimal>(type: "numeric", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_m_practical_template_item", x => x.id);
                    table.ForeignKey(
                        name: "FK_tbl_m_practical_template_item_tbl_m_practical_template_temp~",
                        column: x => x.template_id,
                        principalTable: "tbl_m_practical_template",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_t_practical_assessment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<long>(type: "bigint", nullable: false),
                    vehicle_id = table.Column<long>(type: "bigint", nullable: false),
                    template_id = table.Column<long>(type: "bigint", nullable: false),
                    instructor_user_id = table.Column<long>(type: "bigint", nullable: false),
                    scheduled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    final_numeric_score = table.Column<decimal>(type: "numeric", nullable: true),
                    final_grade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    pass_status = table.Column<bool>(type: "boolean", nullable: true),
                    instructor_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_t_practical_assessment", x => x.id);
                    table.ForeignKey(
                        name: "FK_tbl_t_practical_assessment_tbl_m_employee_employee_id",
                        column: x => x.employee_id,
                        principalTable: "tbl_m_employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_t_practical_assessment_tbl_m_practical_template_templat~",
                        column: x => x.template_id,
                        principalTable: "tbl_m_practical_template",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_t_practical_assessment_tbl_m_user_login_created_by_user~",
                        column: x => x.created_by_user_id,
                        principalTable: "tbl_m_user_login",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_tbl_t_practical_assessment_tbl_m_user_login_instructor_user~",
                        column: x => x.instructor_user_id,
                        principalTable: "tbl_m_user_login",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_t_practical_assessment_tbl_m_vehicle_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "tbl_m_vehicle",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_t_practical_assessment_score",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    session_id = table.Column<long>(type: "bigint", nullable: false),
                    template_item_id = table.Column<long>(type: "bigint", nullable: false),
                    numeric_value = table.Column<decimal>(type: "numeric", nullable: true),
                    grade_value = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_t_practical_assessment_score", x => x.id);
                    table.ForeignKey(
                        name: "FK_tbl_t_practical_assessment_score_tbl_m_practical_template_i~",
                        column: x => x.template_item_id,
                        principalTable: "tbl_m_practical_template_item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_t_practical_assessment_score_tbl_t_practical_assessment~",
                        column: x => x.session_id,
                        principalTable: "tbl_t_practical_assessment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_t_exam_schedule_vehicle_id_scheduled_at",
                table: "tbl_t_exam_schedule",
                columns: new[] { "vehicle_id", "scheduled_at" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_m_practical_template_company_id_vehicle_id_is_active",
                table: "tbl_m_practical_template",
                columns: new[] { "company_id", "vehicle_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_m_practical_template_vehicle_id",
                table: "tbl_m_practical_template",
                column: "vehicle_id");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_m_practical_template_item_template_id_display_order",
                table: "tbl_m_practical_template_item",
                columns: new[] { "template_id", "display_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_t_practical_assessment_created_by_user_id",
                table: "tbl_t_practical_assessment",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_t_practical_assessment_employee_id_vehicle_id_scheduled~",
                table: "tbl_t_practical_assessment",
                columns: new[] { "employee_id", "vehicle_id", "scheduled_at" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_t_practical_assessment_instructor_user_id_scheduled_at",
                table: "tbl_t_practical_assessment",
                columns: new[] { "instructor_user_id", "scheduled_at" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_t_practical_assessment_template_id",
                table: "tbl_t_practical_assessment",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_t_practical_assessment_vehicle_id",
                table: "tbl_t_practical_assessment",
                column: "vehicle_id");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_t_practical_assessment_score_session_id_template_item_id",
                table: "tbl_t_practical_assessment_score",
                columns: new[] { "session_id", "template_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_t_practical_assessment_score_template_item_id",
                table: "tbl_t_practical_assessment_score",
                column: "template_item_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_t_practical_assessment_score");

            migrationBuilder.DropTable(
                name: "tbl_m_practical_template_item");

            migrationBuilder.DropTable(
                name: "tbl_t_practical_assessment");

            migrationBuilder.DropTable(
                name: "tbl_m_practical_template");

            migrationBuilder.DropIndex(
                name: "IX_tbl_t_exam_schedule_vehicle_id_scheduled_at",
                table: "tbl_t_exam_schedule");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_t_exam_schedule_vehicle_id_scheduled_at",
                table: "tbl_t_exam_schedule",
                columns: new[] { "vehicle_id", "scheduled_at" },
                unique: true);
        }
    }
}
