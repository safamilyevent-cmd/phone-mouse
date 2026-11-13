using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using PhoneMouse.Server;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:8765");

var configuredCode = builder.Configuration["PairingCode"];
var pairingCode = string.IsNullOrWhiteSpace(configuredCode)
    ? RandomNumberGenerator.GetInt32(100000, 999999).ToString()
    : configuredCode;

var app = builder.Build();
app.Use(async (context, next) =>
{
    var ip = context.Connection.RemoteIpAddress;
    if (ip?.IsIPv4MappedToIPv6 == true) ip = ip.MapToIPv4();
    if (ip is null || !IPAddress.IsLoopback(ip) && !IsPrivateIPv4(ip))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return;
    }
    await next();
});
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(20) });
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/info", (HttpContext ctx) => Results.Json(new {
    app = "Phone Mouse",
    version = "1.0.0",
    pairingRequired = true,
    server = ctx.Request.Host.Value
}));

app.Map("/ws", async (HttpContext context) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }

    var suppliedCode = context.Request.Query["code"].ToString();
    if (!FixedTimeEquals(suppliedCode, pairingCode))
    {
        context.Response.StatusCode = 401;
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    var buffer = new byte[8192];

    while (socket.State == WebSocketState.Open && !context.RequestAborted.IsCancellationRequested)
    {
        using var ms = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), context.RequestAborted);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
                return;
            }
            ms.Write(buffer, 0, result.Count);
            if (ms.Length > 64 * 1024) throw new InvalidOperationException("Message too large");
        } while (!result.EndOfMessage);

        if (result.MessageType != WebSocketMessageType.Text) continue;

        try
        {
            using var doc = JsonDocument.Parse(ms.ToArray());
            var msg = doc.RootElement;
            var type = msg.GetProperty("type").GetString();

            switch (type)
            {
                case "move":
                    NativeInput.Move(
                        Clamp(msg.GetProperty("dx").GetDouble(), -300, 300),
                        Clamp(msg.GetProperty("dy").GetDouble(), -300, 300));
                    break;
                case "click":
                    NativeInput.Click(msg.TryGetProperty("button", out var b) ? b.GetString() : "left");
                    break;
                case "scroll":
                    NativeInput.Scroll((int)Clamp(msg.GetProperty("delta").GetDouble(), -1200, 1200));
                    break;
                case "text":
                    var text = msg.GetProperty("value").GetString() ?? "";
                    if (text.Length <= 2000) NativeInput.TypeText(text);
                    break;
                case "key":
                    NativeInput.PressKey(msg.GetProperty("value").GetString() ?? "");
                    break;
                case "hotkey":
                    if (msg.TryGetProperty("keys", out var keys) && keys.ValueKind == JsonValueKind.Array)
                        NativeInput.PressHotkey(keys.EnumerateArray().Select(x => x.GetString() ?? "").Take(4).ToArray());
                    break;
            }
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or ArgumentException)
        {
            // Ignore malformed or unsupported input; keep the connection alive.
        }
    }
});

Console.WriteLine();
Console.WriteLine("====================================");
Console.WriteLine("        PHONE MOUSE SERVER");
Console.WriteLine("====================================");
Console.WriteLine($"КОД СОПРЯЖЕНИЯ: {pairingCode}");
foreach (var ip in NetworkInterface.GetAllNetworkInterfaces()
    .Where(n => n.OperationalStatus == OperationalStatus.Up)
    .SelectMany(n => n.GetIPProperties().UnicastAddresses)
    .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a.Address)))
    Console.WriteLine($"Адрес для iPhone: http://{ip.Address}:8765");
Console.WriteLine("Важно: соединение работает только в локальной сети, HTTP-трафик не зашифрован.");
Console.WriteLine("Не открывайте порт 8765 в интернет и не используйте недоверенную Wi-Fi сеть.");
Console.WriteLine("Для остановки нажмите Ctrl+C.");
Console.WriteLine();

app.Run();

static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));

static bool FixedTimeEquals(string a, string b)
{
    var aa = Encoding.UTF8.GetBytes(a);
    var bb = Encoding.UTF8.GetBytes(b);
    return aa.Length == bb.Length && CryptographicOperations.FixedTimeEquals(aa, bb);
}

static bool IsPrivateIPv4(IPAddress ip)
{
    if (ip.AddressFamily != AddressFamily.InterNetwork) return false;
    var b = ip.GetAddressBytes();
    return b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168) || (b[0] == 169 && b[1] == 254);
}