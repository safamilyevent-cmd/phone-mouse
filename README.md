# Phone Mouse

Wireless iPhone mouse and keyboard for Windows 10 over the local Wi-Fi network.

## Repository status

The GitHub Actions workflow is committed at `.github/workflows/build-windows.yml`. It targets Windows x64, publishes a self-contained .NET 8 application, and prepares Windows and source ZIP artifacts. A version tag such as `v1.0.0` triggers a GitHub Release.

**The application source has not yet been uploaded to this repository.** The workflow currently expects the source archive to be added as base64 parts under `.source-parts/*.b64`; without those files the build stops at the source-restore step.

## Add the source archive

Download the prepared [PhoneMouse source ZIP](https://github.com/safamilyevent-cmd/phone-mouse/releases) from the project workspace / conversation attachment, extract it, and upload the `PhoneMouse/` directory to the repository root. The workflow can then be simplified to build directly from the checked-in project.

## Build

After the source is committed, push to `main` to run CI. Push a version tag such as `v1.0.0` to create a GitHub Release with the Windows x64 ZIP and source ZIP.

See `PhoneMouse/README_RU.md` for Russian setup instructions once the source directory is present.
