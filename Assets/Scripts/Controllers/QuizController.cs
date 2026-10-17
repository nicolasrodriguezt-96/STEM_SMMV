using System;
using UnityEngine;

public class QuizController : MonoBehaviour
{
    [Header("View")]
    [SerializeField]
    private QuizView quizView;

    private ImageLoaderService imageLoader;

    private LoadedPackageModel currentPackage;
    private QuizModel currentQuiz;
    private QuestionModel currentQuestion;

    private Action onAnswered;
    private Action onExit;

    private void Awake()
    {
        imageLoader =
            new ImageLoaderService();

        if (quizView == null)
        {
            Debug.LogError(
                "QuizView no está asignado en QuizController."
            );

            return;
        }

        quizView.AnswerAButton
            .onClick
            .AddListener(
                () => SelectAnswer(0)
            );

        quizView.AnswerBButton
            .onClick
            .AddListener(
                () => SelectAnswer(1)
            );

        quizView.AnswerCButton
            .onClick
            .AddListener(
                () => SelectAnswer(2)
            );

        quizView.AnswerDButton
            .onClick
            .AddListener(
                () => SelectAnswer(3)
            );

        quizView.ExitButton
            .onClick
            .AddListener(ExitQuiz);

        quizView.Hide();
    }

    public void ShowQuestion(
        LoadedPackageModel package,
        QuizModel quiz,
        QuestionModel question,
        int currentQuestionNumber,
        int totalQuestions,
        Action answeredCallback,
        Action exitCallback)
    {
        if (package == null ||
            quiz == null ||
            question == null)
        {
            Debug.LogError(
                "No fue posible mostrar la pregunta."
            );

            return;
        }

        if (question.respuestas == null ||
            question.respuestas.Count != 4)
        {
            Debug.LogError(
                $"La pregunta {question.id} no tiene 4 respuestas."
            );

            return;
        }

        currentPackage = package;
        currentQuiz = quiz;
        currentQuestion = question;

        onAnswered =
            answeredCallback;

        onExit =
            exitCallback;

        quizView.SetTitle(
            quiz.nombre
        );

        quizView.SetQuestion(
            question.texto
        );

        quizView.SetProgress(
            currentQuestionNumber,
            totalQuestions
        );

        for (int i = 0; i < 4; i++)
        {
            quizView.SetAnswerText(
                i,
                question.respuestas[i].texto
            );
        }

        Sprite sprite = null;

        if (!string.IsNullOrWhiteSpace(
                question.imagen))
        {
            sprite =
                imageLoader.LoadSprite(
                    package.rootPath,
                    question.imagen
                );
        }

        quizView.SetImage(
            sprite
        );

        quizView.SetAnswersInteractable(
            true
        );

        quizView.Show();

        Debug.Log(
            $"Mostrando pregunta: {question.id}"
        );
    }

    private void SelectAnswer(
        int answerIndex)
    {
        if (currentQuestion == null)
        {
            return;
        }

        if (answerIndex < 0 ||
            answerIndex >=
            currentQuestion.respuestas.Count)
        {
            return;
        }

        AnswerModel selectedAnswer =
            currentQuestion
                .respuestas[answerIndex];

        quizView.SetAnswersInteractable(
            false
        );

        if (selectedAnswer.correcta)
        {
            if (AppController.Instance != null)
            {
                AppController.Instance
                    .AddPoint();
            }

            Debug.Log(
                $"Respuesta correcta: {selectedAnswer.texto}"
            );
        }
        else
        {
            Debug.Log(
                $"Respuesta incorrecta: {selectedAnswer.texto}"
            );
        }

        Action callback =
            onAnswered;

        ClearQuestion();

        quizView.Hide();

        callback?.Invoke();
    }

    private void ExitQuiz()
    {
        Action callback =
            onExit;

        ClearQuestion();

        quizView.Hide();

        callback?.Invoke();
    }

    public void Hide()
    {
        quizView.Hide();

        ClearQuestion();
    }

    private void ClearQuestion()
    {
        currentPackage = null;
        currentQuiz = null;
        currentQuestion = null;

        onAnswered = null;
        onExit = null;
    }
}