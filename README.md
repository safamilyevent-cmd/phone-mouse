# Phone Mouse

Wireless iPhone mouse and keyboard for Windows 10 over the local Wi-Fi network.

## Repository status

The GitHub Actions workflow is committed at `.github/workflows/build-windows.yml`. It targets Windows x64, publishes a self-contained .NET 8 application, and prepares Windows and source ZIP artifacts. A version tag such as `v1.0.0` triggers a GitHub Release.

**The application source has not yet been uploaded to this repository.** The build workflow now detects a checked-in `PhoneMouse/PhoneMouse.sln` and builds it directly. Until that directory is added, CI will stop with a clear source-missing error.

## Add the source

Upload the `PhoneMouse/` directory from the prepared project archive to the repository root. It must contain `PhoneMouse.sln`, `README_RU.md`, and `PhoneMouse.Server/`.

## Build and release

Push to `main` to run CI. Push a version tag such as `v1.0.0` to create a GitHub Release with the Windows x64 ZIP and source ZIP.
