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
        /// Performs configure response compression for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
        /// </summary>
        /// <param name="services">Service collection dependency used by the program workflow to provide the corresponding application capability.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        private static void ConfigureResponseCompression(IServiceCollection services, ILogger logger)
        {
            try
            {
                services.AddResponseCompression(options =>
                {
                    options.EnableForHttps = true;
                    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    [
                        "application/octet-stream"
                    ]);
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ConfigureResponseCompression");
                //TryAppendStartupTrace(ex.ToString(), logger);
            }

        }

        /// <summary>
        /// Performs configure blazor and mvc for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
        /// </summary>
        /// <param name="builder">Builder value supplied to the program operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        private static void ConfigureBlazorAndMvc(WebApplicationBuilder builder, ILogger logger)
        {
            try
            {
                StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

                builder.Services.AddRazorComponents().AddInteractiveServerComponents();
                builder.Services.AddSingleton<CircuitHandler, LocalGptCircuitDiagnosticsHandler>();
                builder.Services.AddLocalization();
                builder.Services.AddSingleton<LocalGPT.Services.Localization.ILocalGptLocalizationService, LocalGPT.Services.Localization.LocalGptLocalizationService>();
                builder.Services.Configure<RequestLocalizationOptions>(options =>
                {
                    // Accept every culture known to the installed .NET runtime. LocalGPT catalogs remain
                    // opt-in files; missing keys continue to fall back through en-US.
                    var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures)
                        .Where(culture => !string.IsNullOrWhiteSpace(culture.Name))
                        .GroupBy(culture => culture.Name, StringComparer.OrdinalIgnoreCase)
                        .Select(group => group.First())
                        .ToArray();
                    options.DefaultRequestCulture = new RequestCulture("en-US");
                    options.SupportedCultures = cultures;
                    options.SupportedUICultures = cultures;
                    options.RequestCultureProviders =
                    [
                        new QueryStringRequestCultureProvider
                        {
                            QueryStringKey = "culture",
                            UIQueryStringKey = "ui-culture"
                        },
                        new CookieRequestCultureProvider()
                    ];
                });
                builder.Services.AddHealthChecks();
                builder.Services.AddDevExpressBlazor(options => options.SizeMode = DevExpress.Blazor.SizeMode.Medium);
                builder.Services.AddScoped<ControllerRequestLoggingFilter>();
                builder.Services.AddMvc(options =>
                    options.Filters.AddService<ControllerRequestLoggingFilter>());
                builder.Services.AddScoped<ThemeService>();
                builder.Services.AddDevExpressServerSideBlazorPdfViewer();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ConfigureBlazorAndMvc");
                //TryAppendStartupTrace(ex.ToString(), logger);
            }
        }

        /// <summary>
        /// Performs configure JSON options for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
        /// </summary>
        /// <param name="services">Service collection dependency used by the program workflow to provide the corresponding application capability.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        private static void ConfigureJsonOptions(IServiceCollection services, ILogger logger)
        {
            try
            {
                services.Configure<JsonOptions>(options =>
                {
                    ConfigureSharedJsonSerializerOptions(options.JsonSerializerOptions, logger);
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ConfigureJsonOptions");
                //TryAppendStartupTrace(ex.ToString(), logger);
            }
        }

        /// <summary>
        /// Performs configure shared JSON serializer options for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
        /// </summary>
        /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        private static void ConfigureSharedJsonSerializerOptions(JsonSerializerOptions options, ILogger logger)
        {
            try
            {
                options.PropertyNameCaseInsensitive = true;
                options.WriteIndented = true;
                options.PropertyNamingPolicy = null;
                options.IgnoreReadOnlyFields = false;
                options.IgnoreReadOnlyProperties = false;
                options.IncludeFields = false;
                options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.AllowTrailingCommas = true;
                options.Converters.Add(new JsonStringEnumConverter());
                options.NumberHandling = JsonNumberHandling.AllowReadingFromString |
                    JsonNumberHandling.WriteAsString;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ConfigureSharedJsonSerializerOptions");
                //TryAppendStartupTrace(ex.ToString(), logger);
            }
        }

        /// <summary>
        /// Performs configure forwarded headers for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
        /// </summary>
        /// <param name="services">Service collection dependency used by the program workflow to provide the corresponding application capability.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        private static void ConfigureForwardedHeaders(IServiceCollection services, ILogger logger)
        {
            try
            {
                services.Configure<ForwardedHeadersOptions>(options =>
                {
                    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                    options.KnownIPNetworks.Clear();
                    options.KnownProxies.Clear();
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ConfigureJsonOptions");
                //TryAppendStartupTrace(ex.ToString(), logger);
            }
        }

    }
}
