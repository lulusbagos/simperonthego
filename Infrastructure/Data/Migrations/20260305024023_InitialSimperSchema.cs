using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SimperSecureOnlineTestSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialSimperSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_m_company",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_m_company", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_m_employee",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nrp = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    employee_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_m_employee", x => x.id);
                    table.ForeignKey(
                        name: "FK_tbl_m_employee_tbl_m_company_company_id",
                        column: x => x.company_id,
                        principalTable: "tbl_m_company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_m_vehicle",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    vehicle_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    simper_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_m_vehicle", x => x.id);
                    table.ForeignKey(
                        name: "FK_tbl_m_vehicle_tbl_m_company_company_id",
                        column: x => x.company_id,
                        principalTable: "tbl_m_company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_m_question",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    vehicle_id = table.Column<long>(type: "bigint", nullable: false),
                    question_text = table.Column<string>(type: "text", nullable: false),
                    option_a = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    option_b = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    option_c = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    option_d = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    correct_answer = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    difficulty = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_m_question", x => x.id);
                    table.ForeignKey(
                        name: "FK_tbl_m_question_tbl_m_company_company_id",
                        column: x => x.company_id,
                        principalTable: "tbl_m_company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_m_question_tbl_m_vehicle_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "tbl_m_vehicle",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_t_exam_session",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<long>(type: "bigint", nullable: false),
                    vehicle_id = table.Column<long>(type: "bigint", nullable: false),
                    token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    camera_active = table.Column<bool>(type: "boolean", nullable: false),
                    tab_switch_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_t_exam_session", x => x.id);
                    table.ForeignKey(
                        name: "FK_tbl_t_exam_session_tbl_m_employee_employee_id",
                        column: x => x.employee_id,
                        principalTable: "tbl_m_employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_t_exam_session_tbl_m_vehicle_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "tbl_m_vehicle",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_r_exam_question",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    session_id = table.Column<long>(type: "bigint", nullable: false),
                    question_id = table.Column<long>(type: "bigint", nullable: false),
                    question_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_r_exam_question", x => x.id);
                    table.ForeignKey(
                        name: "FK_tbl_r_exam_question_tbl_m_question_question_id",
                        column: x => x.question_id,
                        principalTable: "tbl_m_question",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_r_exam_question_tbl_t_exam_session_session_id",
                        column: x => x.session_id,
                        principalTable: "tbl_t_exam_session",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_t_exam_answer",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    session_id = table.Column<long>(type: "bigint", nullable: false),
                    question_id = table.Column<long>(type: "bigint", nullable: false),
                    selected_answer = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    is_correct = table.Column<bool>(type: "boolean", nullable: false),
                    answered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_t_exam_answer", x => x.id);
                    table.ForeignKey(
                        name: "FK_tbl_t_exam_answer_tbl_m_question_question_id",
                        column: x => x.question_id,
                        principalTable: "tbl_m_question",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_t_exam_answer_tbl_t_exam_session_session_id",
                        column: x => x.session_id,
                        principalTable: "tbl_t_exam_session",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_t_exam_log",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    session_id = table.Column<long>(type: "bigint", nullable: false),
                    log_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_t_exam_log", x => x.id);
                    table.ForeignKey(
                        name: "FK_tbl_t_exam_log_tbl_t_exam_session_session_id",
                        column: x => x.session_id,
                        principalTable: "tbl_t_exam_session",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_t_exam_result",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    session_id = table.Column<long>(type: "bigint", nullable: false),
                    total_questions = table.Column<int>(type: "integer", nullable: false),
                    correct_answers = table.Column<int>(type: "integer", nullable: false),
                    score = table.Column<decimal>(type: "numeric", nullable: false),
                    pass_status = table.Column<bool>(type: "boolean", nullable: false),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_t_exam_result", x => x.id);
                    table.ForeignKey(
                        name: "FK_tbl_t_exam_result_tbl_t_exam_session_session_id",
                        column: x => x.session_id,
                        principalTable: "tbl_t_exam_session",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_m_employee_company_id",
                table: "tbl_m_employee",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_m_employee_nrp",
                table: "tbl_m_employee",
                column: "nrp",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_m_question_company_id",
                table: "tbl_m_question",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_m_question_vehicle_id",
                table: "tbl_m_question",
                column: "vehicle_id");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_m_vehicle_company_id",
                table: "tbl_m_vehicle",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_r_exam_question_question_id",
                table: "tbl_r_exam_question",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_r_exam_question_session_id_question_id",
                table: "tbl_r_exam_question",
                columns: new[] { "session_id", "question_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_r_exam_question_session_id_question_order",
                table: "tbl_r_exam_question",
                columns: new[] { "session_id", "question_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_t_exam_answer_question_id",
                table: "tbl_t_exam_answer",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_t_exam_answer_session_id_question_id",
                table: "tbl_t_exam_answer",
                columns: new[] { "session_id", "question_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_t_exam_log_session_id",
                table: "tbl_t_exam_log",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_t_exam_result_session_id",
                table: "tbl_t_exam_result",
                column: "session_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_t_exam_session_employee_id",
                table: "tbl_t_exam_session",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_t_exam_session_token",
                table: "tbl_t_exam_session",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_t_exam_session_vehicle_id",
                table: "tbl_t_exam_session",
                column: "vehicle_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_r_exam_question");

            migrationBuilder.DropTable(
                name: "tbl_t_exam_answer");

            migrationBuilder.DropTable(
                name: "tbl_t_exam_log");

            migrationBuilder.DropTable(
                name: "tbl_t_exam_result");

            migrationBuilder.DropTable(
                name: "tbl_m_question");

            migrationBuilder.DropTable(
                name: "tbl_t_exam_session");

            migrationBuilder.DropTable(
                name: "tbl_m_employee");

            migrationBuilder.DropTable(
                name: "tbl_m_vehicle");

            migrationBuilder.DropTable(
                name: "tbl_m_company");
        }
    }
}
