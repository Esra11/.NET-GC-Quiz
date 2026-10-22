# .NET GC Quiz

A Windows Forms learning app with a three-minute introduction, **143 distinct GC interview questions**, two exam modes and an annotated answer report. The questions progress from a general overview to deeper, grouped concepts. Repeated conversation questions have been consolidated, and detailed explanations address the follow-up discussions.

## Run

1. Open [Releases → Latest release](https://github.com/Esra11/.NET-GC-Quiz/releases/latest).
2. Under **Assets**, download **.NET-GC-Quiz-v1.0.0-win-x64.zip** (not the source-code ZIP).
3. Right-click the ZIP and select **Extract All**.
4. Open the extracted folder and double-click **Run Quiz.cmd**.

Requires Windows x64. The release includes the .NET runtime; no SDK, Python or separate .NET installation is needed. Keep the extracted files together.

### Only if you modify the source code

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), then rebuild and run your changes from the project folder:

```powershell
dotnet run --project src/DotNetGcQuiz
```

You can also open `DotNetGcQuiz.slnx` in Visual Studio with .NET 10 and Windows desktop development support. App behavior is written in C#; questions can be edited directly in `src/DotNetGcQuiz/Content/questions.json`. The Python helper is optional and is not required to edit or run the app.

## How it works

Dark mode is enabled by default. The header button offers **Light Mode** when dark mode is active, and **Dark Mode** when light mode is active. Switching themes does not reset the current exam or answers. Each new app launch starts in dark mode.

- **Learn Introduction to GC:** a short guide to reachability, generations, collection phases, allocation, pinning, finalization and performance.
- **Show Answer and explanation when selecting answer:** select one radio option, read immediate correct/incorrect feedback, then select Next Question. Submitted choices cannot be changed.
- **Live Quiz:** selecting an answer advances immediately. Answers and explanations remain hidden until the exam ends.
- **End Exam:** available on every question. Ends immediately, including with no answers.
- **Display Report:** available after either mode. Shows each choice, your selected answer, the correct answer and an explanation. Unanswered questions are labeled. Save the full report as an offline HTML file.
- Report options start with a red **✕** for an incorrect selected answer or a green **✓** for the correct answer. The entire corresponding row has matching red or green background highlighting and text, in both themes and quiz modes. A correct selection is marked with the green tick and labeled as your answer. Saved HTML reports also highlight these rows.
- **Display Only Incorrect Answers:** filters to questions you answered incorrectly; correct and unanswered questions are excluded. **Display All Answers** restores the complete report. Original question numbers and the full-exam score stay unchanged. Saving HTML exports the currently displayed filter.

The final score is correct answers divided by the **full question count**. Unanswered questions count as zero. There is no time limit, account, network request, telemetry or automatic answer persistence. Closing an active exam asks for confirmation; completed results remain available until you start another exam or close the app.

## Learning path and content

Overview → generations → allocation → roots and metadata → marking → planning → sweeping → relocation and compaction → pinning → handles and weak references → finalization and disposal → suspension and GC modes → diagnostics and memory.

The bank adapts the distinct concepts from the GC interviews in the supplied conversation, including the .NET GC Internals series discussions. It consolidates repeated prompts rather than reproducing a historical transcript. Some explanations correct ambiguities raised during those interviews. The original videos and subtitles are not redistributed. Brick tables, plugs, demotion and mark data structures are presented as runtime implementation details; they are not public API contracts or promises that every runtime version implements identical phases.

Primary references for reviewing or extending the material:

- [GC fundamentals](https://learn.microsoft.com/dotnet/standard/garbage-collection/fundamentals)
- [Background GC](https://learn.microsoft.com/dotnet/standard/garbage-collection/background-gc)
- [Large object heap](https://learn.microsoft.com/dotnet/standard/garbage-collection/large-object-heap)
- [Implementing Dispose](https://learn.microsoft.com/dotnet/standard/garbage-collection/implementing-dispose)
- [GCHandle](https://learn.microsoft.com/dotnet/api/system.runtime.interopservices.gchandle)
- [ConditionalWeakTable](https://learn.microsoft.com/dotnet/api/system.runtime.compilerservices.conditionalweaktable-2)
- [CoreCLR GC design](https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/garbage-collection.md)
- [Understanding different GC modes with Concurrency Visualizer](https://devblogs.microsoft.com/premier-developer/understanding-different-gc-modes-with-concurrency-visualizer/)

## Edit questions

The supplied colorful question-mark artwork is stored in `src/DotNetGcQuiz/Assets/quiz.png`. Both the executable and main window use its converted `quiz.ico` (16-256 px). Regenerate it with `powershell -NoProfile -File tools/build_icon.ps1` on Windows; conversion preserves the artwork's proportions and transparency.

The app embeds `src/DotNetGcQuiz/Content/questions.json`. Its array order is the exam order. Each entry has a unique ID, section, topic, prompt, exactly four options, a zero-based `CorrectIndex`, and an explanation. Rebuild after editing. Questions are validated at startup. There is no runtime dependency on Python or external content files.

`tools/build_bank.py` is an optional maintainer helper containing the question authoring data; `tools/explanation_notes.txt` adds the follow-up explanations. If using it, edit those files and run `python tools/build_bank.py` to regenerate JSON; it deterministically rearranges options. If editing JSON directly, do not later overwrite those edits with the helper without reconciling them.

## Verify

No external test packages are required.

```powershell
dotnet build DotNetGcQuiz.slnx -c Release
dotnet run --project tests/DotNetGcQuiz.Tests -c Release
dotnet run --project src/DotNetGcQuiz -c Release -- --smoke-test artifacts/ui
dotnet run --project src/DotNetGcQuiz -c Release -- --report-benchmark artifacts/report-benchmark
```

The executable checks test progression, scoring, early termination, HTML encoding, question ordering and bank integrity. The Windows UI smoke test exercises both modes and saves screenshots and a report under `artifacts/ui`. It opens a temporary form offscreen and closes it on completion. A failed smoke test exits with an error.

The full-report benchmark uses all 143 questions, records opening/filter/theme timings, and verifies scrolling to the end, viewport-only painting, UI message processing, and cancellation when navigating during layout. Reports use lightweight row data instead of a control per row; text measurement runs on a worker with its own graphics resources, while only visible rows are drawn on the UI thread. A short “Preparing report…” message appears during background layout. Keyboard arrows, Page Up/Down and Home/End are supported when the report has focus.

## Build a portable Windows app

```powershell
dotnet publish src/DotNetGcQuiz -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/publish/win-x64
```

Run `artifacts/publish/win-x64/DotNetGcQuiz.exe`. This build includes the runtime. Use `win-arm64` instead of `win-x64` for a native ARM64 build. The GitHub workflow builds, verifies and uploads the x64 portable app as an artifact.

Publishing the self-contained variant requires access to NuGet for the official runtime packs. GitHub Actions produces this build on Windows; release downloads include that runtime.

## Repository

[Esra11/.NET-GC-Quiz](https://github.com/Esra11/.NET-GC-Quiz). Build outputs, reports, dumps, videos and subtitles are excluded by `.gitignore`. Python and PowerShell files under `tools/` are optional authoring utilities and are excluded from GitHub's application language chart.
