using DevExpress.CodeParser;
using DevExpress.Xpo;
using DevExpress.XtraCharts;
using DevExpress.XtraRichEdit.Import.Html;
using LocalGPT.BusinessObjects;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.AI;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates council runtime behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class CouncilRuntimeService
    {
    /// <summary>Executes the ollama thinking chat client ensure success or throw async operation.</summary>
        /// <summary>
        /// Performs Ollama thinking chat client ensure success or throw as part of the council runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="response">Input value for response.</param>
        /// <param name="cancellationToken">Input value for cancellationToken.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        public async Task OllamaThinkingChatClientEnsureSuccessOrThrowAsync(HttpResponseMessage response, CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                if (response.IsSuccessStatusCode)
                    return;

                var body = await OllamaThinkingChatClientReadErrorBodyAsync(response, cancellationToken, logger).ConfigureAwait(false);
                var message = string.IsNullOrWhiteSpace(body)
                    ? $"Ollama returned {(int)response.StatusCode} {response.StatusCode}."
                    : $"Ollama returned {(int)response.StatusCode} {response.StatusCode}: {body}";
                throw new HttpRequestException(message, null, response.StatusCode);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ollama HTTP response validation failed with status {StatusCode}.", response.StatusCode);

            }
        }

        /// <summary>Executes the ollama thinking chat client read error body async operation.</summary>
        /// <param name="response">Input value for response.</param>
        /// <param name="cancellationToken">Input value for cancellationToken.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>A task that completes with the operation result.</returns>
        public async Task<string> OllamaThinkingChatClientReadErrorBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(body))
                    return string.Empty;

                return body.Length <= 4000 ? body.Trim() : body[..4000].Trim() + "...";
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return string.Empty;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not read an Ollama error response body with status {StatusCode}.", response.StatusCode);
                return string.Empty;
            }
        }

        /// <summary>Executes the ollama thinking chat client create streaming update operation.</summary>
        /// <param name="text">Input value for text.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public ChatResponseUpdate? OllamaThinkingChatClientCreateStreamingUpdate(string text, ILogger logger)
        {
            try
            {
                return new(ChatRole.Assistant, [new TextContent(text)]);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CreateStreamingUpdate text {text.ToString()}");
                return null;
            }
        }


        /// <summary>
        /// Determines whether LocalGPT streaming status update as part of the council runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="text">Text value supplied to the council runtime operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        public bool IsLocalGptStreamingStatusUpdate(string? text, ILogger logger)
        {
            try
            {
                return !string.IsNullOrWhiteSpace(text)
                    && text.Contains("class=\"localgpt-stream-status\"", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not classify a LocalGPT streaming status update.");
                return false;
            }
        }

        /// <summary>Executes the ollama thinking chat client create streaming status update operation.</summary>
        /// <param name="text">Input value for text.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public ChatResponseUpdate? OllamaThinkingChatClientCreateStreamingStatusUpdate(string text, ILogger logger)
        {
            try
            {
                return OllamaThinkingChatClientCreateStreamingUpdate($"<p class=\"localgpt-stream-status\"><em>{WebUtility.HtmlEncode(text)}</em></p>\n\n", logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CreateStreamingStatusUpdate text {text.ToString()}");
                return null;
            }
        }

        /// <summary>Executes the append DevExpress imports async operation.</summary>
        /// <param name="builder">Input value for builder.</param>
        /// <param name="root">Input value for root.</param>
        /// <param name="cancellationToken">Input value for cancellationToken.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        public async Task AppendDevExpressImportsAsync(StringBuilder builder, string root, CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                var importsPath = Path.Combine(root, "src", "LocalGPT", "Components", "_Imports.razor");
                if (!File.Exists(importsPath))
                    return;

                var text = await File.ReadAllTextAsync(importsPath, cancellationToken).ConfigureAwait(false);
                var imports = catalog.DevExpressImportPattern
                    .Matches(text)
                    .Select(match => match.Groups["namespace"].Value.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(value => value)
                    .ToList();

                if (imports.Count == 0)
                    return;

                builder.AppendLine("- Imported DevExpress namespaces in Blazor:");
                foreach (var item in imports)
                    builder.AppendLine($"  - {item}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in AppendDevExpressImportsAsync builder {builder.ToString()} root {root.ToString()}");
            }

        }

        /// <summary>Executes the append DevExpress registrations async operation.</summary>
        /// <param name="builder">Input value for builder.</param>
        /// <param name="root">Input value for root.</param>
        /// <param name="cancellationToken">Input value for cancellationToken.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        public async Task AppendDevExpressRegistrationsAsync(StringBuilder builder, string root, CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                var programPath = Path.Combine(root, "src", "LocalGPT", "Program.cs");
                if (!File.Exists(programPath))
                    return;

                var text = await File.ReadAllTextAsync(programPath, cancellationToken).ConfigureAwait(false);
                var registrations = catalog.DevExpressRegistrationPattern
                    .Matches(text)
                    .Select(match => match.Value.TrimEnd('('))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(value => value)
                    .ToList();

                if (registrations.Count == 0)
                    return;

                builder.AppendLine("- DevExpress services registered in ASP.NET Core:");
                foreach (var registration in registrations)
                    builder.AppendLine($"  - {registration}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in AppendDevExpressRegistrationsAsync builder {builder.ToString()} root {root.ToString()}");
            }
            
        }

        /// <summary>Executes the append loaded DevExpress assemblies operation.</summary>
        /// <param name="builder">Input value for builder.</param>
        /// <param name="logger">Input value for logger.</param>
        public void AppendLoadedDevExpressAssemblies(StringBuilder builder, ILogger logger)
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetName())
                .Where(name => name.Name?.StartsWith("DevExpress.", StringComparison.OrdinalIgnoreCase) == true)
                .OrderBy(name => name.Name)
                .Take(30)
                .ToList();

                if (assemblies.Count == 0)
                    return;

                builder.AppendLine("- Loaded DevExpress assemblies:");
                foreach (var assembly in assemblies)
                    builder.AppendLine($"  - {assembly.Name} {assembly.Version}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in AppendLoadedDevExpressAssemblies builder {builder.ToString()}");
            }
            
        }

        /// <summary>Executes the find repository root operation.</summary>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string? FindRepositoryRoot(ILogger logger)
        {
            try
            {
                foreach (var start in new[]
            {
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory
            })
                {
                    var directory = new DirectoryInfo(start);
                    while (directory is not null)
                    {
                        if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")) ||
                            Directory.Exists(Path.Combine(directory.FullName, ".git")))
                        {
                            return directory.FullName;
                        }

                        directory = directory.Parent;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in FindRepositoryRoot");
                return null;
            }
            
        }
        /// <summary>Executes the create chat upload smoke zip operation.</summary>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public byte[] CreateChatUploadSmokeZip( ILogger logger)
        {
            try
            {
                using var memory = new MemoryStream();
                using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
                {
                    WriteZipEntry(archive, "WeatherHost/WeatherHost.sln", """
                    Microsoft Visual Studio Solution File, Format Version 12.00
                    # Visual Studio Version 17
                    Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "WeatherHost", "src\WeatherHost\WeatherHost.csproj", "{11111111-1111-1111-1111-111111111111}"
                    EndProject
                    Global
                    EndGlobal
                    """, logger);
                    WriteZipEntry(archive, "WeatherHost/src/WeatherHost/WeatherHost.csproj", """
                    <Project Sdk="Microsoft.NET.Sdk.Web">
                      <PropertyGroup>
                        <TargetFramework>net10.0</TargetFramework>
                        <Nullable>enable</Nullable>
                        <ImplicitUsings>enable</ImplicitUsings>
                      </PropertyGroup>
                    </Project>
                    """, logger);
                    WriteZipEntry(archive, "WeatherHost/src/WeatherHost/Program.cs", """
                    using WeatherHost.Services;

                    var builder = WebApplication.CreateBuilder(args);
                    builder.Services.AddRazorPages();
                    builder.Services.AddServerSideBlazor();
                    builder.Services.AddScoped<WeatherForecastService>();

                    var app = builder.Build();
                    app.MapGet("/api/weather", (WeatherForecastService service) => service.GetForecasts());
                    app.MapBlazorHub();
                    app.MapFallbackToPage("/_Host");
                    app.Run();
                    """, logger);
                    WriteZipEntry(archive, "WeatherHost/src/WeatherHost/Services/WeatherForecastService.cs", """
                    namespace WeatherHost.Services;

                    public sealed class WeatherForecastService
                    {
                        public IReadOnlyList<WeatherForecast> GetForecasts() =>
                        [
                            new(DateOnly.FromDateTime(DateTime.Today), 21, "Clear"),
                            new(DateOnly.FromDateTime(DateTime.Today.AddDays(1)), 18, "Rain"),
                            new(DateOnly.FromDateTime(DateTime.Today.AddDays(2)), 24, "Sunny")
                        ];
                    }

                    public sealed record WeatherForecast(DateOnly Date, int TemperatureC, string Summary);
                    """, logger);
                    WriteZipEntry(archive, "WeatherHost/src/WeatherHost/Pages/Index.razor", """
                    @page "/"
                    @inject WeatherHost.Services.WeatherForecastService Weather

                    <h1>Weather Host</h1>

                    <ul>
                        @foreach (var item in Weather.GetForecasts())
                        {
                            <li>@item.Date: @item.TemperatureC C, @item.Summary</li>
                        }
                    </ul>
                    """, logger);
                }

                return memory.ToArray();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"CreateChatUploadSmokeZip");
                return new byte[0];
            }
        }
        /// <summary>Executes the write zip entry operation.</summary>
        /// <param name="archive">Input value for archive.</param>
        /// <param name="path">Input value for path.</param>
        /// <param name="content">Input value for content.</param>
        /// <param name="logger">Input value for logger.</param>
        public void WriteZipEntry(ZipArchive archive, string path, string content, ILogger logger)
        {
            try
            {
                var entry = archive.CreateEntry(path, CompressionLevel.SmallestSize);
                using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
                writer.Write(content.Replace("                    ", string.Empty, StringComparison.Ordinal));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "WriteZipEntry");
            }
        }

    }
}
