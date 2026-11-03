# MW2 FPS / FOV Unlocker — CrossOver edition

A fork of [andersblomqvist/mw2-fps-unlocker](https://github.com/andersblomqvist/mw2-fps-unlocker) with working FPS/FOV controls in a short in-game CrossOver test on Apple Silicon.

The ready-to-run CrossOver build starts at **120 FPS cap / 80 FOV**, keeps both settings applied when MW2 resets them, and retains the FPS/FOV sliders and independent apply switches. It validates the exact executable and dvar layout before writing, and skips writes when values already match. Unknown game builds are rejected.

Run `MW2.CrossOver.Unlocker.exe` inside the same **Steam** CrossOver bottle as MW2. No arguments are needed. Keep the utility open while playing and keep its entire folder together. See [CROSSOVER.md](CROSSOVER.md) for setup and [CROSSOVER_RESULTS.md](CROSSOVER_RESULTS.md) for the tested environment and limitations.

```sh
./scripts/build-crossover.sh
```

Uses SDK 10.0.403 and builds a self-contained Windows x64 folder. The separate diagnostic build, `./scripts/build.sh`, still cannot write game memory. `scripts/build-experimental.sh` provides explicitly selected diagnostic/test modes.

Tested defaults: FPS-cap setting 120 and FOV 80, CrossOver 26.3 on macOS 26.5.1 / Apple Silicon, exact Steam executable SHA-256 recorded in `profiles/mw2-profile.json`. Wider FOV was confirmed in-game. Actual rendered FPS and long-term stability were not measured; other slider targets were not exhaustively tested. No VAC-safety guarantee, anti-cheat bypass, injection or stealth behavior is provided. MIT license retained.
