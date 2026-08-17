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
        /// Performs configure kestrel for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
        /// </summary>
        /// <param name="builder">Builder value supplied to the program operation and used when producing its result.</param>
        /// <param name="requestedPort">Requested port value supplied to the program operation and used when producing its result.</param>
        /// <param name="args">Args value supplied to the program operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The int produced by the operation.</returns>
        private static int ConfigureKestrel(WebApplicationBuilder builder, int requestedPort, string[]? args, ILogger logger)
        {
            try
            {
                var port = requestedPort > 0 ? requestedPort : GetFreePort(logger);
                var remote = ResolveRemoteWebEndpoint(args, builder.Configuration, builder.Environment.ContentRootPath, builder.Environment.ApplicationName, logger);

                builder.WebHost.ConfigureKestrel(options =>
                {
                    // Keep the historical desktop/installer endpoint exactly loopback-only.
                    options.Limits.MaxRequestBodySize = null;
                    options.Limits.MaxRequestBufferSize = null;
                    options.Listen(IPAddress.Loopback, port);

                    if (remote is null)
                        return;

                    void ConfigureRemote(Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions listen)
                    {
                        if (!string.IsNullOrWhiteSpace(remote.CertificatePath))
                        {
                            listen.UseHttps(remote.CertificatePath, remote.CertificatePassword);
                        }
                    }

                    if (remote.Address.Equals("0.0.0.0", StringComparison.OrdinalIgnoreCase) ||
                        remote.Address.Equals("*", StringComparison.OrdinalIgnoreCase))
                    {
                        options.ListenAnyIP(remote.Port, ConfigureRemote);
                    }
                    else if (remote.Address.Equals("::", StringComparison.OrdinalIgnoreCase))
                    {
                        options.Listen(IPAddress.IPv6Any, remote.Port, ConfigureRemote);
                    }
                    else if (IPAddress.TryParse(remote.Address, out var bindAddress))
                    {
                        options.Listen(bindAddress, remote.Port, ConfigureRemote);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Configured LocalGPT remote bind address '{remote.Address}' is not an IP address. Use 0.0.0.0, ::, or a concrete interface address.");
                    }
                });

                if (remote is not null)
                {
                    var scheme = string.IsNullOrWhiteSpace(remote.CertificatePath) ? "http" : "https";
                    logger.LogWarning(
                        "Optional LocalGPT network endpoint enabled at {Scheme}://{Address}:{Port}. Access control remains the responsibility of the host firewall/VPN; HTTP does not encrypt browser traffic.",
                        scheme, remote.Address, remote.Port);
                }

                return port;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Kestrel endpoint configuration failed.");
                throw;
            }
        }

        /// <summary>
        /// Resolves remote web endpoint for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
        /// </summary>
        /// <param name="args">Args value supplied to the program operation and used when producing its result.</param>
        /// <param name="configuration">Configuration containing the caller-supplied values that control this operation.</param>
        /// <param name="contentRootPath">Content root path value supplied to the program operation and used when producing its result.</param>
        /// <param name="applicationName">Application name value supplied to the program operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The remote web endpoint options produced by the operation.</returns>
        private static RemoteWebEndpointOptions? ResolveRemoteWebEndpoint(
            string[]? args,
            IConfiguration configuration,
            string contentRootPath,
            string applicationName,
            ILogger logger)
        {
            try
            {
                var configured = configuration.GetSection(RemoteWebEndpointOptions.SectionName).Get<RemoteWebEndpointOptions>()
                    ?? new RemoteWebEndpointOptions();

                var address = FirstNonEmpty(
                    GetCommandLineValue(args ?? Array.Empty<string>(), "--network-address"),
                    Environment.GetEnvironmentVariable("LOCALGPT_NETWORK_ADDRESS"),
                    configured.Address) ?? "0.0.0.0";

                var portText = FirstNonEmpty(
                    GetCommandLineValue(args ?? Array.Empty<string>(), "--network-port"),
                    Environment.GetEnvironmentVariable("LOCALGPT_NETWORK_PORT"));
                var port = configured.Port;
                if (!string.IsNullOrWhiteSpace(portText) && !int.TryParse(portText, out port))
                    throw new InvalidOperationException("LOCALGPT network port must be numeric.");

                var enabledText = FirstNonEmpty(
                    GetCommandLineValue(args ?? Array.Empty<string>(), "--network-enabled"),
                    Environment.GetEnvironmentVariable("LOCALGPT_NETWORK_ENABLED"));
                var enabled = configured.Enabled || port > 0;
                if (!string.IsNullOrWhiteSpace(enabledText) && bool.TryParse(enabledText, out var parsedEnabled))
                    enabled = parsedEnabled;

                if (!enabled)
                    return null;
                if (port is <= 0 or > 65535)
                    throw new InvalidOperationException("The optional LocalGPT network endpoint requires a port between 1 and 65535.");
                if (port == Port)
                    throw new InvalidOperationException("The optional LocalGPT network endpoint must use a different port than the authoritative loopback endpoint.");

                var certificatePath = FirstNonEmpty(
                    GetCommandLineValue(args ?? Array.Empty<string>(), "--network-certificate"),
                    Environment.GetEnvironmentVariable("LOCALGPT_NETWORK_CERTIFICATE"),
                    configured.CertificatePath) ?? string.Empty;
                var certificatePassword = FirstNonEmpty(
                    GetCommandLineValue(args ?? Array.Empty<string>(), "--network-certificate-password"),
                    Environment.GetEnvironmentVariable("LOCALGPT_NETWORK_CERTIFICATE_PASSWORD"),
                    configured.CertificatePassword) ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(certificatePath))
                {
                    certificatePath = Environment.ExpandEnvironmentVariables(certificatePath.Trim());
                    if (!Path.IsPathRooted(certificatePath))
                        certificatePath = Path.GetFullPath(Path.Combine(contentRootPath, certificatePath));
                    if (!File.Exists(certificatePath))
                        throw new FileNotFoundException("Configured LocalGPT network TLS certificate was not found.", certificatePath);
                }

                return new RemoteWebEndpointOptions
                {
                    Enabled = true,
                    Address = address.Trim(),
                    Port = port,
                    CertificatePath = certificatePath,
                    CertificatePassword = certificatePassword
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not resolve the optional LocalGPT network endpoint for {ApplicationName}.", applicationName);
                throw;
            }
        }

        /// <summary>
        /// Retrieves command line value for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
        /// </summary>
        /// <param name="args">Args value supplied to the program operation and used when producing its result.</param>
        /// <param name="switchName">Switch name value supplied to the program operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        private static string? GetCommandLineValue(string[] args, string switchName)
        {
            for (var index = 0; index < args.Length; index++)
            {
                var current = args[index];
                if (current.StartsWith(switchName + "=", StringComparison.OrdinalIgnoreCase))
                    return current[(switchName.Length + 1)..];
                if (current.Equals(switchName, StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
                    return args[index + 1];
            }
            return null;
        }

        /// <summary>
        /// Performs first non empty for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
        /// </summary>
        /// <param name="values">Values value supplied to the program operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        private static string? FirstNonEmpty(params string?[] values) =>
            values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        /// <summary>
        /// Resolves requested port for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
        /// </summary>
        /// <param name="args">Args value supplied to the program operation and used when producing its result.</param>
        /// <param name="configuration">Configuration containing the caller-supplied values that control this operation.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The int produced by the operation.</returns>
        private static int ResolveRequestedPort(string[]? args, IConfiguration configuration, ILogger logger)
        {
            // The installer historically starts LocalGPT.exe with a positional numeric port.
            // Keep that contract first, while also supporting explicit switches/configuration.
            if (args is { Length: > 0 } && int.TryParse(args[0], out var positionalPort))
            {
                if (positionalPort is > 0 and <= 65535)
                    return positionalPort;
                logger.LogWarning("Ignoring invalid positional LocalGPT port {RequestedPort}; default {DefaultPort} remains active.", args[0], DefaultPort);
            }

            return ResolveConfiguredPort(
                args,
                configuration,
                "--port",
                "LOCALGPT_PORT",
                "LocalGPT:Port",
                configuration.GetValue<int?>("ApiCore:HttpPort") is > 0 and <= 65535 ? configuration.GetValue<int>("ApiCore:HttpPort") : DefaultPort,
                allowDynamic: true,
                logger);
        }

        /// <summary>
        /// Resolves configured port for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
        /// </summary>
        /// <param name="args">Args value supplied to the program operation and used when producing its result.</param>
        /// <param name="configuration">Configuration containing the caller-supplied values that control this operation.</param>
        /// <param name="switchName">Switch name value supplied to the program operation and used when producing its result.</param>
        /// <param name="environmentName">Environment name value supplied to the program operation and used when producing its result.</param>
        /// <param name="configurationKey">Configuration key value supplied to the program operation and used when producing its result.</param>
        /// <param name="fallback">Fallback value supplied to the program operation and used when producing its result.</param>
        /// <param name="allowDynamic">Value indicating whether allow dynamic should apply to this operation.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The int produced by the operation.</returns>
        private static int ResolveConfiguredPort(
            string[]? args,
            IConfiguration configuration,
            string switchName,
            string environmentName,
            string configurationKey,
            int fallback,
            bool allowDynamic,
            ILogger logger)
        {
            if (args is { Length: > 0 })
            {
                for (var index = 0; index < args.Length; index++)
                {
                    if (!string.Equals(args[index], switchName, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (index + 1 < args.Length && int.TryParse(args[index + 1], out var commandLinePort) &&
                        ((commandLinePort is > 0 and <= 65535) || (allowDynamic && commandLinePort == 0)))
                        return commandLinePort;
                    logger.LogWarning("Ignoring invalid {SwitchName} value; fallback port {FallbackPort} remains active.", switchName, fallback);
                }
            }

            var environmentValue = Environment.GetEnvironmentVariable(environmentName);
            if (int.TryParse(environmentValue, out var environmentPort) &&
                ((environmentPort is > 0 and <= 65535) || (allowDynamic && environmentPort == 0)))
                return environmentPort;

            var configuredPort = configuration.GetValue<int?>(configurationKey);
            if ((configuredPort is > 0 and <= 65535) || (allowDynamic && configuredPort == 0))
                return configuredPort.Value;

            return fallback;
        }

        /// <summary>
        /// Validates port contracts for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
        /// </summary>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        private static void ValidatePortContracts(ILogger logger)
        {
            // The installer-selected web port is authoritative. Optional organic wiring must adapt
            // around it and must never prevent the desktop bootstrap from starting.
            if (OneWirePort == Port || OneWirePort == OneWireDiscoveryPort)
            {
                var previous = OneWirePort;
                var replacement = GetFreePortExcluding(logger, Port, OneWireDiscoveryPort);
                if (replacement <= 0)
                {
                    logger.LogError(
                        "No safe organic 1-Wire TCP port could be reserved. The LocalGPT installer/bootstrap port {ApplicationPort} remains authoritative; organic TCP startup will be fault-contained.",
                        Port);
                }
                else
                {
                    System.Threading.Volatile.Write(ref runtimeOneWirePort, replacement);
                    logger.LogWarning(
                        "Reassigned conflicting optional organic TCP port {PreviousPort} to {ReplacementPort}; the installer/bootstrap application port {ApplicationPort} was preserved unchanged.",
                        previous, replacement, Port);
                }
            }

            if (Port == OneWireDiscoveryPort)
            {
                logger.LogInformation(
                    "Application TCP and organic discovery UDP both use numeric port {Port}. They are separate transports; the installer/bootstrap listener remains unchanged.",
                    Port);
            }

            logger.LogInformation(
                "Validated LocalGPT port contracts: app/installer TCP {ApplicationPort}, organic TCP {OneWirePort}, discovery UDP {DiscoveryPort}.",
                Port, OneWirePort, OneWireDiscoveryPort);
        }

        /// <summary>
        /// Retrieves free port excluding for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
        /// </summary>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <param name="excludedPorts">Excluded ports value supplied to the program operation and used when producing its result.</param>
        /// <returns>The int produced by the operation.</returns>
        private static int GetFreePortExcluding(ILogger logger, params int[] excludedPorts)
        {
            var excluded = excludedPorts.ToHashSet();
            for (var attempt = 0; attempt < 12; attempt++)
            {
                var candidate = GetFreePort(logger);
                if (candidate > 0 && !excluded.Contains(candidate))
                    return candidate;
            }
            return 0;
        }

    }
}
