using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Coordinates LocalGPT runtime policy seed behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
public sealed class LocalGptRuntimePolicySeedDataService : ILocalGptRuntimePolicySeedDataService
{
    /// <summary>
    /// Stores the internal seed state used by <see cref="LocalGptRuntimePolicySeedDataService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly LocalGptRuntimePolicySeedModel seed;
    /// <summary>
    /// Stores the logger used by <see cref="LocalGptRuntimePolicySeedDataService"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger<LocalGptRuntimePolicySeedDataService> logger;

    /// <summary>
    /// Initializes a new <see cref="LocalGptRuntimePolicySeedDataService"/> instance and captures the dependencies or initial state required by its LocalGPT runtime policy seed workflow.
    /// </summary>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    public LocalGptRuntimePolicySeedDataService(ILogger<LocalGptRuntimePolicySeedDataService> logger)
    {
        this.logger = logger;
        try
        {
            seed = new LocalGptRuntimePolicySeedModel
            {
                Values =
                [
                    new(LocalGptRuntimeValue.LocalGptCoreProjectId, nameof(LocalGptRuntimeValue.LocalGptCoreProjectId), "7f4d7b4a-b622-4d15-8e44-9dfae2aa6101", "System.Guid"),
                    new(LocalGptRuntimeValue.RegexTimeoutMilliseconds, nameof(LocalGptRuntimeValue.RegexTimeoutMilliseconds), "2000", "System.Int32"),
                    new(LocalGptRuntimeValue.LocalHumanProfileId, nameof(LocalGptRuntimeValue.LocalHumanProfileId), "55e37ae8-c481-4a89-9000-65041fc349f5", "System.Guid"),
                    new(LocalGptRuntimeValue.CommandPolicyAllowedDecision, nameof(LocalGptRuntimeValue.CommandPolicyAllowedDecision), "Allowed", "System.String"),
                    new(LocalGptRuntimeValue.CommandPolicyDeniedDecision, nameof(LocalGptRuntimeValue.CommandPolicyDeniedDecision), "Denied", "System.String"),
                    new(LocalGptRuntimeValue.CommandPolicyDeniedProfile, nameof(LocalGptRuntimeValue.CommandPolicyDeniedProfile), "Denied", "System.String"),
                    new(LocalGptRuntimeValue.DefaultGradleVersion, nameof(LocalGptRuntimeValue.DefaultGradleVersion), "8.14.2", "System.String"),
                    new(LocalGptRuntimeValue.DefaultMinecraftVersion, nameof(LocalGptRuntimeValue.DefaultMinecraftVersion), "26.1", "System.String"),
                    new(LocalGptRuntimeValue.DefaultJavaVersion, nameof(LocalGptRuntimeValue.DefaultJavaVersion), "25", "System.String"),
                    new(LocalGptRuntimeValue.FabricLoaderVersion, nameof(LocalGptRuntimeValue.FabricLoaderVersion), "0.16.9", "System.String"),
                    new(LocalGptRuntimeValue.MaxDxAiChatPromptCharacters, nameof(LocalGptRuntimeValue.MaxDxAiChatPromptCharacters), "2147483647", "System.Int32"),
                    new(LocalGptRuntimeValue.MaxVisiblePromptCharacters, nameof(LocalGptRuntimeValue.MaxVisiblePromptCharacters), "2147483647", "System.Int32"),
                    new(LocalGptRuntimeValue.DefaultOllamaUri, nameof(LocalGptRuntimeValue.DefaultOllamaUri), "http://localhost:11434", "System.String"),
                    new(LocalGptRuntimeValue.MaxParticipants, nameof(LocalGptRuntimeValue.MaxParticipants), "2147483647", "System.Int32"),
                    new(LocalGptRuntimeValue.DefaultMaxParallelModels, nameof(LocalGptRuntimeValue.DefaultMaxParallelModels), "1", "System.Int32"),
                    new(LocalGptRuntimeValue.DefaultHeavyModelGpuLayers, nameof(LocalGptRuntimeValue.DefaultHeavyModelGpuLayers), "20", "System.Int32"),
                    new(LocalGptRuntimeValue.MinContextTokens, nameof(LocalGptRuntimeValue.MinContextTokens), "2048", "System.Int32"),
                    new(LocalGptRuntimeValue.DefaultContextTokens, nameof(LocalGptRuntimeValue.DefaultContextTokens), "65536", "System.Int32"),
                    new(LocalGptRuntimeValue.MaxContextTokens, nameof(LocalGptRuntimeValue.MaxContextTokens), "262144", "System.Int32"),
                    new(LocalGptRuntimeValue.MinOutputTokens, nameof(LocalGptRuntimeValue.MinOutputTokens), "64", "System.Int32"),
                    new(LocalGptRuntimeValue.MaxOutputTokens, nameof(LocalGptRuntimeValue.MaxOutputTokens), "262144", "System.Int32"),
                    new(LocalGptRuntimeValue.MaxArtifactTextFileBytes, nameof(LocalGptRuntimeValue.MaxArtifactTextFileBytes), "2097152", "System.Int64"),
                    new(LocalGptRuntimeValue.MaxFiles, nameof(LocalGptRuntimeValue.MaxFiles), "2147483647", "System.Int32"),
                    new(LocalGptRuntimeValue.MaxSingleFileBytes, nameof(LocalGptRuntimeValue.MaxSingleFileBytes), "2147483647", "System.Int64"),
                    new(LocalGptRuntimeValue.MaxTotalFileBytes, nameof(LocalGptRuntimeValue.MaxTotalFileBytes), "2147483647", "System.Int64"),
                    new(LocalGptRuntimeValue.MaxZipEntries, nameof(LocalGptRuntimeValue.MaxZipEntries), "2147483647", "System.Int32"),
                    new(LocalGptRuntimeValue.MaxZipEntryBytes, nameof(LocalGptRuntimeValue.MaxZipEntryBytes), "2147483647", "System.Int64"),
                    new(LocalGptRuntimeValue.MaxExtractedBytes, nameof(LocalGptRuntimeValue.MaxExtractedBytes), "2147483647", "System.Int64"),
                    new(LocalGptRuntimeValue.MaxContextCharacters, nameof(LocalGptRuntimeValue.MaxContextCharacters), "2147483647", "System.Int32"),
                    new(LocalGptRuntimeValue.MaxExcerptCharactersPerFile, nameof(LocalGptRuntimeValue.MaxExcerptCharactersPerFile), "2147483647", "System.Int32"),
                    new(LocalGptRuntimeValue.MaxBinaryStringCharacters, nameof(LocalGptRuntimeValue.MaxBinaryStringCharacters), "2147483647", "System.Int32"),
                    new(LocalGptRuntimeValue.ContextOmission, nameof(LocalGptRuntimeValue.ContextOmission), "\n\n[...older context trimmed by LocalGPT to fit the local model context window...]\n\n", "System.String"),
                    new(LocalGptRuntimeValue.ShortContextOmission, nameof(LocalGptRuntimeValue.ShortContextOmission), "\n... truncated by LocalGPT upload workspace budget ...", "System.String"),
                    new(LocalGptRuntimeValue.LearnBaseFilePolicySummary, nameof(LocalGptRuntimeValue.LearnBaseFilePolicySummary), "Reads source and docs such as .cs, .razor, .csproj, .sln, .md, .yml, .json, .xml, .py, .js, .ts, .go, .ps1, and .sql. Skips build/cache folders such as bin, obj, node_modules, packages, .git, build, dist, and publish. Binary files, installers, archives, PDFs, certificates, SQLite files, and images are counted or ignored, not stored as knowledge text.", "System.String"),
                    new(LocalGptRuntimeValue.LearnBaseDuplicatePolicySummary, nameof(LocalGptRuntimeValue.LearnBaseDuplicatePolicySummary), "Duplicate handling: each project path and known docs-corpus section gets a stable database id. Re-importing the same source updates/upserts the existing knowledge row instead of adding another copy.", "System.String"),
                    new(LocalGptRuntimeValue.MinCouncilOutputTokens, nameof(LocalGptRuntimeValue.MinCouncilOutputTokens), "256", "System.Int32"),
                    new(LocalGptRuntimeValue.DefaultCouncilOutputTokens, nameof(LocalGptRuntimeValue.DefaultCouncilOutputTokens), "262144", "System.Int32"),
                    new(LocalGptRuntimeValue.MaxCouncilOutputTokens, nameof(LocalGptRuntimeValue.MaxCouncilOutputTokens), "262144", "System.Int32"),
                    new(LocalGptRuntimeValue.MinCouncilContextTokens, nameof(LocalGptRuntimeValue.MinCouncilContextTokens), "2048", "System.Int32"),
                    new(LocalGptRuntimeValue.DefaultCouncilContextTokens, nameof(LocalGptRuntimeValue.DefaultCouncilContextTokens), "262144", "System.Int32"),
                    new(LocalGptRuntimeValue.MaxCouncilContextTokens, nameof(LocalGptRuntimeValue.MaxCouncilContextTokens), "262144", "System.Int32"),
                    new(LocalGptRuntimeValue.CouncilSessionName, nameof(LocalGptRuntimeValue.CouncilSessionName), "AI Council — selected Ollama models", "System.String"),
                    new(LocalGptRuntimeValue.MaxUploadFiles, nameof(LocalGptRuntimeValue.MaxUploadFiles), "2147483647", "System.Int32"),
                    new(LocalGptRuntimeValue.MaxUploadBytes, nameof(LocalGptRuntimeValue.MaxUploadBytes), "2147483647", "System.Int32"),
                    new(LocalGptRuntimeValue.OllamaModeAutoGpu, nameof(LocalGptRuntimeValue.OllamaModeAutoGpu), "auto-gpu", "System.String"),
                    new(LocalGptRuntimeValue.OllamaModeSafeCpu, nameof(LocalGptRuntimeValue.OllamaModeSafeCpu), "safe-cpu", "System.String"),
                    new(LocalGptRuntimeValue.OllamaModeLimitedGpu, nameof(LocalGptRuntimeValue.OllamaModeLimitedGpu), "limited-gpu", "System.String"),
                    new(LocalGptRuntimeValue.DetectedOllamaSessionPrefix, nameof(LocalGptRuntimeValue.DetectedOllamaSessionPrefix), "Ollama detected — ", "System.String"),
                    new(LocalGptRuntimeValue.DefaultOllamaEndpoint, nameof(LocalGptRuntimeValue.DefaultOllamaEndpoint), "http://127.0.0.1:11434", "System.String"),
                    new(LocalGptRuntimeValue.DefaultMaxPromptCharacters, nameof(LocalGptRuntimeValue.DefaultMaxPromptCharacters), "2147483647", "System.Int32"),
                    new(LocalGptRuntimeValue.MaxPromptCharacters, nameof(LocalGptRuntimeValue.MaxPromptCharacters), "2147483647", "System.Int32"),
                    new(LocalGptRuntimeValue.MaxBootstrapCharacters, nameof(LocalGptRuntimeValue.MaxBootstrapCharacters), "6000", "System.Int32"),
                    new(LocalGptRuntimeValue.MaxSingleConversationMessageCharacters, nameof(LocalGptRuntimeValue.MaxSingleConversationMessageCharacters), "2147483647", "System.Int32"),
                    new(LocalGptRuntimeValue.ApplicationDefaultPort, nameof(LocalGptRuntimeValue.ApplicationDefaultPort), "5000", "System.Int32"),
                    new(LocalGptRuntimeValue.ProtocolVersion, nameof(LocalGptRuntimeValue.ProtocolVersion), "2.1", "System.String"),
                    new(LocalGptRuntimeValue.ProtocolMinimumCompatibleVersion, nameof(LocalGptRuntimeValue.ProtocolMinimumCompatibleVersion), "2.0", "System.String"),
                    new(LocalGptRuntimeValue.ProtocolDefaultServicePort, nameof(LocalGptRuntimeValue.ProtocolDefaultServicePort), "51140", "System.Int32"),
                    new(LocalGptRuntimeValue.ProtocolDefaultDiscoveryPort, nameof(LocalGptRuntimeValue.ProtocolDefaultDiscoveryPort), "51141", "System.Int32"),
                    new(LocalGptRuntimeValue.ProtocolMaximumMessageBytes, nameof(LocalGptRuntimeValue.ProtocolMaximumMessageBytes), "8388608", "System.Int32"),
                    new(LocalGptRuntimeValue.ProtocolMaximumDiscoveryBytes, nameof(LocalGptRuntimeValue.ProtocolMaximumDiscoveryBytes), "32768", "System.Int32"),
                    new(LocalGptRuntimeValue.ArtifactBuildMinimumTimeoutSeconds, nameof(LocalGptRuntimeValue.ArtifactBuildMinimumTimeoutSeconds), "5", "System.Int32"),
                    new(LocalGptRuntimeValue.ArtifactBuildMaximumTimeoutSeconds, nameof(LocalGptRuntimeValue.ArtifactBuildMaximumTimeoutSeconds), "900", "System.Int32"),
                    new(LocalGptRuntimeValue.CodeGenerationMaximumPayloadCharacters, nameof(LocalGptRuntimeValue.CodeGenerationMaximumPayloadCharacters), "2147483647", "System.Int32"),
                    new(LocalGptRuntimeValue.CodeGenerationMaximumFileCount, nameof(LocalGptRuntimeValue.CodeGenerationMaximumFileCount), "2147483647", "System.Int32"),
                    new(LocalGptRuntimeValue.CodeGenerationMaximumReviewTake, nameof(LocalGptRuntimeValue.CodeGenerationMaximumReviewTake), "2147483647", "System.Int32"),
                    new(LocalGptRuntimeValue.ComponentActivityCapacity, nameof(LocalGptRuntimeValue.ComponentActivityCapacity), "192", "System.Int32"),
                    new(LocalGptRuntimeValue.ComponentActivityMaximumSummaryCharacters, nameof(LocalGptRuntimeValue.ComponentActivityMaximumSummaryCharacters), "320", "System.Int32"),
                    new(LocalGptRuntimeValue.RuntimeCapabilityRefreshWarning, nameof(LocalGptRuntimeValue.RuntimeCapabilityRefreshWarning), "The live capability directory is available, but its derived LocalGPT Core project artifacts could not be refreshed. Council execution continues.", "System.String"),
                    new(LocalGptRuntimeValue.CouncilCodeGenerationMaximumEmbeddedPlanCharacters, nameof(LocalGptRuntimeValue.CouncilCodeGenerationMaximumEmbeddedPlanCharacters), "4000000", "System.Int32"),
                    new(LocalGptRuntimeValue.CouncilTeamSeedVersion, nameof(LocalGptRuntimeValue.CouncilTeamSeedVersion), "5", "System.Int32"),
                    new(LocalGptRuntimeValue.DebugArtifactMaximumInspectionBytes, nameof(LocalGptRuntimeValue.DebugArtifactMaximumInspectionBytes), "1073741824", "System.Int64"),
                    new(LocalGptRuntimeValue.DeferredDxAiMaximumResultCharacters, nameof(LocalGptRuntimeValue.DeferredDxAiMaximumResultCharacters), "8000", "System.Int32"),
                    new(LocalGptRuntimeValue.DxAiFunctionCatalogDataType, nameof(LocalGptRuntimeValue.DxAiFunctionCatalogDataType), "DxAiFunctionCatalogEntry", "System.String"),
                    new(LocalGptRuntimeValue.FormattingCollapsedThinkingStart, nameof(LocalGptRuntimeValue.FormattingCollapsedThinkingStart), "<details class=\"model-thinking\">", "System.String"),
                    new(LocalGptRuntimeValue.FormattingLiveThinkingStart, nameof(LocalGptRuntimeValue.FormattingLiveThinkingStart), "<details class=\"model-thinking open\" open>", "System.String"),
                    new(LocalGptRuntimeValue.FormattingThinkStartTag, nameof(LocalGptRuntimeValue.FormattingThinkStartTag), "<think>", "System.String"),
                    new(LocalGptRuntimeValue.FormattingThinkEndTag, nameof(LocalGptRuntimeValue.FormattingThinkEndTag), "</think>", "System.String"),
                    new(LocalGptRuntimeValue.FormattingTagLookbehindLength, nameof(LocalGptRuntimeValue.FormattingTagLookbehindLength), "16", "System.Int32"),
                    new(LocalGptRuntimeValue.FormattingMissingFinalAnswerNotice, nameof(LocalGptRuntimeValue.FormattingMissingFinalAnswerNotice), "The model stream ended without a final-answer section.", "System.String"),
                    new(LocalGptRuntimeValue.HardwareGpuInventoryScript, nameof(LocalGptRuntimeValue.HardwareGpuInventoryScript), "$i=0; Get-CimInstance Win32_VideoController | ForEach-Object { '{0}|{1}|{2}' -f $i,$_.Name,$_.AdapterRAM; $i++ }", "System.String"),
                    new(LocalGptRuntimeValue.HumanCollaborationMaximumTextLength, nameof(LocalGptRuntimeValue.HumanCollaborationMaximumTextLength), "1000000", "System.Int32"),
                    new(LocalGptRuntimeValue.NativeCommandMinimumTimeoutSeconds, nameof(LocalGptRuntimeValue.NativeCommandMinimumTimeoutSeconds), "5", "System.Int32"),
                    new(LocalGptRuntimeValue.NativeCommandMaximumTimeoutSeconds, nameof(LocalGptRuntimeValue.NativeCommandMaximumTimeoutSeconds), "3600", "System.Int32"),
                    new(LocalGptRuntimeValue.NavigationToggleSidebarName, nameof(LocalGptRuntimeValue.NavigationToggleSidebarName), "toggledSidebar", "System.String"),
                    new(LocalGptRuntimeValue.OllamaMaximumAutomaticToolRounds, nameof(LocalGptRuntimeValue.OllamaMaximumAutomaticToolRounds), "3", "System.Int32"),
                    new(LocalGptRuntimeValue.OllamaMaximumToolResultCharacters, nameof(LocalGptRuntimeValue.OllamaMaximumToolResultCharacters), "16000", "System.Int32"),
                    new(LocalGptRuntimeValue.LocalVisionMaximumImageBytes, nameof(LocalGptRuntimeValue.LocalVisionMaximumImageBytes), "6291456", "System.Int32"),
                    new(LocalGptRuntimeValue.OneWireSecuritySchemaVersion, nameof(LocalGptRuntimeValue.OneWireSecuritySchemaVersion), "1", "System.Int32"),
                    new(LocalGptRuntimeValue.OneWireTotpPeriodSeconds, nameof(LocalGptRuntimeValue.OneWireTotpPeriodSeconds), "30", "System.Int32"),
                    new(LocalGptRuntimeValue.OneWireTotpAlphabet, nameof(LocalGptRuntimeValue.OneWireTotpAlphabet), "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567", "System.String"),
                    new(LocalGptRuntimeValue.SqliteTableEditorMaximumRows, nameof(LocalGptRuntimeValue.SqliteTableEditorMaximumRows), "500", "System.Int32"),
                    new(LocalGptRuntimeValue.ProjectMaintenanceMaximumCompilerCandidates, nameof(LocalGptRuntimeValue.ProjectMaintenanceMaximumCompilerCandidates), "200", "System.Int32"),
                    new(LocalGptRuntimeValue.ProjectMaintenanceMaximumCapturedCharacters, nameof(LocalGptRuntimeValue.ProjectMaintenanceMaximumCapturedCharacters), "2000000", "System.Int32"),
                    new(LocalGptRuntimeValue.ProjectOrganicArtifactKind, nameof(LocalGptRuntimeValue.ProjectOrganicArtifactKind), "OrganicProjectContext", "System.String"),
                    new(LocalGptRuntimeValue.ProjectOrganicArtifactName, nameof(LocalGptRuntimeValue.ProjectOrganicArtifactName), "LocalGPT organic project wiring", "System.String"),
                    new(LocalGptRuntimeValue.SafeTextDocumentMaximumBytes, nameof(LocalGptRuntimeValue.SafeTextDocumentMaximumBytes), "8388608", "System.Int32"),
                    new(LocalGptRuntimeValue.ThemeDefaultName, nameof(LocalGptRuntimeValue.ThemeDefaultName), "office-white", "System.String"),
                    new(LocalGptRuntimeValue.ThemeContractPath, nameof(LocalGptRuntimeValue.ThemeContractPath), "css/localgpt-theme-contract.css", "System.String"),
                    new(LocalGptRuntimeValue.BootstrapDarkModePostfix, nameof(LocalGptRuntimeValue.BootstrapDarkModePostfix), "-dark", "System.String"),
                    new(LocalGptRuntimeValue.ProjectMaintenanceToastName, nameof(LocalGptRuntimeValue.ProjectMaintenanceToastName), "ProjectMaintenanceToasts", "System.String"),
                    new(LocalGptRuntimeValue.ProjectToastName, nameof(LocalGptRuntimeValue.ProjectToastName), "ProjectToasts", "System.String"),
 //                   new(LocalGptRuntimeValue.DatabaseMigrationOrganicSkillTableRepairSql, nameof(LocalGptRuntimeValue.DatabaseMigrationOrganicSkillTableRepairSql), """
 //CREATE TABLE IF NOT EXISTS "OrganicSkills" (
 //       "Id" TEXT NOT NULL CONSTRAINT "PK_OrganicSkills" PRIMARY KEY, "Key" TEXT NOT NULL DEFAULT '',
 //       "DisplayName" TEXT NOT NULL DEFAULT '', "Description" TEXT NOT NULL DEFAULT '', "SourcePeerId" TEXT NOT NULL DEFAULT 'localgpt',
 //       "OrgansJson" TEXT NOT NULL DEFAULT '[]', "CapabilityKeysJson" TEXT NOT NULL DEFAULT '[]', "UiActivationKeysJson" TEXT NOT NULL DEFAULT '[]',
 //       "IsOnline" INTEGER NOT NULL DEFAULT 1, "IsEnabled" INTEGER NOT NULL DEFAULT 1, "IsUserApproved" INTEGER NOT NULL DEFAULT 0,
 //       "CreatedAtUtc" TEXT NOT NULL DEFAULT '0001-01-01T00:00:00', "UpdatedAtUtc" TEXT NOT NULL DEFAULT '0001-01-01T00:00:00');
 //CREATE TABLE IF NOT EXISTS "ProjectOrganicSkillLinks" (
 //       "Id" TEXT NOT NULL CONSTRAINT "PK_ProjectOrganicSkillLinks" PRIMARY KEY, "ProjectId" TEXT NOT NULL DEFAULT '',
 //       "SkillId" TEXT NOT NULL DEFAULT '', "IsRequired" INTEGER NOT NULL DEFAULT 1, "IsEnabled" INTEGER NOT NULL DEFAULT 1,
 //       "Notes" TEXT NOT NULL DEFAULT '', "UpdatedAtUtc" TEXT NOT NULL DEFAULT '0001-01-01T00:00:00',
 //       CONSTRAINT "FK_ProjectOrganicSkillLinks_LocalGptProjects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "LocalGptProjects" ("Id") ON DELETE CASCADE,
 //       CONSTRAINT "FK_ProjectOrganicSkillLinks_OrganicSkills_SkillId" FOREIGN KEY ("SkillId") REFERENCES "OrganicSkills" ("Id") ON DELETE CASCADE);
 //CREATE TABLE IF NOT EXISTS "CouncilMemberOrganicSkillLinks" (
 //       "Id" TEXT NOT NULL CONSTRAINT "PK_CouncilMemberOrganicSkillLinks" PRIMARY KEY, "MemberKey" TEXT NOT NULL DEFAULT '',
 //       "SkillId" TEXT NOT NULL DEFAULT '', "Proficiency" INTEGER NOT NULL DEFAULT 50, "IsSelfRevealed" INTEGER NOT NULL DEFAULT 0,
 //       "IsEnabled" INTEGER NOT NULL DEFAULT 0, "Evidence" TEXT NOT NULL DEFAULT '', "DxFunctionsJson" TEXT NOT NULL DEFAULT '[]',
 //       "ControllerMethodsJson" TEXT NOT NULL DEFAULT '[]', "OrganicCapabilitiesJson" TEXT NOT NULL DEFAULT '[]',
 //       "UpdatedAtUtc" TEXT NOT NULL DEFAULT '0001-01-01T00:00:00',
 //       CONSTRAINT "FK_CouncilMemberOrganicSkillLinks_OrganicSkills_SkillId" FOREIGN KEY ("SkillId") REFERENCES "OrganicSkills" ("Id") ON DELETE CASCADE);
 //                   """, "System.String"),
 //                   new(LocalGptRuntimeValue.DatabaseMigrationOrganicSkillIndexRepairSql, nameof(LocalGptRuntimeValue.DatabaseMigrationOrganicSkillIndexRepairSql), """
 //   CREATE UNIQUE INDEX IF NOT EXISTS "IX_OrganicSkills_Key" ON "OrganicSkills" ("Key");
 //   CREATE INDEX IF NOT EXISTS "IX_OrganicSkills_IsEnabled_IsOnline_UpdatedAtUtc" ON "OrganicSkills" ("IsEnabled", "IsOnline", "UpdatedAtUtc");
 //   CREATE UNIQUE INDEX IF NOT EXISTS "IX_ProjectOrganicSkillLinks_ProjectId_SkillId" ON "ProjectOrganicSkillLinks" ("ProjectId", "SkillId");
 //   CREATE INDEX IF NOT EXISTS "IX_ProjectOrganicSkillLinks_ProjectId_IsEnabled_IsRequired" ON "ProjectOrganicSkillLinks" ("ProjectId", "IsEnabled", "IsRequired");
 //   CREATE INDEX IF NOT EXISTS "IX_ProjectOrganicSkillLinks_SkillId" ON "ProjectOrganicSkillLinks" ("SkillId");
 //   CREATE UNIQUE INDEX IF NOT EXISTS "IX_CouncilMemberOrganicSkillLinks_MemberKey_SkillId" ON "CouncilMemberOrganicSkillLinks" ("MemberKey", "SkillId");
 //   CREATE INDEX IF NOT EXISTS "IX_CouncilMemberOrganicSkillLinks_MemberKey_IsEnabled_Proficiency" ON "CouncilMemberOrganicSkillLinks" ("MemberKey", "IsEnabled", "Proficiency");
 //   CREATE INDEX IF NOT EXISTS "IX_CouncilMemberOrganicSkillLinks_SkillId" ON "CouncilMemberOrganicSkillLinks" ("SkillId");
 //                   """, "System.String"),
 //                   new(LocalGptRuntimeValue.DatabaseMigrationCouncilTeamTableRepairSql, nameof(LocalGptRuntimeValue.DatabaseMigrationCouncilTeamTableRepairSql), """
 //   CREATE TABLE IF NOT EXISTS "CouncilTeamConfigurations" (
 //       "Id" TEXT NOT NULL CONSTRAINT "PK_CouncilTeamConfigurations" PRIMARY KEY, "Key" TEXT NOT NULL DEFAULT '',
 //       "DisplayName" TEXT NOT NULL DEFAULT '', "Purpose" TEXT NOT NULL DEFAULT '', "RolesJson" TEXT NOT NULL DEFAULT '[]',
 //       "PreferredCapabilitiesJson" TEXT NOT NULL DEFAULT '[]', "ArchitectureContractsJson" TEXT NOT NULL DEFAULT '[]',
 //       "WorkflowStepsJson" TEXT NOT NULL DEFAULT '[]', "ExpertPreparationPromptTemplate" TEXT NOT NULL DEFAULT '',
 //       "LeaderSynthesisPromptTemplate" TEXT NOT NULL DEFAULT '', "MainRoundInstructionTemplate" TEXT NOT NULL DEFAULT '',
 //       "SeedVersion" INTEGER NOT NULL DEFAULT 1, "IsSystemSeed" INTEGER NOT NULL DEFAULT 1,
 //       "IsUserModified" INTEGER NOT NULL DEFAULT 0, "IsEnabled" INTEGER NOT NULL DEFAULT 1,
 //       "CreatedAtUtc" TEXT NOT NULL DEFAULT '0001-01-01T00:00:00', "UpdatedAtUtc" TEXT NOT NULL DEFAULT '0001-01-01T00:00:00');
 //                   """, "System.String"),
    //                new(LocalGptRuntimeValue.DatabaseMigrationCouncilTeamIndexRepairSql, nameof(LocalGptRuntimeValue.DatabaseMigrationCouncilTeamIndexRepairSql), """
    //CREATE UNIQUE INDEX IF NOT EXISTS "IX_CouncilTeamConfigurations_Key" ON "CouncilTeamConfigurations" ("Key");
    //CREATE INDEX IF NOT EXISTS "IX_CouncilTeamConfigurations_IsEnabled_UpdatedAtUtc" ON "CouncilTeamConfigurations" ("IsEnabled", "UpdatedAtUtc");
    //                """, "System.String"),
                    new(LocalGptRuntimeValue.SqliteGuidExpression, nameof(LocalGptRuntimeValue.SqliteGuidExpression), "lower(hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-4' || substr(hex(randomblob(2)),2) || '-' || substr('89ab',abs(random()) % 4 + 1,1) || substr(hex(randomblob(2)),2) || '-' || hex(randomblob(6)))", "System.String"),
                    new(LocalGptRuntimeValue.LearnBasePresetsJson, nameof(LocalGptRuntimeValue.LearnBasePresetsJson), """
[
  {"Label":"Selected local learn-base","RootPath":"","Description":"Choose the local parent folder that contains the source or documentation corpora you want LocalGPT to inspect.","RecommendedMaxProjects":80},
  {"Label":"Microsoft .NET docs + C# compiler","RootPath":"","Description":"Select the local Microsoft .NET documentation/compiler checkout; LocalGPT builds bounded source maps from the selected host path.","RecommendedMaxProjects":30},
  {"Label":"Windows developer docs","RootPath":"","Description":"Select the local Windows developer documentation checkout.","RecommendedMaxProjects":24},
  {"Label":"DevExpress Blazor samples","RootPath":"","Description":"Select the installed/local DevExpress Blazor samples folder appropriate to your version.","RecommendedMaxProjects":60},
  {"Label":"DevExpress examples","RootPath":"","Description":"Select a local DevExpress example repository folder.","RecommendedMaxProjects":60},
  {"Label":"Custom path","RootPath":"","Description":"Choose any local source or documentation folder with the shared path explorer.","RecommendedMaxProjects":40}
]
""", "System.String"),
                    new(LocalGptRuntimeValue.LearnBaseScanProfilesJson, nameof(LocalGptRuntimeValue.LearnBaseScanProfilesJson), "[{\"Label\": \"Focused scan\", \"MaxProjects\": 12, \"Description\": \"Best for one documentation corpus or one repository.\"}, {\"Label\": \"Balanced scan\", \"MaxProjects\": 40, \"Description\": \"Best default for useful breadth without excessive noise.\"}, {\"Label\": \"Broad scan\", \"MaxProjects\": 100, \"Description\": \"Best after adding many repositories or documentation corpora.\"}, {\"Label\": \"Custom limit\", \"MaxProjects\": 40, \"Description\": \"Use the advanced import limit.\"}]", "System.String"),
                    new(LocalGptRuntimeValue.TestLabRoutesJson, nameof(LocalGptRuntimeValue.TestLabRoutesJson), "[{\"Label\": \"Health\", \"Path\": \"/health\", \"Style\": \"Secondary\"}, {\"Label\": \"Diagnostics\", \"Path\": \"/__diag\", \"Style\": \"Secondary\"}, {\"Label\": \"DXAiFunctions\", \"Path\": \"/__diag/dxaichat-functions\", \"Style\": \"Secondary\"}, {\"Label\": \"Minecraft 26.1\", \"Path\": \"/__diag/minecraft/datapack-version?minecraftVersion=26.1\", \"Style\": \"Secondary\"}, {\"Label\": \"Datapack ZIP\", \"Path\": \"/__diag/council/artifact-smoke?target=datapack\", \"Style\": \"Primary\"}, {\"Label\": \"AI Host ZIP\", \"Path\": \"/__diag/council/artifact-smoke?target=ai-host\", \"Style\": \"Primary\"}, {\"Label\": \"Minecraft Benchmark\", \"Path\": \"/__diag/minecraft/datapack-benchmark?minecraftVersion=26.1\", \"Style\": \"Secondary\"}, {\"Label\": \"Engineering Benchmark\", \"Path\": \"/__diag/benchmark/engineering?taskSet=engineering&saveToKnowledge=true\", \"Style\": \"Secondary\"}, {\"Label\": \"Replacement Benchmark\", \"Path\": \"/__diag/benchmark/engineering?taskSet=replacement&validateBuildableArtifacts=true&maxBuildArtifacts=4&saveToKnowledge=true\", \"Style\": \"Primary\"}, {\"Label\": \"Council Feedback\", \"Path\": \"/__diag/council/development-feedback-talk?maxOutputTokens=2048&maxContextTokens=32768&maxRounds=0\", \"Style\": \"Primary\"}]", "System.String"),
                    new(LocalGptRuntimeValue.VocabularyJson, nameof(LocalGptRuntimeValue.VocabularyJson), """
{
  "CouncilSpoolerRunning": "Running",
  "CouncilSpoolerCompleted": "Completed",
  "CouncilSpoolerFailed": "Failed",
  "HumanRequestApproval": "Approval",
  "HumanRequestFeedback": "Feedback",
  "HumanRequestGuidance": "Guidance",
  "HumanStatusPending": "Pending",
  "HumanStatusApproved": "Approved",
  "HumanStatusDeclined": "Declined",
  "HumanStatusAnswered": "Answered",
  "HumanStatusConsumed": "Consumed",
  "HumanStatusExpired": "Expired",
  "ContributionQueued": "Queued",
  "ContributionInjected": "Injected",
  "ContributionEvaluated": "Evaluated",
  "VerdictPending": "Pending",
  "VerdictSupported": "Supported",
  "VerdictNeedsCorrection": "NeedsCorrection",
  "VerdictMixed": "Mixed",
  "VerdictNotReviewed": "NotReviewed",
  "DeferredPendingApproval": "PendingApproval",
  "DeferredExecuting": "Executing",
  "DeferredCompleted": "Completed",
  "DeferredFailed": "Failed",
  "DeferredDeclined": "Declined",
  "DeferredCompletedElsewhere": "CompletedElsewhere",
  "ActorSystem": "System",
  "ActorHuman": "Human",
  "ActorAiModel": "AiModel",
  "ActorCouncil": "Council",
  "ActorApiClient": "ApiClient",
  "AuthorityNone": "None",
  "AuthorityHumanInteraction": "HumanInteraction",
  "AuthorityHumanApproval": "HumanApproval",
  "CatalogDxFunction": "DxFunction",
  "CatalogPublicServiceMethod": "PublicServiceMethod"
}
""", "System.String"),
                ],
                Collections =
                [
                    new(LocalGptRuntimeCollection.AllowedNativeExecutables, nameof(LocalGptRuntimeCollection.AllowedNativeExecutables), ["powershell.exe", "pwsh.exe", "gradle", "gradle.bat", "gradlew", "gradlew.bat", "java", "java.exe"]),
                    new(LocalGptRuntimeCollection.DebugExtensions, nameof(LocalGptRuntimeCollection.DebugExtensions), [".pdb", ".pdg", ".appxsym"]),
                    new(LocalGptRuntimeCollection.TextExtensions, nameof(LocalGptRuntimeCollection.TextExtensions), [".txt", ".md", ".json", ".xml", ".csv", ".cs", ".razor", ".cshtml", ".css", ".scss", ".js", ".ts", ".tsx", ".html", ".htm", ".xaml", ".sln", ".csproj", ".vbproj", ".fsproj", ".props", ".targets", ".config", ".editorconfig", ".yml", ".yaml", ".toml", ".sql", ".ps1", ".cmd", ".bat", ".sh", ".java", ".kt", ".gradle", ".mcfunction", ".mcmeta", ".properties"]),
                    new(LocalGptRuntimeCollection.BinaryDiagnosticExtensions, nameof(LocalGptRuntimeCollection.BinaryDiagnosticExtensions), [".dll", ".exe", ".pdb", ".appxsym", ".nupkg", ".wasm"]),
                    new(LocalGptRuntimeCollection.ExcludedDirectoryNames, nameof(LocalGptRuntimeCollection.ExcludedDirectoryNames), [".git", ".vs", ".idea", "bin", "obj", "node_modules", "packages", ".venv", "__pycache__", ".gradle", ".mypy_cache", ".pytest_cache", "build", "dist", "publish", "AppPackages"]),
                    new(LocalGptRuntimeCollection.BinaryExtensions, nameof(LocalGptRuntimeCollection.BinaryExtensions), [".dll", ".exe", ".pdb", ".msi", ".pfx", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".pdf", ".db", ".sqlite", ".sqlite3", ".zip", ".nupkg", ".fzz", ".fzpz"]),
                    new(LocalGptRuntimeCollection.SourceExtensions, nameof(LocalGptRuntimeCollection.SourceExtensions), [".cs", ".csproj", ".sln", ".razor", ".cshtml", ".xaml", ".json", ".xml", ".c", ".h", ".hh", ".hpp", ".hxx", ".cc", ".cpp", ".cxx", ".cmake", ".mk", ".mak", ".make", ".py", ".pyi", ".js", ".jsx", ".mjs", ".ts", ".tsx", ".html", ".css", ".scss", ".sql", ".md", ".yml", ".yaml", ".ps1", ".props", ".targets", ".config", ".resx", ".mdx", ".java", ".kt", ".kts", ".gradle", ".gradle.kts", ".go", ".mod", ".sum", ".rs", ".swift", ".php", ".rb", ".lua", ".proto", ".toml", ".ini", ".sh", ".bat", ".cmd", ".gotmpl", ".scad", ".openscad", ".txt", ".text", ".log", ".csv", ".tsv", ".http", ".rest", ".tmpl"]),
                    new(LocalGptRuntimeCollection.LearnBaseKnownExtensions, nameof(LocalGptRuntimeCollection.LearnBaseKnownExtensions), [".cs", ".csproj", ".sln", ".razor", ".cshtml", ".xaml", ".xml", ".json", ".md", ".mdx", ".rst", ".adoc", ".txt", ".csv", ".tsv", ".yml", ".yaml", ".toml", ".ini", ".config", ".conf", ".cfg", ".props", ".targets", ".resx", ".editorconfig", ".c", ".h", ".hh", ".hxx", ".cc", ".cpp", ".cxx", ".hpp", ".ipp", ".tpp", ".inc", ".ino", ".pde", ".cmake", ".mk", ".mak", ".make", ".py", ".pyi", ".ipynb", ".js", ".jsx", ".ts", ".tsx", ".mjs", ".css", ".scss", ".html", ".htm", ".php", ".java", ".kt", ".kts", ".gradle", ".go", ".mod", ".sum", ".rs", ".swift", ".proto", ".sql", ".ps1", ".cmd", ".bat", ".sh", ".dockerignore", ".gitignore", ".http", ".rest", ".mcfunction", ".mcmeta", ".properties", ".ld", ".s", ".asm", ".dts", ".dtsi", ".overlay", ".idf", ".map", ".v", ".sv", ".vhd", ".vhdl", ".fz", ".fzb", ".fzp", ".kicad_sch", ".kicad_pcb", ".kicad_pro", ".kicad_sym", ".kicad_mod", ".kicad_wks", ".brd", ".sch", ".net", ".dsn", ".openscad", ".scad", ".svg"]),
                    new(LocalGptRuntimeCollection.ArtifactTextExtensions, nameof(LocalGptRuntimeCollection.ArtifactTextExtensions), [".cs", ".razor", ".cshtml", ".csproj", ".sln", ".props", ".targets", ".c", ".h", ".hh", ".hpp", ".hxx", ".cc", ".cpp", ".cxx", ".cmake", ".mk", ".mak", ".make", ".java", ".kt", ".kts", ".gradle", ".gradle.kts", ".js", ".jsx", ".mjs", ".ts", ".tsx", ".py", ".pyi", ".go", ".rs", ".swift", ".php", ".rb", ".lua", ".proto", ".ps1", ".sh", ".bat", ".cmd", ".sql", ".scad", ".openscad", ".md", ".txt", ".json", ".xml", ".css", ".scss", ".html", ".htm", ".yml", ".yaml", ".toml", ".ini", ".properties", ".mcfunction", ".mcmeta"]),
                    new(LocalGptRuntimeCollection.KnowledgeFiles, nameof(LocalGptRuntimeCollection.KnowledgeFiles), ["AGENTS.md", "SECURITY.md", "docs/index.md", "docs/architecture/system-overview.md", "docs/architecture/ai-host.md", "docs/architecture/council-runtime.md", "docs/architecture/project-data.md", "docs/architecture/onewire-security.md", "docs/architecture/frontend-and-themes.md", "docs/engineering/build-validation.md", "docs/reference/capability-map.md", "docs/reference/design-evolution.md", "docs/guide/embedded-and-games.md", "docs/COUNCIL_KNOWLEDGE_SEED.sql"]),
                    new(LocalGptRuntimeCollection.AllowedUploadExtensions, nameof(LocalGptRuntimeCollection.AllowedUploadExtensions), [".7z", ".apk", ".avi", ".bat", ".c", ".cer", ".cmd", ".conf", ".cpp", ".crt", ".cs", ".csproj", ".css", ".csv", ".db", ".deb", ".doc", ".dockerfile", ".dockerignore", ".docx", ".dwg", ".dxf", ".editorconfig", ".env", ".exe", ".gif", ".gitignore", ".go", ".gz", ".h", ".hpp", ".html", ".img", ".ini", ".iso", ".jar", ".java", ".jpeg", ".jpg", ".js", ".json", ".jsx", ".key", ".log", ".md", ".mkv", ".mov", ".mp3", ".mp4", ".msi", ".obj", ".odp", ".ods", ".odt", ".parquet", ".pdf", ".pem", ".pfx", ".php", ".pkl", ".png", ".ppt", ".pptx", ".ps1", ".py", ".qcow2", ".rar", ".rpm", ".rs", ".rtf", ".sh", ".sln", ".sql", ".sqlite", ".srt", ".step", ".stl", ".svg", ".tar", ".tf", ".toml", ".ts", ".tsx", ".txt", ".vhdx", ".vmdk", ".wav", ".webp", ".xls", ".xlsx", ".xml", ".xz", ".yaml", ".yml", ".zip", ".zst"]),
                    new(LocalGptRuntimeCollection.AllowedUploadMimeTypes, nameof(LocalGptRuntimeCollection.AllowedUploadMimeTypes), ["*/*", "application/*", "audio/*", "image/*", "model/*", "text/*", "video/*", "application/octet-stream"]),
                    new(LocalGptRuntimeCollection.ArchitectureLanguageToolchainOptions, nameof(LocalGptRuntimeCollection.ArchitectureLanguageToolchainOptions), ["Ask me before choosing language/toolchain", "Use target repository language/toolchain", "C# / .NET", "C++ / CMake or existing native build", "Java / Maven or Gradle", "JavaScript / TypeScript / Node.js", "PowerShell / pwsh", "Python", "Rust / Cargo", "Go", "HTML/CSS/JavaScript only", "Other target-specific language/toolchain"]),
                    new(LocalGptRuntimeCollection.ArchitectureUiStackOptions, nameof(LocalGptRuntimeCollection.ArchitectureUiStackOptions), ["Ask me before choosing UI stack", "Use target repository UI stack", "DevExpress Blazor components", "Plain Blazor components", "Web HTML/CSS/JavaScript", "Native/desktop target UI", "No UI / backend or tool only", "Other target-specific UI"]),
                    new(LocalGptRuntimeCollection.ArchitectureSolutionShapeOptions, nameof(LocalGptRuntimeCollection.ArchitectureSolutionShapeOptions), ["Ask me before choosing solution shape", "Preserve existing repository/project graph", "Single cohesive solution/workspace", "Multi-language solution/workspace", "Split backend and frontend projects", "Library/plugin/package only", "Script/tool workspace only", "Datapack/mod workspace only"]),
                    new(LocalGptRuntimeCollection.ArchitectureRenderModeOptions, nameof(LocalGptRuntimeCollection.ArchitectureRenderModeOptions), ["Ask me before choosing runtime/rendering", "Use target repository runtime", "Blazor Server / InteractiveServer", "Blazor WebAssembly with ASP.NET Core backend", "Static SSR plus interactive islands", "ASP.NET Core API / backend only", "Desktop wrapper / WebView2", "Native desktop/application runtime", "Native C/C++ runtime", "Java/JVM runtime", "Node.js runtime", "PowerShell/script runtime", "Python runtime", "Minecraft Java/datapack runtime", "CLI/tooling runtime", "Other target-specific runtime"]),
                    new(LocalGptRuntimeCollection.ArchitectureReferenceLookOptions, nameof(LocalGptRuntimeCollection.ArchitectureReferenceLookOptions), ["Ask me before choosing visual fidelity", "Recreate the goal app look closely", "Use LocalGPT style but preserve goal app structure", "Functional prototype first", "No visual reference"]),
                    new(LocalGptRuntimeCollection.ProjectRequirementTargetKinds, nameof(LocalGptRuntimeCollection.ProjectRequirementTargetKinds), ["DXFunction", "BusinessObject", "Configuration", "SystemVariable", "Regex", "Prompt", "Knowledge", "DatabaseTable", "Service", "Controller", "CodeDomTarget"]),
                    new(LocalGptRuntimeCollection.ProjectArtifactKinds, nameof(LocalGptRuntimeCollection.ProjectArtifactKinds), ["Regex", "SystemVariable", "Configuration", "Prompt", "KnowledgeReference", "BusinessObjectReference", "DXFunctionReference", "CodeDomTarget", "DocumentReference"]),
                    new(LocalGptRuntimeCollection.ChatHarmonyModelHints, nameof(LocalGptRuntimeCollection.ChatHarmonyModelHints), ["harmony", "gpt-oss"]),
                    new(LocalGptRuntimeCollection.ChatDeepSeekModelHints, nameof(LocalGptRuntimeCollection.ChatDeepSeekModelHints), ["deepseek", "deep-seek", "r1-distill"]),
                    new(LocalGptRuntimeCollection.ChatDeepSeekControlTokens, nameof(LocalGptRuntimeCollection.ChatDeepSeekControlTokens), ["<｜begin▁of▁sentence｜>", "<｜end▁of▁sentence｜>", "<｜User｜>", "<｜Assistant｜>"]),
                    new(LocalGptRuntimeCollection.ChatGemmaModelHints, nameof(LocalGptRuntimeCollection.ChatGemmaModelHints), ["gemma", "codegemma", "shieldgemma"]),
                    new(LocalGptRuntimeCollection.ChatGemmaControlTokens, nameof(LocalGptRuntimeCollection.ChatGemmaControlTokens), ["<bos>", "<eos>", "<start_of_turn>model\n", "<start_of_turn>assistant\n", "<start_of_turn>model", "<start_of_turn>assistant", "<end_of_turn>"]),
                    new(LocalGptRuntimeCollection.ChatAppleModelHints, nameof(LocalGptRuntimeCollection.ChatAppleModelHints), ["apple", "openelm", "afm", "foundation-model", "mlx-"]),
                    new(LocalGptRuntimeCollection.ChatAppleControlTokens, nameof(LocalGptRuntimeCollection.ChatAppleControlTokens), ["<|start_of_role|>assistant<|end_of_role|>", "<|start_of_role|>analysis<|end_of_role|>", "<|start_of_turn|>assistant", "<|end_of_turn|>", "<|end_of_text|>", "<|eot_id|>"]),
                    new(LocalGptRuntimeCollection.ChatThinkTagsModelHints, nameof(LocalGptRuntimeCollection.ChatThinkTagsModelHints), ["qwq", "qwen3", "thinking"]),
                ],
                RegexPatterns =
                [
                    new(LocalGptRuntimePattern.NameCleaner, "builtin.name-cleaner", "[^a-zA-Z0-9_.-]", ""),
                    new(LocalGptRuntimePattern.ModIdCleaner, "builtin.mod-id-cleaner", "[^a-z0-9_]", ""),
                    new(LocalGptRuntimePattern.PackagePartCleaner, "builtin.package-part-cleaner", "[^a-z0-9_]", ""),
                    new(LocalGptRuntimePattern.MissingFeaturePattern, "builtin.missing-feature-pattern", "(missing feature|missing capability|not implemented|not yet implemented|blocked by|cannot build|requires implementation|feature gap|capability gap|<localgpt-capability-gap>)", "i,c"),
                    new(LocalGptRuntimePattern.CapabilityGapBlockPattern, "builtin.capability-gap-block-pattern", "<localgpt-capability-gap>(?<body>.*?)</localgpt-capability-gap>", "i,s,c"),
                    new(LocalGptRuntimePattern.TruncatedTailPattern, "builtin.truncated-tail-pattern", "\\b(?:with|and|or|the|a|an|for|to|in|of|by|as|if|when|once|then|because|from|into|that|this|which|th)\\s*$", "i,c"),
                    new(LocalGptRuntimePattern.ThinkingBlockPattern, "builtin.thinking-block-pattern", "<details\\s+class=\"model-thinking open\"[^>]*>\\s*<summary>Model thinking</summary>\\s*(?<thinking>.*?)\\s*</details>", "i,s,c"),
                    new(LocalGptRuntimePattern.CouncilPromptFencePattern, "builtin.council-prompt-fence-pattern", "```text\\s*(?<prompt>.*?)\\s*```", "i,s,c"),
                    new(LocalGptRuntimePattern.CouncilRequestBlockPattern, "builtin.council-request-block-pattern", "AI Council (?:continuation )?request:\\s*(?<prompt>.*?)(?:\\n\\s*##|\\z)", "i,s,c"),
                    new(LocalGptRuntimePattern.TargetFrameworkPattern, "builtin.target-framework-pattern", "<TargetFrameworks?>(?<value>[^<]+)</TargetFrameworks?>", "i,c"),
                    new(LocalGptRuntimePattern.PackageReferencePattern, "builtin.package-reference-pattern", "<PackageReference\\s+Include=\"(?<value>[^\"]+)\"", "i,c"),
                    new(LocalGptRuntimePattern.SensitiveNamePattern, "builtin.sensitive-name-pattern", "(?i)(fuck|shit|bitch|cunt|dick|pussy|whore|slut|porn|xxx)", ""),
                    new(LocalGptRuntimePattern.StreamStatusPattern, "builtin.stream-status-pattern", "<p\\s+class=\"localgpt-stream-status\"[^>]*>.*?</p>\\s*", "i,s,c"),
                    new(LocalGptRuntimePattern.WordPattern, "builtin.word-pattern", "\\b[\\p{L}\\p{N}_'-]+\\b", "c"),
                    new(LocalGptRuntimePattern.DevelopmentRequestPattern, "builtin.development-request-pattern", "(implement|implementation|develop|development|build|create|add|generate|scaffold|feature|code|page|component|service|endpoint|database|settings|artifact|solution|plugin|mod|datapack)", "i,c"),
                    new(LocalGptRuntimePattern.ExplicitArtifactIntentPattern, "builtin.explicit-artifact-intent-pattern", "(downloadable|download link|download route|zip|\\.zip|\\.cs\\b|\\.razor\\b|\\.dll\\b|\\.sln\\b|\\.csproj\\b|artifact|solution zip|project zip|whole solution|full solution)", "i,c"),
                    new(LocalGptRuntimePattern.AdviceOnlyPromptPattern, "builtin.advice-only-prompt-pattern", "(review|code review|diagnose|diagnostic|release readiness|readiness|go or no-go|blockers|evidence|what failed|why failed|build/deploy/package/publish|publish cycle|release cycle|maintenance cycle)", "i,c"),
                    new(LocalGptRuntimePattern.ExplicitArtifactCreationCommandPattern, "builtin.explicit-artifact-creation-command-pattern", "(generate|create|produce|write|implement|make|build)\\b.{0,120}\\b(downloadable|artifact|zip|solution|source code|\\.sln|\\.csproj|\\.cs\\b|\\.razor\\b|ai host|localgpt replacement|application|app|datapack|modpack)\\b|\\b(downloadable|artifact|zip|solution)\\b.{0,120}\\b(generate|create|produce|write|implement|make|build)\\b", "i,s,c"),
                    new(LocalGptRuntimePattern.ConcreteMinecraftArtifactPattern, "builtin.concrete-minecraft-artifact-pattern", "(minecraft|living cities|modpack|datapack|data pack|pack\\.mcmeta|mcfunction).*(generate|create|build|zip|download|artifact)|(generate|create|build|zip|download|artifact).*(minecraft|living cities|modpack|datapack|data pack|pack\\.mcmeta|mcfunction)", "i,c"),
                    new(LocalGptRuntimePattern.ConcreteDotNetArtifactPattern, "builtin.concrete-dot-net-artifact-pattern", "(dotnet|\\.net|c#|blazor|razor|devexpress|aspnet|asp\\.net|ollama).*(solution|project|zip|download|artifact|page|component|api|route|service)|(solution|project|zip|download|artifact|page|component|api|route|service).*(dotnet|\\.net|c#|blazor|razor|devexpress|aspnet|asp\\.net|ollama)", "i,c"),
                    new(LocalGptRuntimePattern.AiHostSetupPattern, "builtin.ai-host-setup-pattern", "(ai host|local ai host|model host|inference host|native runner|model-file runner|model file runner|iinferencerunner|nativemodelfile|llama\\.cpp|gguf)", "i,c"),
                    new(LocalGptRuntimePattern.ImplementationDecisionPattern, "builtin.implementation-decision-pattern", "(decision poll required|user decision poll|implementation path|architecture choice|architecture decision|target platform|runtime choice|ui stack|unclear implementation|unclear scope|scope is uncertain|ownership is uncertain|ask the user|needs user choice|choose between|pick between|multiple reasonable|trade-?off|depends on|which path|which approach)", "i,c"),
                    new(LocalGptRuntimePattern.ImplementationChoicePattern, "builtin.implementation-choice-pattern", "(choose|decide|pick|option|alternative|trade-?off|depends|uncertain|scope|ownership|clarify|question)", "i,c"),
                    new(LocalGptRuntimePattern.BlockingArtifactDecisionPattern, "builtin.blocking-artifact-decision-pattern", "(decision poll required|no (?:code|files?|artifacts?) will be generated until|do not generate (?:code|files?|artifacts?) until|stop before generating|\u0061wait (?:your )?(?:selection|choice|answer|decision)|waiting for (?:your )?(?:selection|choice|answer|decision)|please choose .* before|select .* and reply|will generate .* once (?:chosen|selected|confirmed))", "i,c"),
                    new(LocalGptRuntimePattern.SafeSandboxConsentPattern, "builtin.safe-sandbox-consent-pattern", "(prior consent for safe sandbox details:\\s*granted|let council choose safe sandbox details|you may decide safe sandbox details|council may choose safe sandbox defaults|make reasonable sandbox assumptions|decide yourself for the sandbox)", "i,c"),
                    new(LocalGptRuntimePattern.ExplicitDoNotGenerateUntilUserDecisionPattern, "builtin.explicit-do-not-generate-until-user-decision-pattern", "(ask me first|do not generate|don't generate|wait for my decision|stop before coding|stop before generating|no files until|no artifact until)", "i,c"),
                    new(LocalGptRuntimePattern.DeveloperExecutionIntentPattern, "builtin.developer-execution-intent-pattern", "(work as (?:the )?developers|you are the developers|continue until (?:you )?(?:produce|create|generate)|develop and debug|produce .* artifact|generate .* artifact|create .* artifact)", "i,c"),
                    new(LocalGptRuntimePattern.DevExpressImportPattern, "builtin.dev-express-import-pattern", "^\\s*@using\\s+(?<namespace>DevExpress(?:\\.[A-Za-z0-9_]+)+)", "m,c"),
                    new(LocalGptRuntimePattern.DevExpressRegistrationPattern, "builtin.dev-express-registration-pattern", "AddDevExpress[A-Za-z0-9_]*\\(", "c"),
                    new(LocalGptRuntimePattern.DevExpressDocumentPattern, "builtin.dev-express-document-pattern", "(devexpress|richedit|pdfviewer|pivot|report|xtrareport|office|docx|xlsx|pdf export|spreadsheet|document generation)", "i,c"),
                    new(LocalGptRuntimePattern.ExportFormatPattern, "builtin.export-format-pattern", "(\\.xlsx|xlsx|excel|\\.pptx|pptx|powerpoint|\\.pdf|pdf|\\.docx|docx|word|export format|file generation)", "i,c"),
                    new(LocalGptRuntimePattern.BlazorFrontendPattern, "builtin.blazor-frontend-pattern", "(blazor|razor|component|page|dxgrid|dxformlayout|dxbutton|dxmemo|dxtextbox|dxcombobox|dxaichat|devexpress blazor|interactive(server|webassembly|auto))", "i,c"),
                    new(LocalGptRuntimePattern.DotNetPattern, "builtin.dot-net-pattern", "(dotnet|\\.net|aspnet|asp\\.net|blazor|c#|codedom|entityframework|sqlite|winui|webview2)", "i,c"),
                    new(LocalGptRuntimePattern.MinecraftPattern, "builtin.minecraft-pattern", "(minecraft|fabric|neoforge|paper|datapack|gradle|java)", "i,c"),
                    new(LocalGptRuntimePattern.DatapackPattern, "builtin.datapack-pattern", "(datapack|data pack|pack\\.mcmeta|mcfunction|living cities)", "i,c"),
                    new(LocalGptRuntimePattern.MinecraftSkeletonMatrixPattern, "builtin.minecraft-skeleton-matrix-pattern", "(fabric.*paper.*neoforge|neoforge.*paper.*fabric|loader.*matrix|skeleton.*distinction|project skeleton distinction)", "i,c"),
                    new(LocalGptRuntimePattern.MinecraftVersionPattern, "builtin.minecraft-version-pattern", "(?<!\\d)(?<version>(?:1\\.\\d{1,2}|26\\.\\d)(?:\\.\\d{1,2})?(?:-snapshot-\\d+)?)(?!\\d)", "i,c"),
                    new(LocalGptRuntimePattern.LeadingSlashCommandPattern, "builtin.leading-slash-command-pattern", "(?m)^\\s*/", "c"),
                    new(LocalGptRuntimePattern.RootStorageRemovePattern, "builtin.root-storage-remove-pattern", "\\bdata\\s+remove\\s+storage\\b", "i,c"),
                    new(LocalGptRuntimePattern.MalformedStorageTargetPattern, "builtin.malformed-storage-target-pattern", "\\bstore\\s+result\\s+storage\\s+[a-z0-9_.-]+:[a-z0-9_/-]+\\.[a-z0-9_.-]+\\s+(?:byte|short|int|long|float|double)\\b", "i,c"),
                    new(LocalGptRuntimePattern.FrontendPattern, "builtin.frontend-pattern", "(frontend|razor|devexpress|dxaichat|css|javascript)", "i,c"),
                    new(LocalGptRuntimePattern.WholeSolutionPattern, "builtin.whole-solution-pattern", "(whole solution|full solution|entire solution|solution zip|project zip|\\.sln|\\.csproj|all source files|tacosportalopen|localgpt\\s+(?:clone|replacement|workbench|app|application|solution)|(?:clone|replace|rebuild)\\s+localgpt|whole ai host|ai host dotnet|local ai host|whole ollama|ollama dotnet|ollama \\.net)", "i,c"),
                    new(LocalGptRuntimePattern.AiHostExperimentPattern, "builtin.ai-host-experiment-pattern", @"(ai\s*host|local\s*model\s*host|model[- ]file\s*runner|native\s*runner|ollama[- ]compatible|/api/(?:chat|generate|tags|ps|version)|host\s+gpt-oss|provider[- ]compatible).*(dotnet|\.net|blazor|devexpress|aspnet|asp\.net|api|route|endpoint|sqlite|ollama|model|runner)|(dotnet|\.net|blazor|devexpress|aspnet|asp\.net|api|route|endpoint|sqlite|model|runner).*(ai\s*host|local\s*model\s*host|model[- ]file\s*runner|native\s*runner|ollama[- ]compatible|/api/(?:chat|generate|tags|ps|version)|provider[- ]compatible)", "i,s,c"),
                    new(LocalGptRuntimePattern.LocalGptReplacementPattern, "builtin.local-gpt-replacement-pattern", "(localgpt|local gpt).*(clone|replacement|workbench|app|application|solution|dxaichat|ai council|sqlite memory|test lab)|(clone|replace|rebuild).*(localgpt|local gpt)|(dxaichat|ai council|sqlite memory|test lab).*(localgpt|local gpt)", "i,s,c"),
                    new(LocalGptRuntimePattern.TacosPortalPattern, "builtin.tacos-portal-pattern", "(tacosportalopen|tacos portal|restaurant portal|orders.*menu|menu.*orders|reservation|kitchen queue)", "i,c"),
                    new(LocalGptRuntimePattern.BotBackendPattern, "builtin.bot-backend-pattern", "(bot backend|telegram bot|botapi|webhook|conversation state|python\\.net|whisper|translator bot)", "i,c"),
                    new(LocalGptRuntimePattern.LoggingPattern, "builtin.logging-pattern", "(log|logger|diagnostic|error|warning|telemetry)", "i,c"),
                    new(LocalGptRuntimePattern.WhitespacePattern, "builtin.whitespace-pattern", "\\s+", "c"),
                    new(LocalGptRuntimePattern.HelpfulSourceLinePattern, "builtin.helpful-source-line-pattern", "(?im)^\\s*(?:[-*]\\s*)?(?<line>(?:helpful sources?|source request|needed sources?|references?|docs?|documentation|official docs?|examples?|sample projects?|spec(?:ification)?s?|tutorials?)\\s*[:\\-].+)$", "c"),
                    new(LocalGptRuntimePattern.LocalGptKnowledgeBlock, "builtin.localgpt-knowledge-block", "<localgpt-knowledge>(?<body>.*?)</localgpt-knowledge>", "i,s,c"),
                    new(LocalGptRuntimePattern.LocalGptSelfAssessmentBlock, "builtin.localgpt-self-assessment-block", "<localgpt-self-assessment>(?<body>.*?)</localgpt-self-assessment>", "i,s,c"),
                    new(LocalGptRuntimePattern.SolutionProjectReference, "builtin.solution-project-reference", "<ProjectReference\\s+Include=\"(?<path>[^\"]+)\"", "i,c"),
                    new(LocalGptRuntimePattern.CSharpNamespace, "builtin.csharp-namespace", "(?m)^\\s*namespace\\s+(?<namespace>[A-Za-z_][A-Za-z0-9_.]*)\\s*[;{]", "m,c"),
                    new(LocalGptRuntimePattern.CSharpServiceRegistration, "builtin.csharp-service-registration", "Add(?<lifetime>Singleton|Scoped|Transient)(?:<(?<service>[^>,]+)(?:,\\s*(?<implementation>[^>]+))?>|\\((?<expression>[^;]+)\\))", "c"),
                    new(LocalGptRuntimePattern.AspNetControllerRoute, "builtin.aspnet-controller-route", "\\[(?:Route|HttpGet|HttpPost|HttpPut|HttpDelete|HttpPatch)\\((?<route>[^)]*)\\)\\]", "i,c"),
                    new(LocalGptRuntimePattern.DotNetSolutionProject, "builtin.dotnet-solution-project", "Project\\(\"\\{[^}]+\\}\"\\)\\s*=\\s*\"(?<name>[^\"]+)\",\\s*\"(?<path>[^\"]+\\.csproj)\"", "i,c"),
                    new(LocalGptRuntimePattern.InstallerPortContract, "builtin.installer-port-contract", "(?i)(?:default|installer|bootstrap|webview|kestrel|listen|port)[^\\r\\n]{0,120}?(?<port>\\b(?:[1-9][0-9]{2,4})\\b)", "i,c"),
                    new(LocalGptRuntimePattern.OneWireCapabilityKey, "builtin.onewire-capability-key", "(?i)(?:capability|skill|uiActivationKey|operationKey)[^\\r\\n]{0,80}?[\"'](?<key>[a-z0-9][a-z0-9._-]+)[\"']", "i,c"),
                    new(LocalGptRuntimePattern.FilePathWithExtension, "builtin.file-path-with-extension", "(?<path>(?:[A-Za-z]:)?[\\\\/A-Za-z0-9_. -]+\\.(?<extension>[A-Za-z0-9]{1,12}))", "c"),
                    new(LocalGptRuntimePattern.PowerShellInlineCommand, "runtime.native.powershell-inline-command", @"(^|\s)-EncodedCommand(\s|$)|(^|\s)-Command(\s|$)|(^|\s)-c(\s|$)", "i,c,compiled"),
                    new(LocalGptRuntimePattern.PowerShellFile, "runtime.native.powershell-file", @"(^|\s)-File\s+(?:""(?<path>[^""]+)""|'(?<path>[^']+)'|(?<path>\S+))", "i,c,compiled"),
                    new(LocalGptRuntimePattern.SensitiveArgument, "runtime.native.sensitive-argument", @"(?<name>--?(?:api[-_]?key|key|token|secret|password|passwd|pwd))(?<separator>\s+|=)(?<value>""[^""]*""|'[^']*'|\S+)", "i,c,compiled"),
                    new(LocalGptRuntimePattern.DownloadUrl, "runtime.download-url", "\"downloadUrl\"\\s*:\\s*\"(?<url>[^\"]+)\"", "i,c,compiled"),
                    new(LocalGptRuntimePattern.ModelCapabilitySelfAssessment, "runtime.model-capability-self-assessment", "<localgpt-self-assessment>(?<json>[\\s\\S]*?)</localgpt-self-assessment>", "i,c"),
                    new(LocalGptRuntimePattern.CouncilTaggedPlan, "runtime.council.tagged-plan", @"<localgpt-change-review>\s*(?<json>.*?)\s*</localgpt-change-review>", "i,s,c"),
                    new(LocalGptRuntimePattern.CouncilFencedPlan, "runtime.council.fenced-plan", @"```(?:localgpt-change-review|json\s+localgpt-change-review)\s*(?<json>.*?)\s*```", "i,s,c"),
                    new(LocalGptRuntimePattern.ChatHarmonyThinking, "runtime.chat.harmony-thinking", @"<\|start\|>assistant<\|channel\|>(analysis|commentary)<\|message\|>(?<content>.*?)(?=<\|channel\|>|<\|end\|>|$)|<\|channel\|>(analysis|commentary)<\|message\|>(?<content>.*?)(?=<\|channel\|>|<\|end\|>|$)", "i,s,c,compiled"),
                    new(LocalGptRuntimePattern.ChatHarmonyFinal, "runtime.chat.harmony-final", @"<\|start\|>assistant<\|channel\|>final<\|message\|>(?<content>.*?)(?=<\|end\|>|$)|<\|channel\|>final<\|message\|>(?<content>.*?)(?=<\|end\|>|<\|start\|>|$)", "i,s,c,compiled"),
                    new(LocalGptRuntimePattern.ChatHarmonyMarker, "runtime.chat.harmony-marker", @"<\|[^>]+\|>", "i,c,compiled"),
                    new(LocalGptRuntimePattern.RenderThinkingDetailsStart, "runtime.render.thinking-details-start", "<details\\s+class=\\\"model-thinking(?:\\s+open)?\\\"(?:\\s+open)?\\s*>", "i,c,compiled"),
                    new(LocalGptRuntimePattern.CouncilCompletionMarker, "runtime.render.council-completion-marker", @"<!--localgpt-council-stream-complete:(?<id>[a-f0-9]{32})-->", "i,c,compiled"),
                    new(LocalGptRuntimePattern.ListAfterHtml, "runtime.render.list-after-html", @"(</(?:p|details|pre|div)>)\s*((?:[-*]|\d+\.)\s+)", "i,c,compiled"),
                    new(LocalGptRuntimePattern.ControlledDetailsStart, "runtime.render.controlled-details-start", "<details\\s+class=\\\"(?:model-thinking(?:\\s+open)?|council-step(?:\\s+council-live)?|council-prompt)\\\"[^>]*>", "i,c,compiled"),
                    new(LocalGptRuntimePattern.DetailsEnd, "runtime.render.details-end", @"</details>", "i,c,compiled"),
                    new(LocalGptRuntimePattern.StablePanelStart, "runtime.render.stable-panel-start", "<details\\s+class=\\\"(?<class>model-thinking(?:\\s+open)?|council-step(?:\\s+council-live)?|council-prompt)\\\"(?<attributes>[^>]*)>", "i,c,compiled"),
                    new(LocalGptRuntimePattern.StreamIdAttribute, "runtime.render.stream-id-attribute", "data-localgpt-stream-id=\\\"(?<id>[a-f0-9]{32})\\\"", "i,c,compiled"),
                    new(LocalGptRuntimePattern.PreStart, "runtime.render.pre-start", @"<pre(?:\s[^>]*)?>", "i,c,compiled"),
                    new(LocalGptRuntimePattern.PreEnd, "runtime.render.pre-end", @"</pre>", "i,c,compiled"),
                ]
            };
            logger.LogInformation($"Prepared {seed.Values.Count} runtime values, {seed.Collections.Count} runtime collections and {seed.RegexPatterns.Count} runtime regex records.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not prepare LocalGPT runtime-policy seed data: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Retrieves seed as part of the LocalGPT runtime policy seed service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The LocalGPT runtime policy seed model produced by the operation.</returns>
    public LocalGptRuntimePolicySeedModel GetSeed()
    {
        try
        {
            logger.LogTrace($"Returned the LocalGPT runtime-policy seed model.");
            return seed;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not return LocalGPT runtime-policy seed data: {exception.Message}");
            throw;
        }
    }
}
