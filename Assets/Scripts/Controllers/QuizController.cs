using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class QuizController : MonoBehaviour
{
    [Header("View")]
    [SerializeField] private QuizView quizView;

    [Header("Feedback (configurable desde Inspector)")]
    [SerializeField, Min(0.1f)] private float feedbackSeconds = 2.5f;
    [SerializeField] private Color correctColor = new Color(0.20f, 0.75f, 0.36f);
    [SerializeField] private Color incorrectColor = new Color(0.90f, 0.25f, 0.26f);

    [Header("Audio predeterminado")]
    [SerializeField] private AudioClip correctSound;
    [SerializeField] private AudioClip incorrectSound;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.85f;

    private AudioSource audioSource;
    private ImageLoaderService imageLoader;
    private QuestionModel currentQuestion;
    private Action onAnswered;
    private Action onExit;
    private Coroutine feedbackCoroutine;
    private bool answerLocked;

    private void Awake()
    {
        imageLoader = new ImageLoaderService();
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f; // Sonido 2D para UI.

        if (quizView == null)
        {
            Debug.LogError("QuizView no está asignado en QuizController.");
            return;
        }

        if (quizView.AnswerAButton != null)
            quizView.AnswerAButton.onClick.AddListener(() => SelectAnswer(0));
        if (quizView.AnswerBButton != null)
            quizView.AnswerBButton.onClick.AddListener(() => SelectAnswer(1));
        if (quizView.AnswerCButton != null)
            quizView.AnswerCButton.onClick.AddListener(() => SelectAnswer(2));
        if (quizView.AnswerDButton != null)
            quizView.AnswerDButton.onClick.AddListener(() => SelectAnswer(3));
        if (quizView.ExitButton != null)
            quizView.ExitButton.onClick.AddListener(ExitQuiz);

        quizView.Hide();
    }

    // Misma firma que ya utiliza SequenceController: no hay que modificarlo.
    public void ShowQuestion(
        LoadedPackageModel package,
        QuizModel quiz,
        QuestionModel question,
        int currentQuestionNumber,
        int totalQuestions,
        Action answeredCallback,
        Action exitCallback)
    {
        if (quizView == null || package == null || quiz == null || question == null)
        {
            Debug.LogError("No fue posible mostrar la pregunta.");
            return;
        }

        if (question.respuestas == null || question.respuestas.Count != 4)
        {
            Debug.LogError($"La pregunta {question.id} debe tener 4 respuestas.");
            return;
        }

        CancelPendingFeedback();
        quizView.ResetAnswerStyles();

        currentQuestion = question;
        onAnswered = answeredCallback;
        onExit = exitCallback;
        answerLocked = false;

        quizView.SetTitle(quiz.nombre);
        quizView.SetQuestion(question.texto);
        quizView.SetProgress(currentQuestionNumber, totalQuestions);

        for (int i = 0; i < 4; i++)
            quizView.SetAnswerText(i, question.respuestas[i].texto);

        Sprite sprite = string.IsNullOrWhiteSpace(question.imagen)
            ? null
            : imageLoader.LoadSprite(package.rootPath, question.imagen);
        quizView.SetImage(sprite);
        quizView.Show();
    }

    private void SelectAnswer(int selectedIndex)
    {
        if (answerLocked || currentQuestion == null) return;
        if (selectedIndex < 0 || selectedIndex >= currentQuestion.respuestas.Count) return;

        int correctIndex = currentQuestion.respuestas.FindIndex(
            answer => answer != null && answer.correcta);

        if (correctIndex < 0)
        {
            Debug.LogError($"La pregunta {currentQuestion.id} no tiene respuesta correcta.");
            return;
        }

        answerLocked = true; // Evita dobles clics y puntajes duplicados.
        bool correct = currentQuestion.respuestas[selectedIndex].correcta;

        if (correct)
        {
            if (AppController.Instance != null)
                AppController.Instance.AddPoint();

            Debug.Log($"Respuesta correcta: {currentQuestion.respuestas[selectedIndex].texto}");
        }
        else
        {
            Debug.Log($"Respuesta incorrecta. Correcta: {currentQuestion.respuestas[correctIndex].texto}");
        }

        quizView.ShowAnswerFeedback(
            selectedIndex, correctIndex, correctColor, incorrectColor);

        AudioClip clip = correct ? correctSound : incorrectSound;
        if (clip != null)
            audioSource.PlayOneShot(clip, soundVolume);

        feedbackCoroutine = StartCoroutine(ContinueAfterFeedback());
    }

    private IEnumerator ContinueAfterFeedback()
    {
        // Tiempo real: funciona aunque Time.timeScale sea 0.
        yield return new WaitForSecondsRealtime(feedbackSeconds);

        feedbackCoroutine = null;
        quizView.ResetAnswerStyles();
        quizView.Hide();

        Action callback = onAnswered;
        ClearQuestion();
        callback?.Invoke();
    }

    private void ExitQuiz()
    {
        Action callback = onExit;
        CancelPendingFeedback();
        if (quizView != null)
        {
            quizView.ResetAnswerStyles();
            quizView.Hide();
        }
        ClearQuestion();
        callback?.Invoke();
    }

    // Lo llama SequenceController al cancelar un test.
    public void Hide()
    {
        CancelPendingFeedback();
        if (quizView != null)
        {
            quizView.ResetAnswerStyles();
            quizView.Hide();
        }
        ClearQuestion();
    }

    private void CancelPendingFeedback()
    {
        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
            feedbackCoroutine = null;
        }

        if (audioSource != null)
            audioSource.Stop();
    }

    private void ClearQuestion()
    {
        currentQuestion = null;
        onAnswered = null;
        onExit = null;
        answerLocked = false;
    }
}
