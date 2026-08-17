using Azure;
using Azure.AI.OpenAI;
using DevExpress.AIIntegration.Blazor.Chat;
using DevExpress.CodeParser;
using DevExpress.DataProcessing.InMemoryDataProcessor;
using DevExpress.XtraCharts;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Components;
using LocalGPT.Diagnostics;
using LocalGPT.Helper;
using LocalGPT.Hubs;
using LocalGPT.Interfaces;
using LocalGPT.Services;
using LocalGPT.Services.Formatting;
using LocalGPT.Services.Persistence;
using LocalGPT.Services.OneWire;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using LocalGPT.Services.Helpers;

namespace LocalGPT
{
    /// <summary>
    /// Represents a program application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    public static partial class Program
    {
        /// <summary>
        /// Performs configure middleware and endpoints for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
        /// </summary>
        /// <param name="app">App value supplied to the program operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        private static void ConfigureMiddlewareAndEndpoints(WebApplication app, ILogger logger)
        {
            try
            {
                if (!app.Environment.IsDevelopment())
                {
                    app.UseExceptionHandler("/Error", createScopeForErrors: true);
                    app.UseHsts();
                }
                _ = app.UseForwardedHeaders(
                    new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                    });
                // The bundled desktop/WebView host binds to a random HTTP loopback port.
                // HTTPS redirection has no target port there and only produces noisy startup warnings.
                _ = app.UseRequestLocalization();
                app.UseStaticFiles();
                app.UseRouting();
                if (!app.Environment.IsDevelopment())
                    _ = app.UseResponseCompression();
                app.UseAntiforgery();                 // ✅ after routing, before endpoints
                app.MapControllers();
                _ = app.MapHub<ChatHub>("/chathub");
                app.MapStaticAssets();
                app.MapHealthChecks("/health");
                //should be autoresolved soon via MapControllers
                //app.MapLocalGptDiagnosticEndpoints(logger);
                //app.MapMinecraftDiagnosticEndpoints();
                app.MapRazorComponents<App>()
                   .AddInteractiveServerRenderMode()
                   .AllowAnonymous();
                //using (var scope = app.Services.CreateScope())
                //{
                //    var migrator = new MigrationMigratorFactory()
                //        .Create<MigrationBuilder>(scope.ServiceProvider);

                //    await migrator.MigrateAsync();
                //}
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ConfigureMiddlewareAndEndpoints");
                //TryAppendStartupTrace(ex.ToString(), logger);
            }
        }

        /// <summary>
        /// Determines whether generated static web asset root for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
        /// </summary>
        /// <param name="path">Path value supplied to the program operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        private static bool IsGeneratedStaticWebAssetRoot(string path, ILogger logger)
        {
            try
            {
                var normalized = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                var objSegment = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}";
                if (!normalized.Contains(objSegment, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                var trimmed = normalized.TrimEnd(Path.DirectorySeparatorChar);
                return trimmed.EndsWith($"{Path.DirectorySeparatorChar}compressed", StringComparison.OrdinalIgnoreCase)
                    || trimmed.EndsWith(
                        $"{Path.DirectorySeparatorChar}scopedcss{Path.DirectorySeparatorChar}bundle",
                        StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsGeneratedStaticWebAssetRoot path {path}");
                //TryAppendStartupTrace(ex.ToString(), logger);
                return false;
            }
        }

        /// <summary>
        /// Writes runtime endpoint file for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
        /// </summary>
        /// <param name="port">Port value supplied to the program operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        private static void WriteRuntimeEndpointFile(int port, ILogger logger)
        {
            try
            {
                var directory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "LocalGPT",
                    "runtime");
                Directory.CreateDirectory(directory);

                var payload = new
                {
                    ProcessId = Environment.ProcessId,
                    BaseUrl = BaseUrl,
                    Port = port,
                    OneWirePort,
                    OneWireDiscoveryPort,
                    StartedAtUtc = DateTimeOffset.UtcNow
                };

                File.WriteAllText(
                    Path.Combine(directory, "server.json"),
                    JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
                Console.WriteLine($"LocalGPT listening on {BaseUrl}");
                logger.LogInformation("LocalGPT runtime endpoint {BaseUrl} was written for process {ProcessId}.", BaseUrl, Environment.ProcessId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in WriteRuntimeEndpointFile");
                //TryAppendStartupTrace(ex.ToString(), logger);
            }
        }

        /// <summary>
        /// Deletes runtime endpoint file for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
        /// </summary>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        private static void DeleteRuntimeEndpointFile(ILogger logger)
        {
            try
            {
                var path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "LocalGPT",
                    "runtime",
                    "server.json");
                if (!File.Exists(path))
                    return;

                using var document = JsonDocument.Parse(File.ReadAllText(path));
                if (document.RootElement.TryGetProperty("ProcessId", out var processId)
                    && processId.TryGetInt32(out var ownerProcessId)
                    && ownerProcessId == Environment.ProcessId)
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not remove the LocalGPT runtime endpoint file during shutdown.");
            }
        }

        /// <summary>
        /// Retrieves free port for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
        /// </summary>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The int produced by the operation.</returns>
        private static int GetFreePort(ILogger logger)
        {
            try
            {
                var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                var port = ((IPEndPoint)listener.LocalEndpoint).Port;
                listener.Stop();
                return port;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetFreePort");
                //TryAppendStartupTrace(ex.ToString(), logger);
                return 0;
            }
        }

    
    }
}
