using System;
using System.IO;
using UnityEngine;

public class SequenceController : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField] private QuizController quizController;
    [SerializeField] private StoryController storyController;
    [SerializeField] private QuizResultController quizResultController;

    [Header("Views")]
    [SerializeField] private MainMenuView mainMenuView;

    private QuizHistoryService historyService;

    private bool sequenceRunning;

    private const string DeviceIdKey = "STEM_INSTALLATION_DEVICE_ID";

    private void Awake()
    {
        historyService = new QuizHistoryService();
    }

    public void StartSequence()
    {
        if (AppController.Instance == null)
        {
            Debug.LogError("No existe AppController.");
            return;
        }

        QuizModel quiz = AppController.Instance.CurrentQuiz;
        LoadedPackageModel package = AppController.Instance.CurrentPackage;

        if (quiz == null || package == null)
        {
            Debug.LogError("No existe un test o paquete activo.");
            return;
        }

        if (quiz.secuencia == null || quiz.secuencia.Count == 0)
        {
            Debug.LogError(
                $"El test {quiz.nombre} no tiene secuencia."
            );

            return;
        }

        quiz.secuencia.Sort(
            (a, b) => a.orden.CompareTo(b.orden)
        );

        sequenceRunning = true;

        mainMenuView?.HideAllPanels();

        ExecuteCurrentItem();
    }

    private void ExecuteCurrentItem()
    {
        if (!sequenceRunning)
        {
            return;
        }

        if (AppController.Instance == null)
        {
            AbortQuiz();
            return;
        }

        SequenceItemModel item =
            AppController.Instance.GetCurrentSequenceItem();

        if (item == null)
        {
            FinishQuiz();
            return;
        }

        if (string.IsNullOrWhiteSpace(item.tipo))
        {
            Debug.LogError("Elemento de secuencia sin tipo.");

            AdvanceSequence();
            return;
        }

        Debug.Log(
            $"Ejecutando secuencia: {item.tipo} - {item.id}"
        );

        switch (item.tipo.ToLowerInvariant())
        {
            case "pregunta":
                ExecuteQuestion(item);
                break;

            case "historia":
                ExecuteStory(item);
                break;

            case "video":
                ExecuteVideo(item);
                break;

            case "minijuego":
                ExecuteMinigame(item);
                break;

            default:
                Debug.LogError(
                    $"Tipo de secuencia desconocido: {item.tipo}"
                );

                AdvanceSequence();
                break;
        }
    }

    private void ExecuteQuestion(SequenceItemModel item)
    {
        AppController app = AppController.Instance;

        QuizModel quiz = app.CurrentQuiz;
        LoadedPackageModel package = app.CurrentPackage;

        if (quiz == null ||
            package == null ||
            quiz.preguntas == null)
        {
            Debug.LogError(
                "No existe el cuestionario, paquete o lista de preguntas."
            );

            AbortQuiz();
            return;
        }

        if (quizController == null)
        {
            Debug.LogError(
                "QuizController no está asignado en SequenceController."
            );

            AbortQuiz();
            return;
        }

        QuestionModel question = quiz.preguntas.Find(
            q => q != null && q.id == item.id
        );

        if (question == null)
        {
            Debug.LogError(
                $"No existe la pregunta: {item.id}"
            );

            AdvanceSequence();
            return;
        }

        GetQuestionProgress(
            out int current,
            out int total
        );

        quizController.ShowQuestion(
            package,
            quiz,
            question,
            current,
            total,
            AdvanceSequence,
            AbortQuiz
        );
    }

    private void ExecuteStory(SequenceItemModel item)
    {
        LoadedPackageModel package =
            AppController.Instance.CurrentPackage;

        if (package == null)
        {
            AbortQuiz();
            return;
        }

        StoryModel story = package.GetStory(item.id);

        if (story == null)
        {
            Debug.LogError(
                $"No existe la historia: {item.id}"
            );

            AdvanceSequence();
            return;
        }

        if (storyController == null)
        {
            Debug.LogError(
                "StoryController no está asignado en SequenceController."
            );

            AbortQuiz();
            return;
        }

        storyController.OpenStoryFromQuiz(
            package,
            story,
            AdvanceSequence
        );
    }

    private void ExecuteVideo(SequenceItemModel item)
    {
        Debug.LogWarning(
            $"Video pendiente de implementar: {item.id}"
        );

        AdvanceSequence();
    }

    private void ExecuteMinigame(SequenceItemModel item)
    {
        Debug.LogWarning(
            $"Minijuego pendiente de implementar: {item.id}"
        );

        AdvanceSequence();
    }

    private void AdvanceSequence()
    {
        if (!sequenceRunning)
        {
            return;
        }

        if (AppController.Instance == null)
        {
            AbortQuiz();
            return;
        }

        if (AppController.Instance.MoveNext())
        {
            ExecuteCurrentItem();
        }
        else
        {
            FinishQuiz();
        }
    }

    private void FinishQuiz()
    {
        if (!sequenceRunning)
        {
            return;
        }

        sequenceRunning = false;

        AppController app = AppController.Instance;

        if (app == null)
        {
            return;
        }

        QuizModel quiz = app.CurrentQuiz;

        if (quiz == null)
        {
            app.ResetApplication();

            mainMenuView?.ShowTests();
            return;
        }

        // Obtenemos los datos del cuestionario.
        DateTime endTime = DateTime.Now;
        DateTime startTime = app.QuizStartTime;

        float elapsedSeconds =
            (float)(endTime - startTime).TotalSeconds;

        int maximumScore = CountQuestions();
        int correctAnswers = app.Score;

        string quizName = quiz.nombre;

        // NUEVO:
        // Guardamos el resultado en SQLite y en el historial JSON.
        SaveQuizResults(
            quiz,
            app.CurrentPackage,
            startTime,
            endTime,
            elapsedSeconds,
            correctAnswers,
            maximumScore
        );

        Debug.Log(
            $"TEST FINALIZADO: {quizName} - " +
            $"{correctAnswers}/{maximumScore}"
        );

        // Reiniciamos el estado después de guardar.
        app.ResetApplication();

        // Mostramos la pantalla de resultados.
        if (quizResultController != null)
        {
            quizResultController.ShowResult(
                quizName,
                correctAnswers,
                maximumScore,
                elapsedSeconds
            );
        }
        else
        {
            Debug.LogWarning(
                "QuizResultController no está asignado. Regresando a Tests."
            );

            mainMenuView?.ShowTests();
        }
    }

    // =====================================================
    // NUEVO: GUARDADO DE RESULTADOS
    // =====================================================

    private void SaveQuizResults(
        QuizModel quiz,
        LoadedPackageModel package,
        DateTime startTime,
        DateTime endTime,
        float elapsedSeconds,
        int correctAnswers,
        int totalQuestions)
    {
        float percentage = 0f;

        if (totalQuestions > 0)
        {
            percentage =
                (float)correctAnswers / totalQuestions * 100f;
        }

        bool savedToSqlite = false;
        bool savedToJson = false;

        // =============================================
        // 1. GUARDAR EN SQLITE
        // =============================================

        try
        {
            string packageId = "";

            if (package != null &&
                !string.IsNullOrWhiteSpace(package.rootPath))
            {
                // Los paquetes instalados se encuentran
                // en carpetas identificadas por su ID.
                string cleanPath = package.rootPath.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar
                );

                packageId = Path.GetFileName(cleanPath);
            }

            LocalResultRecord result = new LocalResultRecord
            {
                // Cada ejecución tiene un identificador único.
                Id = Guid.NewGuid().ToString("N"),

                // Identificador persistente de esta instalación.
                DeviceId = GetOrCreateDeviceId(),

                ResultType = "QUIZ",

                PackageId = packageId,

                ContentId = quiz.id,

                ContentName = quiz.nombre,

                // Guardamos fechas en UTC.
                StartedAtUtc =
                    startTime.ToUniversalTime().ToString("o"),

                FinishedAtUtc =
                    endTime.ToUniversalTime().ToString("o"),

                DurationSeconds = elapsedSeconds,

                CorrectAnswers = correctAnswers,

                TotalQuestions = totalQuestions,

                ScorePercentage = percentage,

                // Estos campos corresponden al test vocacional.
                VocationalScoresJson = null,

                RecommendedCareersJson = null,

                // Todavía NO se ha enviado a Firebase.
                SyncStatus = 0,

                SyncedAtUtc = null
            };

            using (LocalDatabaseService database =
                new LocalDatabaseService())
            {
                database.SaveResult(result);
            }

            savedToSqlite = true;

            Debug.Log(
                "RESULTADO GUARDADO EN SQLITE\n" +
                $"ID: {result.Id}\n" +
                $"Cuestionario: {result.ContentName}\n" +
                $"Correctas: {correctAnswers}/{totalQuestions}\n" +
                $"Porcentaje: {percentage:0.##}%\n" +
                $"Tiempo: {elapsedSeconds:0.##} segundos\n" +
                "Sincronización: PENDIENTE"
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "ERROR GUARDANDO EL CUESTIONARIO EN SQLITE:\n" +
                ex
            );
        }

        // =============================================
        // 2. CONSERVAR EL HISTORIAL JSON
        // =============================================

        try
        {
            historyService.SaveExecution(
                quiz.id,
                quiz.nombre,
                startTime,
                endTime,
                elapsedSeconds,
                correctAnswers,
                totalQuestions
            );

            savedToJson = true;
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "ERROR GUARDANDO EL HISTORIAL JSON:\n" +
                ex
            );
        }

        // =============================================
        // 3. COMPROBAR EL RESULTADO DEL GUARDADO
        // =============================================

        if (!savedToSqlite && !savedToJson)
        {
            Debug.LogError(
                "ATENCIÓN: No se pudo guardar el resultado " +
                "ni en SQLite ni en JSON."
            );
        }
        else if (!savedToSqlite)
        {
            Debug.LogWarning(
                "El resultado solo se guardó en JSON. " +
                "No está disponible todavía para sincronizar con Firebase."
            );
        }
    }

    // =====================================================
    // NUEVO: IDENTIFICADOR ÚNICO DE INSTALACIÓN
    // =====================================================

    private string GetOrCreateDeviceId()
    {
        string deviceId = PlayerPrefs.GetString(
            DeviceIdKey,
            ""
        );

        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            return deviceId;
        }

        deviceId = Guid.NewGuid().ToString("N");

        PlayerPrefs.SetString(
            DeviceIdKey,
            deviceId
        );

        // Persistimos el identificador inmediatamente.
        PlayerPrefs.Save();

        Debug.Log(
            $"Nuevo identificador de instalación STEM: {deviceId}"
        );

        return deviceId;
    }

    // =====================================================
    // FUNCIONALIDAD ORIGINAL
    // =====================================================

    private void AbortQuiz()
    {
        sequenceRunning = false;

        if (quizController != null)
        {
            quizController.Hide();
        }

        if (AppController.Instance != null)
        {
            AppController.Instance.ResetApplication();
        }

        mainMenuView?.ShowTests();

        Debug.Log("Test cancelado.");
    }

    private int CountQuestions()
    {
        QuizModel quiz =
            AppController.Instance != null
                ? AppController.Instance.CurrentQuiz
                : null;

        if (quiz == null || quiz.secuencia == null)
        {
            return 0;
        }

        int count = 0;

        foreach (SequenceItemModel item in quiz.secuencia)
        {
            if (item != null &&
                string.Equals(
                    item.tipo,
                    "pregunta",
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                count++;
            }
        }

        return count;
    }

    private void GetQuestionProgress(
        out int current,
        out int total)
    {
        current = 0;
        total = 0;

        AppController app = AppController.Instance;

        QuizModel quiz =
            app != null ? app.CurrentQuiz : null;

        if (quiz == null || quiz.secuencia == null)
        {
            return;
        }

        int currentIndex = app.CurrentSequenceIndex;

        for (int i = 0; i < quiz.secuencia.Count; i++)
        {
            SequenceItemModel item = quiz.secuencia[i];

            if (item == null ||
                !string.Equals(
                    item.tipo,
                    "pregunta",
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                continue;
            }

            total++;

            if (i <= currentIndex)
            {
                current++;
            }
        }
    }
}