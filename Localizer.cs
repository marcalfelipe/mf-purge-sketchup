using System.Text.RegularExpressions;

namespace MFPurgeSketchUp;

internal enum AppLanguage
{
    English,
    Portuguese,
    Spanish,
    German,
    French,
    Russian,
    Vietnamese,
    Chinese
}

internal static partial class Localizer
{
    public static string T(AppLanguage language, string key, params object[] args)
    {
        var fallback = English[key];
        var text = language switch
        {
            AppLanguage.Portuguese => Portuguese.GetValueOrDefault(key, fallback),
            AppLanguage.Spanish => Spanish.GetValueOrDefault(key, fallback),
            AppLanguage.German => German.GetValueOrDefault(key, fallback),
            AppLanguage.French => French.GetValueOrDefault(key, fallback),
            AppLanguage.Russian => Russian.GetValueOrDefault(key, fallback),
            AppLanguage.Vietnamese => Vietnamese.GetValueOrDefault(key, fallback),
            AppLanguage.Chinese => Chinese.GetValueOrDefault(key, fallback),
            _ => fallback
        };
        return args.Length == 0 ? text : string.Format(text, args);
    }

    public static string WorkerMessage(AppLanguage language, string message)
    {
        if (language == AppLanguage.Portuguese)
            return message;

        if (WorkerExact.TryGetValue(message, out var key))
            return T(language, key);

        var match = ApiPathRegex().Match(message);
        if (match.Success)
            return T(language, "worker.api", match.Groups[1].Value);
        match = ComponentTotalRegex().Match(message);
        if (match.Success)
            return T(language, "worker.component_total", match.Groups[1].Value);
        match = ComponentProgressRegex().Match(message);
        if (match.Success)
            return T(language, "worker.component_progress", match.Groups[1].Value, match.Groups[2].Value);
        match = RemovedComponentsRegex().Match(message);
        if (match.Success)
            return T(language, "worker.removed_components", match.Groups[1].Value);
        match = MaterialProgressRegex().Match(message);
        if (match.Success)
            return T(language, "worker.material_progress", match.Groups[1].Value, match.Groups[2].Value);
        match = RemovedMaterialsRegex().Match(message);
        if (match.Success)
            return T(language, "worker.removed_materials", match.Groups[1].Value);
        match = PreservedDefinitionRegex().Match(message);
        if (match.Success)
            return T(language, "worker.preserved", match.Groups[1].Value);
        if (message.StartsWith("Fallback:", StringComparison.OrdinalIgnoreCase))
            return T(language, "worker.fallback");
        if (message.StartsWith("Erro:", StringComparison.OrdinalIgnoreCase))
            return T(language, "worker.error", message[5..].Trim());

        return message;
    }

    private static readonly Dictionary<string, string> WorkerExact = new()
    {
        ["Preparando o processamento…"] = "worker.preparing",
        ["Carregando o modelo sem abrir o SketchUp…"] = "worker.loading",
        ["Modelo carregado com sucesso."] = "worker.loaded",
        ["Contando componentes, materiais e texturas…"] = "worker.inventory",
        ["Verificando referências de cenas e planos de seção…"] = "worker.scenes",
        ["Analisando materiais usados na geometria principal…"] = "worker.materials",
        ["Gravando o arquivo de saída…"] = "worker.saving",
        ["Arquivo gravado; reabrindo a saída para validar…"] = "worker.validating",
        ["Purge concluído com sucesso; a saída foi reaberta pela API."] = "worker.completed"
    };

    private static readonly Dictionary<string, string> Portuguese = new()
    {
        ["title"] = "Limpar modelo do SketchUp",
        ["description"] = "Remova componentes e materiais não utilizados sem abrir o SketchUp.\nO arquivo original nunca será alterado.",
        ["select"] = "Selecionar…",
        ["purge"] = "Aplicar purge",
        ["none"] = "Arraste um arquivo .skp aqui ou selecione-o acima.",
        ["log"] = "Log do processamento",
        ["dialog.title"] = "Selecione um modelo do SketchUp",
        ["dialog.filter"] = "Modelo do SketchUp (*.skp)|*.skp",
        ["invalid"] = "Selecione um arquivo .skp válido.",
        ["output"] = "Saída: {0}",
        ["ready"] = "Pronto para iniciar.",
        ["elapsed"] = "Tempo decorrido: {0}  •  {1}",
        ["starting"] = "Iniciando…",
        ["start_log"] = "Processamento iniciado. O arquivo original não será alterado.",
        ["done_status"] = "Concluído: {0} componentes/grupos e {1} materiais removidos.",
        ["success"] = "Modelo limpo com sucesso.\n\nComponentes/grupos removidos: {0}\nMateriais removidos: {1}\nTexturas removidas: {2}\n{3}\nArquivo criado:\n{4}",
        ["preserved"] = "Definições preservadas por segurança: {0} ({1}) — detalhes no log\n",
        ["success_title"] = "Purge concluído",
        ["failed"] = "Não foi possível concluir o purge.",
        ["failure_log"] = "FALHA: {0}",
        ["error_title"] = "Erro",
        ["service.same_output"] = "A saída coincide com o arquivo original; a operação foi cancelada.",
        ["service.runtime_missing"] = "O componente interno de limpeza não foi encontrado.",
        ["service.start_failed"] = "O Windows não conseguiu iniciar o processo de limpeza.",
        ["service.started"] = "Processo de limpeza iniciado.",
        ["service.active"] = "Processo ativo",
        ["service.unexpected"] = "O processo terminou inesperadamente (código {0}). O arquivo original não foi alterado.",
        ["service.invalid_response"] = "O processo de limpeza produziu uma resposta inválida.",
        ["service.could_not_clean"] = "Não foi possível limpar o modelo.",
        ["service.invalid_output"] = "A API informou sucesso, mas o arquivo de saída não foi criado corretamente.",
        ["service.no_cpu"] = "Sem CPU detectável há {0}s; pode estar em disco ou travado • RAM {1:F1} GB",
        ["service.stats"] = "Ativo • CPU {0:F0}s • RAM {1:F1} GB",
        ["worker.preparing"] = "Preparando o processamento…",
        ["worker.api"] = "API do SketchUp localizada: {0}",
        ["worker.loading"] = "Carregando o modelo sem abrir o SketchUp…",
        ["worker.loaded"] = "Modelo carregado com sucesso.",
        ["worker.inventory"] = "Contando componentes, materiais e texturas…",
        ["worker.scenes"] = "Verificando referências de cenas e planos de seção…",
        ["worker.component_total"] = "Analisando {0} definições de componentes e grupos…",
        ["worker.component_progress"] = "Componentes analisados: {0}/{1}",
        ["worker.removed_components"] = "Componentes/grupos não utilizados removidos: {0}",
        ["worker.materials"] = "Analisando materiais usados na geometria principal…",
        ["worker.material_progress"] = "Definições verificadas para materiais: {0}/{1}",
        ["worker.removed_materials"] = "Materiais não utilizados removidos: {0}",
        ["worker.saving"] = "Gravando o arquivo de saída…",
        ["worker.validating"] = "Arquivo gravado; reabrindo a saída para validar…",
        ["worker.completed"] = "Purge concluído com sucesso; a saída foi reaberta pela API.",
        ["worker.preserved"] = "Definição não utilizada “{0}” preservada por segurança porque ainda é referenciada por uma cena.",
        ["worker.fallback"] = "Modo de segurança ativo: procurando preservar somente os itens que impedem a gravação.",
        ["worker.error"] = "Erro: {0}"
    };

    private static readonly Dictionary<string, string> English = new()
    {
        ["title"] = "Clean SketchUp model",
        ["description"] = "Remove unused components and materials without opening SketchUp.\nThe original file is never modified.",
        ["select"] = "Choose file…",
        ["purge"] = "Run purge",
        ["none"] = "Drop a .skp file here or choose one above.",
        ["log"] = "Processing log",
        ["dialog.title"] = "Choose a SketchUp model",
        ["dialog.filter"] = "SketchUp model (*.skp)|*.skp",
        ["invalid"] = "Choose a valid .skp file.",
        ["output"] = "Output: {0}",
        ["ready"] = "Ready to start.",
        ["elapsed"] = "Elapsed: {0}  •  {1}",
        ["starting"] = "Starting…",
        ["start_log"] = "Processing started. The original file will not be modified.",
        ["done_status"] = "Completed: {0} components/groups and {1} materials removed.",
        ["success"] = "Model cleaned successfully.\n\nComponents/groups removed: {0}\nMaterials removed: {1}\nTextures removed: {2}\n{3}\nFile created:\n{4}",
        ["preserved"] = "Definitions preserved for safety: {0} ({1}) — see the log for details\n",
        ["success_title"] = "Purge completed",
        ["failed"] = "The purge could not be completed.",
        ["failure_log"] = "FAILED: {0}",
        ["error_title"] = "Error",
        ["service.same_output"] = "The output matches the original file; the operation was cancelled.",
        ["service.runtime_missing"] = "The internal cleanup component was not found.",
        ["service.start_failed"] = "Windows could not start the cleanup process.",
        ["service.started"] = "Cleanup process started.",
        ["service.active"] = "Process active",
        ["service.unexpected"] = "The process ended unexpectedly (code {0}). The original file was not modified.",
        ["service.invalid_response"] = "The cleanup process returned an invalid response.",
        ["service.could_not_clean"] = "The model could not be cleaned.",
        ["service.invalid_output"] = "The API reported success, but no valid output file was created.",
        ["service.no_cpu"] = "No CPU activity for {0}s; it may be using the disk or stalled • RAM {1:F1} GB",
        ["service.stats"] = "Active • CPU {0:F0}s • RAM {1:F1} GB",
        ["worker.preparing"] = "Preparing processing…",
        ["worker.api"] = "SketchUp API found: {0}",
        ["worker.loading"] = "Loading the model without opening SketchUp…",
        ["worker.loaded"] = "Model loaded successfully.",
        ["worker.inventory"] = "Counting components, materials, and textures…",
        ["worker.scenes"] = "Checking scene and section-plane references…",
        ["worker.component_total"] = "Analyzing {0} component and group definitions…",
        ["worker.component_progress"] = "Components analyzed: {0}/{1}",
        ["worker.removed_components"] = "Unused components/groups removed: {0}",
        ["worker.materials"] = "Analyzing materials used by model geometry…",
        ["worker.material_progress"] = "Definitions checked for materials: {0}/{1}",
        ["worker.removed_materials"] = "Unused materials removed: {0}",
        ["worker.saving"] = "Saving the output file…",
        ["worker.validating"] = "File saved; reopening it for validation…",
        ["worker.completed"] = "Purge completed successfully; the output was reopened by the API.",
        ["worker.preserved"] = "Unused definition “{0}” was preserved because a scene still references it.",
        ["worker.fallback"] = "Safety mode active: preserving only the items that prevent the file from being saved.",
        ["worker.error"] = "Error: {0}"
    };

    private static readonly Dictionary<string, string> Spanish = new()
    {
        ["title"] = "Limpiar modelo de SketchUp",
        ["description"] = "Elimine componentes y materiales no utilizados sin abrir SketchUp.\nEl archivo original nunca se modifica.",
        ["select"] = "Seleccionar…",
        ["purge"] = "Aplicar purge",
        ["none"] = "Arrastre un archivo .skp aquí o selecciónelo arriba.",
        ["log"] = "Registro del procesamiento",
        ["dialog.title"] = "Seleccione un modelo de SketchUp",
        ["dialog.filter"] = "Modelo de SketchUp (*.skp)|*.skp",
        ["invalid"] = "Seleccione un archivo .skp válido.",
        ["output"] = "Salida: {0}",
        ["ready"] = "Listo para iniciar.",
        ["elapsed"] = "Tiempo transcurrido: {0}  •  {1}",
        ["starting"] = "Iniciando…",
        ["start_log"] = "Procesamiento iniciado. El archivo original no será modificado.",
        ["done_status"] = "Completado: {0} componentes/grupos y {1} materiales eliminados.",
        ["success"] = "Modelo limpiado correctamente.\n\nComponentes/grupos eliminados: {0}\nMateriales eliminados: {1}\nTexturas eliminadas: {2}\n{3}\nArchivo creado:\n{4}",
        ["preserved"] = "Definiciones conservadas por seguridad: {0} ({1}) — consulte el registro\n",
        ["success_title"] = "Purge completado",
        ["failed"] = "No fue posible completar el purge.",
        ["failure_log"] = "FALLO: {0}",
        ["error_title"] = "Error",
        ["service.same_output"] = "La salida coincide con el archivo original; la operación fue cancelada.",
        ["service.runtime_missing"] = "No se encontró el componente interno de limpieza.",
        ["service.start_failed"] = "Windows no pudo iniciar el proceso de limpieza.",
        ["service.started"] = "Proceso de limpieza iniciado.",
        ["service.active"] = "Proceso activo",
        ["service.unexpected"] = "El proceso terminó inesperadamente (código {0}). El archivo original no fue modificado.",
        ["service.invalid_response"] = "El proceso de limpieza produjo una respuesta inválida.",
        ["service.could_not_clean"] = "No fue posible limpiar el modelo.",
        ["service.invalid_output"] = "La API informó éxito, pero no se creó un archivo de salida válido.",
        ["service.no_cpu"] = "Sin actividad de CPU durante {0}s; puede estar usando el disco o bloqueado • RAM {1:F1} GB",
        ["service.stats"] = "Activo • CPU {0:F0}s • RAM {1:F1} GB",
        ["worker.preparing"] = "Preparando el procesamiento…",
        ["worker.api"] = "API de SketchUp localizada: {0}",
        ["worker.loading"] = "Cargando el modelo sin abrir SketchUp…",
        ["worker.loaded"] = "Modelo cargado correctamente.",
        ["worker.inventory"] = "Contando componentes, materiales y texturas…",
        ["worker.scenes"] = "Verificando referencias de escenas y planos de sección…",
        ["worker.component_total"] = "Analizando {0} definiciones de componentes y grupos…",
        ["worker.component_progress"] = "Componentes analizados: {0}/{1}",
        ["worker.removed_components"] = "Componentes/grupos no utilizados eliminados: {0}",
        ["worker.materials"] = "Analizando materiales utilizados en la geometría…",
        ["worker.material_progress"] = "Definiciones verificadas para materiales: {0}/{1}",
        ["worker.removed_materials"] = "Materiales no utilizados eliminados: {0}",
        ["worker.saving"] = "Guardando el archivo de salida…",
        ["worker.validating"] = "Archivo guardado; reabriendo para validarlo…",
        ["worker.completed"] = "Purge completado; la API volvió a abrir la salida correctamente.",
        ["worker.preserved"] = "La definición no utilizada “{0}” fue conservada porque una escena todavía la referencia.",
        ["worker.fallback"] = "Modo de seguridad activo: conservando únicamente los elementos que impiden guardar el archivo.",
        ["worker.error"] = "Error: {0}"
    };

    private static readonly Dictionary<string, string> German = new()
    {
        ["title"] = "SketchUp-Modell bereinigen",
        ["description"] = "Entfernen Sie ungenutzte Komponenten und Materialien, ohne SketchUp zu öffnen.\nDie Originaldatei wird niemals verändert.",
        ["select"] = "Datei auswählen…",
        ["purge"] = "Bereinigung starten",
        ["none"] = "Legen Sie hier eine .skp-Datei ab oder wählen Sie oben eine aus.",
        ["log"] = "Verarbeitungsprotokoll",
        ["dialog.title"] = "SketchUp-Modell auswählen",
        ["dialog.filter"] = "SketchUp-Modell (*.skp)|*.skp",
        ["invalid"] = "Wählen Sie eine gültige .skp-Datei aus.",
        ["output"] = "Ausgabe: {0}",
        ["ready"] = "Startbereit.",
        ["elapsed"] = "Verstrichene Zeit: {0}  •  {1}",
        ["starting"] = "Wird gestartet…",
        ["start_log"] = "Verarbeitung gestartet. Die Originaldatei wird nicht verändert.",
        ["done_status"] = "Abgeschlossen: {0} Komponenten/Gruppen und {1} Materialien entfernt.",
        ["success"] = "Modell erfolgreich bereinigt.\n\nEntfernte Komponenten/Gruppen: {0}\nEntfernte Materialien: {1}\nEntfernte Texturen: {2}\n{3}\nErstellte Datei:\n{4}",
        ["preserved"] = "Aus Sicherheitsgründen beibehaltene Definitionen: {0} ({1}) — Details im Protokoll\n",
        ["success_title"] = "Bereinigung abgeschlossen",
        ["failed"] = "Die Bereinigung konnte nicht abgeschlossen werden.",
        ["failure_log"] = "FEHLER: {0}",
        ["error_title"] = "Fehler",
        ["service.same_output"] = "Die Ausgabedatei entspricht der Originaldatei; der Vorgang wurde abgebrochen.",
        ["service.runtime_missing"] = "Die interne Bereinigungskomponente wurde nicht gefunden.",
        ["service.start_failed"] = "Windows konnte den Bereinigungsprozess nicht starten.",
        ["service.started"] = "Bereinigungsprozess gestartet.",
        ["service.active"] = "Prozess aktiv",
        ["service.unexpected"] = "Der Prozess wurde unerwartet beendet (Code {0}). Die Originaldatei wurde nicht verändert.",
        ["service.invalid_response"] = "Der Bereinigungsprozess hat eine ungültige Antwort zurückgegeben.",
        ["service.could_not_clean"] = "Das Modell konnte nicht bereinigt werden.",
        ["service.invalid_output"] = "Die API meldete Erfolg, aber es wurde keine gültige Ausgabedatei erstellt.",
        ["service.no_cpu"] = "Seit {0}s keine CPU-Aktivität; möglicherweise Datenträgerzugriff oder Stillstand • RAM {1:F1} GB",
        ["service.stats"] = "Aktiv • CPU {0:F0}s • RAM {1:F1} GB",
        ["worker.preparing"] = "Verarbeitung wird vorbereitet…",
        ["worker.api"] = "SketchUp-API gefunden: {0}",
        ["worker.loading"] = "Modell wird geladen, ohne SketchUp zu öffnen…",
        ["worker.loaded"] = "Modell erfolgreich geladen.",
        ["worker.inventory"] = "Komponenten, Materialien und Texturen werden gezählt…",
        ["worker.scenes"] = "Szenen- und Schnittebenenreferenzen werden geprüft…",
        ["worker.component_total"] = "{0} Komponenten- und Gruppendefinitionen werden analysiert…",
        ["worker.component_progress"] = "Komponenten analysiert: {0}/{1}",
        ["worker.removed_components"] = "Ungenutzte Komponenten/Gruppen entfernt: {0}",
        ["worker.materials"] = "Von der Modellgeometrie verwendete Materialien werden analysiert…",
        ["worker.material_progress"] = "Definitionen auf Materialien geprüft: {0}/{1}",
        ["worker.removed_materials"] = "Ungenutzte Materialien entfernt: {0}",
        ["worker.saving"] = "Ausgabedatei wird gespeichert…",
        ["worker.validating"] = "Datei gespeichert; zur Prüfung wird sie erneut geöffnet…",
        ["worker.completed"] = "Bereinigung erfolgreich abgeschlossen; die API hat die Ausgabe erneut geöffnet.",
        ["worker.preserved"] = "Die ungenutzte Definition „{0}“ wurde beibehalten, weil eine Szene noch darauf verweist.",
        ["worker.fallback"] = "Sicherheitsmodus aktiv: Nur Elemente, die das Speichern verhindern, werden beibehalten.",
        ["worker.error"] = "Fehler: {0}"
    };

    private static readonly Dictionary<string, string> French = new()
    {
        ["title"] = "Nettoyer le modèle SketchUp",
        ["description"] = "Supprimez les composants et matériaux inutilisés sans ouvrir SketchUp.\nLe fichier original n’est jamais modifié.",
        ["select"] = "Choisir un fichier…",
        ["purge"] = "Lancer le nettoyage",
        ["none"] = "Déposez un fichier .skp ici ou choisissez-en un ci-dessus.",
        ["log"] = "Journal du traitement",
        ["dialog.title"] = "Choisir un modèle SketchUp",
        ["dialog.filter"] = "Modèle SketchUp (*.skp)|*.skp",
        ["invalid"] = "Choisissez un fichier .skp valide.",
        ["output"] = "Sortie : {0}",
        ["ready"] = "Prêt à démarrer.",
        ["elapsed"] = "Temps écoulé : {0}  •  {1}",
        ["starting"] = "Démarrage…",
        ["start_log"] = "Traitement démarré. Le fichier original ne sera pas modifié.",
        ["done_status"] = "Terminé : {0} composants/groupes et {1} matériaux supprimés.",
        ["success"] = "Modèle nettoyé avec succès.\n\nComposants/groupes supprimés : {0}\nMatériaux supprimés : {1}\nTextures supprimées : {2}\n{3}\nFichier créé :\n{4}",
        ["preserved"] = "Définitions conservées par sécurité : {0} ({1}) — détails dans le journal\n",
        ["success_title"] = "Nettoyage terminé",
        ["failed"] = "Le nettoyage n’a pas pu être terminé.",
        ["failure_log"] = "ÉCHEC : {0}",
        ["error_title"] = "Erreur",
        ["service.same_output"] = "La sortie correspond au fichier original ; l’opération a été annulée.",
        ["service.runtime_missing"] = "Le composant interne de nettoyage est introuvable.",
        ["service.start_failed"] = "Windows n’a pas pu démarrer le processus de nettoyage.",
        ["service.started"] = "Processus de nettoyage démarré.",
        ["service.active"] = "Processus actif",
        ["service.unexpected"] = "Le processus s’est terminé de façon inattendue (code {0}). Le fichier original n’a pas été modifié.",
        ["service.invalid_response"] = "Le processus de nettoyage a renvoyé une réponse non valide.",
        ["service.could_not_clean"] = "Le modèle n’a pas pu être nettoyé.",
        ["service.invalid_output"] = "L’API a signalé un succès, mais aucun fichier de sortie valide n’a été créé.",
        ["service.no_cpu"] = "Aucune activité CPU depuis {0}s ; accès disque ou blocage possible • RAM {1:F1} Go",
        ["service.stats"] = "Actif • CPU {0:F0}s • RAM {1:F1} Go",
        ["worker.preparing"] = "Préparation du traitement…",
        ["worker.api"] = "API SketchUp trouvée : {0}",
        ["worker.loading"] = "Chargement du modèle sans ouvrir SketchUp…",
        ["worker.loaded"] = "Modèle chargé avec succès.",
        ["worker.inventory"] = "Comptage des composants, matériaux et textures…",
        ["worker.scenes"] = "Vérification des références de scènes et de plans de section…",
        ["worker.component_total"] = "Analyse de {0} définitions de composants et de groupes…",
        ["worker.component_progress"] = "Composants analysés : {0}/{1}",
        ["worker.removed_components"] = "Composants/groupes inutilisés supprimés : {0}",
        ["worker.materials"] = "Analyse des matériaux utilisés par la géométrie…",
        ["worker.material_progress"] = "Définitions vérifiées pour les matériaux : {0}/{1}",
        ["worker.removed_materials"] = "Matériaux inutilisés supprimés : {0}",
        ["worker.saving"] = "Enregistrement du fichier de sortie…",
        ["worker.validating"] = "Fichier enregistré ; réouverture pour validation…",
        ["worker.completed"] = "Nettoyage terminé ; l’API a rouvert la sortie avec succès.",
        ["worker.preserved"] = "La définition inutilisée « {0} » a été conservée car une scène y fait encore référence.",
        ["worker.fallback"] = "Mode de sécurité actif : conservation des seuls éléments empêchant l’enregistrement.",
        ["worker.error"] = "Erreur : {0}"
    };

    private static readonly Dictionary<string, string> Russian = new()
    {
        ["title"] = "Очистить модель SketchUp",
        ["description"] = "Удалите неиспользуемые компоненты и материалы, не открывая SketchUp.\nИсходный файл никогда не изменяется.",
        ["select"] = "Выбрать файл…",
        ["purge"] = "Запустить очистку",
        ["none"] = "Перетащите сюда файл .skp или выберите его выше.",
        ["log"] = "Журнал обработки",
        ["dialog.title"] = "Выберите модель SketchUp",
        ["dialog.filter"] = "Модель SketchUp (*.skp)|*.skp",
        ["invalid"] = "Выберите корректный файл .skp.",
        ["output"] = "Выходной файл: {0}",
        ["ready"] = "Готово к запуску.",
        ["elapsed"] = "Прошло времени: {0}  •  {1}",
        ["starting"] = "Запуск…",
        ["start_log"] = "Обработка начата. Исходный файл не будет изменён.",
        ["done_status"] = "Готово: удалено компонентов/групп — {0}, материалов — {1}.",
        ["success"] = "Модель успешно очищена.\n\nУдалено компонентов/групп: {0}\nУдалено материалов: {1}\nУдалено текстур: {2}\n{3}\nСоздан файл:\n{4}",
        ["preserved"] = "Сохранено определений из соображений безопасности: {0} ({1}) — подробности в журнале\n",
        ["success_title"] = "Очистка завершена",
        ["failed"] = "Не удалось завершить очистку.",
        ["failure_log"] = "ОШИБКА: {0}",
        ["error_title"] = "Ошибка",
        ["service.same_output"] = "Выходной и исходный файлы совпадают; операция отменена.",
        ["service.runtime_missing"] = "Внутренний компонент очистки не найден.",
        ["service.start_failed"] = "Windows не удалось запустить процесс очистки.",
        ["service.started"] = "Процесс очистки запущен.",
        ["service.active"] = "Процесс активен",
        ["service.unexpected"] = "Процесс неожиданно завершился (код {0}). Исходный файл не был изменён.",
        ["service.invalid_response"] = "Процесс очистки вернул некорректный ответ.",
        ["service.could_not_clean"] = "Не удалось очистить модель.",
        ["service.invalid_output"] = "API сообщила об успехе, но корректный выходной файл не создан.",
        ["service.no_cpu"] = "Нет активности CPU в течение {0} с; возможна работа с диском или зависание • RAM {1:F1} ГБ",
        ["service.stats"] = "Активен • CPU {0:F0} с • RAM {1:F1} ГБ",
        ["worker.preparing"] = "Подготовка обработки…",
        ["worker.api"] = "Найдена API SketchUp: {0}",
        ["worker.loading"] = "Загрузка модели без открытия SketchUp…",
        ["worker.loaded"] = "Модель успешно загружена.",
        ["worker.inventory"] = "Подсчёт компонентов, материалов и текстур…",
        ["worker.scenes"] = "Проверка ссылок сцен и секущих плоскостей…",
        ["worker.component_total"] = "Анализ определений компонентов и групп: {0}…",
        ["worker.component_progress"] = "Компонентов проанализировано: {0}/{1}",
        ["worker.removed_components"] = "Удалено неиспользуемых компонентов/групп: {0}",
        ["worker.materials"] = "Анализ материалов, используемых геометрией…",
        ["worker.material_progress"] = "Определений проверено на материалы: {0}/{1}",
        ["worker.removed_materials"] = "Удалено неиспользуемых материалов: {0}",
        ["worker.saving"] = "Сохранение выходного файла…",
        ["worker.validating"] = "Файл сохранён; повторное открытие для проверки…",
        ["worker.completed"] = "Очистка завершена; API успешно повторно открыла выходной файл.",
        ["worker.preserved"] = "Неиспользуемое определение «{0}» сохранено, поскольку на него всё ещё ссылается сцена.",
        ["worker.fallback"] = "Активен безопасный режим: сохраняются только элементы, мешающие записи файла.",
        ["worker.error"] = "Ошибка: {0}"
    };

    private static readonly Dictionary<string, string> Vietnamese = new()
    {
        ["title"] = "Làm sạch mô hình SketchUp",
        ["description"] = "Xóa các thành phần và vật liệu không sử dụng mà không cần mở SketchUp.\nTệp gốc không bao giờ bị thay đổi.",
        ["select"] = "Chọn tệp…",
        ["purge"] = "Bắt đầu dọn dẹp",
        ["none"] = "Thả tệp .skp vào đây hoặc chọn một tệp ở phía trên.",
        ["log"] = "Nhật ký xử lý",
        ["dialog.title"] = "Chọn mô hình SketchUp",
        ["dialog.filter"] = "Mô hình SketchUp (*.skp)|*.skp",
        ["invalid"] = "Hãy chọn một tệp .skp hợp lệ.",
        ["output"] = "Tệp đầu ra: {0}",
        ["ready"] = "Sẵn sàng bắt đầu.",
        ["elapsed"] = "Thời gian đã chạy: {0}  •  {1}",
        ["starting"] = "Đang bắt đầu…",
        ["start_log"] = "Đã bắt đầu xử lý. Tệp gốc sẽ không bị thay đổi.",
        ["done_status"] = "Hoàn tất: đã xóa {0} thành phần/nhóm và {1} vật liệu.",
        ["success"] = "Đã làm sạch mô hình thành công.\n\nThành phần/nhóm đã xóa: {0}\nVật liệu đã xóa: {1}\nKết cấu đã xóa: {2}\n{3}\nTệp đã tạo:\n{4}",
        ["preserved"] = "Định nghĩa được giữ lại để bảo đảm an toàn: {0} ({1}) — xem nhật ký để biết chi tiết\n",
        ["success_title"] = "Đã hoàn tất dọn dẹp",
        ["failed"] = "Không thể hoàn tất quá trình dọn dẹp.",
        ["failure_log"] = "THẤT BẠI: {0}",
        ["error_title"] = "Lỗi",
        ["service.same_output"] = "Tệp đầu ra trùng với tệp gốc; thao tác đã bị hủy.",
        ["service.runtime_missing"] = "Không tìm thấy thành phần dọn dẹp nội bộ.",
        ["service.start_failed"] = "Windows không thể khởi động tiến trình dọn dẹp.",
        ["service.started"] = "Đã khởi động tiến trình dọn dẹp.",
        ["service.active"] = "Tiến trình đang hoạt động",
        ["service.unexpected"] = "Tiến trình kết thúc bất ngờ (mã {0}). Tệp gốc không bị thay đổi.",
        ["service.invalid_response"] = "Tiến trình dọn dẹp trả về phản hồi không hợp lệ.",
        ["service.could_not_clean"] = "Không thể làm sạch mô hình.",
        ["service.invalid_output"] = "API báo thành công nhưng không tạo được tệp đầu ra hợp lệ.",
        ["service.no_cpu"] = "Không có hoạt động CPU trong {0} giây; có thể đang truy cập đĩa hoặc bị treo • RAM {1:F1} GB",
        ["service.stats"] = "Đang hoạt động • CPU {0:F0} giây • RAM {1:F1} GB",
        ["worker.preparing"] = "Đang chuẩn bị xử lý…",
        ["worker.api"] = "Đã tìm thấy API SketchUp: {0}",
        ["worker.loading"] = "Đang tải mô hình mà không mở SketchUp…",
        ["worker.loaded"] = "Đã tải mô hình thành công.",
        ["worker.inventory"] = "Đang đếm thành phần, vật liệu và kết cấu…",
        ["worker.scenes"] = "Đang kiểm tra tham chiếu cảnh và mặt phẳng cắt…",
        ["worker.component_total"] = "Đang phân tích {0} định nghĩa thành phần và nhóm…",
        ["worker.component_progress"] = "Thành phần đã phân tích: {0}/{1}",
        ["worker.removed_components"] = "Thành phần/nhóm không sử dụng đã xóa: {0}",
        ["worker.materials"] = "Đang phân tích vật liệu được hình học của mô hình sử dụng…",
        ["worker.material_progress"] = "Định nghĩa đã kiểm tra vật liệu: {0}/{1}",
        ["worker.removed_materials"] = "Vật liệu không sử dụng đã xóa: {0}",
        ["worker.saving"] = "Đang lưu tệp đầu ra…",
        ["worker.validating"] = "Đã lưu tệp; đang mở lại để xác thực…",
        ["worker.completed"] = "Đã hoàn tất dọn dẹp; API đã mở lại tệp đầu ra thành công.",
        ["worker.preserved"] = "Định nghĩa không sử dụng “{0}” được giữ lại vì một cảnh vẫn đang tham chiếu đến nó.",
        ["worker.fallback"] = "Chế độ an toàn đang hoạt động: chỉ giữ lại các mục ngăn không cho lưu tệp.",
        ["worker.error"] = "Lỗi: {0}"
    };

    private static readonly Dictionary<string, string> Chinese = new()
    {
        ["title"] = "清理 SketchUp 模型",
        ["description"] = "无需打开 SketchUp 即可移除未使用的组件和材质。\n原始文件绝不会被修改。",
        ["select"] = "选择文件…",
        ["purge"] = "开始清理",
        ["none"] = "将 .skp 文件拖放到此处，或在上方选择文件。",
        ["log"] = "处理日志",
        ["dialog.title"] = "选择 SketchUp 模型",
        ["dialog.filter"] = "SketchUp 模型 (*.skp)|*.skp",
        ["invalid"] = "请选择有效的 .skp 文件。",
        ["output"] = "输出：{0}",
        ["ready"] = "准备就绪。",
        ["elapsed"] = "已用时间：{0}  •  {1}",
        ["starting"] = "正在启动…",
        ["start_log"] = "处理已开始。原始文件不会被修改。",
        ["done_status"] = "已完成：移除了 {0} 个组件/组和 {1} 个材质。",
        ["success"] = "模型清理成功。\n\n移除的组件/组：{0}\n移除的材质：{1}\n移除的纹理：{2}\n{3}\n已创建文件：\n{4}",
        ["preserved"] = "为安全起见保留的定义：{0}（{1}）— 详情请查看日志\n",
        ["success_title"] = "清理完成",
        ["failed"] = "无法完成清理。",
        ["failure_log"] = "失败：{0}",
        ["error_title"] = "错误",
        ["service.same_output"] = "输出文件与原始文件相同；操作已取消。",
        ["service.runtime_missing"] = "未找到内部清理组件。",
        ["service.start_failed"] = "Windows 无法启动清理进程。",
        ["service.started"] = "清理进程已启动。",
        ["service.active"] = "进程正在运行",
        ["service.unexpected"] = "进程意外结束（代码 {0}）。原始文件未被修改。",
        ["service.invalid_response"] = "清理进程返回了无效响应。",
        ["service.could_not_clean"] = "无法清理模型。",
        ["service.invalid_output"] = "API 报告成功，但未创建有效的输出文件。",
        ["service.no_cpu"] = "已有 {0} 秒无 CPU 活动；可能正在访问磁盘或已卡住 • 内存 {1:F1} GB",
        ["service.stats"] = "运行中 • CPU {0:F0} 秒 • 内存 {1:F1} GB",
        ["worker.preparing"] = "正在准备处理…",
        ["worker.api"] = "已找到 SketchUp API：{0}",
        ["worker.loading"] = "正在加载模型，无需打开 SketchUp…",
        ["worker.loaded"] = "模型加载成功。",
        ["worker.inventory"] = "正在统计组件、材质和纹理…",
        ["worker.scenes"] = "正在检查场景和剖切面引用…",
        ["worker.component_total"] = "正在分析 {0} 个组件和组定义…",
        ["worker.component_progress"] = "已分析组件：{0}/{1}",
        ["worker.removed_components"] = "已移除未使用的组件/组：{0}",
        ["worker.materials"] = "正在分析模型几何体使用的材质…",
        ["worker.material_progress"] = "已检查材质的定义：{0}/{1}",
        ["worker.removed_materials"] = "已移除未使用的材质：{0}",
        ["worker.saving"] = "正在保存输出文件…",
        ["worker.validating"] = "文件已保存；正在重新打开以验证…",
        ["worker.completed"] = "清理成功完成；API 已重新打开输出文件。",
        ["worker.preserved"] = "未使用的定义“{0}”已保留，因为某个场景仍在引用它。",
        ["worker.fallback"] = "安全模式已启用：仅保留阻止文件保存的项目。",
        ["worker.error"] = "错误：{0}"
    };

    [GeneratedRegex(@"^API do SketchUp localizada: (.+)$")]
    private static partial Regex ApiPathRegex();
    [GeneratedRegex(@"^Analisando (\d+) definições de componentes e grupos…$")]
    private static partial Regex ComponentTotalRegex();
    [GeneratedRegex(@"^Componentes analisados: (\d+)/(\d+)$")]
    private static partial Regex ComponentProgressRegex();
    [GeneratedRegex(@"^Componentes/grupos não utilizados removidos: (\d+)$")]
    private static partial Regex RemovedComponentsRegex();
    [GeneratedRegex(@"^Definições verificadas para materiais: (\d+)/(\d+)$")]
    private static partial Regex MaterialProgressRegex();
    [GeneratedRegex(@"^Materiais não utilizados removidos: (\d+)$")]
    private static partial Regex RemovedMaterialsRegex();
    [GeneratedRegex("^Fallback: definição não usada “([^”]+)” preservada porque .+$")]
    private static partial Regex PreservedDefinitionRegex();
}
