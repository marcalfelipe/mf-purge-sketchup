using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace MFPurgeSketchUp;

internal static class PurgeService
{
    public static string SugerirSaida(string entrada)
    {
        var pasta = Path.GetDirectoryName(entrada)!;
        var nome = Path.GetFileNameWithoutExtension(entrada);
        var candidato = Path.Combine(pasta, $"{nome}_PURGADO.skp");
        var numero = 2;

        while (File.Exists(candidato))
            candidato = Path.Combine(pasta, $"{nome}_PURGADO_{numero++}.skp");

        return candidato;
    }

    public static async Task<ResultadoPurge> ExecutarAsync(
        string entrada,
        AppLanguage language,
        IProgress<ProgressoPurge>? progresso = null)
    {
        var saida = SugerirSaida(entrada);
        if (string.Equals(Path.GetFullPath(entrada), Path.GetFullPath(saida), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(Localizer.T(language, "service.same_output"));
        var pastaTrabalho = Path.Combine(Path.GetTempPath(), "MF_Purge", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(pastaTrabalho);
        var resultadoPath = Path.Combine(pastaTrabalho, "resultado.json");
        var executavel = EmbeddedRuntime.GetWorkerExecutable();
        if (!File.Exists(executavel))
            throw new InvalidOperationException(Localizer.T(language, "service.runtime_missing"));

        var inicio = new ProcessStartInfo
        {
            FileName = executavel,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        inicio.ArgumentList.Add(Path.GetFullPath(entrada));
        inicio.ArgumentList.Add(saida);
        inicio.ArgumentList.Add(resultadoPath);

        using var processo = Process.Start(inicio)
            ?? throw new InvalidOperationException(Localizer.T(language, "service.start_failed"));

        progresso?.Report(new ProgressoPurge(0, Localizer.T(language, "service.started"), Localizer.T(language, "service.active"), false, true));
        var saidaTask = LerSaidaAsync(processo.StandardOutput, language, progresso);
        var errosTask = LerErrosAsync(processo.StandardError, language, progresso);
        var monitorTask = MonitorarProcessoAsync(processo, language, progresso);
        await processo.WaitForExitAsync();
        await Task.WhenAll(saidaTask, errosTask, monitorTask);

        if (!File.Exists(resultadoPath))
            throw new InvalidOperationException(Localizer.T(language, "service.unexpected", processo.ExitCode));

        var resposta = JsonSerializer.Deserialize<ResultadoWorker>(await File.ReadAllTextAsync(resultadoPath))
            ?? throw new InvalidOperationException(Localizer.T(language, "service.invalid_response"));

        if (!string.Equals(resposta.status, "ok", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                resposta.erro is null
                    ? Localizer.T(language, "service.could_not_clean")
                    : Localizer.WorkerMessage(language, resposta.erro));

        if (!File.Exists(saida) || new FileInfo(saida).Length == 0)
            throw new InvalidOperationException(Localizer.T(language, "service.invalid_output"));

        return new ResultadoPurge(
            saida,
            resposta.componentes_removidos,
            resposta.materiais_removidos,
            resposta.texturas_removidas,
            resposta.definicoes_preservadas ?? [],
            resposta.fallback);
    }

    private static async Task LerSaidaAsync(
        StreamReader leitor,
        AppLanguage language,
        IProgress<ProgressoPurge>? progresso)
    {
        while (await leitor.ReadLineAsync() is { } linha)
        {
            if (string.IsNullOrWhiteSpace(linha))
                continue;

            try
            {
                var evento = JsonSerializer.Deserialize<EventoWorker>(linha);
                if (evento?.type == "progress" && evento.message is not null)
                {
                    progresso?.Report(new ProgressoPurge(
                        evento.percent,
                        Localizer.WorkerMessage(language, evento.message),
                        Localizer.T(language, "service.active"),
                        false,
                        true));
                    continue;
                }
            }
            catch (JsonException)
            {
                // Linhas não estruturadas também são úteis no log de diagnóstico.
            }

            progresso?.Report(new ProgressoPurge(null, linha, Localizer.T(language, "service.active"), false, true));
        }
    }

    private static async Task LerErrosAsync(
        StreamReader leitor,
        AppLanguage language,
        IProgress<ProgressoPurge>? progresso)
    {
        while (await leitor.ReadLineAsync() is { } linha)
        {
            if (!string.IsNullOrWhiteSpace(linha))
                progresso?.Report(new ProgressoPurge(null, $"Worker: {linha}", Localizer.T(language, "service.active"), false, true));
        }
    }

    private static async Task MonitorarProcessoAsync(
        Process processo,
        AppLanguage language,
        IProgress<ProgressoPurge>? progresso)
    {
        var cpuAnterior = TimeSpan.Zero;
        var ultimaAtividade = DateTime.UtcNow;
        var ultimoRegistro = DateTime.MinValue;

        while (!processo.HasExited)
        {
            await Task.Delay(1000);
            if (processo.HasExited)
                break;

            try
            {
                processo.Refresh();
                var cpuAtual = processo.TotalProcessorTime;
                if (cpuAtual - cpuAnterior >= TimeSpan.FromMilliseconds(20))
                    ultimaAtividade = DateTime.UtcNow;
                cpuAnterior = cpuAtual;

                var inativo = DateTime.UtcNow - ultimaAtividade;
                var ramGb = processo.WorkingSet64 / 1024d / 1024d / 1024d;
                var atividade = inativo >= TimeSpan.FromSeconds(30)
                    ? Localizer.T(language, "service.no_cpu", (int)inativo.TotalSeconds, ramGb)
                    : Localizer.T(language, "service.stats", cpuAtual.TotalSeconds, ramGb);

                var registrar = DateTime.UtcNow - ultimoRegistro >= TimeSpan.FromSeconds(15);
                progresso?.Report(new ProgressoPurge(
                    null,
                    atividade,
                    atividade,
                    true,
                    registrar));
                if (registrar)
                    ultimoRegistro = DateTime.UtcNow;
            }
            catch (InvalidOperationException)
            {
                break;
            }
        }
    }

    internal sealed class ResultadoWorker
    {
        public string? status { get; set; }
        public string? erro { get; set; }
        public int componentes_removidos { get; set; }
        public int materiais_removidos { get; set; }
        public int texturas_removidas { get; set; }
        public List<string>? definicoes_preservadas { get; set; }
        public string? fallback { get; set; }
    }

    internal sealed class EventoWorker
    {
        public string? type { get; set; }
        public int? percent { get; set; }
        public string? message { get; set; }
    }
}

internal sealed record ProgressoPurge(
    int? Percentual,
    string Mensagem,
    string Atividade,
    bool ApenasAtividade,
    bool RegistrarNoLog);

internal sealed record ResultadoPurge(
    string Saida,
    int ComponentesRemovidos,
    int MateriaisRemovidos,
    int TexturasRemovidas,
    IReadOnlyList<string> DefinicoesPreservadas,
    string? Fallback);
