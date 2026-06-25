using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Feedback360.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionCategories : Migration
    {
        private static readonly Guid SkillsId = Guid.Parse("aaaaaaaa-0001-0001-0001-000000000001");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuestionCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionCategories_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionCategories_CreatedById",
                table: "QuestionCategories",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionCategories_Name",
                table: "QuestionCategories",
                column: "Name",
                unique: true);

            migrationBuilder.Sql($"""
                INSERT INTO "QuestionCategories" ("Id", "Name", "CreatedById", "CreatedAt")
                SELECT '{SkillsId}', 'Skills', u."Id", NOW() AT TIME ZONE 'UTC'
                FROM "Users" u
                WHERE u."Role" = 1
                LIMIT 1;
                """);

            migrationBuilder.Sql($"""
                INSERT INTO "QuestionCategories" ("Id", "Name", "CreatedById", "CreatedAt")
                SELECT 'aaaaaaaa-0001-0001-0001-000000000002', 'Performance', u."Id", NOW() AT TIME ZONE 'UTC'
                FROM "Users" u
                WHERE u."Role" = 1
                LIMIT 1;
                """);

            migrationBuilder.Sql($"""
                INSERT INTO "QuestionCategories" ("Id", "Name", "CreatedById", "CreatedAt")
                SELECT 'aaaaaaaa-0001-0001-0001-000000000003', 'Leadership', u."Id", NOW() AT TIME ZONE 'UTC'
                FROM "Users" u
                WHERE u."Role" = 1
                LIMIT 1;
                """);

            migrationBuilder.Sql($"""
                INSERT INTO "QuestionCategories" ("Id", "Name", "CreatedById", "CreatedAt")
                SELECT 'aaaaaaaa-0001-0001-0001-000000000004', 'Teamwork', u."Id", NOW() AT TIME ZONE 'UTC'
                FROM "Users" u
                WHERE u."Role" = 1
                LIMIT 1;
                """);

            migrationBuilder.Sql($"""
                INSERT INTO "QuestionCategories" ("Id", "Name", "CreatedById", "CreatedAt")
                SELECT 'aaaaaaaa-0001-0001-0001-000000000005', 'Communication', u."Id", NOW() AT TIME ZONE 'UTC'
                FROM "Users" u
                WHERE u."Role" = 1
                LIMIT 1;
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "TemplateQuestions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "SurveyQuestions",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql($"""
                UPDATE "TemplateQuestions" SET "CategoryId" = '{SkillsId}' WHERE "CategoryId" IS NULL;
                UPDATE "SurveyQuestions" SET "CategoryId" = '{SkillsId}' WHERE "CategoryId" IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "TemplateQuestions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "SurveyQuestions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateQuestions_CategoryId",
                table: "TemplateQuestions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyQuestions_CategoryId",
                table: "SurveyQuestions",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_SurveyQuestions_QuestionCategories_CategoryId",
                table: "SurveyQuestions",
                column: "CategoryId",
                principalTable: "QuestionCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateQuestions_QuestionCategories_CategoryId",
                table: "TemplateQuestions",
                column: "CategoryId",
                principalTable: "QuestionCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SurveyQuestions_QuestionCategories_CategoryId",
                table: "SurveyQuestions");

            migrationBuilder.DropForeignKey(
                name: "FK_TemplateQuestions_QuestionCategories_CategoryId",
                table: "TemplateQuestions");

            migrationBuilder.DropTable(
                name: "QuestionCategories");

            migrationBuilder.DropIndex(
                name: "IX_TemplateQuestions_CategoryId",
                table: "TemplateQuestions");

            migrationBuilder.DropIndex(
                name: "IX_SurveyQuestions_CategoryId",
                table: "SurveyQuestions");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "TemplateQuestions");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "SurveyQuestions");
        }
    }
}
