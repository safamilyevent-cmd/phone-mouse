# Phone Mouse

Wireless iPhone mouse and keyboard for Windows 10/11 over the local Wi-Fi network.

## Windows desktop app

The Windows package now includes a graphical launcher:
- Start and stop the local server.
- Display the pairing code and detected private IPv4 addresses.
- Generate a QR code that opens the iPhone Safari interface with the pairing code prefilled.
- Copy/open the connection URL.
- Show whether an iPhone WebSocket client is connected.
- View server output and errors.

The desktop app is `PhoneMouse.exe`; the server is bundled under `Server/PhoneMouse.Server.exe`. Extract the complete ZIP and launch `PhoneMouse.exe` or `Run-PhoneMouse.bat`.

## Build and release

GitHub Actions builds a self-contained Windows x64 GUI and server, then attaches Windows and source ZIP archives to the workflow run. Pushing a tag such as `v1.0.0` creates a GitHub Release.

See [Russian project documentation](PhoneMouse/README_RU.md) for setup, architecture and limitations.

## Security notice

The current local-network connection uses unencrypted HTTP/WebSocket. Do not expose port 8765 to the public internet or use untrusted Wi-Fi networks.