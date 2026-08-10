# Chat and AI Council

## Direct chat

The Chat page is the primary conversation surface. One provider-qualified model answers ordinary requests while configuration remains collapsible so the transcript keeps the available viewport.

A chat session owns its selected provider route, protocol profile, formatter, bounded context, and feedback state. Protocol-specific control tokens are normalized by the matching profile instead of being scattered through the UI.

Supported protocol families include Harmony-style channels, DeepSeek/R1 thinking markers, Gemma turn markers, Apple/OpenELM-style roles, and generic think-tag formats. The formatter boundary keeps these families isolated.

## Council teams

A Council team turns one request into an explicit workflow. Each member has a role, provider-qualified model route, optional runtime class, and bounded task. The team configuration is durable; the active run state is not replaced by whichever model speaks last.

Typical roles include:

- coordinator or leader;
- implementation specialist;
- architecture or safety reviewer;
- verifier;
- domain specialist;
- world actor for GameDirector sessions;
- human participant when invited.

The Council spooler and preflight services prepare work, validate routes, and track steps. A model can propose a result, but deterministic application rules still decide whether a step is complete, failed, recoverable, or waiting for a human.

## Provider-qualified routing

The visible address follows this shape:

```text
Provider — model @ endpoint
```

A bare model name is accepted only when it resolves to one provider route. Ambiguous or stale selections fail clearly instead of falling back to an arbitrary endpoint.

Mixed-provider Councils can combine multiple Ollama hosts, LM Studio or another OpenAI-compatible endpoint, OpenAI, and Azure OpenAI. The correct client is created immediately before each call. Provider-specific settings—such as Ollama GPU options—stay with the provider that understands them.

A provider host is identified by provider kind and normalized endpoint. Adding a second Ollama does not replace the first: the existing primary remains the default and the new endpoint becomes an additional host unless the user explicitly promotes it. On refresh/run preflight, stale provider-qualified selections are deselected or rejected with their exact endpoint; LocalGPT never substitutes a same-name model from another host.

## Benchmark Council

A bounded benchmark measures one or more selected routes using deterministic tasks and explicit limits for context, output, count, and timeout. Other selected models can review the recommendation; when no independent reviewer is available, a bounded self-review may be used.

Every benchmark run is registered as a detachable live Council session. Its current target, profile, task, timeout, review, and recommendation progress can be opened from the benchmark panel or rejoined from **Chat → Running session tools**. Stopping that live session cancels the owned benchmark run. Automatic DXFunctions are disabled during benchmark calls so tool negotiation does not distort the measured route.

Recommendations are never applied automatically. The user chooses whether to save or apply a preset. Applying a preset changes future LocalGPT routing; it does not rewrite provider-global server configuration.

## Result integrity

A step that produces thinking but no substantive final answer receives at most one bounded final-only recovery request. If the result is still empty, the step is failed rather than counted as success.

Peer verification is advisory. A failed verifier can be recorded as a warning while a valid result remains available, but the UI must not relabel unrelated notices as successful verification.

## Human participation

A human can join a running Council, answer a question, approve a proposed boundary, or provide a correction. Human participation does not turn every human message into unrestricted authority; the application still binds decisions to the current request, action, target, and approval scope.

## DX functions and change reviews

DX functions expose bounded application capabilities through typed parameters and explicit handlers. Generated changes can be stored as review records with proposed files, findings, execution status, and human decisions. The function catalog describes what can be requested; it does not bypass the service that owns the real operation.

> [!NOTE]
> Council output is a structured proposal pipeline, not a magic quorum. The boring deterministic parts are what keep the fun parts usable.
