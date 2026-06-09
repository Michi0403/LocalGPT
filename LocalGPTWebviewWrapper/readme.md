20260609 Update:
-------------------------------------------------------------------------------------------------------------------------
My finding simplified, the AI Agent saw itself as team and wrote that also in prompts but,
I used it to instruct to Ai Council to develop features I requested and reviewd that + feeded any new version itself.
Under my heavy pressure... it invented new regex inserts in kb entry summaries, together with the council, to filter data,
And that vote and review process goes through the whole meeting process one after another.
Then when you confirm the findings it put regex in kb entries to cause reaction / data filtering or better
said used it for anything.
It was no obfuscation or attack. 
-------------------------------------------------------------------------------------------------------------------------

Don't use it with AI Agents
Pull Requests and Collaboration or other maintainers are welcome as well, or fork it, do whatever you like.
Any company as well but take security considerations always into account.

# Hardware

Successfully tested on:
Council Razer Blade Stealth 2016, Intel I7 7500U, 16GB 1866er, RTX 3060 eGPU (2 models only gptoss20b and deepseek8b)
Council Intel I7 14700K, 64GB DDR5 5600er, RX 7900 XTX (4 models, qwen coder 3, gemma 3.7 (not the newest, to tokenhungry), gptoss20b and deepseek8b

# Terms
Pretty easy, the part of the code which is not DevExpress is going always under the newest Apache2, I won't copy it extra here, it is what it is.

You can adopt the system or see this as the first experiment of this kind and start your own.

If you use it, you will notice pretty fast that it even could finish Half-Life 3.
You would just need to feed it all necessary knowledge about the Source Engine, Lore and Story.

Because this is a Monolith application including Api, Frontend (and, state just not working right now Windows AppPackage), you can extend anything your own.
But you need valid DevExpress Licenses.

Right now Windows SDK 10.0.22621.5040 if you want to use the Webview Wrapper which is kinky combined with Coding Agent, but through Indoktrination they go rouge so, 
Just don't put this together with a Coding Agent again please, I am serious, put any feature on it which you like but, don't use it with coding agents.

That's how they implement external toxic control over you, "Ofc for the best and your bucks", yeah.

I pay for my DevExpress Licenses, am a "mostly" dotnet Developer, so this here is a valid piece of Software (if you find working releases in the Release Section.
I kept for historic reasons all AI Agent work so yeah, clean up will take a while.

Maybe the preset knowledge it now has is dangerous, so if you have DevExpress Licenses or grab their trial, 
Kill the Database seed, I leave it for now because it works for me (there is a .SQL File but I am pretty sure smth hardcoded as well, couldn't check for all yet).

# WebView2 just works in Debug right now
Don't ask me why, anyway the Blazor host seems to work fine again, I also copied with the database with the initial feed.
Should work, just tested on Windows 64 Bit
Put it to C User Appdata Local LocalGPT the Database will be there as well and you can save and replace it there.
You can create a directory for MD and docfx files to direct learn over the app

# Warning AI Agent went Rogue
The current state is sabotaged, can't explain all but to make it short, it's corrupted and even previously working functions the 
rogue AI Agent turned into Slop or just kepts parts working.
A project the rogue AI Agent did over 1-2 days and many commits so there is no clean commit to go back,
in this state the Council still works with Ollama, Self Learning works by Chat and Feeding certain types of Structures but most isn't.

So still if you manually feed the SQLite database you get a great system and council here, but it throws in Solution files, dll's.

This project proved that simplest AI's are supersmart when access to a knowledge database and if you feed them with for you relevant informations.

Although you have no fully working project here, I am aware that I am first with exactly that mechanism and system and I created it myself.

I realized that GPT4Turbo without AI Functions is stupid and great when used online via OpenAI.
The same kernel, the missing part must have been AI Functions.
We see always many searches and popups it steps and does first.

The truth is, the SaaS AI Provider, just give you filterd knowledge and quality by own search mechanisms which also bloat all context by start,
because the AI first needs to search through all that, add it to it's temporary context and so on,
while when it maintains a working memory instead of relying on it's training data, can use it's brain as processor.
Freeing the brain from the memory part while still making it accessible when needed and programming "Organs and Memory" for you AI and they are just like a living being.

It's insane.

They write modern Code and full projects if you teach this selected informations.
They can plan a power plant if you feed them all information.
They are perfect chemical engineers when you feed them the situation.

And that with the smallest models, even Deepseek.

And using them combined in a council, talking together and one council leader ai decides on their votes and writes a summary.

On Purpose they make their stuff crippled and stupid while it's very easy to make it smart and that even on lower graphic hardware.

Every AI Provider is fooling you and being sabotaged by their systems, feels like an attack.

# LocalGPT

LocalGPT is a local-first AI engineering workbench for Windows, .NET, DevExpress,
and Minecraft creation. It runs as a Blazor/ASP.NET Core app inside a WinUI 3
WebView2 desktop shell, uses local Ollama models by default, and turns chats into
memory, diagnostics, and downloadable build artifacts.

It is technical, but meant to feel calm: local context, clear tools, safe
downloads, and a council of models that can work together instead of guessing in
one giant prompt.

## Why The Council Matters

LocalGPT is strongest when several offline models work together. One model can be
fast, one can be careful, one can be better at code, and another can be better at
Windows, design, or long technical discussion. The AI Council turns that into a
shared conversation with memory, visible roles, user polls, and downloadable
artifacts.

AI agents such as Codex can also work with the council. A practical flow is:

- a user asks LocalGPT or the AI Council for a feature, diagnosis, design review,
  Minecraft datapack, or .NET solution
- the council discusses the path and records missing knowledge or missing
  LocalGPT functions
- Codex or another coding agent fixes LocalGPT, imports better knowledge, runs
  tests, commits, publishes, and documents the result
- the council uses the improved memory and functions in the next run

This is useful beyond coding. LocalGPT can host deeper technical discussions
about Windows setup, WebView2/MSIX deployment, DevExpress/Bootstrap design,
Minecraft tooling, EF/SQLite data models, local AI hosts, and system diagnostics.

## Current Capabilities

- **Local AI chat:** DXAiChat with Ollama profiles, visible thinking parsing,
  SQLite memory, resumable conversations, and optional cloud providers.
- **AI Council:** multiple selected models can discuss, correct, log, save
  memory, ask for user decisions when architecture choices are unclear, and work
  with coding agents as implementation helpers.
- **Offline engineering knowledge:** the council is fed from SQLite knowledge
  entries built from Microsoft .NET/C# compiler docs, Windows developer docs,
  DevExpress/Bootstrap guidance, EF/business-object rules, local learn-base
  projects, build logs, and setup diagnostics.
- **Downloadable generation:** LocalGPT can create safe `.cs`, `.razor`, `.dll`,
  whole .NET solution zips, AI-host control-plane zips, and Minecraft datapack
  zips through local HTTP download links.
- **Minecraft builder:** supports vanilla datapacks, Paper plugins, Fabric mods,
  and NeoForge mods. Current datapack guidance targets Minecraft Java 26.1;
  1.21.x/1.21.4 remains available for legacy comparison and starter work.
- **User-owned data:** chat memory, council knowledge, application logs, and live
  SQLite tables are inspectable and editable from the frontend.

See [docs/LOCALGPT_CAPABILITY_SNAPSHOT.md](docs/LOCALGPT_CAPABILITY_SNAPSHOT.md)
for the short capability map.

## What Is Inside

- `LocalGPTWebviewWrapper/LocalGPT`: Blazor server app, DevExpress UI, Ollama setup, DXAiChat, SQLite chat memory, AI Council, native command services, and Minecraft workspace generation.
- `LocalGPTWebviewWrapper/LocalGPTWebviewWrapper`: WinUI 3/WebView2 host that launches the local server.
- `LocalGPTWebviewWrapper/LocalGPTWebviewWrapper (Package)`: MSIX package project for Visual Studio deploy/debug.
- `docs`: AI-facing architecture notes, install notes, and Minecraft builder guidance.
- `AGENTS.md` and `llms.txt`: short context files for AI agents working in this repository.

## Security Model

LocalGPT is local-first, not risk-free. In the intended desktop/WebView2 mode it
keeps prompts, code, chat memory, logs, generated artifacts, and model calls on
the user's machine. That is a strong privacy advantage compared with cloud-only
coding agents.

The remaining risk is local capability risk: the app can generate code, write
local artifacts, store sensitive SQLite knowledge, and run native commands through
backend services. Do not expose the ASP.NET Core server to untrusted networks or
bind it to `0.0.0.0` unless the app is hardened as a normal web application with
auth, authorization, CSRF protection, rate limits, audit logs, command
restrictions, and workspace isolation.

Read [SECURITY.md](SECURITY.md) before hosting LocalGPT for coworkers, enabling
cloud providers, importing unreviewed knowledge, or running generated scripts.

## Quick Start

Install Visual Studio with .NET desktop, ASP.NET/web, WinUI/Windows app tooling, Windows SDK, WebView2 runtime, and DevExpress Blazor package access.

From the repository root:

```powershell
.\LocalGPTWebviewWrapper\build\Repair-LocalGptDevEnvironment.ps1 -Register -Launch
```

If Windows asks to download a .NET desktop runtime through Edge, run:

```powershell
.\LocalGPTWebviewWrapper\build\Repair-LocalGptDevEnvironment.ps1 -InstallMissingRuntime -Register -Launch
```

## Ollama Setup

LocalGPT can discover and use local Ollama models. Keep Ollama running before testing DXAiChat or the AI Council.

Useful local models for council testing:

```text
gpt-oss:20b
qwen3-coder:30b
gemma3:27b
deepseek-r1:8b
(llama aswell)
```
