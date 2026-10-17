using System.Collections.Generic;
using UnityEngine;

public class MainMenuView : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField]
    private GameObject mainPanel;

    [SerializeField]
    private GameObject testsPanel;

    [SerializeField]
    private GameObject storiesPanel;

    [SerializeField]
    private GameObject storyPanel;

    [SerializeField]
    private GameObject quizPanel;

    [Header("Containers")]
    [SerializeField]
    private Transform testsContainer;

    [SerializeField]
    private Transform storiesContainer;

    [Header("Prefab")]
    [SerializeField]
    private GameObject contentCardPrefab;

    [Header("Controllers")]
    [SerializeField]
    private StoryController storyController;

    [SerializeField]
    private SequenceController sequenceController;

    private PackageLoaderService packageLoader;
    private ImageLoaderService imageLoader;

    private List<LoadedPackageModel> loadedPackages =
        new List<LoadedPackageModel>();

    private void Start()
    {
        packageLoader =
            new PackageLoaderService();

        imageLoader =
            new ImageLoaderService();

        LoadPackages();

        ShowMainMenu();
    }

    /*
     * Carga todos los paquetes STEM
     * instalados en persistentDataPath.
     */
    private void LoadPackages()
    {
        loadedPackages =
            packageLoader.LoadAllPackages();

        Debug.Log(
            $"Paquetes cargados para el menú: {loadedPackages.Count}"
        );
    }

    /*
     * PANTALLA PRINCIPAL
     *
     * Test Vocacional
     * Historias
     * Minijuegos
     */
    public void ShowMainMenu()
    {
        HideAllPanels();

        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
        }
    }

    /*
     * Muestra todos los tests vocacionales
     * encontrados en todos los paquetes.
     */
    public void ShowTests()
    {
        HideAllPanels();

        if (testsPanel != null)
        {
            testsPanel.SetActive(true);
        }

        ClearContainer(
            testsContainer
        );

        int totalTests = 0;

        foreach (
            LoadedPackageModel package
            in loadedPackages)
        {
            if (package == null)
            {
                continue;
            }

            if (package.cuestionarios == null)
            {
                continue;
            }

            foreach (
                QuizModel quiz
                in package.cuestionarios)
            {
                if (quiz == null)
                {
                    continue;
                }

                CreateQuizCard(
                    package,
                    quiz
                );

                totalTests++;
            }
        }

        Debug.Log(
            $"Tests vocacionales disponibles: {totalTests}"
        );
    }

    /*
     * Muestra todas las historias
     * disponibles en los paquetes.
     */
    public void ShowStories()
    {
        HideAllPanels();

        if (storiesPanel != null)
        {
            storiesPanel.SetActive(true);
        }

        ClearContainer(
            storiesContainer
        );

        int totalStories = 0;

        foreach (
            LoadedPackageModel package
            in loadedPackages)
        {
            if (package == null)
            {
                continue;
            }

            if (package.historias == null)
            {
                continue;
            }

            foreach (
                StoryModel story
                in package.historias)
            {
                if (story == null)
                {
                    continue;
                }

                CreateStoryCard(
                    package,
                    story
                );

                totalStories++;
            }
        }

        Debug.Log(
            $"Historias disponibles: {totalStories}"
        );
    }

    /*
     * Oculta absolutamente todos los paneles.
     *
     * Esto evita que QuizPanel o StoryPanel
     * queden visibles accidentalmente.
     */
    public void HideAllPanels()
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
        }

        if (testsPanel != null)
        {
            testsPanel.SetActive(false);
        }

        if (storiesPanel != null)
        {
            storiesPanel.SetActive(false);
        }

        if (storyPanel != null)
        {
            storyPanel.SetActive(false);
        }

        if (quizPanel != null)
        {
            quizPanel.SetActive(false);
        }
    }

    /*
     * Crea una tarjeta visual
     * para un Test Vocacional.
     */
    private void CreateQuizCard(
        LoadedPackageModel package,
        QuizModel quiz)
    {
        if (contentCardPrefab == null)
        {
            Debug.LogError(
                "No se asignó ContentCardPrefab en MainMenuView."
            );

            return;
        }

        if (testsContainer == null)
        {
            Debug.LogError(
                "No se asignó TestsContainer en MainMenuView."
            );

            return;
        }

        GameObject cardObject =
            Instantiate(
                contentCardPrefab,
                testsContainer
            );

        ContentCardView card =
            cardObject
                .GetComponent<ContentCardView>();

        if (card == null)
        {
            Debug.LogError(
                "El prefab ContentCard no tiene ContentCardView."
            );

            Destroy(
                cardObject
            );

            return;
        }

        /*
         * Nombre del test.
         */
        if (card.TitleText != null)
        {
            card.TitleText.text =
                quiz.nombre;
        }

        /*
         * Imagen de portada del quiz.
         */
        Sprite sprite = null;

        if (!string.IsNullOrWhiteSpace(
                quiz.imagen))
        {
            sprite =
                imageLoader.LoadSprite(
                    package.rootPath,
                    quiz.imagen
                );
        }

        card.SetImage(
            sprite
        );

        /*
         * Click de la tarjeta.
         */
        if (card.Button != null)
        {
            card.Button
                .onClick
                .AddListener(
                    () =>
                        OpenQuiz(
                            package,
                            quiz
                        )
                );
        }
    }

    /*
     * Crea una tarjeta visual
     * para una historia.
     */
    private void CreateStoryCard(
        LoadedPackageModel package,
        StoryModel story)
    {
        if (contentCardPrefab == null)
        {
            Debug.LogError(
                "No se asignó ContentCardPrefab en MainMenuView."
            );

            return;
        }

        if (storiesContainer == null)
        {
            Debug.LogError(
                "No se asignó StoriesContainer en MainMenuView."
            );

            return;
        }

        GameObject cardObject =
            Instantiate(
                contentCardPrefab,
                storiesContainer
            );

        ContentCardView card =
            cardObject
                .GetComponent<ContentCardView>();

        if (card == null)
        {
            Debug.LogError(
                "El prefab ContentCard no tiene ContentCardView."
            );

            Destroy(
                cardObject
            );

            return;
        }

        /*
         * Nombre de la historia.
         */
        if (card.TitleText != null)
        {
            card.TitleText.text =
                story.nombre;
        }

        /*
         * Tomamos la primera imagen
         * disponible entre las escenas.
         */
        string imagePath =
            GetFirstStoryImage(
                story
            );

        Sprite sprite = null;

        if (!string.IsNullOrWhiteSpace(
                imagePath))
        {
            sprite =
                imageLoader.LoadSprite(
                    package.rootPath,
                    imagePath
                );
        }

        card.SetImage(
            sprite
        );

        /*
         * Click de la tarjeta.
         */
        if (card.Button != null)
        {
            card.Button
                .onClick
                .AddListener(
                    () =>
                        OpenStory(
                            package,
                            story
                        )
                );
        }
    }

    /*
     * Busca la primera imagen disponible
     * dentro de las escenas de una historia.
     */
    private string GetFirstStoryImage(
        StoryModel story)
    {
        if (story == null)
        {
            return null;
        }

        if (story.escenas == null)
        {
            return null;
        }

        if (story.escenas.Count == 0)
        {
            return null;
        }

        foreach (
            StorySceneModel scene
            in story.escenas)
        {
            if (scene == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(
                    scene.imagen))
            {
                return scene.imagen;
            }
        }

        return null;
    }

    /*
     * Abre una historia directamente
     * desde el menú de historias.
     */

private void OpenStory(
    LoadedPackageModel package,
    StoryModel story)
{
    if (AppController.Instance == null)
    {
        Debug.LogError(
            "No existe un AppController activo."
        );

        return;
    }

    if (storyController == null)
    {
        Debug.LogError(
            "StoryController no está asignado."
        );

        return;
    }

    AppController.Instance
        .SelectPackage(
            package
        );

    Debug.Log(
        $"Abriendo historia independiente: {story.nombre}"
    );

    storyController
        .OpenStandaloneStory(
            package,
            story
        );
}

    /*
     * Inicia un Test Vocacional.
     */
    private void OpenQuiz(
        LoadedPackageModel package,
        QuizModel quiz)
    {
        if (AppController.Instance == null)
        {
            Debug.LogError(
                "No existe un AppController activo."
            );

            return;
        }

        if (sequenceController == null)
        {
            Debug.LogError(
                "SequenceController no está asignado en MainMenuView."
            );

            return;
        }

        /*
         * Guardamos el paquete actual.
         */
        AppController.Instance
            .SelectPackage(
                package
            );

        /*
         * Inicializamos:
         *
         * CurrentQuiz
         * Score = 0
         * CurrentSequenceIndex = 0
         * QuizStartTime
         */
        AppController.Instance
            .StartQuiz(
                quiz
            );

        Debug.Log(
            $"Iniciando Test Vocacional: {quiz.nombre}"
        );

        /*
         * IMPORTANTE:
         *
         * NO abrimos QuizPanel directamente.
         *
         * La secuencia del JSON decide qué debe
         * aparecer primero.
         *
         * Podría ser:
         *
         * historia
         * pregunta
         * video
         * minijuego
         */
        sequenceController
            .StartSequence();
    }

    /*
     * Elimina las tarjetas anteriores
     * antes de volver a crearlas.
     */
    private void ClearContainer(
        Transform container)
    {
        if (container == null)
        {
            return;
        }

        for (
            int i =
                container.childCount - 1;

            i >= 0;

            i--)
        {
            Destroy(
                container
                    .GetChild(i)
                    .gameObject
            );
        }
    }

    /*
     * Se puede usar después cuando instalemos
     * nuevos ZIP sin reiniciar la aplicación.
     */
    public void ReloadContent()
    {
        LoadPackages();

        ShowMainMenu();

        Debug.Log(
            "Contenido recargado."
        );
    }
}