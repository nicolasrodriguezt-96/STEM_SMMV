using System;
using UnityEngine;

public class SequenceController : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField]
    private QuizController quizController;

    [SerializeField]
    private StoryController storyController;

    [Header("Views")]
    [SerializeField]
    private MainMenuView mainMenuView;

    private QuizHistoryService historyService;

    private bool sequenceRunning;

    private void Awake()
    {
        historyService =
            new QuizHistoryService();
    }

    public void StartSequence()
    {
        if (AppController.Instance == null)
        {
            Debug.LogError(
                "No existe AppController."
            );

            return;
        }

        QuizModel quiz =
            AppController.Instance.CurrentQuiz;

        LoadedPackageModel package =
            AppController.Instance.CurrentPackage;

        if (quiz == null)
        {
            Debug.LogError(
                "No existe un quiz activo."
            );

            return;
        }

        if (package == null)
        {
            Debug.LogError(
                "No existe un paquete activo."
            );

            return;
        }

        if (quiz.secuencia == null ||
            quiz.secuencia.Count == 0)
        {
            Debug.LogError(
                $"El test {quiz.nombre} no tiene secuencia."
            );

            return;
        }

        /*
         * Ordenamos la secuencia según
         * el campo "orden" definido en el JSON.
         */
        quiz.secuencia.Sort(
            (a, b) =>
                a.orden.CompareTo(b.orden)
        );

        sequenceRunning = true;

        if (mainMenuView != null)
        {
            mainMenuView.HideAllPanels();
        }

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
            AppController.Instance
                .GetCurrentSequenceItem();

        /*
         * Si ya no existe un elemento actual,
         * significa que terminó la secuencia.
         */
        if (item == null)
        {
            FinishQuiz();

            return;
        }

        Debug.Log(
            $"Ejecutando secuencia: " +
            $"{item.tipo} - {item.id}"
        );

        if (string.IsNullOrWhiteSpace(
                item.tipo))
        {
            Debug.LogError(
                "Elemento de secuencia sin tipo."
            );

            AdvanceSequence();

            return;
        }

        switch (
            item.tipo.ToLowerInvariant())
        {
            case "pregunta":

                ExecuteQuestion(
                    item
                );

                break;

            case "historia":

                ExecuteStory(
                    item
                );

                break;

            case "video":

                ExecuteVideo(
                    item
                );

                break;

            case "minijuego":

                ExecuteMinigame(
                    item
                );

                break;

            default:

                Debug.LogError(
                    $"Tipo de secuencia desconocido: {item.tipo}"
                );

                AdvanceSequence();

                break;
        }
    }

    /*
     * =========================================
     * PREGUNTA
     * =========================================
     */
    private void ExecuteQuestion(
        SequenceItemModel item)
    {
        AppController app =
            AppController.Instance;

        QuizModel quiz =
            app.CurrentQuiz;

        LoadedPackageModel package =
            app.CurrentPackage;

        if (quiz == null ||
            package == null)
        {
            Debug.LogError(
                "No existe el quiz o paquete actual."
            );

            AbortQuiz();

            return;
        }

        if (quiz.preguntas == null)
        {
            Debug.LogError(
                $"El quiz {quiz.nombre} no tiene preguntas."
            );

            AdvanceSequence();

            return;
        }

        QuestionModel question =
            quiz.preguntas.Find(
                q =>
                    q != null &&
                    q.id == item.id
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

            /*
             * Cuando responde:
             * avanzamos al siguiente elemento.
             */
            AdvanceSequence,

            /*
             * Si pulsa salir:
             * cancelamos el test.
             */
            AbortQuiz
        );
    }

    /*
     * =========================================
     * HISTORIA DENTRO DEL TEST
     * =========================================
     */
    private void ExecuteStory(
        SequenceItemModel item)
    {
        AppController app =
            AppController.Instance;

        LoadedPackageModel package =
            app.CurrentPackage;

        if (package == null)
        {
            Debug.LogError(
                "No existe el paquete actual."
            );

            AbortQuiz();

            return;
        }

        StoryModel story =
            package.GetStory(
                item.id
            );

        if (story == null)
        {
            Debug.LogError(
                $"No existe la historia: {item.id}"
            );

            /*
             * No bloqueamos todo el test.
             * Saltamos el elemento inválido.
             */
            AdvanceSequence();

            return;
        }

        /*
         * IMPORTANTE:
         *
         * Esta historia se abrió DESDE
         * un Test Vocacional.
         *
         * Por tanto, cuando termine,
         * StoryController ejecutará
         * AdvanceSequence().
         */
        storyController
            .OpenStoryFromQuiz(
                package,
                story,
                AdvanceSequence
            );
    }

    /*
     * =========================================
     * VIDEO
     * =========================================
     */
    private void ExecuteVideo(
        SequenceItemModel item)
    {
        /*
         * Todavía no tenemos VideoController.
         *
         * Por ahora no detenemos el flujo.
         */
        Debug.LogWarning(
            $"Video pendiente de implementar: {item.id}"
        );

        AdvanceSequence();
    }

    /*
     * =========================================
     * MINIJUEGO
     * =========================================
     */
    private void ExecuteMinigame(
        SequenceItemModel item)
    {
        /*
         * Los minijuegos 3D se conectarán
         * posteriormente.
         */
        Debug.LogWarning(
            $"Minijuego pendiente de implementar: {item.id}"
        );

        AdvanceSequence();
    }

    /*
     * =========================================
     * AVANZAR
     * =========================================
     */
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

        bool hasNext =
            AppController.Instance
                .MoveNext();

        if (hasNext)
        {
            ExecuteCurrentItem();
        }
        else
        {
            FinishQuiz();
        }
    }

    /*
     * =========================================
     * TERMINAR TEST
     * =========================================
     */
    private void FinishQuiz()
    {
        if (!sequenceRunning)
        {
            return;
        }

        sequenceRunning = false;

        if (AppController.Instance == null)
        {
            return;
        }

        AppController app =
            AppController.Instance;

        QuizModel quiz =
            app.CurrentQuiz;

        if (quiz == null)
        {
            app.ResetApplication();

            if (mainMenuView != null)
            {
                mainMenuView.ShowTests();
            }

            return;
        }

        DateTime endTime =
            DateTime.Now;

        DateTime startTime =
            app.QuizStartTime;

        float totalSeconds =
            (float)
            (endTime - startTime)
            .TotalSeconds;

        int maximumScore =
            CountQuestions();

        int score =
            app.Score;

        /*
         * Guardamos el historial.
         */
        historyService.SaveExecution(
            quiz.id,
            quiz.nombre,
            startTime,
            endTime,
            totalSeconds,
            score,
            maximumScore
        );

        Debug.Log(
            $"TEST FINALIZADO: " +
            $"{quiz.nombre} - " +
            $"{score}/{maximumScore} - " +
            $"{totalSeconds:0.0} segundos"
        );

        /*
         * Limpiamos el test actual.
         */
        app.ResetApplication();

        /*
         * MUY IMPORTANTE:
         *
         * Al terminar un test NO volvemos
         * al menú principal.
         *
         * Volvemos a la lista de tests para
         * que el usuario pueda ejecutar otro.
         */
        if (mainMenuView != null)
        {
            mainMenuView.ShowTests();
        }
    }

    /*
     * =========================================
     * CANCELAR TEST
     * =========================================
     */
    private void AbortQuiz()
    {
        sequenceRunning = false;

        if (quizController != null)
        {
            quizController.Hide();
        }

        if (AppController.Instance != null)
        {
            AppController.Instance
                .ResetApplication();
        }

        /*
         * Si el usuario abandona un test,
         * volvemos igualmente a la lista
         * de tests.
         */
        if (mainMenuView != null)
        {
            mainMenuView.ShowTests();
        }

        Debug.Log(
            "Test cancelado."
        );
    }

    /*
     * =========================================
     * TOTAL DE PREGUNTAS
     * =========================================
     */
    private int CountQuestions()
    {
        if (AppController.Instance == null)
        {
            return 0;
        }

        QuizModel quiz =
            AppController.Instance
                .CurrentQuiz;

        if (quiz == null ||
            quiz.secuencia == null)
        {
            return 0;
        }

        int count = 0;

        foreach (
            SequenceItemModel item
            in quiz.secuencia)
        {
            if (item == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(
                    item.tipo))
            {
                continue;
            }

            if (
                item.tipo.ToLowerInvariant()
                == "pregunta"
            )
            {
                count++;
            }
        }

        return count;
    }

    /*
     * =========================================
     * PROGRESO DE PREGUNTAS
     *
     * Ejemplo:
     *
     * historia
     * pregunta
     * video
     * pregunta
     *
     * Mostrará:
     *
     * pregunta 1 / 2
     * pregunta 2 / 2
     * =========================================
     */
    private void GetQuestionProgress(
        out int current,
        out int total)
    {
        current = 0;
        total = 0;

        if (AppController.Instance == null)
        {
            return;
        }

        QuizModel quiz =
            AppController.Instance
                .CurrentQuiz;

        if (quiz == null ||
            quiz.secuencia == null)
        {
            return;
        }

        int currentSequenceIndex =
            AppController.Instance
                .CurrentSequenceIndex;

        for (
            int i = 0;
            i < quiz.secuencia.Count;
            i++)
        {
            SequenceItemModel item =
                quiz.secuencia[i];

            if (item == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(
                    item.tipo))
            {
                continue;
            }

            if (
                item.tipo.ToLowerInvariant()
                != "pregunta"
            )
            {
                continue;
            }

            total++;

            if (i <= currentSequenceIndex)
            {
                current++;
            }
        }
    }
}