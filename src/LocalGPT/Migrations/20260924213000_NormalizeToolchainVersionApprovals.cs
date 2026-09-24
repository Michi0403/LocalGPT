using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations;

/// <summary>Converts pending 4.8.8 toolchain knowledge-writing requests into one-click local version approvals.</summary>
[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20260924213000_NormalizeToolchainVersionApprovals")]
public partial class NormalizeToolchainVersionApprovals : Migration
{
    /// <summary>Normalizes pending exact-version requests without discarding the existing correlation identity.</summary>
    /// <param name="migrationBuilder">Migration builder used to update persisted Human Collaboration requests.</param>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE HumanCollaborationRequests
            SET RequestKind = 'Approval',
                Title = REPLACE(Title, 'Toolchain knowledge needed:', 'Trust locally detected toolchain:'),
                Description = 'LocalGPT already identified this exact toolchain version with a bounded local version probe. Approve if the detected version should be trusted by LocalGPT; decline if the result is unexpected. Writing technical documentation or free text is not required.',
                RequestedRole = 'Toolchain version approver',
                SuggestedResponsesText = '',
                ResponsePrompt = 'Approve this locally detected version or decline it if the probe result is unexpected.',
                PrefillText = '',
                AllowFreeText = 0,
                UpdatedAtUtc = CURRENT_TIMESTAMP
            WHERE OperationKey = 'toolchain.knowledge.request'
              AND Status = 'Pending'
              AND RequestKind <> 'Approval';
            """);
    }

    /// <summary>Restores the interaction kind when rolling back while retaining already persisted request content.</summary>
    /// <param name="migrationBuilder">Migration builder used to revert the persisted interaction kind.</param>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE HumanCollaborationRequests
            SET RequestKind = 'Guidance',
                RequestedRole = 'Toolchain knowledge provider',
                AllowFreeText = 1,
                UpdatedAtUtc = CURRENT_TIMESTAMP
            WHERE OperationKey = 'toolchain.knowledge.request'
              AND Status = 'Pending'
              AND RequestKind = 'Approval';
            """);
    }
}
