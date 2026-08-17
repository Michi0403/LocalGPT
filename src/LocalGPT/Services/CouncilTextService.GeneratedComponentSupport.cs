using DevExpress.AIIntegration.Blazor.Chat;
using DevExpress.Blazor.Viewer.Internal;
using DevExpress.DataAccess.DataFederation;
using DevExpress.Utils.About;
using DevExpress.XtraCharts;
using DevExpress.XtraReports.Serialization;
using DevExpress.XtraRichEdit.Import.Html;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Services;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.CSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.AI;
using SQLitePCL;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Net;
using System.Reactive;
using System.Security.AccessControl;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.RegularExpressions;
using LocalGPT.Extensions;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates council text behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class CouncilTextService
    {
        /// <summary>
        /// Generates blazor DevExpress razor example as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="result">Result value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GenerateBlazorDevExpressRazorExample(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result, ILogger logger)
        {
            try
            {
                var requestSummary = TrimForCodeComment(request.Prompt, 700, logger);
                var consensusSummary = TrimForCodeComment(result.FinalAnswer, 900, logger);
                return $$"""
                @page "/generated/localgpt-health-summary"
                @rendermode InteractiveServer
                @using DevExpress.Blazor

                <PageTitle>LocalGPT Health Summary</PageTitle>

                <div class="main-container generated-feature-page">
                    <h3>LocalGPT Health Summary</h3>

                    <DxLoadingPanel CssClass="w-100"
                                    @bind-Visible="PanelVisible"
                                    CloseOnClick="true"
                                    IndicatorVisible="true"
                                    IsContentBlocked="false"
                                    IndicatorAreaVisible="false"
                                    Text="Refreshing diagnostics...">
                        <div class="top-container">
                            <DxButton Text="Refresh"
                                      RenderStyle="ButtonRenderStyle.Primary"
                                      RenderStyleMode="ButtonRenderStyleMode.Contained"
                                      Click="RefreshAsync" />
                            <DxCheckBox @bind-Checked="ShowTechnicalDetails"
                                        Text="Show technical details" />
                        </div>

                        <DxGrid Data="@Cards"
                                KeyFieldName="@nameof(HealthCard.Area)"
                                ShowSearchBox="true"
                                ShowFilterRow="true"
                                AllowSort="true"
                                HighlightRowOnHover="true"
                                TextWrapEnabled="false"
                                ColumnResizeMode="GridColumnResizeMode.NextColumn">
                            <Columns>
                                <DxGridDataColumn FieldName="@nameof(HealthCard.Area)" Caption="Area" />
                                <DxGridDataColumn FieldName="@nameof(HealthCard.Status)" Caption="Status" />
                                <DxGridDataColumn FieldName="@nameof(HealthCard.NextAction)" Caption="Next Action" />
                                @if (ShowTechnicalDetails)
                                {
                                    <DxGridDataColumn FieldName="@nameof(HealthCard.Detail)" Caption="Detail" />
                                }
                            </Columns>
                        </DxGrid>

                        <DxFormLayout CssClass="mt-3" SizeMode="SizeMode.Medium">
                            <DxFormLayoutGroup Caption="Implementation Note" ColSpanMd="12">
                                <DxFormLayoutItem Caption="Request" ColSpanMd="12">
                                    <DxMemo Text="@RequestSummary" Rows="4" ReadOnly="true" />
                                </DxFormLayoutItem>
                                <DxFormLayoutItem Caption="Council Consensus" ColSpanMd="12">
                                    <DxMemo Text="@CouncilConsensus" Rows="5" ReadOnly="true" />
                                </DxFormLayoutItem>
                            </DxFormLayoutGroup>
                        </DxFormLayout>
                    </DxLoadingPanel>
                </div>

                @code {
                    bool PanelVisible { get; set; }
                    bool ShowTechnicalDetails { get; set; } = true;
                    List<HealthCard> Cards { get; set; } = new();
                    string RequestSummary { get; } = "{{EscapeCSharpString(requestSummary, logger)}}";
                    string CouncilConsensus { get; } = "{{EscapeCSharpString(consensusSummary, logger)}}";

                    protected override Task OnInitializedAsync() => RefreshAsync();

                    Task RefreshAsync()
                    {
                        PanelVisible = true;
                        Cards =
                        [
                            new("AI Host", "Needs verification", "Check /__diag/council/models before selecting a model.", "Use CPU-only mode after a GPU driver reset."),
                            new("Blazor UI", "Prototype", "Add this page under Components/Pages, then add a NavMenu entry if the user approves integration.", "Uses @rendermode InteractiveServer and known DevExpress Blazor components."),
                            new("Download Route", "Ready", "Serve generated files through /__artifacts/council/{fileName}.", "Keep generated code sandboxed until the user explicitly permits integration.")
                        ];
                        PanelVisible = false;
                        return Task.CompletedTask;
                    }

                    sealed record HealthCard(string Area, string Status, string NextAction, string Detail);
                }
                """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "GenerateBlazorDevExpressRazorExample");
                return string.Empty;
            }
        }

        /// <summary>
        /// Generates blazor support code as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="result">Result value supplied to the council text operation and used when producing its result.</param>
        /// <param name="targetArea">Target area value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GenerateBlazorSupportCode(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            string targetArea, ILogger logger)
        {
            try
            {
                return $$"""
                // <auto-generated>
                // LocalGPT AI Council Blazor support example.
                // </auto-generated>

                namespace LocalGPT.GeneratedExamples;

                public sealed record LocalGptGeneratedHealthCard(
                    string Area,
                    string Status,
                    string NextAction,
                    string Detail);

                public sealed class LocalGptGeneratedHealthSummaryService
                {
                    public const string TargetArea = "{{EscapeCSharpString(targetArea, logger)}}";
                    public const string CouncilMembers = "{{EscapeCSharpString(string.Join(", ", result.ModelNames), logger)}}";
                    public const string OriginalRequest = "{{EscapeCSharpString(TrimForCodeComment(request.Prompt, 900, logger), logger)}}";

                    public IReadOnlyList<LocalGptGeneratedHealthCard> GetCards()
                    {
                        return
                        [
                            new("AI Host", "Needs verification", "Call /__diag/council/models and keep unstable runs CPU-only.", "Do not assume Ollama or LM Studio is running until discovery confirms it."),
                            new("Blazor UI", "Prototype", "Generate a .razor page with @page, @rendermode InteractiveServer, and DevExpress controls.", "Prefer DxGrid, DxFormLayout, DxButton, DxCheckBox, DxMemo, and existing LocalGPT CSS classes."),
                            new("Sandbox", "Required", "Keep generated code downloadable until the user permits integration.", "Generated features must never self-expand into the real project without user approval.")
                        ];
                    }
                }
                """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "GenerateBlazorSupportCode");
                return string.Empty;
            }

        }

        /// <summary>
        /// Generates code DOM example as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="result">Result value supplied to the council text operation and used when producing its result.</param>
        /// <param name="targetArea">Target area value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GenerateCodeDomExample(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            string targetArea, ILogger logger)
        {
            try
            {
                var unit = new CodeCompileUnit();
                var namespaceDeclaration = new CodeNamespace("LocalGPT.GeneratedExamples");
                namespaceDeclaration.Imports.Add(new CodeNamespaceImport("System"));
                namespaceDeclaration.Imports.Add(new CodeNamespaceImport("System.Text"));
                unit.Namespaces.Add(namespaceDeclaration);

                var type = new CodeTypeDeclaration("CouncilFeatureRequestExample")
                {
                    IsClass = true,
                    TypeAttributes = System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Sealed
                };
                type.Comments.Add(new CodeCommentStatement("Generated by LocalGPT AI Council as a downloadable implementation sketch."));
                type.Comments.Add(new CodeCommentStatement("Treat this as a starter example, not as production code."));
                namespaceDeclaration.Types.Add(type);

                var privateConstructor = new CodeConstructor
                {
                    Attributes = MemberAttributes.Private
                };
                type.Members.Add(privateConstructor);

                type.Members.Add(CreateConstant("TargetArea", targetArea, logger));
                type.Members.Add(CreateConstant("CouncilMembers", string.Join(", ", result.ModelNames, logger), logger));
                type.Members.Add(CreateConstant("OriginalRequest", TrimForCodeComment(request.Prompt, 900, logger), logger));

                var method = new CodeMemberMethod
                {
                    Name = "BuildImplementationRequestMarkdown",
                    Attributes = MemberAttributes.Public | MemberAttributes.Static,
                    ReturnType = new CodeTypeReference(typeof(string))
                };
                method.Comments.Add(new CodeCommentStatement("This shape can be pasted into DXAiChat or an AI Council continuation round."));
                method.Statements.Add(new CodeVariableDeclarationStatement(typeof(StringBuilder), "builder", new CodeObjectCreateExpression(typeof(StringBuilder))));
                AppendLine(method, "# LocalGPT Implementation Request", logger);
                AppendLine(method, "", logger);
                AppendLine(method, $"Target area: {targetArea}", logger);
                AppendLine(method, $"Council members: {string.Join(", ", result.ModelNames)}", logger);
                AppendLine(method, "", logger);
                AppendLine(method, "## Requested feature", logger);
                AppendLine(method, TrimForCodeComment(request.Prompt, 1000, logger), logger);
                AppendLine(method, "", logger);
                AppendLine(method, "## Current council consensus", logger);
                AppendLine(method, TrimForCodeComment(result.FinalAnswer, 1600, logger), logger);
                AppendLine(method, "", logger);
                AppendLine(method, "## Implementation checklist", logger);
                AppendLine(method, "- Identify the owning LocalGPT service/page/project.", logger);
                AppendLine(method, "- Check /__diag/devexpress before proposing DevExpress APIs or UI components.", logger);
                AppendLine(method, "- Put DevExpress Office/report/PDF/export generation in ASP.NET Core backend services and expose safe download links.", logger);
                AppendLine(method, "- Keep native commands in backend services.", logger);
                AppendLine(method, "- Save user-visible state to EF/SQLite when it affects future chats.", logger);
                AppendLine(method, "- Prototype requested features in a harmless sandbox artifact or temporary workspace before integrating into the real project.", logger);
                AppendLine(method, "- Ask the user for explicit permission before integrating any generated expansion into LocalGPT.", logger);
                AppendLine(method, "- Never overrule a user decision that denies or limits self-expansion.", logger);
                AppendLine(method, "- List helpful official docs, examples, specs, or source repositories needed before implementation.", logger);
                AppendLine(method, "- Add a diagnostic endpoint or smoke path before relying on UI behavior.", logger);
                AppendLine(method, "- Mark unknown dependencies as Needs verification.", logger);
                method.Statements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("builder"), "ToString")));
                type.Members.Add(method);

                using var writer = new StringWriter();
                writer.WriteLine("// <auto-generated>");
                writer.WriteLine("// LocalGPT AI Council implementation example.");
                writer.WriteLine("// </auto-generated>");
                writer.WriteLine();

                using var provider = new CSharpCodeProvider();
                provider.GenerateCodeFromCompileUnit(unit, writer, new CodeGeneratorOptions
                {
                    BracingStyle = "C",
                    BlankLinesBetweenMembers = true
                });

                return writer.ToString();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "GenerateBlazorSupportCode");
                return string.Empty;
            }

            
        }

        /// <summary>
        /// Creates constant as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="name">Name value supplied to the council text operation and used when producing its result.</param>
        /// <param name="value">Value value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The code member field produced by the operation.</returns>
        public CodeMemberField? CreateConstant(string name, string value, ILogger logger)
        {
            try
            {
                return new CodeMemberField(typeof(string), name)
                {
                    Attributes = MemberAttributes.Public | MemberAttributes.Const,
                    InitExpression = new CodePrimitiveExpression(value)
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CreateConstant name:{name} value:{value}");
                return null;
            }
        }
        /// <summary>
        /// Retrieves discovered model button text as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="model">Model value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GetDiscoveredModelButtonText(LocalAiModelInfo model, ILogger logger)
        {
            try
            {
                var state = model.IsLoaded ? "loaded" : "installed";
                return string.IsNullOrWhiteSpace(model.Details)
                    ? $"{model.Name} ({state})"
                    : $"{model.Name} ({state}, {model.Details})";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetDiscoveredModelButtonText model:{model.ToString()}");
                return string.Empty;
            }

        }
        /// <summary>
        /// Performs append line as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="method">Method value supplied to the council text operation and used when producing its result.</param>
        /// <param name="line">Line value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        public void AppendLine(CodeMemberMethod method, string line, ILogger logger)
        {
            try
            {
                method.Statements.Add(new CodeMethodInvokeExpression(
                 new CodeVariableReferenceExpression("builder"),
                 "AppendLine",
                 new CodePrimitiveExpression(line)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in AppendLine method:{method.ToString()} line:{line}");
            }
        }

        /// <summary>
        /// Performs to pascal identifier as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string ToPascalIdentifier(string value, ILogger logger)
        {
            try
            {
                var words = patterns.AlphaNumericWordPattern.Matches(value)
                .Select(match => match.Value)
                .Where(word => !string.IsNullOrWhiteSpace(word))
                .Take(5);
                return string.Concat(words.Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ToPascalIdentifier value:{value}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Performs to kebab route as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string ToKebabRoute(string value, ILogger logger)
        {
            try
            {
                var normalized = patterns.IdentifierSeparatorPattern.Replace(value.ToLowerInvariant(), "-").Trim('-');
                return string.IsNullOrWhiteSpace(normalized) ? "promise-module" : normalized;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ToKebabRoute value:{value}");
                return string.Empty;
            }
        }

    }
}
