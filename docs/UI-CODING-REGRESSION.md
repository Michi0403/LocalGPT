# Coding workflow regression in the user interface

Build the current checkout in Visual Studio with the repository PowerShell gates enabled. Start LocalGPT from Visual Studio. Keep its Microsoft Edge chat and the latest Council run visible so the operator can inspect the stream and intervene. Perform each application action through its user interface.

1. Select Knowledge-Assisted Coding Subagents in Chat Configuration. Assign the available coding models by role and use the existing configured context limits.
2. Start a new chat and request a .NET 10 HelloMath console project with a project file, a separate arithmetic class, and a console entry point. Request a downloadable ZIP and a real build result.
3. Inspect the tool receipts. The draft should show three distinct file completions, offer a hash-bound approval only after all three, and expose the review ID. Check that a retry does not duplicate a completed file.
4. Inspect the approval and respond through Human Collaboration. Record the actual build status and open the download from chat. Verify the ZIP contains the three files and compare them to the review.
5. For workspace learning, attach the same source archive and select the saved Learning Round3 team. Also test a small ZIP with two readable files and one directly attached text file. Verify semantic read receipts and successive offsets for large files.
