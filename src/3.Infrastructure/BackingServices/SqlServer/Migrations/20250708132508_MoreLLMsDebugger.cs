using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentAI.Infrastructure.BackingServices.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class MoreLLMsDebugger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LLMsInput",
                table: "ChatHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LLMsInputToken",
                table: "ChatHistories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LLMsOutputToken",
                table: "ChatHistories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LLMsPercentAccuracyByAI",
                table: "ChatHistories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LLMsPercentAccuracyByHuman",
                table: "ChatHistories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LLMsProcessingTime",
                table: "ChatHistories",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LLMsInput",
                table: "ChatHistories");

            migrationBuilder.DropColumn(
                name: "LLMsInputToken",
                table: "ChatHistories");

            migrationBuilder.DropColumn(
                name: "LLMsOutputToken",
                table: "ChatHistories");

            migrationBuilder.DropColumn(
                name: "LLMsPercentAccuracyByAI",
                table: "ChatHistories");

            migrationBuilder.DropColumn(
                name: "LLMsPercentAccuracyByHuman",
                table: "ChatHistories");

            migrationBuilder.DropColumn(
                name: "LLMsProcessingTime",
                table: "ChatHistories");
        }
    }
}
