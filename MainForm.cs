namespace MFPurgeSketchUp;

internal sealed class MainForm : Form
{
    private readonly Label _titulo = new();
    private readonly Label _descricao = new();
    private readonly Label _tituloLog = new();
    private readonly ComboBox _idioma = new();
    private readonly TextBox _arquivo = new();
    private readonly Button _selecionar = new();
    private readonly Button _purgar = new();
    private readonly Label _status = new();
    private readonly ProgressBar _progresso = new();
    private readonly Label _percentual = new();
    private readonly Label _atividade = new();
    private readonly TextBox _log = new();
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
        ClientSize = new Size(760, 620);
        MinimumSize = new Size(680, 550);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.FromArgb(245, 247, 248);
        Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? SystemIcons.Application;
        AllowDrop = true;

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 102,
            BackColor = Color.FromArgb(237, 246, 249)
        };

        var logo = new PictureBox
        {
            Image = Icon.ToBitmap(),
            SizeMode = PictureBoxSizeMode.Zoom,
            Location = new Point(26, 18),
            Size = new Size(64, 64),
            TabStop = false
        };

        _titulo.Font = new Font("Segoe UI Semibold", 18F);
        _titulo.AutoSize = true;
        _titulo.ForeColor = Color.FromArgb(152, 24, 31);
        _titulo.Location = new Point(104, 18);

        _descricao.AutoSize = true;
        _descricao.ForeColor = Color.FromArgb(73, 79, 83);
        _descricao.Location = new Point(107, 54);

        _idioma.DropDownStyle = ComboBoxStyle.DropDownList;
        _idioma.Items.AddRange(["English", "Português", "Español", "Deutsch", "Français", "Русский", "Tiếng Việt", "中文"]);
        _idioma.Location = new Point(610, 25);
        _idioma.Size = new Size(122, 30);
        _idioma.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _idioma.SelectedIndexChanged += (_, _) => AplicarIdioma();
        _idioma.SelectedIndex = 0;

        header.Controls.AddRange([logo, _titulo, _descricao, _idioma]);

        _arquivo.Location = new Point(34, 120);
        _arquivo.Size = new Size(565, 30);
        _arquivo.ReadOnly = true;
        _arquivo.BackColor = Color.White;
        _arquivo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        _selecionar.Location = new Point(610, 118);
        _selecionar.Size = new Size(115, 34);
        _selecionar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _selecionar.FlatStyle = FlatStyle.Flat;
        _selecionar.FlatAppearance.BorderColor = Color.FromArgb(210, 178, 181);
        _selecionar.BackColor = Color.White;
        _selecionar.ForeColor = Color.FromArgb(125, 25, 32);
        _selecionar.Cursor = Cursors.Hand;
        _selecionar.Click += (_, _) => SelecionarArquivo();

        _purgar.Location = new Point(34, 172);
        _purgar.Size = new Size(160, 42);
        _purgar.FlatStyle = FlatStyle.Flat;
        _purgar.FlatAppearance.BorderSize = 0;
        _purgar.BackColor = Color.FromArgb(205, 43, 51);
        _purgar.ForeColor = Color.White;
        _purgar.Font = new Font("Segoe UI Semibold", 10F);
        _purgar.Cursor = Cursors.Hand;
        _purgar.Enabled = false;
        _purgar.Click += async (_, _) => await ExecutarPurgeAsync();

        _progresso.Location = new Point(211, 181);
        _progresso.Size = new Size(464, 23);
        _progresso.Style = ProgressBarStyle.Continuous;
        _progresso.Visible = false;
        _progresso.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        _percentual.Location = new Point(685, 181);
        _percentual.Size = new Size(42, 23);
        _percentual.TextAlign = ContentAlignment.MiddleRight;
        _percentual.Visible = false;
        _percentual.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        _status.Location = new Point(34, 230);
        _status.Size = new Size(693, 42);
        _status.ForeColor = Color.FromArgb(75, 75, 75);
        _status.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        _atividade.Location = new Point(34, 271);
        _atividade.Size = new Size(693, 24);
        _atividade.ForeColor = Color.FromArgb(75, 75, 75);
        _atividade.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        _tituloLog.AutoSize = true;
        _tituloLog.Font = new Font("Segoe UI Semibold", 10F);
        _tituloLog.ForeColor = Color.FromArgb(91, 30, 35);
        _tituloLog.Location = new Point(34, 307);

        _log.Location = new Point(34, 334);
        _log.Size = new Size(693, 250);
        _log.Multiline = true;
        _log.ReadOnly = true;
        _log.ScrollBars = ScrollBars.Vertical;
        _log.BackColor = Color.White;
        _log.Font = new Font("Consolas", 9F);
        _log.BorderStyle = BorderStyle.FixedSingle;
        _log.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        _relogio.Tick += (_, _) =>
        {
            if (_inicioProcessamento != default)
                _atividade.Text = Localizer.T(Language, "elapsed", FormatarTempo(DateTime.Now - _inicioProcessamento), _atividade.Tag ?? "");
        };

        Controls.AddRange([header, _arquivo, _selecionar, _purgar, _progresso,
            _percentual, _status, _atividade, _tituloLog, _log]);

        DragEnter += AoArrastar;
        DragDrop += AoSoltar;

        AplicarIdioma();

        if (!string.IsNullOrWhiteSpace(arquivoInicial))
            DefinirArquivo(arquivoInicial);
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
            _status.Text = Localizer.T(
                Language,
                "done_status",
                resultado.ComponentesRemovidos,
                resultado.MateriaisRemovidos);

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
            _progresso.Value = Math.Clamp(percentual, 0, 100);
            _percentual.Text = $"{percentual}%";
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
        _descricao.Text = Localizer.T(Language, "description");
        _selecionar.Text = Localizer.T(Language, "select");
        _purgar.Text = Localizer.T(Language, "purge");
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
        _progresso.Visible = processando;
        _percentual.Visible = processando;
        if (processando)
        {
            _progresso.Value = 0;
            _percentual.Text = "0%";
        }
    }
}
