# File Transfer Manager

A small desktop tool for copying files and folders sequentially (one after another) instead of spawning many parallel copy operations like Windows Explorer. The goal is predictable throughput, reduced disk/IO contention, and clearer progress reporting when moving large sets of files.

## Why sequential copying?

- Avoids multiple simultaneous copy operations that compete for disk throughput and CPU.
- Produces a single, accurate progress indicator instead of many overlapping progress bars.
- Makes error handling and overwrite decisions deterministic (you decide once, the queue respects it).
- Reduces system load and improves responsiveness on machines with mechanical disks or constrained IO.

## Key features

- Queue files and folders to copy; each item is processed in order.
- Option to include subfolders.
- Overwrite mode: `Ask` / `Always` / `Never`.
- Single consolidated progress bar and estimated remaining time.
- Simple UI (WPF + WinForms frontends in the repo) for batching copy operations.
- Graceful abort support for the current copy run.

## Quick start

1. Open the solution in Visual Studio (targets .NET Framework 4.7.2).
2. Build and run the `FileTransferWpf` or `FileTransferManager` project.
3. Add folders or files using the UI, choose overwrite behaviour, then press `Copy`.

## UI notes

- The WPF main window is `FileTransferWpf/MainWindow.xaml` (code-behind `MainWindow.xaml.cs`).
- The WinForms version uses `MainForm.cs`.
- The list shows queued items; the app uses a `CopyItem` model with a `DisplayText` property for UI display.

## Design notes

- The tool deliberately queues copy operations and processes them sequentially to avoid the problems described above.
- For responsiveness, UI updates are marshalled to the UI thread via the `Dispatcher`.
- The image/icon conversion helper uses `Imaging.CreateBitmapSourceFromHIcon` and freezes the result so it can be used safely by the UI thread.

## Contributing

Open a PR for bug fixes or improvements. Keep changes small and focused on a single behavior (queueing, progress, error handling, UI).

## License

See the repository `LICENSE` file for details.

## Contact

Use the GitHub repository issues to report bugs or propose changes.
