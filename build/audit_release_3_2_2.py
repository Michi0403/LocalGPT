#!/usr/bin/env python3
from pathlib import Path
import hashlib
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
checks=[]

def read(rel): return (ROOT/rel).read_text(encoding='utf-8')
def rawhash(rel): return hashlib.sha256((ROOT/rel).read_bytes()).hexdigest()
def normhash(rel):
    data=(ROOT/rel).read_text(encoding='utf-8').replace('\r\n','\n').replace('\r','\n').encode('utf-8')
    return hashlib.sha256(data).hexdigest()
def req(rel, needle, label=None):
    if needle not in read(rel): raise AssertionError(f'{rel}: missing {label or needle!r}')
    checks.append(label or needle)
def forbid(rel, needle, label=None):
    if needle in read(rel): raise AssertionError(f'{rel}: forbidden {label or needle!r}')
    checks.append(label or f'forbid:{needle}')
def heq(rel, expected, label=None):
    h=rawhash(rel)
    if h != expected: raise AssertionError(f'{rel}: sha256 {h} != {expected}')
    checks.append(label or f'hash:{rel}')

try:
    for rel in ['src/LocalGPT/LocalGPT.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj']:
        req(rel, '<Version>3.2.2</Version>', '3.2.2 package version')
    req('src/LocalGPT/Components/App.razor','js/localgpt-chat-ui.js?v=3.2.2','3.2.2 JS cache key')
    req('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs','LocalGPT/3.2.2','3.2.2 outbound user agent')

    # Protected chat structure/layout: 3.2.2 does not touch the working composer or quick row.
    heq('src/LocalGPT/Components/Pages/Chat.razor','0d9ab6ed72f41eebbbf8839c54b5fda9a409d424a1fa11c87d2994352c837569','protected Chat.razor')
    heq('src/LocalGPT/Components/Pages/Chat.razor.css','2a620187aa41712f53dddab92ee2ab834c4f46fe512925dce94efb387f28b0e4','protected Chat.razor.css')

    # Real-build compile/guard repairs reported by the user.
    req('src/LocalGPT/Components/Pages/Chat.PresetsAndCouncilConfiguration.razor.cs','ActiveCouncilConfigurationRunId is Guid activeRunId','CS0136 outer pattern variable repair')
    req('src/LocalGPT/Components/Pages/Chat.PresetsAndCouncilConfiguration.razor.cs','UpdateHardwarePerformancePresetIdentity(activeRunId, null)','CS0136 repaired use')
    forbid('src/LocalGPT/Services/CouncilRuntimeService.cs',"raw.TrimStart().StartsWith('{')",'iterator-guard brace-literal false-positive source removed')
    req('src/LocalGPT/Services/CouncilRuntimeService.cs','trimmed[0] == (char)123','iterator-safe JSON opening brace test')

    # Repetition watchdog: preserve old short-loop sensitivity and add long sentence/paragraph cycles.
    w='src/LocalGPT/Services/ProviderStreamRepetitionWatchdog.cs'
    for needle in ['MaximumBufferedCharacters = 32_768','MaximumPeriodTokens = 512','ShortPeriodMaximumTokens = 32','MinimumLongPeriodRepeatedCycles = 4','MinimumLongPeriodAgreement = 0.985d','RequiredSuspiciousSamples = 4','minimumSuspiciousDuration = TimeSpan.FromSeconds(6)']:
        req(w,needle,'watchdog:'+needle)
    req(w,'requiredCycles = isLongPeriod ? MinimumLongPeriodRepeatedCycles : MinimumRepeatedCycles','period-specific cycle threshold')
    req(w,'requiredAgreement = isLongPeriod ? MinimumLongPeriodAgreement : MinimumPeriodicAgreement','period-specific agreement threshold')

    # Ollama restart/recovery: exact identity + same run-scoped road; no CPU demotion.
    req('src/LocalGPT/Interfaces/IProviderModelRuntimeService.cs','Task<bool> WaitForAvailabilityAsync(','provider availability contract')
    p='src/LocalGPT/Services/ProviderModelRuntimeService.cs'
    req(p,'public async Task<bool> WaitForAvailabilityAsync(','provider availability implementation')
    req(p,'candidate.ModelName.Equals(model.ModelName, StringComparison.OrdinalIgnoreCase)','exact Ollama model reavailability')
    r='src/LocalGPT/Services/MultiModelCouncilService.LiveInputAndHealth.cs'
    req(r,'.WaitForAvailabilityAsync(recoveryModel, availabilityWait, cancellationToken)','Ollama reavailability wait')
    req(r,'var recoveryPlan = executionPlan with','preserved run-scoped road')
    req(r,'isOllama ? recoveryPlan.OllamaNumGpu : null','preserved Ollama GPU policy')
    forbid(r,'safe Ollama CPU','no forced CPU fallback text')
    forbid(r,'usesOllamaCpuFallback ? 0','no forced num_gpu=0 fallback')
    pe='src/LocalGPT/Services/MultiModelCouncilService.ParticipantExecution.cs'
    if read(pe).count('message, executionPlan).ConfigureAwait(false)') < 1 or read(pe).count('ex.Message, executionPlan).ConfigureAwait(false)') < 1:
        raise AssertionError('participant recovery calls do not both preserve executionPlan')
    checks.append('participant recovery plan forwarding')

    # Formatting: nested entities decode only at inert code/JSON display boundaries.
    c='src/LocalGPT/Services/CouncilRuntimeService.cs'
    req(c,'for (var pass = 0; pass < 3; pass++)','nested structured entity decode bound')
    req(c,'WebUtility.HtmlEncode(formatted)','structured final HTML encode')
    f='src/LocalGPT/Services/Formatting/ChatContentRenderer.cs'
    req(f,'private string DecodeFencedCodeEntities(string text)','fenced code entity normalizer')
    req(f,'text = DecodeFencedCodeEntities(text);','fenced code normalizer wired before Markdown')
    req(f,'WebUtility.HtmlDecode(line)','fenced code display decode')

    # Rejoin copy: leave persisted marker identity intact but override native copy only for rendered live-Council messages.
    js='src/LocalGPT/wwwroot/js/localgpt-chat-ui.js'
    for needle in ['function liveCouncilCopyText(button)','function bindLiveCouncilCopy(button)','function writeLiveCouncilClipboard(text)','liveCouncilCopyText(button) === null','event.stopImmediatePropagation()','bindLiveCouncilCopy(button);']:
        req(js,needle,'rejoin copy:'+needle)
    req(js,"content.querySelector('.localgpt-live-update-footer,.localgpt-live-participant-board')",'copy interception limited to live Council rendering')
    req(js,"clone.querySelectorAll(\n                '.localgpt-live-participant-board,.localgpt-message-utility-row,.localgpt-live-update-footer')",'copy excludes UI-only live lanes/status')
    req('src/LocalGPT/Components/Pages/Chat.LiveCouncil.razor.cs','var marker = $"{LiveCouncilMessageMarkerPrefix}{runId:N} -->";','stable persisted live-run marker retained')

    manifest=read('build/javascript-diagnostics-files.sha256')
    match=re.search(r'^([0-9a-f]{64})  src/LocalGPT/wwwroot/js/localgpt-chat-ui\.js$',manifest,re.M)
    if not match: raise AssertionError('localgpt-chat-ui.js diagnostics manifest entry missing')
    if match.group(1) != normhash(js): raise AssertionError('localgpt-chat-ui.js diagnostics manifest hash is stale')
    checks.append('JS diagnostics manifest current')

    # Persistence compatibility remains untouched.
    heq('src/LocalGPT/Services/Persistence/DatabaseMigrationCompatibilityService.cs','50bb2f62df4b6cfe5846063d5e4f20c2ab930a57cb95efa580ad6617f3a748ba','DB compatibility service unchanged')

    print(f'LocalGPT 3.2.2 recovery/repetition/rejoin-copy audit passed: {len(checks)} checks.')
except AssertionError as exc:
    print(f'LocalGPT 3.2.2 recovery/repetition/rejoin-copy audit failed: {exc}',file=sys.stderr)
    sys.exit(1)
