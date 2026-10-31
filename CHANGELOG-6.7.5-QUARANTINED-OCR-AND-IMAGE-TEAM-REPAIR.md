# LocalGPT 6.7.5 — Quarantined image OCR path and team recommendation repair

## Evidence from the 2026-10-30 images and Visual Studio output

- The browser's quarantined image upload succeeded. `ChatUploadController.DropAsync` returned HTTP 200; `ChatUploadWorkspaceService` recorded one analyzed file of 1,952,588 declared bytes.
- The OCR availability check reported an installed `deepseek-ocr:3b` on `http://127.0.0.1:11434`, but the explicit OCR action failed **before model inference**: `WorkspaceVisionOcrService.RecognizeWorkspaceImageAsync` rejected a filename that lacked `original/`.
- `ProjectIngestionService.InspectAsync` intentionally records `ProjectIngestionFileEvidence.RelativePath` relative to the `original/` directory. The prior workspace OCR API expected paths relative to the workspace root. That mismatch is the diagnosed failure, not the model itself.
- The advisor incorrectly suggested `Game Engine Extension Development Team` based on broad model-text/substring matching despite a single image and no verified image contents.
- The log independently records an unreachable configured LAN Ollama endpoint. This is **not** proof that the local OCR model failed; the failed OCR call never reached inference.

## Source changes

1. `WorkspaceVisionOcrService`: accept both the ingestion gate's original-root-relative file names and explicitly `original/`/`extracted/` workspace-relative paths. Unqualified paths are resolved **only under `original/`**. Refuse absolute paths, empty/dot/traversal components, drive-style segments, symlinks/reparse points, missing files, unsupported extensions and over-limit image sizes. Keep existing workspace containment, runtime capability checks, evidence provenance and review requirements.
2. `UploadProcessingRecommendationService`: image-only uploads use the deterministic advisory instead of calling a text-only model without image evidence. Filter the Council candidate pool to teams whose saved `PreferredCapabilities` explicitly declare a vision/OCR capability (before ranking); otherwise use Solo. This does not create team mappings or alter saved teams.
3. `UploadProcessingAdvisor`: reset prior OCR status/evidence on a new dropped workspace.
4. Build-wired `Assert-CouncilXRoundWiring.ps1`: add PowerShell source assertions for the path normalization, traversal/link checks and deterministic image recommendations.
5. Version advances 6.7.4 → 6.7.5 (single-digit patch convention).

## Remaining limitations

No OCR request was run on the actual user's image in this environment. `dotnet`, Windows PowerShell and PowerShell Core are not installed here, so Windows build, Blazor/DevExpress UI, Ollama inference, and Linux/macOS compilation require host-side acceptance testing. This source repair does not resolve all earlier Blazor stream/circuit disconnections or the LAN Ollama timeout.
