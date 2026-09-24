using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>Reuses managed Python and upload admission for microphone and browser evidence.</summary>
public sealed partial class LocalAiRuntimeService
{
    /// <summary>Admits browser-created PCM WAV audio and transcribes with the selected installed model and saved speech settings.</summary>
    /// <param name="audio">Audio value supplied to the local AI runtime operation and used when producing its result.</param>
    /// <param name="length">Length value supplied to the local AI runtime operation and used when producing its result.</param>
    /// <param name="installationId">Identifier of the installation to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local AI runtime job result produced by the operation.</returns>
    public async Task<LocalAiRuntimeJobResult> TranscribeMicrophoneAsync(Stream audio, long length, string installationId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (length <= 44 || length > 16 * 1024 * 1024)
                throw new ArgumentException("Microphone audio must be a nonempty WAV of at most 16 MiB.");
            RequireCapability(installationId, LocalAiCapability.SpeechRecognition);
            var workspace = await uploads.CreateWorkspaceFromStreamsAsync(string.Empty,
                [new ChatUploadWorkspaceStreamInput("microphone.wav", "audio/wav", length, () => audio)], cancellationToken).ConfigureAwait(false);
            var file = workspace.Files.Single(item => item.RelativePath.EndsWith("microphone.wav", StringComparison.OrdinalIgnoreCase));
            return await TranscribeWorkspaceAudioAsync(new LocalAiSpeechRecognitionRequest
            {
                ModelInstallationId = installationId,
                WorkspaceName = workspace.WorkspaceName,
                RelativePath = file.RelativePath
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogError("Microphone transcription failed with {ExceptionType}; audio and transcript omitted.", exception.GetType().Name);
            throw;
        }
    }

    /// <summary>Installs Chromium through the existing managed environment after the dispatcher obtains approval.</summary>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task InstallWebBrowserAsync(bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            RequireConfirmation(userConfirmed, "installing the managed web browser");
            await EnsureRuntimePolicyLoadedAsync(cancellationToken).ConfigureAwait(false);
            var environmentPath = ResolveEnvironmentPath(CurrentConfig());
            var executable = RequireExistingFile(ResolveEnvironmentPython(environmentPath), "Managed environment Python executable");
            var result = await RunProcessAsync(executable, ["-m", "playwright", "install", "chromium"],
                environmentPath, TimeSpan.FromMinutes(20), cancellationToken).ConfigureAwait(false);
            EnsureProcessSuccess(result, "Managed web browser installation");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogError("Managed browser installation failed with {ExceptionType}; process output omitted.", exception.GetType().Name);
            throw;
        }
    }

    /// <summary>Runs an isolated, time-bounded browser process in the same managed Python environment.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local AI runtime job result produced by the operation.</returns>
    public async Task<LocalAiRuntimeJobResult> ExtractWebContentAsync(WebContentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) || !string.IsNullOrEmpty(uri.UserInfo))
                throw new ArgumentException("An absolute HTTP(S) URL without embedded credentials is required.");
            if (request.ScrollSteps is < 0 or > 20 || request.MaximumCharacters is < 1000 or > 100000 ||
                request.TimeoutSeconds is < 5 or > 120 || request.RevealSelectors.Count > 12 ||
                request.WaitForSelector.Length > 500 || request.RevealSelectors.Any(value => string.IsNullOrWhiteSpace(value) || value.Length > 500))
                throw new ArgumentException("The browser request exceeds its bounded extraction limits.");
            await EnsureRuntimePolicyLoadedAsync(cancellationToken).ConfigureAwait(false);
            return await RunBridgeProcessAsync("web_extract", new Dictionary<string, object?>
            {
                ["url"] = uri.AbsoluteUri,
                ["wait_for_selector"] = request.WaitForSelector,
                ["reveal_selectors"] = request.RevealSelectors,
                ["scroll_steps"] = request.ScrollSteps,
                ["maximum_characters"] = request.MaximumCharacters,
                ["timeout_seconds"] = request.TimeoutSeconds
            }, TimeSpan.FromSeconds(request.TimeoutSeconds + 15), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogError("Web evidence extraction failed with {ExceptionType}; URL and page content omitted.", exception.GetType().Name);
            throw;
        }
    }
}
