using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentAI.Infrastructure.BackingServices.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class Initiated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WebhookEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Processed = table.Column<bool>(type: "bit", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LineWebhookEventId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GroupId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReplyToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LineDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WebhookEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LatestChatSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReplyMode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_WebhookEvents_WebhookEventId",
                        column: x => x.WebhookEventId,
                        principalTable: "WebhookEvents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ChatSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WebhookEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReplyMode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatSessions_WebhookEvents_WebhookEventId",
                        column: x => x.WebhookEventId,
                        principalTable: "WebhookEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserReplyTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WebhookEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReplyToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReplyTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserReplyTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserReplyTokens_WebhookEvents_WebhookEventId",
                        column: x => x.WebhookEventId,
                        principalTable: "WebhookEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChatSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WebhookEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SenderType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MessageMode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatHistories_ChatSessions_ChatSessionId",
                        column: x => x.ChatSessionId,
                        principalTable: "ChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatHistories_ChatSessionId",
                table: "ChatHistories",
                column: "ChatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_UserId",
                table: "ChatSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_WebhookEventId",
                table: "ChatSessions",
                column: "WebhookEventId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReplyTokens_UserId",
                table: "UserReplyTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReplyTokens_WebhookEventId",
                table: "UserReplyTokens",
                column: "WebhookEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_WebhookEventId",
                table: "Users",
                column: "WebhookEventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatHistories");

            migrationBuilder.DropTable(
                name: "UserReplyTokens");

            migrationBuilder.DropTable(
                name: "ChatSessions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "WebhookEvents");
        }
    }
}
