using System;
using UnityEngine;

public class StoryController : MonoBehaviour
{
    [Header("Views")]
    [SerializeField]
    private StoryView storyView;

    [SerializeField]
    private MainMenuView mainMenuView;

    private ImageLoaderService imageLoader;

    private LoadedPackageModel currentPackage;
    private StoryModel currentStory;

    private int currentSceneIndex;

    private bool isStoryFromQuiz = false;

    private Action onStoryFinished;

    private void Awake()
    {
        imageLoader =
            new ImageLoaderService();

        if (storyView == null)
        {
            Debug.LogError(
                "StoryView no está asignado en StoryController."
            );

            return;
        }

        storyView.PreviousButton
            .onClick
            .AddListener(PreviousScene);

        storyView.NextButton
            .onClick
            .AddListener(NextScene);

        storyView.ExitButton
            .onClick
            .AddListener(ExitStory);

        storyView.Hide();
    }

    /*
     * =========================================
     * HISTORIA ABIERTA DESDE EL MENÚ HISTORIAS
     * =========================================
     */
    public void OpenStandaloneStory(
        LoadedPackageModel package,
        StoryModel story)
    {
        isStoryFromQuiz = false;

        onStoryFinished = null;

        OpenStoryInternal(
            package,
            story
        );
    }

    /*
     * =========================================
     * HISTORIA ABIERTA DENTRO DE UN QUIZ
     * =========================================
     */
    public void OpenStoryFromQuiz(
        LoadedPackageModel package,
        StoryModel story,
        Action finishedCallback)
    {
        isStoryFromQuiz = true;

        onStoryFinished =
            finishedCallback;

        OpenStoryInternal(
            package,
            story
        );
    }

    /*
     * =========================================
     * MÉTODO INTERNO
     * =========================================
     */
    private void OpenStoryInternal(
        LoadedPackageModel package,
        StoryModel story)
    {
        if (package == null)
        {
            Debug.LogError(
                "No se recibió un paquete válido."
            );

            return;
        }

        if (story == null)
        {
            Debug.LogError(
                "No se recibió una historia válida."
            );

            return;
        }

        if (story.escenas == null ||
            story.escenas.Count == 0)
        {
            Debug.LogError(
                $"La historia {story.nombre} no tiene escenas."
            );

            return;
        }

        currentPackage =
            package;

        currentStory =
            story;

        currentSceneIndex = 0;

        /*
         * Ordenamos las escenas
         * según el campo "orden".
         */
        currentStory.escenas.Sort(
            (a, b) =>
                a.orden.CompareTo(b.orden)
        );

        if (mainMenuView != null)
        {
            mainMenuView.HideAllPanels();
        }

        storyView.Show();

        ShowCurrentScene();
    }

    /*
     * =========================================
     * MOSTRAR ESCENA ACTUAL
     * =========================================
     */
    private void ShowCurrentScene()
    {
        if (currentStory == null)
        {
            return;
        }

        if (currentSceneIndex < 0 ||
            currentSceneIndex >=
            currentStory.escenas.Count)
        {
            return;
        }

        StorySceneModel scene =
            currentStory.escenas[
                currentSceneIndex
            ];

        storyView.SetTitle(
            currentStory.nombre
        );

        storyView.SetText(
            scene.texto
        );

        storyView.SetProgress(
            currentSceneIndex + 1,
            currentStory.escenas.Count
        );

        storyView.SetPreviousEnabled(
            currentSceneIndex > 0
        );

        if (
            currentSceneIndex ==
            currentStory.escenas.Count - 1
        )
        {
            storyView.SetNextText(
                "FINALIZAR"
            );
        }
        else
        {
            storyView.SetNextText(
                "SIGUIENTE"
            );
        }

        Sprite sprite = null;

        if (!string.IsNullOrWhiteSpace(
                scene.imagen))
        {
            sprite =
                imageLoader.LoadSprite(
                    currentPackage.rootPath,
                    scene.imagen
                );
        }

        storyView.SetImage(
            sprite
        );

        Debug.Log(
            $"Historia: {currentStory.nombre} - " +
            $"Escena {currentSceneIndex + 1}/" +
            $"{currentStory.escenas.Count}"
        );

        /*
         * Después agregaremos aquí
         * la reproducción del audio:
         *
         * scene.audio
         */
    }

    /*
     * =========================================
     * SIGUIENTE
     * =========================================
     */
    private void NextScene()
    {
        if (currentStory == null)
        {
            return;
        }

        if (
            currentSceneIndex <
            currentStory.escenas.Count - 1
        )
        {
            currentSceneIndex++;

            ShowCurrentScene();

            return;
        }

        FinishStory();
    }

    /*
     * =========================================
     * ANTERIOR
     * =========================================
     */
    private void PreviousScene()
    {
        if (currentStory == null)
        {
            return;
        }

        if (currentSceneIndex <= 0)
        {
            return;
        }

        currentSceneIndex--;

        ShowCurrentScene();
    }

    /*
     * =========================================
     * FINALIZAR HISTORIA
     * =========================================
     */
    private void FinishStory()
    {
        storyView.Hide();

        bool cameFromQuiz =
            isStoryFromQuiz;

        Action callback =
            onStoryFinished;

        ClearStory();

        /*
         * Si estaba dentro de un quiz:
         * continúa la secuencia.
         */
        if (cameFromQuiz)
        {
            callback?.Invoke();

            return;
        }

        /*
         * Si se abrió desde HISTORIAS:
         * vuelve a la lista de historias.
         */
        if (mainMenuView != null)
        {
            mainMenuView.ShowStories();
        }
    }

    /*
     * =========================================
     * SALIR MANUALMENTE
     * =========================================
     */
    private void ExitStory()
    {
        storyView.Hide();

        bool cameFromQuiz =
            isStoryFromQuiz;

        ClearStory();

        if (mainMenuView == null)
        {
            return;
        }

        /*
         * Si salió de una historia normal,
         * volvemos a HISTORIAS.
         */
        if (!cameFromQuiz)
        {
            mainMenuView.ShowStories();

            return;
        }

        /*
         * Si abandonó una historia dentro
         * de un quiz, por ahora volvemos
         * a la lista de tests.
         */
        if (AppController.Instance != null)
        {
            AppController.Instance
                .ResetApplication();
        }

        mainMenuView.ShowTests();
    }

    private void ClearStory()
    {
        currentPackage = null;
        currentStory = null;

        currentSceneIndex = 0;

        isStoryFromQuiz = false;

        onStoryFinished = null;
    }
}