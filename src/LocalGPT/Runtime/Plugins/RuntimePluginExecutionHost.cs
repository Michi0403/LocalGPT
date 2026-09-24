using LocalGPT.BusinessObjects;
using LocalGPT.PluginContracts;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace LocalGPT.Runtime.Plugins;

/// <summary>Materializes, builds, loads, unloads, and invokes executable runtime-extension payloads.</summary>
/// <param name="definitionSupport">Validation and bounded-text helpers shared with the persistence service.</param>
public sealed class RuntimePluginExecutionHost(RuntimePluginDefinitionSupport definitionSupport)
{
    private readonly ConcurrentDictionary<Guid, LoadedRuntimePlugin> loadedPlugins = new();

    /// <summary>Builds a persisted C# script through the selected configured .NET toolchain and loads its generated assembly.</summary>
    public async Task<RuntimePluginBuildResult> BuildCSharpAsync(RuntimePluginDefinition definition, ProjectCompilerInstallation compiler, CancellationToken cancellationToken)
    {
        var root = PrepareRoot(definition.Id);
        var projectPath = Path.Combine(root, "RuntimePlugin.csproj");
        var sourcePath = Path.Combine(root, "RuntimePlugin.cs");
        var contractPath = typeof(ILocalGptRuntimePlugin).Assembly.Location;
        var source = $$"""
using LocalGPT.PluginContracts;
using System.Text.Json;

public sealed class LocalGptUserRuntimePlugin : ILocalGptRuntimePlugin
{
    public async Task<string> ExecuteAsync(JsonElement parameters, CancellationToken cancellationToken = default)
    {
{{Indent(definition.SourceCode, 8)}}
    }
}
""";
        var project = $$"""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <AssemblyName>LocalGptUserRuntimePlugin</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="LocalGPT.PluginContracts">
      <HintPath>{{System.Security.SecurityElement.Escape(contractPath)}}</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
</Project>
""";
        await File.WriteAllTextAsync(sourcePath, source, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(projectPath, project, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
        var run = await RunProcessAsync(compiler, root, ["build", projectPath, "--configuration", "Release", "--nologo"], string.Empty, cancellationToken).ConfigureAwait(false);
        if (run.ExitCode != 0)
            return Failure(definition.Id, "BuildFailed", definitionSupport.Bound(string.IsNullOrWhiteSpace(run.StandardError) ? run.StandardOutput : run.StandardError, 4000));
        var assemblyPath = Directory.EnumerateFiles(Path.Combine(root, "bin", "Release"), "LocalGptUserRuntimePlugin.dll", SearchOption.AllDirectories).FirstOrDefault();
        if (assemblyPath is null)
            return Failure(definition.Id, "BuildFailed", "The configured .NET toolchain completed without producing the expected runtime plugin assembly.");
        LoadAssembly(definition, assemblyPath, "LocalGptUserRuntimePlugin");
        return Success(definition.Id, "Loaded", "C# runtime extension compiled through the configured .NET toolchain and loaded into an isolated collectible context.");
    }

    /// <summary>Materializes and loads a user-supplied compiled plugin ZIP into an isolated collectible load context.</summary>
    public async Task<RuntimePluginBuildResult> LoadCompiledPackageAsync(RuntimePluginDefinition definition, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(definition.PackagePayloadBase64))
            return Failure(definition.Id, "PackageMissing", "Compiled plugins require a persisted base64 ZIP payload.");
        if (string.IsNullOrWhiteSpace(definition.EntryAssemblyName) || string.IsNullOrWhiteSpace(definition.EntryTypeName))
            return Failure(definition.Id, "EntryPointMissing", "Compiled plugins require both entry assembly and entry type names.");
        var root = PrepareRoot(definition.Id);
        var bytes = Convert.FromBase64String(definition.PackagePayloadBase64);
        var memory = new MemoryStream(bytes, writable: false);
        await using (memory.ConfigureAwait(false))
        {
            using var archive = new ZipArchive(memory, ZipArchiveMode.Read, leaveOpen: false);
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(entry.Name))
                    continue;
                var target = Path.GetFullPath(Path.Combine(root, entry.FullName));
                var prefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
                if (!target.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Plugin package contains an unsafe archive path.");
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                var source = entry.Open();
                await using (source.ConfigureAwait(false))
                {
                    var destination = File.Create(target);
                    await using (destination.ConfigureAwait(false))
                    {
                        await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }
        var assemblyPath = Directory.EnumerateFiles(root, definition.EntryAssemblyName, SearchOption.AllDirectories).FirstOrDefault()
            ?? throw new FileNotFoundException("Plugin entry assembly was not found after package materialization.", definition.EntryAssemblyName);
        LoadAssembly(definition, assemblyPath, definition.EntryTypeName);
        return Success(definition.Id, "Loaded", "Compiled plugin materialized from database payload and loaded into an isolated collectible context.");
    }

    /// <summary>Validates that the configured JavaScript runtime can start before the script is exposed for invocation.</summary>
    public async Task<RuntimePluginBuildResult> ValidateJavaScriptRuntimeAsync(RuntimePluginDefinition definition, ProjectCompilerInstallation compiler, CancellationToken cancellationToken)
    {
        var root = PrepareRoot(definition.Id);
        await File.WriteAllTextAsync(Path.Combine(root, "plugin.js"), definition.SourceCode, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
        var args = SplitArguments(string.IsNullOrWhiteSpace(compiler.ValidationArguments) ? "--version" : compiler.ValidationArguments);
        var run = await RunProcessAsync(compiler, root, args, string.Empty, cancellationToken).ConfigureAwait(false);
        return run.ExitCode == 0
            ? Success(definition.Id, "Ready", $"JavaScript runtime validated: {definitionSupport.Bound(run.StandardOutput.Trim(), 500)}")
            : Failure(definition.Id, "RuntimeValidationFailed", definitionSupport.Bound(run.StandardError, 4000));
    }

    /// <summary>Invokes a loaded .NET runtime extension.</summary>
    public async Task<string> InvokeLoadedAsync(RuntimePluginDefinition definition, JsonElement parameters, CancellationToken cancellationToken)
    {
        if (!loadedPlugins.TryGetValue(definition.Id, out var loaded))
            throw new InvalidOperationException("The runtime extension is not loaded. Use Setup → Toolchains → Runtime extensions → Build / load first.");
        return await loaded.Instance.ExecuteAsync(parameters, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Invokes a persisted JavaScript extension through its selected configured runtime.</summary>
    public async Task<string> InvokeJavaScriptAsync(RuntimePluginDefinition definition, ProjectCompilerInstallation compiler, JsonElement parameters, CancellationToken cancellationToken)
    {
        var root = PrepareRoot(definition.Id);
        var script = Path.Combine(root, "plugin.js");
        if (!File.Exists(script))
            await File.WriteAllTextAsync(script, definition.SourceCode, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
        var run = await RunProcessAsync(compiler, root, [script], parameters.GetRawText(), cancellationToken).ConfigureAwait(false);
        if (run.ExitCode != 0)
            throw new InvalidOperationException(definitionSupport.Bound(run.StandardError, 4000));
        return run.StandardOutput.Trim();
    }

    /// <summary>Unloads one materialized .NET extension if it is currently active.</summary>
    public void Unload(Guid id)
    {
        if (loadedPlugins.TryRemove(id, out var loaded))
            loaded.Context.Unload();
    }

    private void LoadAssembly(RuntimePluginDefinition definition, string assemblyPath, string typeName)
    {
        Unload(definition.Id);
        var context = new RuntimePluginLoadContext(assemblyPath);
        try
        {
            var assembly = context.LoadFromAssemblyPath(Path.GetFullPath(assemblyPath));
            var type = assembly.GetType(typeName, throwOnError: true, ignoreCase: false)
                ?? throw new InvalidOperationException($"Plugin entry type '{typeName}' was not found.");
            if (!typeof(ILocalGptRuntimePlugin).IsAssignableFrom(type))
                throw new InvalidOperationException($"Plugin entry type '{typeName}' does not implement {nameof(ILocalGptRuntimePlugin)}.");
            var instance = Activator.CreateInstance(type) as ILocalGptRuntimePlugin
                ?? throw new InvalidOperationException("Plugin entry type could not be constructed through its public parameterless constructor.");
            loadedPlugins[definition.Id] = new LoadedRuntimePlugin(context, instance);
        }
        catch
        {
            context.Unload();
            throw;
        }
    }

    private string PrepareRoot(Guid id)
    {
        var baseRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(baseRoot))
            baseRoot = AppContext.BaseDirectory;
        var root = Path.GetFullPath(Path.Combine(baseRoot, "LocalGPT", "RuntimePlugins", id.ToString("N")));
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
        Directory.CreateDirectory(root);
        return root;
    }

    private async Task<ProcessResult> RunProcessAsync(ProjectCompilerInstallation compiler, string workingDirectory, IReadOnlyList<string> arguments, string standardInput, CancellationToken cancellationToken)
    {
        if (!File.Exists(compiler.ExecutablePath))
            throw new FileNotFoundException("Configured toolchain executable does not exist.", compiler.ExecutablePath);
        var start = new ProcessStartInfo
        {
            FileName = compiler.ExecutablePath,
            WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? Path.GetDirectoryName(compiler.ExecutablePath)! : workingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);
        ApplyEnvironment(start, compiler.EnvironmentVariablesJson);
        using var process = new Process { StartInfo = start };
        if (!process.Start())
            throw new InvalidOperationException("Configured toolchain process could not be started.");
        if (!string.IsNullOrEmpty(standardInput))
            await process.StandardInput.WriteAsync(standardInput.AsMemory(), cancellationToken).ConfigureAwait(false);
        process.StandardInput.Close();
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(5));
        try
        {
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            throw new TimeoutException("Runtime extension toolchain process exceeded five minutes.");
        }
        return new ProcessResult(process.ExitCode, definitionSupport.Bound(await outputTask.ConfigureAwait(false), 16000), definitionSupport.Bound(await errorTask.ConfigureAwait(false), 16000));
    }

    private void ApplyEnvironment(ProcessStartInfo start, string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            return;
        foreach (var property in document.RootElement.EnumerateObject())
            start.Environment[property.Name] = property.Value.GetString() ?? string.Empty;
    }

    private IReadOnlyList<string> SplitArguments(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];
        var result = new List<string>();
        var current = new StringBuilder();
        var quoted = false;
        foreach (var ch in text)
        {
            if (ch == '"') { quoted = !quoted; continue; }
            if (char.IsWhiteSpace(ch) && !quoted)
            {
                if (current.Length > 0) { result.Add(current.ToString()); current.Clear(); }
                continue;
            }
            current.Append(ch);
        }
        if (current.Length > 0)
            result.Add(current.ToString());
        return result;
    }

    private string Indent(string source, int spaces)
    {
        var prefix = new string(' ', spaces);
        return string.Join(Environment.NewLine, (source ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').Select(line => prefix + line));
    }

    private RuntimePluginBuildResult Success(Guid id, string status, string message) => new() { PluginId = id, Succeeded = true, Status = status, Message = message };
    private RuntimePluginBuildResult Failure(Guid id, string status, string message) => new() { PluginId = id, Status = status, Message = message };
    private sealed record LoadedRuntimePlugin(RuntimePluginLoadContext Context, ILocalGptRuntimePlugin Instance);
    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
