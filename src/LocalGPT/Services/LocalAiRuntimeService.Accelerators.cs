using System.Runtime.InteropServices;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>Provides setup profiles and device checks for supported PyTorch accelerators.</summary>
public sealed partial class LocalAiRuntimeService
{
    private sealed record AcceleratorProfile(
        string Key,
        string Name,
        string Description,
        string IndexUrl,
        string Backend,
        int MinPythonMinor,
        int MaxPythonMinor,
        string[] Packages);

    private readonly AcceleratorProfile[] acceleratorProfiles =
    [
        new("pytorch-cuda", "PyTorch for NVIDIA CUDA",
            "For supported NVIDIA GPUs on x64 Windows or Linux with a CUDA 13.0 compatible driver. Requires CPython 3.10–3.15.",
            "https://download.pytorch.org/whl/cu130/", "cuda", 10, 15,
            ["torch==2.14.1+cu130", "torchvision==0.29.1+cu130"]),
        new("pytorch-rocm", "PyTorch for AMD ROCm",
            "For AMD GPUs supported by ROCm 10.2 on x64 Windows or Linux with a compatible driver. Requires CPython 3.11–3.14.",
            "https://stable.repo.amd.com/rocm/whl-next/", "rocm", 11, 14,
            ["torch[device-all]==2.14.0+rocm10.2.0", "torchvision[device-all]==0.29.0+rocm10.2.0", "torchaudio==2.11.0.3+rocm10.2.0"]),
        new("pytorch-xpu", "PyTorch for Intel Arc",
            "For supported Intel Arc GPUs on x64 Windows 11 or Linux with the Intel graphics driver. Requires CPython 3.10–3.15.",
            "https://download.pytorch.org/whl/xpu/", "xpu", 10, 15,
            ["torch==2.14.1+xpu", "torchvision==0.29.1+xpu"]),
        new("pytorch-mps", "PyTorch for Apple Metal",
            "For Apple silicon with macOS 14 or later. Requires native arm64 CPython 3.10–3.15.",
            "https://pypi.org/simple/", "mps", 10, 15,
            ["torch", "torchvision", "torchaudio"])
    ];

    private bool IsAcceleratorPlatformSupported(AcceleratorProfile profile) => profile.Backend switch
    {
        "mps" => OperatingSystem.IsMacOSVersionAtLeast(14) && RuntimeInformation.ProcessArchitecture == Architecture.Arm64,
        "xpu" => (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000) || OperatingSystem.IsLinux())
            && RuntimeInformation.OSArchitecture == Architecture.X64,
        _ => (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
            && RuntimeInformation.OSArchitecture == Architecture.X64
    };

    private LocalAiPackageProfile ToPackageProfile(AcceleratorProfile profile) => new()
    {
        Key = profile.Key,
        DisplayName = profile.Name,
        Description = profile.Description,
        Packages = [.. profile.Packages],
        IncludesTorch = true
    };

    private async Task ValidateAcceleratorEnvironmentAsync(AcceleratorProfile profile, string executable, string environmentPath, CancellationToken cancellationToken)
    {
        if (!IsAcceleratorPlatformSupported(profile))
            throw new InvalidOperationException(profile.Description);

        const string script = "import platform,struct,sys,sysconfig; print(platform.python_implementation(), sys.version_info.major, sys.version_info.minor, platform.machine().lower(), struct.calcsize('P'), int(sysconfig.get_config_var('Py_GIL_DISABLED') or 0))";
        var result = await RunProcessAsync(executable, ["-c", script], environmentPath, TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(false);
        EnsureProcessSuccess(result, "Managed Python compatibility check");
        var fields = result.StdOut.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var valid = fields is ["CPython", "3", _, _, "8", "0"]
            && int.TryParse(fields[2], out var minor) && minor >= profile.MinPythonMinor && minor <= profile.MaxPythonMinor
            && (profile.Backend == "mps" ? fields[3] == "arm64" : fields[3] is "amd64" or "x86_64");
        if (!valid)
            throw new InvalidOperationException($"The managed Python interpreter is incompatible with this profile. {profile.Description} Select a compatible interpreter and a new managed environment path in Local AI runtimes.");
    }

    private async Task VerifyAcceleratorAsync(AcceleratorProfile profile, string executable, string environmentPath, CancellationToken cancellationToken, IProgress<string>? progress)
    {
        var probe = profile.Backend switch
        {
            "cuda" => ("cuda", "torch.cuda.is_available() and bool(torch.version.cuda) and not torch.version.hip"),
            "rocm" => ("cuda", "torch.cuda.is_available() and bool(torch.version.hip)"),
            "mps" => ("mps", "torch.backends.mps.is_available()"),
            _ => ("xpu", "torch.xpu.is_available()")
        };
        var script = $"""
            import torch, torchvision
            if not ({probe.Item2}):
                raise RuntimeError('PyTorch cannot use the selected accelerator on this machine.')
            print((torch.ones(1, device='{probe.Item1}') + 1).item())
            """;
        ReportInstallProgress(progress, $"Checking {profile.Name} with a tensor operation…");
        var result = await RunProcessAsync(executable, ["-c", script], environmentPath, TimeSpan.FromMinutes(2), cancellationToken, progress, profile.Name).ConfigureAwait(false);
        EnsureProcessSuccess(result, $"{profile.Name} device check. Verify the GPU and driver prerequisites and run the runtime probe again");
    }
}
