using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechUnited_AiStudio.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeChunks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KnowledgeChunks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VectorJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeChunks", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEPuJB01MtiQr2E7eyk8BKdvKdNyoWFmTTJDtD4F/6wlmaGkW28Bmp42I8KOPsMUedQ==");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeChunks_FileName",
                table: "KnowledgeChunks",
                column: "FileName");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeChunks_UserId",
                table: "KnowledgeChunks",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KnowledgeChunks");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEGK9XNS9SfdoiwPTipie8SPUEj2i0YEiFlLYtuwFUyons7ybu3mQJU38jMONyqA3IA==");
        }
    }
}
