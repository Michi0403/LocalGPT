using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Hyperlight Unikraft (`hluk`) provider. The guest has no network or host mounts by default.
/// The CLI receives only one temporary source file; it never launches Python, dotnet, or a shell on the host.
/// </summary>
public sealed class HyperlightUnikraftSessionSandboxProvider : ISessionSandboxExecutionProvider
{
    /// <summary>Stable Hyperlight Unikraft provider ID.</summary>
    public string Id => "hyperlight-unikraft";

    /// <summary>Guest runtimes supported without any host-side execution fallback.</summary>
    public IReadOnlyList<SessionSandboxLanguage> Languages { get; } =
    [
        new("hyperlight-python", "hyperlight-unikraft", "Python · Hyperlight micro-VM", "python", ".py"),
        new("hyperlight-csharp", "hyperlight-unikraft", "C# · Hyperlight .NET JIT micro-VM", "dotnet-jit", ".cs"),
        new("hyperlight-powershell", "hyperlight-unikraft", "PowerShell · Hyperlight micro-VM", "powershell", ".ps1")
    ];

    /// <summary>Constructs a fail-closed argument-list invocation of hluk.</summary>
    public ProcessStartInfo PrepareLaunch(SessionSandboxLanguage language, string submittedSourcePath, string workingDirectory)
    {
        if (!Languages.Contains(language))
            throw new InvalidOperationException("The selected language is not supplied by this isolated execution provider.");

        // Build/publish provision version-matched executable and guest rootfs artifacts.
        // An explicit administrator override remains possible for supported custom deployments.
        var configuredPath = Environment.GetEnvironmentVariable("LOCALGPT_HLUK_PATH");
        if (!string.IsNullOrWhiteSpace(configuredPath) && (!Path.IsPathFullyQualified(configuredPath) || !File.Exists(configuredPath)))
            throw new InvalidOperationException("LOCALGPT_HLUK_PATH must point to an existing absolute hluk executable path.");
        var bundledExecutable = Path.Combine(AppContext.BaseDirectory, "hyperlight", "bin",
            OperatingSystem.IsWindows() ? "hluk.exe" : "hluk");
        var guestImage = Path.Combine(AppContext.BaseDirectory, "hyperlight", "runtimes", language.Runtime + ".cpio");
        if (!File.Exists(guestImage))
            throw new InvalidOperationException($"Hyperlight {language.Runtime} guest image was not provisioned for this installation. Run the repository PowerShell provisioning/build step; no host interpreter will run the submitted code.");
        var executable = !string.IsNullOrWhiteSpace(configuredPath) ? configuredPath : bundledExecutable;
        if (!File.Exists(executable))
            throw new InvalidOperationException("The bundled Hyperlight Unikraft executable is missing. Rebuild with guest provisioning enabled or set LOCALGPT_HLUK_PATH to a verified executable.");
        VerifyBundledRuntime(language, guestImage, bundledExecutable,
            verifyBundledExecutable: string.IsNullOrWhiteSpace(configuredPath));
        var start = new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        // No --net, --net-allow, --mount, --port or exposed host functions.
        // The CLI imports the submitted source into the guest; no project files are passed.
        start.ArgumentList.Add("run");
        start.ArgumentList.Add("--initrd");
        start.ArgumentList.Add(guestImage);
        start.ArgumentList.Add("--scratch-mb");
        start.ArgumentList.Add(language.Runtime switch
        {
            "dotnet-jit" => "768",
            "powershell" => "1024",
            _ => "256"
        });
        start.ArgumentList.Add(submittedSourcePath);

        // Avoid forwarding LocalGPT secrets/API tokens through the environment to a subprocess.
        // These variables are required to locate the trusted executable and per-user runtime image cache.
        start.Environment.Clear();
        foreach (var key in new[]
        {
            "PATH", "HOME", "USERPROFILE", "LOCALAPPDATA", "APPDATA", "XDG_CACHE_HOME",
            "XDG_DATA_HOME", "XDG_CONFIG_HOME", "TEMP", "TMP", "TMPDIR", "SYSTEMROOT",
            "WINDIR", "LANG", "LC_ALL", "HLUK_CACHE_DIR"
        })
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(value))
                start.Environment[key] = value;
        }
        return start;
    }
    /// <summary>Refuses modified guest rootfs or bundled CLI artifacts before launching untrusted code.</summary>
    private void VerifyBundledRuntime(SessionSandboxLanguage language, string guestImage,
        string bundledExecutable, bool verifyBundledExecutable)
    {
        var manifestPath = Path.Combine(AppContext.BaseDirectory, "hyperlight", "runtimes", "manifest.json");
        if (!File.Exists(manifestPath))
            throw new InvalidOperationException("Hyperlight runtime image verification manifest is missing; rebuild or publish with guest provisioning enabled.");
        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;
        if (root.GetProperty("Version").GetString() != "v0.19.1")
            throw new InvalidOperationException("The bundled Hyperlight driver protocol is not the version supported by this LocalGPT build.");
        var expectedImage = root.GetProperty("Images").GetProperty(language.Runtime).GetString();
        if (string.IsNullOrWhiteSpace(expectedImage) || !HashFile(guestImage).Equals(expectedImage, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Hyperlight guest image SHA-256 did not match the build-time verification receipt.");
        if (verifyBundledExecutable)
        {
            var expectedBinary = root.GetProperty("BinarySha256").GetString();
            if (string.IsNullOrWhiteSpace(expectedBinary) || !HashFile(bundledExecutable).Equals(expectedBinary, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Bundled Hyperlight CLI SHA-256 did not match the build-time verification receipt.");
        }
    }

    private string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

}
