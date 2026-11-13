using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using QRCoder;

namespace PhoneMouse.Desktop;

public sealed class MainForm : Form
{
    private readonly Button startButton = new();
    private readonly Button stopButton = new();
    private readonly Button copyButton = new();
    private readonly Button openButton = new();
    private readonly ComboBox addressBox = new();
    private readonly Label statusLabel = new();
    private readonly Label codeLabel = new();
    private readonly Label connectionLabel = new();
    private readonly Label addressHint = new();
    private readonly PictureBox qrPicture = new();
    private readonly TextBox logBox = new();
    private Process? serverProcess;
    private string pairingCode = "";

    public MainForm()
    {
        Text = "Phone Mouse";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(760, 620);
        Size = new Size(900, 720);
        BackColor = Color.FromArgb(18, 19, 25);
        ForeColor = Color.FromArgb(240, 241, 247);
        Font = new Font("Segoe UI", 10F);
        BuildInterface();
        FormClosing += (_, _) => StopServer();
        Load += (_, _) => RefreshAddresses();
    }

    private void BuildInterface()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 82, Padding = new Padding(24, 16, 24, 12), BackColor = Color.FromArgb(25, 26, 35) };
        var title = new Label { Text = "Phone Mouse", Font = new Font("Segoe UI Semibold", 21F), AutoSize = true, Location = new Point(22, 10), ForeColor = Color.White };
        var subtitle = new Label { Text = "Управление компьютером с iPhone по Wi-Fi", AutoSize = true, Location = new Point(25, 47), ForeColor = Color.FromArgb(160, 164, 180) };
        statusLabel.Text = "● Сервер остановлен";
        statusLabel.AutoSize = true;
        statusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        statusLabel.Location = new Point(650, 28);
        statusLabel.ForeColor = Color.FromArgb(240, 160, 160);
        header.Controls.AddRange(new Control[] { title, subtitle, statusLabel });
        Controls.Add(header);

        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(22), AutoScroll = true };
        Controls.Add(body);
        header.BringToFront();

        var serverGroup = MakeGroup("УПРАВЛЕНИЕ СЕРВЕРОМ", 22, 18, 810, 112);
        startButton.Text = "▶  Запустить сервер";
        StyleButton(startButton, Color.FromArgb(116, 83, 218));
        startButton.SetBounds(18, 34, 190, 42);
        startButton.Click += (_, _) => StartServer();
        stopButton.Text = "■  Остановить";
        StyleButton(stopButton, Color.FromArgb(62, 65, 78));
        stopButton.SetBounds(220, 34, 150, 42);
        stopButton.Enabled = false;
        stopButton.Click += (_, _) => StopServer();
        var port = new Label { Text = "Порт: 8765  •  Только локальная сеть", AutoSize = true, Location = new Point(395, 47), ForeColor = Color.FromArgb(166, 169, 183) };
        serverGroup.Controls.AddRange(new Control[] { startButton, stopButton, port });
        body.Controls.Add(serverGroup);

        var connectGroup = MakeGroup("ПОДКЛЮЧЕНИЕ IPHONE", 22, 145, 810, 300);
        qrPicture.SetBounds(18, 38, 220, 220);
        qrPicture.BackColor = Color.White;
        qrPicture.SizeMode = PictureBoxSizeMode.Zoom;
        qrPicture.BorderStyle = BorderStyle.FixedSingle;
        connectGroup.Controls.Add(qrPicture);

        var infoTitle = new Label { Text = "Отсканируйте QR-код камерой iPhone", AutoSize = true, Font = new Font("Segoe UI Semibold", 12F), Location = new Point(260, 43) };
        var instruction = new Label { Text = "Откройте появившуюся ссылку в Safari.\nТелефон и компьютер должны быть в одной Wi-Fi сети.", AutoSize = true, Location = new Point(260, 75), ForeColor = Color.FromArgb(169, 172, 186) };
        var addressLabel = new Label { Text = "Адрес сервера", AutoSize = true, Location = new Point(260, 135), ForeColor = Color.FromArgb(169, 172, 186) };
        addressBox.DropDownStyle = ComboBoxStyle.DropDownList;
        addressBox.SetBounds(260, 158, 360, 34);
        addressBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        addressBox.SelectedIndexChanged += (_, _) => UpdateQr();
        addressHint.Text = "Адрес появится после запуска сервера";
        addressHint.AutoSize = true;
        addressHint.Location = new Point(260, 198);
        addressHint.ForeColor = Color.FromArgb(130, 134, 150);
        codeLabel.Text = "Код сопряжения: —";
        codeLabel.AutoSize = true;
        codeLabel.Font = new Font("Segoe UI Semibold", 13F);
        codeLabel.Location = new Point(260, 226);
        copyButton.Text = "Копировать ссылку";
        StyleButton(copyButton, Color.FromArgb(57, 61, 76));
        copyButton.SetBounds(260, 257, 170, 34);
        copyButton.Click += (_, _) => CopyConnectionUrl();
        openButton.Text = "Открыть в браузере";
        StyleButton(openButton, Color.FromArgb(57, 61, 76));
        openButton.SetBounds(440, 257, 170, 34);
        openButton.Click += (_, _) => OpenConnectionUrl();
        copyButton.Enabled = openButton.Enabled = false;
        connectGroup.Controls.AddRange(new Control[] { infoTitle, instruction, addressLabel, addressBox, addressHint, codeLabel, copyButton, openButton });
        body.Controls.Add(connectGroup);

        var connectionGroup = MakeGroup("СОСТОЯНИЕ ПОДКЛЮЧЕНИЯ", 22, 460, 390, 78);
        connectionLabel.Text = "● iPhone не подключён";
        connectionLabel.AutoSize = true;
        connectionLabel.Location = new Point(18, 37);
        connectionLabel.ForeColor = Color.FromArgb(240, 180, 140);
        connectionGroup.Controls.Add(connectionLabel);
        body.Controls.Add(connectionGroup);

        var logGroup = MakeGroup("ЖУРНАЛ СЕРВЕРА", 430, 460, 402, 180);
        logBox.Multiline = true;
        logBox.ReadOnly = true;
        logBox.ScrollBars = ScrollBars.Vertical;
        logBox.BackColor = Color.FromArgb(13, 14, 19);
        logBox.ForeColor = Color.FromArgb(195, 199, 212);
        logBox.BorderStyle = BorderStyle.FixedSingle;
        logBox.Font = new Font("Consolas", 8.5F);
        logBox.SetBounds(12, 32, 376, 132);
        logBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        logGroup.Controls.Add(logBox);
        body.Controls.Add(logGroup);

        var footer = new Label { Text = "Безопасность: сервер доступен в локальной сети по HTTP. Не публикуйте порт 8765 в интернет.", AutoSize = true, ForeColor = Color.FromArgb(190, 150, 105), Location = new Point(25, 655) };
        footer.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        body.Controls.Add(footer);
    }

    private static Panel MakeGroup(string caption, int x, int y, int width, int height)
    {
        var group = new Panel { Location = new Point(x, y), Size = new Size(width, height), BackColor = Color.FromArgb(25, 27, 36), BorderStyle = BorderStyle.FixedSingle, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        group.Controls.Add(new Label { Text = caption, AutoSize = true, Location = new Point(14, 9), Font = new Font("Segoe UI Semibold", 8.5F), ForeColor = Color.FromArgb(173, 151, 239) });
        return group;
    }

    private static void StyleButton(Button button, Color color)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = color;
        button.ForeColor = Color.White;
        button.Font = new Font("Segoe UI Semibold", 9.5F);
        button.Cursor = Cursors.Hand;
    }

    private void RefreshAddresses()
    {
        addressBox.Items.Clear();
        foreach (var ip in NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork && IsPrivateIPv4(a.Address))
            .Select(a => a.Address.ToString()).Distinct())
            addressBox.Items.Add(ip);
        if (addressBox.Items.Count > 0) addressBox.SelectedIndex = 0;
    }

    private void StartServer()
    {
        if (serverProcess is { HasExited: false }) return;
        var serverPath = Path.Combine(AppContext.BaseDirectory, "Server", "PhoneMouse.Server.exe");
        if (!File.Exists(serverPath))
        {
            MessageBox.Show($"Не найден файл сервера:\n{serverPath}\n\nРаспакуйте ZIP целиком.", "Phone Mouse", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        pairingCode = "";
        RefreshAddresses();
        qrPicture.Image?.Dispose();
        qrPicture.Image = null;
        addressHint.Text = "Запуск сервера…";
        statusLabel.Text = "● Запуск сервера…";
        statusLabel.ForeColor = Color.FromArgb(240, 205, 125);
        startButton.Enabled = false;
        stopButton.Enabled = true;
        Log("Запуск сервера на порту 8765…");

        try
        {
            var startInfo = new ProcessStartInfo(serverPath)
            {
                WorkingDirectory = Path.GetDirectoryName(serverPath)!,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            serverProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            serverProcess.OutputDataReceived += (_, e) => { if (e.Data is not null) HandleServerLine(e.Data); };
            serverProcess.ErrorDataReceived += (_, e) => { if (e.Data is not null) Ui(() => Log("ОШИБКА: " + e.Data)); };
            serverProcess.Exited += (_, _) => Ui(ServerExited);
            if (!serverProcess.Start()) throw new InvalidOperationException("Не удалось запустить процесс сервера.");
            serverProcess.BeginOutputReadLine();
            serverProcess.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            Log("Не удалось запустить сервер: " + ex.Message);
            ServerExited();
        }
    }

    private void HandleServerLine(string line)
    {
        Ui(() =>
        {
            Log(line);
            if (line.StartsWith("КОД СОПРЯЖЕНИЯ:", StringComparison.Ordinal))
            {
                pairingCode = line["КОД СОПРЯЖЕНИЯ:".Length..].Trim();
                codeLabel.Text = "Код сопряжения: " + pairingCode;
                statusLabel.Text = "● Сервер работает";
                statusLabel.ForeColor = Color.FromArgb(90, 220, 160);
                UpdateQr();
            }
            else if (line.StartsWith("Адрес для iPhone:", StringComparison.Ordinal))
            {
                var url = line["Адрес для iPhone:".Length..].Trim();
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    var ip = uri.Host;
                    if (!addressBox.Items.Contains(ip)) addressBox.Items.Add(ip);
                    addressBox.SelectedItem = ip;
                    addressHint.Text = "Ссылка для подключения: " + url;
                    UpdateQr();
                }
            }
            else if (line == "CLIENT_CONNECTED")
            {
                connectionLabel.Text = "● iPhone подключён";
                connectionLabel.ForeColor = Color.FromArgb(90, 220, 160);
                statusLabel.Text = "● Сервер работает";
                statusLabel.ForeColor = Color.FromArgb(90, 220, 160);
            }
            else if (line == "CLIENT_DISCONNECTED")
            {
                connectionLabel.Text = "● iPhone отключён";
                connectionLabel.ForeColor = Color.FromArgb(240, 180, 140);
            }
        });
    }

    private void UpdateQr()
    {
        if (pairingCode.Length == 0 || addressBox.SelectedItem is not string ip)
        {
            copyButton.Enabled = openButton.Enabled = false;
            return;
        }
        var url = BuildConnectionUrl(ip);
        try
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            using var qr = new QRCode(data);
            var bitmap = qr.GetGraphic(8, Color.FromArgb(20, 20, 25), Color.White, true);
            var old = qrPicture.Image;
            qrPicture.Image = bitmap;
            old?.Dispose();
            copyButton.Enabled = openButton.Enabled = true;
        }
        catch (Exception ex) { Log("Ошибка QR-кода: " + ex.Message); }
    }

    private string BuildConnectionUrl(string ip) => $"http://{ip}:8765/?code={Uri.EscapeDataString(pairingCode)}";

    private void CopyConnectionUrl()
    {
        if (addressBox.SelectedItem is string ip && pairingCode.Length > 0)
        {
            Clipboard.SetText(BuildConnectionUrl(ip));
            Log("Ссылка подключения скопирована.");
        }
    }

    private void OpenConnectionUrl()
    {
        if (addressBox.SelectedItem is string ip && pairingCode.Length > 0)
            Process.Start(new ProcessStartInfo(BuildConnectionUrl(ip)) { UseShellExecute = true });
    }

    private void StopServer()
    {
        var process = serverProcess;
        serverProcess = null;
        if (process is not null)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(2500);
                }
            }
            catch (Exception ex) { Log("Ошибка остановки: " + ex.Message); }
            process.Dispose();
        }
        ServerExited();
    }

    private void ServerExited()
    {
        startButton.Enabled = true;
        stopButton.Enabled = false;
        statusLabel.Text = "● Сервер остановлен";
        statusLabel.ForeColor = Color.FromArgb(240, 160, 160);
        connectionLabel.Text = "● iPhone не подключён";
        connectionLabel.ForeColor = Color.FromArgb(240, 180, 140);
        addressHint.Text = "Сервер остановлен";
        copyButton.Enabled = openButton.Enabled = false;
        qrPicture.Image?.Dispose();
        qrPicture.Image = null;
        Log("Сервер остановлен.");
    }

    private void Log(string message)
    {
        if (IsDisposed) return;
        logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    private void Ui(Action action)
    {
        if (IsDisposed || !IsHandleCreated) return;
        try { BeginInvoke(action); } catch (InvalidOperationException) { }
    }

    private static bool IsPrivateIPv4(IPAddress ip)
    {
        var b = ip.GetAddressBytes();
        return b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168) || (b[0] == 169 && b[1] == 254);
    }
}