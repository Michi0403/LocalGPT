# LocalGPT 4.6.9

## Local Chat workspace drag/drop repair

- Removed the permanent Upload advisor panel from the normal `/chat` document flow. The advisor component now renders no idle upload form, goal editor, grid or Select File button.
- Added a PublisherStudio-style external-file landing overlay that is created only while files are actively dragged over the Local Chat surface and removed when the drag leaves or completes.
- Added a bounded multipart drop endpoint that streams browser file handles into the existing `IChatUploadWorkspaceService` quarantine path without echoing file content.
- Preserved the existing Chat paperclip attachment path for ordinary message attachments. External desktop drops use the workspace/quarantine analysis workflow instead.
- After a successful drop, LocalGPT performs the existing deterministic ingestion inspection and processing recommendation, then opens only the post-drop review popup. Promotion remains gated by deterministic checks, independent review and explicit user approval.
- Drag/drop limits are taken from the existing `LocalGptCatalogService` upload policy and are enforced both in the browser-facing bridge and on the server boundary.
- Added release regression coverage that forbids the old permanent upload panel and requires the idle-invisible drag/drop bridge, streamed quarantine endpoint and post-drop review contract.

## Preserved 4.6.8 repairs

Kernel Creature Tournament exact-session correlation, high-resolution tournament frames, ASCII/Pixel presentation switching, bounded same-run rejoin, Operator-to-Game return and non-fullscreen console layout/scroll repairs remain intact.

PublisherStudio source is unchanged in this release.
