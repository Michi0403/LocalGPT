# LocalGPT 7.7.6 · Workshop Wren

## Windows test configuration

| Setting | Test value |
| --- | --- |
| LocalGPT origin | `http://127.0.0.1:5000` |
| Build toolchain | .NET SDK 10.0.404, discovered and validated in `/install?section=toolchains` |
| Artifact builds | Enabled by the shipped configuration. Check `/install` for a persisted override from an older install. |
| Approval | Exact request once, consumed after use |
| Runtime data root | Check Runtime Storage in `/install`. The default Windows location is `%LOCALAPPDATA%\LocalGPT`. |

Add the Ollama providers under `/install` and test each connection before saving the `Four-PC C# Council` preset. Use the tested model IDs from each host's inventory.

| Host | Model |
| --- | --- |
| `http://192.168.178.66:11434` | `gemma4:12b` |
| `http://192.168.178.74:11434` | `qwen3.5:9b` |
| `http://192.168.178.77:11434` | `qwen3.5:4b` |
| `http://192.168.178.68:11434` | Qwen 3 Coder 30B |

The successful solution recovery check used `qwen2.5-coder:7b` locally. Use it as a single-model diagnostic if a LAN participant is unavailable.

## Council solution and binary check

1. Ask the Council to create `DiskParity`, a .NET 10 C# console app that accepts an integer and reports whether it is odd or even. Ask it to write `Program.cs`, `DiskParity.csproj` and a valid `DiskParity.slnx`, then build and package the result. The solution should contain `<Solution><Project Path="DiskParity.csproj" /></Solution>`.
2. Inspect the proposed files under `CouncilArtifacts\DiskParity`. Verify the solution with `dotnet sln <path-to-DiskParity.slnx> list`. In Approvals & Team, approve the exact reviewed build once.
3. Check `Workspaces\LocalGPT-CodeGeneration\review-*` and `CouncilArtifacts\localgpt-change-review-*.zip` for the solution and Release executable and DLL. Run the executable with 7 and 8 using the input method implemented by the model. Expect `Odd` and `Even`.

## Uploaded repository check

1. ZIP a disposable .NET 10 console project with `Program.cs`, its `.csproj` and a `README.md`. Upload it through Chat. Record the original extracted file hashes under `ChatUploadWorkspaces`.
2. Ask for an exact edit to `Program.cs` that reports odd or even and exits with code 2 for invalid input. Approve the staging request for the uploaded archive and then approve the reviewed edit in the staged copy.
3. Confirm `CouncilArtifacts\repository-stage-*` contains the complete repository with only `Program.cs` changed. Compare the extraction hashes again. Build the staged project with `dotnet build -c Release` and try `7`, `8` and `hello`. The separate build command isolates verification of the file edit from Council build orchestration.

## PublisherStudio integration check

Open PublisherStudio and LocalGPT in the same browser. Use **Connect LocalGPT** in PublisherStudio, enter the LocalGPT loopback origin, and complete reciprocal 1-Wire approval. Enable browser automation for the PublisherStudio tab. Use a blank, disposable publication to request a short story, a two-column spreadsheet and a one-page PDF. Check that the text is inserted in the intended story, the spreadsheet cells contain the requested values, and the exported PDF opens with the expected text. Record which outputs used the live linked LocalGPT session.
