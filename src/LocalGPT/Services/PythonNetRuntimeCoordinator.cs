using LocalGPT.BusinessObjects;
using LocalGptConfigurationRoot = LocalGPT.BusinessObjects.ConfigurationRoot;
using LocalGPT.Interfaces;
using System.Reflection;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace LocalGPT.Services;

/// <summary>
/// Owns the single embedded Python.NET interpreter and exposes it through a bounded asynchronous queue.
/// The caller supplies fixed operation keys and structured data, never Python source.
/// </summary>
public sealed class PythonNetRuntimeCoordinator : IPythonNetRuntimeCoordinator, IAsyncDisposable
{
    private readonly HashSet<string> AllowedOperations = new(StringComparer.Ordinal)
    {
        "runtime_probe",
        "hf_snapshot_download",
        "image_generate",
        "image_edit",
        "video_generate",
        "image_to_video",
        "speech_recognize",
        "model_evict",
        "cache_clear"
    };

    private readonly IOptionsMonitor<LocalGptConfigurationRoot> options;
    private readonly IWebHostEnvironment environment;
    private readonly ILogger<PythonNetRuntimeCoordinator> logger;
    private readonly Channel<QueuedJob> queue;
    private readonly CancellationTokenSource shutdown = new();
    private readonly Task worker;
    private Assembly? pythonAssembly;
    private Type? pythonEngineType;
    private Type? pyType;
    private string initializedFingerprint = string.Empty;
    private volatile bool initialized;
    private volatile bool restartRequired;
    private bool bridgeModuleLoaded;
    private int queueLength;

    public PythonNetRuntimeCoordinator(
        IOptionsMonitor<LocalGptConfigurationRoot> options,
        IWebHostEnvironment environment,
        ILogger<PythonNetRuntimeCoordinator> logger)
    {
        this.options = options;
        this.environment = environment;
        this.logger = logger;
        var capacity = Math.Clamp(options.CurrentValue.PythonCore?.QueueCapacity ?? 64, 1, 512);
        queue = Channel.CreateBounded<QueuedJob>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

        // No Local-AI job can exist before this singleton is constructed. Anything left in
        // these ephemeral roots therefore belongs to a previous crashed/stopped process.
        // Remove it before accepting new work so request JSON and private media copies do
        // not become accidental long-lived storage. Published artifacts are not touched.
        TryDeleteDirectory(LocalGptApplicationDataPaths.ResolveUserPath("LocalAiRuntime", "Scratch"));
        TryDeleteDirectory(LocalGptApplicationDataPaths.ResolveUserPath("LocalAiRuntime", "Staging"));
        TryDeleteDirectory(LocalGptApplicationDataPaths.ResolveUserPath("LocalAiRuntime", "InputScratch"));
        TryDeleteDirectory(LocalGptApplicationDataPaths.ResolveUserPath("LocalAiRuntime", "InstallScratch"));
        var configuredModelRoot = options.CurrentValue.PythonCore?.ModelRoot;
        var modelRoot = string.IsNullOrWhiteSpace(configuredModelRoot)
            ? LocalGptApplicationDataPaths.ResolveUserPath("LocalAiRuntime", "Models")
            : Path.GetFullPath(configuredModelRoot);
        TryDeleteDirectory(Path.Combine(modelRoot, ".localgpt", "incoming"));

        worker = Task.Run(() => WorkerAsync(shutdown.Token));
    }

    public bool IsInitialized => initialized;
    public bool RestartRequired => restartRequired;
    public int QueueLength => Volatile.Read(ref queueLength);

    public async Task<LocalAiRuntimeJobResult> ExecuteAsync(LocalAiRuntimeJobRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(request);
            if (!AllowedOperations.Contains(request.Operation))
                throw new InvalidOperationException($"LocalGPT Python operation '{request.Operation}' is not registered.");

            var item = new QueuedJob(request, cancellationToken);
            var written = false;
            Interlocked.Increment(ref queueLength);
            try
            {
                await queue.Writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);
                written = true;
                return await item.Completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Once written, the single reader owns the queue-length decrement even if the caller
                // cancels while waiting for completion. This avoids a cancelled waiter racing the worker
                // into a negative queue count.
                if (!written)
                    Interlocked.Decrement(ref queueLength);
                throw;
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PythonNetRuntimeCoordinator)}.{nameof(ExecuteAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PythonNetRuntimeCoordinator)}.{nameof(ExecuteAsync)} failed.");
        throw;
    }
}

    private async Task WorkerAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var item in queue.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                Interlocked.Decrement(ref queueLength);
                if (item.CancellationToken.IsCancellationRequested)
                {
                    item.Completion.TrySetCanceled(item.CancellationToken);
                    continue;
                }
                try
                {
                    using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(item.CancellationToken, cancellationToken);
                    var result = await Task.Run(() => ExecuteOne(item.Request, linkedCancellation.Token), CancellationToken.None).ConfigureAwait(false);
                    item.Completion.TrySetResult(result);
                }
                catch (OperationCanceledException exception)
                {
                    var cancelledToken = exception.CancellationToken.IsCancellationRequested
                        ? exception.CancellationToken
                        : item.CancellationToken.IsCancellationRequested ? item.CancellationToken : cancellationToken;
                    item.Completion.TrySetCanceled(cancelledToken);
                }
                catch (Exception exception)
                {
                    logger.LogError("Embedded Python job {Operation} failed with {ExceptionType}; prompts, model content, exception text, runtime paths and media were omitted from logs.", item.Request.Operation, exception.GetType().Name);
                    item.Completion.TrySetResult(new LocalAiRuntimeJobResult
                    {
                        JobId = Guid.NewGuid(),
                        Succeeded = false,
                        Status = "Failed",
                        Message = "The embedded Python job failed. Review LocalGPT logs for the exception type."
                    });
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            while (queue.Reader.TryRead(out var pending))
            {
                Interlocked.Decrement(ref queueLength);
                pending.Completion.TrySetCanceled(cancellationToken);
            }
        }
    }

    private LocalAiRuntimeJobResult ExecuteOne(LocalAiRuntimeJobRequest request, CancellationToken cancellationToken)
    {
    try
    {
            EnsureInitialized();
            cancellationToken.ThrowIfCancellationRequested();
            var jobId = Guid.NewGuid();
            var scratchRoot = LocalGptApplicationDataPaths.ResolveUserPath("LocalAiRuntime", "Scratch");
            var stagingRoot = LocalGptApplicationDataPaths.ResolveUserPath("LocalAiRuntime", "Staging");
            Directory.CreateDirectory(scratchRoot);
            Directory.CreateDirectory(stagingRoot);
            var jobDirectory = Path.Combine(scratchRoot, jobId.ToString("N"));
            Directory.CreateDirectory(jobDirectory);
            var requestPath = Path.Combine(jobDirectory, "request.json");
            var resultPath = Path.Combine(jobDirectory, "result.json");
            var cancelPath = Path.Combine(jobDirectory, "cancel");
            var parameters = new Dictionary<string, object?>(request.Parameters, StringComparer.OrdinalIgnoreCase);
            if (request.Operation.Equals("image_generate", StringComparison.Ordinal) || request.Operation.Equals("image_edit", StringComparison.Ordinal))
                parameters["output_path"] = Path.Combine(jobDirectory, "generated.png");
            else if (request.Operation.Equals("video_generate", StringComparison.Ordinal) || request.Operation.Equals("image_to_video", StringComparison.Ordinal))
                parameters["output_path"] = Path.Combine(jobDirectory, "generated.mp4");

            var envelope = new { operation = request.Operation, parameters };
            File.WriteAllText(requestPath, JsonSerializer.Serialize(envelope, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
            using var cancellationRegistration = cancellationToken.Register(static state =>
            {
                try { File.WriteAllText((string)state!, "cancel"); } catch { }
            }, cancelPath);

            try
            {
                using var gil = AcquireGil();
                ExecuteBridge(requestPath, resultPath, cancelPath);
                var result = ReadResult(jobId, resultPath);
                if (result.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                    throw new OperationCanceledException("The embedded Python job was cancelled.", cancellationToken);

                if (result.Metadata.TryGetValue("artifact_path", out var artifactPath) && File.Exists(artifactPath))
                {
                    var stageDirectory = Path.Combine(stagingRoot, jobId.ToString("N"));
                    Directory.CreateDirectory(stageDirectory);
                    var stagedPath = Path.Combine(stageDirectory, Path.GetFileName(artifactPath));
                    File.Move(artifactPath, stagedPath, overwrite: true);
                    result.Metadata.Remove("artifact_path");
                    result.Metadata["artifactPath"] = stagedPath;
                }
                return result;
            }
            finally
            {
                TryDeleteDirectory(jobDirectory);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PythonNetRuntimeCoordinator)}.{nameof(ExecuteOne)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PythonNetRuntimeCoordinator)}.{nameof(ExecuteOne)} failed.");
        throw;
    }
}

    private void EnsureInitialized()
    {
    try
    {
            var config = options.CurrentValue.PythonCore ?? new PythonCoreOptions();
            var fingerprint = string.Join('|', config.PythonRuntime, config.PythonHome, config.VirtualEnvironmentPath);
            if (initialized)
            {
                if (!string.Equals(initializedFingerprint, fingerprint, StringComparison.Ordinal))
                {
                    restartRequired = true;
                    throw new InvalidOperationException("Python runtime binding changed after the embedded interpreter was initialized. Restart LocalGPT before running another Python job.");
                }
                return;
            }
            if (string.IsNullOrWhiteSpace(config.PythonRuntime) || !File.Exists(config.PythonRuntime))
                throw new InvalidOperationException("No valid CPython shared runtime library is configured.");
            if (string.IsNullOrWhiteSpace(config.VirtualEnvironmentPath) || !Directory.Exists(config.VirtualEnvironmentPath))
                throw new InvalidOperationException("No LocalGPT-managed Python environment is configured.");

            // Python.NET is an application dependency (NuGet package), not a pip package that users
            // are expected to install into every managed environment. Only the selected CPython
            // shared library and site-packages belong to the external Python toolchain.
            pythonAssembly = typeof(Python.Runtime.PythonEngine).Assembly;
            var runtimeType = pythonAssembly.GetType("Python.Runtime.Runtime", throwOnError: true)!;
            pythonEngineType = pythonAssembly.GetType("Python.Runtime.PythonEngine", throwOnError: true)!;
            pyType = pythonAssembly.GetType("Python.Runtime.Py", throwOnError: true)!;

            var pythonDll = runtimeType.GetProperty("PythonDLL", BindingFlags.Public | BindingFlags.Static)
                ?? throw new MissingMemberException("Python.Runtime.Runtime.PythonDLL was not found.");
            pythonDll.SetValue(null, Path.GetFullPath(config.PythonRuntime));
            if (!string.IsNullOrWhiteSpace(config.PythonHome) && Directory.Exists(config.PythonHome))
                pythonEngineType.GetProperty("PythonHome", BindingFlags.Public | BindingFlags.Static)?.SetValue(null, Path.GetFullPath(config.PythonHome));

            var sitePackages = FindSitePackages(config.VirtualEnvironmentPath);
            InvokeOptionalStatic(pythonEngineType, "Initialize");
            InvokeOptionalStatic(pythonEngineType, "BeginAllowThreads");
            if (!string.IsNullOrWhiteSpace(sitePackages))
            {
                using var gil = AcquireGil();
                ExecuteFixedPython($"import sys\np={JsonSerializer.Serialize(sitePackages)}\nif p not in sys.path: sys.path.insert(0,p)");
            }
            initializedFingerprint = fingerprint;
            initialized = true;
            restartRequired = false;
            logger.LogInformation("Initialized the serialized LocalGPT Python.NET interpreter lane; runtime and environment paths were omitted from logs.");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PythonNetRuntimeCoordinator)}.{nameof(EnsureInitialized)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PythonNetRuntimeCoordinator)}.{nameof(EnsureInitialized)} failed.");
        throw;
    }
}

    private IDisposable AcquireGil()
    {
    try
    {
            var method = pyType?.GetMethod("GIL", BindingFlags.Public | BindingFlags.Static, binder: null, types: Type.EmptyTypes, modifiers: null)
                ?? throw new MissingMethodException("Python.Runtime.Py.GIL was not found.");
            return method.Invoke(null, null) as IDisposable
                ?? throw new InvalidOperationException("Python.Runtime.Py.GIL did not return a disposable lock scope.");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PythonNetRuntimeCoordinator)}.{nameof(AcquireGil)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PythonNetRuntimeCoordinator)}.{nameof(AcquireGil)} failed.");
        throw;
    }
}

    private void ExecuteBridge(string requestPath, string resultPath, string cancelPath)
    {
    try
    {
            EnsureBridgeModuleLoaded();
            var quotedRequest = JsonSerializer.Serialize(Path.GetFullPath(requestPath));
            var quotedResult = JsonSerializer.Serialize(Path.GetFullPath(resultPath));
            var quotedCancel = JsonSerializer.Serialize(Path.GetFullPath(cancelPath));
            ExecuteFixedPython($"import sys\nsys.modules['_localgpt_runtime_bridge'].run_job({quotedRequest},{quotedResult},{quotedCancel})");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PythonNetRuntimeCoordinator)}.{nameof(ExecuteBridge)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PythonNetRuntimeCoordinator)}.{nameof(ExecuteBridge)} failed.");
        throw;
    }
}

    private void EnsureBridgeModuleLoaded()
    {
    try
    {
            if (bridgeModuleLoaded)
                return;
            var bridgePath = Path.Combine(environment.ContentRootPath, "Runtime", "Python", "localgpt_runtime_bridge.py");
            if (!File.Exists(bridgePath))
                throw new FileNotFoundException("The packaged LocalGPT Python runtime bridge is missing.", bridgePath);
            var quotedBridge = JsonSerializer.Serialize(Path.GetFullPath(bridgePath));
            const string moduleName = "_localgpt_runtime_bridge";
            var code = $"import importlib.util,sys\n_p={quotedBridge}\n_s=importlib.util.spec_from_file_location('{moduleName}',_p)\n_m=importlib.util.module_from_spec(_s)\nsys.modules['{moduleName}']=_m\n_s.loader.exec_module(_m)";
            ExecuteFixedPython(code);
            bridgeModuleLoaded = true;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PythonNetRuntimeCoordinator)}.{nameof(EnsureBridgeModuleLoaded)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PythonNetRuntimeCoordinator)}.{nameof(EnsureBridgeModuleLoaded)} failed.");
        throw;
    }
}

    private void ExecuteFixedPython(string code)
    {
    try
    {
            var exec = pythonEngineType?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.Name == "Exec")
                .OrderBy(method => method.GetParameters().Length)
                .FirstOrDefault(method => method.GetParameters().Length > 0 && method.GetParameters()[0].ParameterType == typeof(string))
                ?? throw new MissingMethodException("Python.Runtime.PythonEngine.Exec was not found.");
            var args = BuildInvocationArguments(exec, code);
            try
            {
                exec.Invoke(null, args);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                throw exception.InnerException;
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PythonNetRuntimeCoordinator)}.{nameof(ExecuteFixedPython)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PythonNetRuntimeCoordinator)}.{nameof(ExecuteFixedPython)} failed.");
        throw;
    }
}

    private object?[] BuildInvocationArguments(MethodInfo method, string firstValue)
    {
    try
    {
            var parameters = method.GetParameters();
            var args = new object?[parameters.Length];
            args[0] = firstValue;
            for (var index = 1; index < parameters.Length; index++)
                args[index] = parameters[index].HasDefaultValue ? parameters[index].DefaultValue : Type.Missing;
            return args;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PythonNetRuntimeCoordinator)}.{nameof(BuildInvocationArguments)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PythonNetRuntimeCoordinator)}.{nameof(BuildInvocationArguments)} failed.");
        throw;
    }
}

    private void InvokeOptionalStatic(Type type, string methodName)
    {
    try
    {
            var method = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(candidate => candidate.Name == methodName)
                .OrderBy(candidate => candidate.GetParameters().Length)
                .FirstOrDefault(candidate => candidate.GetParameters().All(parameter => parameter.IsOptional));
            if (method is null)
                method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, binder: null, types: Type.EmptyTypes, modifiers: null);
            if (method is null)
                throw new MissingMethodException(type.FullName, methodName);
            var args = method.GetParameters().Select(parameter => parameter.HasDefaultValue ? parameter.DefaultValue : Type.Missing).ToArray();
            method.Invoke(null, args);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PythonNetRuntimeCoordinator)}.{nameof(InvokeOptionalStatic)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PythonNetRuntimeCoordinator)}.{nameof(InvokeOptionalStatic)} failed.");
        throw;
    }
}

    private LocalAiRuntimeJobResult ReadResult(Guid jobId, string resultPath)
    {
    try
    {
            if (!File.Exists(resultPath))
                throw new InvalidOperationException("The Python bridge returned without a result envelope.");
            using var document = JsonDocument.Parse(File.ReadAllText(resultPath));
            var root = document.RootElement;
            var result = new LocalAiRuntimeJobResult
            {
                JobId = jobId,
                Succeeded = root.TryGetProperty("succeeded", out var succeeded) && succeeded.ValueKind == JsonValueKind.True,
                Status = root.TryGetProperty("status", out var status) ? status.GetString() ?? string.Empty : string.Empty,
                Message = root.TryGetProperty("message", out var message) ? message.GetString() ?? string.Empty : string.Empty,
                Text = root.TryGetProperty("text", out var text) ? text.GetString() ?? string.Empty : string.Empty
            };
            if (root.TryGetProperty("artifact_path", out var artifactPath) && artifactPath.ValueKind == JsonValueKind.String)
                result.Metadata["artifact_path"] = artifactPath.GetString() ?? string.Empty;
            if (root.TryGetProperty("metadata", out var metadata) && metadata.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in metadata.EnumerateObject())
                    result.Metadata[property.Name] = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() ?? string.Empty : property.Value.ToString();
            }
            return result;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PythonNetRuntimeCoordinator)}.{nameof(ReadResult)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PythonNetRuntimeCoordinator)}.{nameof(ReadResult)} failed.");
        throw;
    }
}


    private string? FindSitePackages(string environmentPath)
    {
    try
    {
            try
            {
                return Directory.EnumerateDirectories(environmentPath, "site-packages", SearchOption.AllDirectories)
                    .OrderBy(path => path.Length)
                    .FirstOrDefault();
            }
            catch { return null; }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PythonNetRuntimeCoordinator)}.{nameof(FindSitePackages)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PythonNetRuntimeCoordinator)}.{nameof(FindSitePackages)} failed.");
        throw;
    }
}

    private void TryDeleteDirectory(string path)
    {
    try
    {
            try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); } catch { }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PythonNetRuntimeCoordinator)}.{nameof(TryDeleteDirectory)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PythonNetRuntimeCoordinator)}.{nameof(TryDeleteDirectory)} failed.");
        throw;
    }
}

    public async ValueTask DisposeAsync()
    {
    try
    {
            queue.Writer.TryComplete();
            shutdown.Cancel();
            try { await worker.ConfigureAwait(false); } catch (OperationCanceledException) { }
            shutdown.Dispose();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PythonNetRuntimeCoordinator)}.{nameof(DisposeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PythonNetRuntimeCoordinator)}.{nameof(DisposeAsync)} failed.");
        throw;
    }
}

    private sealed class QueuedJob(LocalAiRuntimeJobRequest request, CancellationToken cancellationToken)
    {
        public LocalAiRuntimeJobRequest Request { get; } = request;
        public CancellationToken CancellationToken { get; } = cancellationToken;
        public TaskCompletionSource<LocalAiRuntimeJobResult> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
