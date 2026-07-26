using System;
using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations;

[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20260725150000_AddChatSessionControl")]
public partial class AddChatSessionControl : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ApplicationVersion",
            table: "ChatMemoryConversations",
            type: "TEXT",
            maxLength: 120,
            nullable: false,
            defaultValue: "legacy");

        migrationBuilder.AddColumn<Guid>(
            name: "ProjectId",
            table: "ChatMemoryConversations",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "ProjectVersionId",
            table: "ChatMemoryConversations",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "FeedbackComment",
            table: "ChatMemoryMessages",
            type: "TEXT",
            maxLength: 4000,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<DateTime>(
            name: "FeedbackUpdatedAtUtc",
            table: "ChatMemoryMessages",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsPositiveFeedback",
            table: "ChatMemoryMessages",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_ChatMemoryConversations_ProjectId_ProjectVersionId_UpdatedAtUtc",
            table: "ChatMemoryConversations",
            columns: new[] { "ProjectId", "ProjectVersionId", "UpdatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_ChatMemoryMessages_IsPositiveFeedback_FeedbackUpdatedAtUtc",
            table: "ChatMemoryMessages",
            columns: new[] { "IsPositiveFeedback", "FeedbackUpdatedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ChatMemoryConversations_ProjectId_ProjectVersionId_UpdatedAtUtc",
            table: "ChatMemoryConversations");

        migrationBuilder.DropIndex(
            name: "IX_ChatMemoryMessages_IsPositiveFeedback_FeedbackUpdatedAtUtc",
            table: "ChatMemoryMessages");

        migrationBuilder.DropColumn(name: "ApplicationVersion", table: "ChatMemoryConversations");
        migrationBuilder.DropColumn(name: "ProjectId", table: "ChatMemoryConversations");
        migrationBuilder.DropColumn(name: "ProjectVersionId", table: "ChatMemoryConversations");
        migrationBuilder.DropColumn(name: "FeedbackComment", table: "ChatMemoryMessages");
        migrationBuilder.DropColumn(name: "FeedbackUpdatedAtUtc", table: "ChatMemoryMessages");
        migrationBuilder.DropColumn(name: "IsPositiveFeedback", table: "ChatMemoryMessages");
    }
}
