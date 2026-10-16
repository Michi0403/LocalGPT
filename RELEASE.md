# LocalGPT 5.8.3

Build maintenance release. See `CHANGELOG-v5.8.3-RAZOR-XML-BUILD-REPAIR.md` and `VALIDATION-v5.8.3-source.md`.

Fixes the two source-level build failures reported against 5.8.2: the Razor architecture guard's rejection of native `<section>` containers in `Chat.razor`, and the three CS1573 missing XML parameter descriptions. The Game Council source-read, project, and evidence repairs from 5.8.2 remain intact. No PDFsharp, PublisherStudio, maintenance validator, or PowerShell behavior has been changed.
