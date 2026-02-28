# Oracle Log Render Slowdown Theories

Date: 2026-02-28

## Scope

This note captures current theories only. No code changes were made as part of this investigation.

## Primary Theory: UI Thread Backpressure

The most likely cause is that log ingestion is being throttled by synchronous UI work.

- The WebSocket receive loop raises `RawMessageReceived` for each incoming message.
- `MainWindow.Transport_RawMessageReceived` sends each line into `AppendLog`.
- `AppendLog` uses `Dispatcher.Invoke(...)`, which blocks the calling thread until the UI thread finishes processing.

Because of that, the transport thread cannot continue reading at full speed when the UI thread is busy. The result would be:

- raw lines still arrive from the SHP quickly
- Oracle falls behind while rendering
- lines accumulate in memory or the socket buffer
- the UI shows them in bursts or chunks instead of immediately

This matches the reported behavior that even raw lines now appear slow.

## Why Raw Output Also Looks Slow

Even when only looking at raw output, the path still does more than a simple append.

Per raw line, the app currently does all of the following on the UI thread:

- updates timestamp bounds
- appends to `_rawLogLines`
- checks active filters
- updates the visible count text
- appends formatted text to the raw RichTextBox
- optionally auto-scrolls
- may update find state
- if the processing engine is active, also generates and appends processed output

So a slowdown in UI rendering affects both raw and processed streams because they share the same synchronous path.

## High-Likelihood Regression: Processed Output Added Inline

One likely regression point is the change that added processed-line generation directly inside the raw append path.

- `AppendLog` now calls `AppendProcessedLineIfNumbered(...)` whenever `_processingEngine` is active.
- That means every raw line can trigger processing-engine work before the transport thread is allowed to continue.

This creates extra per-line CPU and UI cost:

- parse numbered line
- run processing engine
- classify processed line
- append to a second RichTextBox
- update processed counters/state

If this feature was added after the app previously felt responsive, it is a strong candidate for the slowdown.

## Additional Likely Regression: Tagged Message Recording

Another likely contributor is the later addition of per-line tagged message recording for processed output.

For each processed line, the code now may:

- scan for diagnostic tags
- normalize the line
- run multiple regex matches to infer a driver name
- update nested dictionaries and hash sets

This is also happening on the UI thread during live rendering. Even if each operation is small, repeated per line at log-stream rates can materially slow the app.

## RichTextBox Rendering Cost

Both raw and processed panes appear to append into WPF `RichTextBox` documents.

That can get expensive because each line may involve:

- creating `Run` objects
- inserting `LineBreak` elements
- highlight handling
- text classification
- width/layout recalculation
- optional scrolling

As the document grows, incremental UI updates can become progressively more expensive.

## Combined Effect

The likely problem is not one single slow function, but a stacked synchronous pipeline:

1. socket message received
2. sync marshal to UI thread
3. raw bookkeeping and raw RichTextBox append
4. processed-line generation
5. processed bookkeeping, tagging, classification, and RichTextBox append
6. layout and scroll work

Since step 2 is synchronous, the transport is effectively gated by the total cost of steps 3 through 6.

## Most Plausible Recent Change Sequence

The most plausible sequence is:

1. log ingestion originally felt immediate because the UI path was lighter
2. processed output was added inline per raw line
3. more per-line processed work was later added (tag recording and related parsing)
4. the cumulative UI cost crossed the threshold where the transport now stalls behind rendering

## What To Verify Later

When investigation resumes, the highest-value checks would be:

- measure whether the delay disappears when processed output is disabled
- measure whether the delay disappears when tagged message recording is disabled
- compare behavior when `Dispatcher.Invoke` is replaced by an asynchronous queue
- measure whether the slowdown worsens as the RichTextBox document grows

## Current Conclusion

The strongest current theory is:

Oracle is not receiving the SHP logs slowly. Oracle is blocking its own receive path because each line waits for synchronous UI-thread rendering and per-line processing work to finish.
