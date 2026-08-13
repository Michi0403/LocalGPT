using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations;

/// <summary>
/// Defines the Entity Framework Core migration AddReusableHumanApprovalDecisions, applying and reverting the schema changes represented by this versioned database step.
/// </summary>
[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20260803090000_AddReusableHumanApprovalDecisions")]
public partial class AddReusableHumanApprovalDecisions : Migration
{
    /// <summary>
    /// Applies the schema changes defined by the <see cref="AddReusableHumanApprovalDecisions"/> Entity Framework Core migration to move the database forward.
    /// </summary>
    /// <param name="migrationBuilder">Migration builder value supplied to the add reusable human approval decisions operation and used when producing its result.</param>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "ApprovalReuseScope",
            table: "HumanCollaborationRequests",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<Guid>(
            name: "ApprovalSessionId",
            table: "HumanCollaborationRequests",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "ConsumeApproval",
            table: "HumanCollaborationRequests",
            type: "INTEGER",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<int>(
            name: "DecisionVersion",
            table: "HumanCollaborationRequests",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateIndex(
            name: "IX_HumanCollaborationRequests_OperationKey_ParameterFingerprint_Status_UpdatedAtUtc",
            table: "HumanCollaborationRequests",
            columns: new[] { "OperationKey", "ParameterFingerprint", "Status", "UpdatedAtUtc" });
    }

    /// <summary>
    /// Reverts the schema changes defined by the <see cref="AddReusableHumanApprovalDecisions"/> Entity Framework Core migration to return the database to its preceding shape.
    /// </summary>
    /// <param name="migrationBuilder">Migration builder value supplied to the add reusable human approval decisions operation and used when producing its result.</param>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_HumanCollaborationRequests_OperationKey_ParameterFingerprint_Status_UpdatedAtUtc",
            table: "HumanCollaborationRequests");

        migrationBuilder.DropColumn(name: "ApprovalReuseScope", table: "HumanCollaborationRequests");
        migrationBuilder.DropColumn(name: "ApprovalSessionId", table: "HumanCollaborationRequests");
        migrationBuilder.DropColumn(name: "ConsumeApproval", table: "HumanCollaborationRequests");
        migrationBuilder.DropColumn(name: "DecisionVersion", table: "HumanCollaborationRequests");
    }
}
