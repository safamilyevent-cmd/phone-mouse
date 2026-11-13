# Phone Mouse — Windows GUI

Windows 10/11 x64 graphical launcher for the Phone Mouse local-network server.

- Start / stop the server process.
- Show discovered private IPv4 addresses and the pairing code.
- Generate a QR code containing the direct iPhone Safari URL and pairing code.
- Copy or open the connection URL.
- Display the connected/disconnected WebSocket client state and server log.

The GUI expects the server executable at `Server/PhoneMouse.Server.exe`, as packaged by GitHub Actions. The connection uses HTTP/WebSocket on the local network and is not encrypted; do not expose port 8765 to the internet.