namespace MFPurgeSketchUp;

internal sealed class MainForm : Form
{
    private static readonly Color AccentRed = Color.FromArgb(231, 45, 55);
    private static readonly Color HeaderBlue = Color.FromArgb(226, 244, 251);
    private static readonly Color PageBackground = Color.FromArgb(238, 246, 251);
    private static readonly Color CardBorder = Color.FromArgb(211, 222, 230);
    private static readonly Color DarkText = Color.FromArgb(24, 38, 56);
    private static readonly Color MutedText = Color.FromArgb(70, 82, 94);

    private readonly RoundedPanel _headerCard = new() { BackColor = HeaderBlue, BorderColor = Color.FromArgb(209, 228, 237), CornerRadius = 14 };
    private readonly RoundedPanel _actionCard = new() { BackColor = Color.White, BorderColor = CardBorder, CornerRadius = 12 };
    private readonly RoundedPanel _logCard = new() { BackColor = Color.White, BorderColor = CardBorder, CornerRadius = 12 };
    private readonly Panel _logHeader = new() { BackColor = Color.FromArgb(235, 241, 246) };
    private readonly PictureBox _logo = new();
    private readonly Label _titulo = new();
    private readonly Label _tituloLog = new();
    private readonly ComboBox _idioma = new();
    private readonly TextBox _arquivo = new();
    private readonly RoundedButton _selecionar = new();
    private readonly RoundedButton _purgar = new();
    private readonly Label _status = new();
    private readonly SmoothProgressBar _progresso = new();
    private readonly Label _percentual = new();
    private readonly Label _atividade = new();
    private readonly TextBox _log = new();
    private readonly Label _cpuLabel = new() { Text = "CPU", AutoSize = true };
    private readonly Label _cpuValor = new() { Text = "0%", AutoSize = true };
    private readonly Label _ramLabel = new() { Text = "RAM", AutoSize = true };
    private readonly Label _ramValor = new() { Text = "0%", AutoSize = true };
    private readonly SmoothProgressBar _cpuBar = new() { FillColor = Color.FromArgb(39, 139, 235), CornerRadius = 6 };
    private readonly SmoothProgressBar _ramBar = new() { FillColor = Color.FromArgb(39, 139, 235), CornerRadius = 6 };
    private readonly System.Windows.Forms.Timer _relogio = new() { Interval = 1000 };
    private DateTime _inicioProcessamento;
    private bool _processando;

    private AppLanguage Language => _idioma.SelectedIndex switch
    {
        1 => AppLanguage.Portuguese,
        2 => AppLanguage.Spanish,
        3 => AppLanguage.German,
        4 => AppLanguage.French,
        5 => AppLanguage.Russian,
        6 => AppLanguage.Vietnamese,
        7 => AppLanguage.Chinese,
        _ => AppLanguage.English
    };

    public MainForm(string? arquivoInicial)
    {
        Text = "MF Purge SketchUp";
        ClientSize = new Size(1040, 680);
        MinimumSize = new Size(900, 620);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10F);
        BackColor = PageBackground;
        Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? SystemIcons.Application;
        AllowDrop = true;

        ConfigurarCabecalho();
        ConfigurarAcao();
        ConfigurarLog();
        ConfigurarRodape();

        Controls.AddRange([_headerCard, _actionCard, _logCard, _cpuLabel, _cpuBar,
            _cpuValor, _ramLabel, _ramBar, _ramValor, _atividade]);

        Resize += (_, _) => OrganizarInterface();
        DragEnter += AoArrastar;
        DragDrop += AoSoltar;
        _relogio.Tick += (_, _) =>
        {
            if (_inicioProcessamento != default)
                _atividade.Text = Localizer.T(Language, "elapsed", FormatarTempo(DateTime.Now - _inicioProcessamento), _atividade.Tag ?? "");
        };

        _idioma.SelectedIndex = 0;
        AplicarIdioma();
        OrganizarInterface();

        if (!string.IsNullOrWhiteSpace(arquivoInicial))
            DefinirArquivo(arquivoInicial);
    }

    private void ConfigurarCabecalho()
    {
        using var logoStream = typeof(MainForm).Assembly.GetManifestResourceStream("MFPurgeSketchUp.mf_purge_icon.png");
        if (logoStream is not null)
        {
            using var logoOriginal = Image.FromStream(logoStream);
            _logo.Image = new Bitmap(logoOriginal);
        }
        else
        {
            _logo.Image = Icon?.ToBitmap();
        }
        _logo.SizeMode = PictureBoxSizeMode.Zoom;
        _logo.TabStop = false;

        _titulo.Font = new Font("Segoe UI Semibold", 26F);
        _titulo.AutoSize = true;
        _titulo.ForeColor = DarkText;

        _idioma.DropDownStyle = ComboBoxStyle.DropDownList;
        _idioma.Items.AddRange(["English", "Português", "Español", "Deutsch", "Français", "Русский", "Tiếng Việt", "中文"]);
        _idioma.Font = new Font("Segoe UI", 11F);
        _idioma.SelectedIndexChanged += (_, _) => AplicarIdioma();

        _headerCard.Controls.AddRange([_logo, _titulo, _idioma]);
    }

    private void ConfigurarAcao()
    {
        _arquivo.ReadOnly = true;
        _arquivo.BackColor = Color.White;
        _arquivo.BorderStyle = BorderStyle.FixedSingle;
        _arquivo.Font = new Font("Segoe UI", 11F);

        _selecionar.BackColor = Color.FromArgb(246, 249, 251);
        _selecionar.ForeColor = DarkText;
        _selecionar.Font = new Font("Segoe UI", 11F);
        _selecionar.Cursor = Cursors.Hand;
        _selecionar.Click += (_, _) => SelecionarArquivo();

        _purgar.BackColor = AccentRed;
        _purgar.ForeColor = Color.White;
        _purgar.Font = new Font("Segoe UI Semibold", 12F);
        _purgar.Cursor = Cursors.Hand;
        _purgar.Enabled = false;
        _purgar.Click += async (_, _) => await ExecutarPurgeAsync();

        _progresso.FillColor = Color.FromArgb(39, 177, 83);
        _progresso.Value = 0;

        _percentual.Font = new Font("Segoe UI Semibold", 11F);
        _percentual.ForeColor = DarkText;
        _percentual.Text = "0%";
        _percentual.TextAlign = ContentAlignment.MiddleRight;

        _status.ForeColor = DarkText;
        _status.Font = new Font("Segoe UI", 10.5F);
        _status.AutoEllipsis = true;

        _actionCard.Controls.AddRange([_arquivo, _selecionar, _purgar, _progresso, _percentual, _status]);
    }

    private void ConfigurarLog()
    {
        _tituloLog.Font = new Font("Segoe UI Semibold", 11F);
        _tituloLog.ForeColor = DarkText;
        _tituloLog.AutoSize = true;

        _log.Multiline = true;
        _log.ReadOnly = true;
        _log.ScrollBars = ScrollBars.Vertical;
        _log.BackColor = Color.White;
        _log.ForeColor = DarkText;
        _log.Font = new Font("Consolas", 9.5F);
        _log.BorderStyle = BorderStyle.None;

        _logHeader.Controls.Add(_tituloLog);
        _logCard.Controls.AddRange([_logHeader, _log]);
    }

    private void ConfigurarRodape()
    {
        foreach (var label in new[] { _cpuLabel, _cpuValor, _ramLabel, _ramValor })
        {
            label.Font = new Font("Segoe UI Semibold", 10F);
            label.ForeColor = DarkText;
        }

        _atividade.ForeColor = MutedText;
        _atividade.Font = new Font("Segoe UI", 9.5F);
        _atividade.TextAlign = ContentAlignment.MiddleRight;
        _atividade.AutoEllipsis = true;
    }

    private void OrganizarInterface()
    {
        const int margin = 22;
        var contentWidth = ClientSize.Width - margin * 2;
        var footerTop = ClientSize.Height - 51;
        const int logTop = 375;
        var logHeight = Math.Max(190, footerTop - logTop - 14);

        _headerCard.SetBounds(margin, 18, contentWidth, 140);
        _logo.SetBounds(34, 25, 88, 88);
        _titulo.Location = new Point(145, 47);
        _idioma.SetBounds(Math.Max(650, contentWidth - 230), 50, 190, 34);

        _actionCard.SetBounds(margin, 174, contentWidth, 185);
        var innerWidth = contentWidth - 48;
        var runWidth = Math.Min(240, Math.Max(205, innerWidth / 4));
        const int selectWidth = 140;
        const int gap = 14;
        var fileWidth = innerWidth - runWidth - selectWidth - gap * 2;
        _arquivo.SetBounds(24, 24, fileWidth, 38);
        _selecionar.SetBounds(24 + fileWidth + gap, 20, selectWidth, 46);
        _purgar.SetBounds(24 + fileWidth + gap + selectWidth + gap, 20, runWidth, 46);
        _progresso.SetBounds(24, 93, innerWidth - 70, 20);
        _percentual.SetBounds(contentWidth - 67, 85, 43, 34);
        _status.SetBounds(24, 132, innerWidth, 30);

        _logCard.SetBounds(margin, logTop, contentWidth, logHeight);
        _logHeader.SetBounds(1, 1, contentWidth - 2, 48);
        _tituloLog.Location = new Point(23, 13);
        _log.SetBounds(24, 61, contentWidth - 48, logHeight - 78);

        var metricsY = footerTop + 9;
        _cpuLabel.Location = new Point(margin + 28, metricsY + 3);
        _cpuBar.SetBounds(margin + 76, metricsY + 5, 150, 16);
        _cpuValor.Location = new Point(margin + 237, metricsY + 2);
        _ramLabel.Location = new Point(margin + 305, metricsY + 3);
        _ramBar.SetBounds(margin + 358, metricsY + 5, 150, 16);
        _ramValor.Location = new Point(margin + 519, metricsY + 2);
        _atividade.SetBounds(margin + 580, metricsY - 2, Math.Max(100, contentWidth - 580), 28);
    }

    private void SelecionarArquivo()
    {
        using var dialogo = new OpenFileDialog
        {
            Title = Localizer.T(Language, "dialog.title"),
            Filter = Localizer.T(Language, "dialog.filter"),
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialogo.ShowDialog(this) == DialogResult.OK)
            DefinirArquivo(dialogo.FileName);
    }

    private void AoArrastar(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] { Length: 1 } arquivos &&
            Path.GetExtension(arquivos[0]).Equals(".skp", StringComparison.OrdinalIgnoreCase))
        {
            e.Effect = DragDropEffects.Copy;
        }
        else
        {
            e.Effect = DragDropEffects.None;
        }
    }

    private void AoSoltar(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] { Length: 1 } arquivos)
            DefinirArquivo(arquivos[0]);
    }

    private void DefinirArquivo(string caminho)
    {
        caminho = Path.GetFullPath(caminho.Trim('"'));
        if (!File.Exists(caminho) || !Path.GetExtension(caminho).Equals(".skp", StringComparison.OrdinalIgnoreCase))
        {
            _arquivo.Clear();
            _purgar.Enabled = false;
            _status.Text = Localizer.T(Language, "invalid");
            return;
        }

        _arquivo.Text = caminho;
        _purgar.Enabled = true;
        _status.Text = Localizer.T(Language, "output", PurgeService.SugerirSaida(caminho));
        _atividade.Text = Localizer.T(Language, "ready");
    }

    private async Task ExecutarPurgeAsync()
    {
        var entrada = _arquivo.Text;
        if (!File.Exists(entrada))
        {
            DefinirArquivo(entrada);
            return;
        }

        AlterarEstado(true);
        _log.Clear();
        _inicioProcessamento = DateTime.Now;
        _atividade.Tag = Localizer.T(Language, "starting");
        _relogio.Start();
        RegistrarLog(Localizer.T(Language, "start_log"));

        var progresso = new Progress<ProgressoPurge>(AtualizarProgresso);

        try
        {
            var resultado = await PurgeService.ExecutarAsync(entrada, Language, progresso);
            _status.Text = Localizer.T(Language, "done_status", resultado.ComponentesRemovidos, resultado.MateriaisRemovidos);

            var preservadas = resultado.DefinicoesPreservadas.Count > 0
                ? Localizer.T(
                    Language,
                    "preserved",
                    resultado.DefinicoesPreservadas.Count,
                    string.Join(", ", resultado.DefinicoesPreservadas.Take(5)) +
                    (resultado.DefinicoesPreservadas.Count > 5 ? ", …" : ""))
                : "";

            MessageBox.Show(
                this,
                Localizer.T(
                    Language,
                    "success",
                    resultado.ComponentesRemovidos,
                    resultado.MateriaisRemovidos,
                    resultado.TexturasRemovidas,
                    preservadas,
                    resultado.Saida),
                Localizer.T(Language, "success_title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _status.Text = Localizer.T(Language, "failed");
            RegistrarLog(Localizer.T(Language, "failure_log", ex.Message));
            MessageBox.Show(this, ex.Message, Localizer.T(Language, "error_title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _relogio.Stop();
            AlterarEstado(false);
        }
    }

    private void AtualizarProgresso(ProgressoPurge progresso)
    {
        if (progresso.Percentual is int percentual)
        {
            _progresso.Value = percentual;
            _percentual.Text = $"{percentual}%";
        }

        if (progresso.CpuPercentual is double cpu)
        {
            var cpuInteira = (int)Math.Round(Math.Clamp(cpu, 0, 100));
            _cpuBar.Value = cpuInteira;
            _cpuValor.Text = $"{cpuInteira}%";
        }

        if (progresso.RamGb is double ramGb)
        {
            var totalDisponivel = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024d / 1024d / 1024d;
            var ramPercentual = totalDisponivel > 0 ? (int)Math.Round(ramGb / totalDisponivel * 100) : 0;
            ramPercentual = Math.Clamp(ramPercentual, 0, 100);
            _ramBar.Value = ramPercentual;
            _ramValor.Text = $"{ramPercentual}%";
        }

        _atividade.Tag = progresso.Atividade;
        if (!progresso.ApenasAtividade)
            _status.Text = progresso.Mensagem;

        if (progresso.RegistrarNoLog)
            RegistrarLog(progresso.Mensagem);
    }

    private void RegistrarLog(string mensagem)
    {
        _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {mensagem}{Environment.NewLine}");
        _log.SelectionStart = _log.TextLength;
        _log.ScrollToCaret();
    }

    private static string FormatarTempo(TimeSpan tempo) =>
        tempo.TotalHours >= 1 ? tempo.ToString(@"hh\:mm\:ss") : tempo.ToString(@"mm\:ss");

    private void AplicarIdioma()
    {
        _titulo.Text = Localizer.T(Language, "title");
        _selecionar.Text = Localizer.T(Language, "select");
        _purgar.Text = $"▶  {Localizer.T(Language, "purge")}";
        _tituloLog.Text = Localizer.T(Language, "log");

        if (File.Exists(_arquivo.Text))
        {
            _status.Text = Localizer.T(Language, "output", PurgeService.SugerirSaida(_arquivo.Text));
            if (!_processando)
                _atividade.Text = Localizer.T(Language, "ready");
        }
        else
        {
            _status.Text = Localizer.T(Language, "none");
            if (!_processando)
                _atividade.Text = Localizer.T(Language, "ready");
        }
    }

    private void AlterarEstado(bool processando)
    {
        _processando = processando;
        _arquivo.Enabled = !processando;
        _selecionar.Enabled = !processando;
        _idioma.Enabled = !processando;
        _purgar.Enabled = !processando && File.Exists(_arquivo.Text);
        AllowDrop = !processando;
        if (processando)
        {
            _progresso.Value = 0;
            _percentual.Text = "0%";
            _cpuBar.Value = 0;
            _cpuValor.Text = "0%";
            _ramBar.Value = 0;
            _ramValor.Text = "0%";
        }
    }
}
