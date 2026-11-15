using System.Collections.Generic;
using UnityEngine;

public class MainMenuView : MonoBehaviour
{
    // ========================================
    // PANELES
    // ========================================

    [Header("Paneles principales")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject testsPanel;
    [SerializeField] private GameObject storiesPanel;

    [Header("Paneles de contenido")]
    [SerializeField] private GameObject storyPanel;
    [SerializeField] private GameObject quizPanel;
    [SerializeField] private GameObject resultPanel;

    [Header("Paneles del test vocacional")]
    [SerializeField] private GameObject vocationalTestPanel;
    [SerializeField] private GameObject vocationalResultPanel;

    // ========================================
    // CONTENEDORES
    // ========================================

    [Header("Contenedores")]
    [SerializeField] private Transform testsContainer;
    [SerializeField] private Transform storiesContainer;

    [Header("Prefab de tarjetas")]
    [SerializeField] private GameObject contentCardPrefab;

    // ========================================
    // CONTROLADORES
    // ========================================

    [Header("Controladores")]
    [SerializeField] private StoryController storyController;
    [SerializeField] private SequenceController sequenceController;

    [Header("Controlador vocacional")]
    [SerializeField]
    private VocationalTestController vocationalTestController;

    // ========================================
    // SERVICIOS
    // ========================================

    private PackageLoaderService packageLoader;
    private ImageLoaderService imageLoader;

    private List<LoadedPackageModel> loadedPackages =
        new List<LoadedPackageModel>();

    // ========================================
    // INICIALIZACIÓN
    // ========================================

    private void Awake()
    {
        // Evitar que los paneles vocacionales
        // aparezcan al iniciar la escena.

        if (vocationalTestPanel != null)
        {
            vocationalTestPanel.SetActive(false);
        }

        if (vocationalResultPanel != null)
        {
            vocationalResultPanel.SetActive(false);
        }
    }

    private void Start()
    {
        packageLoader = new PackageLoaderService();

        imageLoader = new ImageLoaderService();

        LoadPackages();

        ShowMainMenu();
    }

    // ========================================
    // CARGAR PAQUETES
    // ========================================

    private void LoadPackages()
    {
        loadedPackages = packageLoader.LoadAllPackages();

        if (loadedPackages == null)
        {
            loadedPackages = new List<LoadedPackageModel>();
        }

        Debug.Log(
            $"Paquetes cargados: {loadedPackages.Count}"
        );
    }

    // ========================================
    // MENÚ PRINCIPAL
    // ========================================

    public void ShowMainMenu()
    {
        HideAllPanels();

        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
        }
    }

    // ========================================
    // MOSTRAR CUESTIONARIOS NORMALES
    // ========================================

    public void ShowTests()
    {
        HideAllPanels();

        if (testsPanel != null)
        {
            testsPanel.SetActive(true);
        }

        ClearContainer(testsContainer);

        int totalTests = 0;

        foreach (LoadedPackageModel package in loadedPackages)
        {
            if (package == null ||
                package.cuestionarios == null)
            {
                continue;
            }

            foreach (QuizModel quiz in package.cuestionarios)
            {
                if (quiz == null)
                {
                    continue;
                }

                CreateQuizCard(package, quiz);

                totalTests++;
            }
        }

        Debug.Log(
            $"Retos de conocimiento disponibles: {totalTests}"
        );
    }

    // ========================================
    // ABRIR EL TEST VOCACIONAL
    // ========================================

    public void OpenVocationalTest()
    {
        if (vocationalTestController == null)
        {
            Debug.LogError(
                "No se ha asignado VocationalTestController."
            );

            return;
        }

        if (loadedPackages == null)
        {
            Debug.LogError(
                "No se han cargado los paquetes."
            );

            return;
        }

        LoadedPackageModel selectedPackage = null;

        int totalVocationalTests = 0;

        foreach (LoadedPackageModel package in loadedPackages)
        {
            if (package == null ||
                package.testVocacional == null)
            {
                continue;
            }

            totalVocationalTests++;

            selectedPackage = package;
        }

        if (totalVocationalTests == 0)
        {
            Debug.LogWarning(
                "No hay ningún test vocacional instalado."
            );

            return;
        }

        if (totalVocationalTests > 1)
        {
            Debug.LogError(
                "Hay más de un test vocacional instalado. " +
                "Soluciona el conflicto antes de continuar."
            );

            return;
        }

        Debug.Log(
            "Abriendo test vocacional: " +
            selectedPackage.testVocacional.nombre
        );

        HideAllPanels();

        vocationalTestController.StartTest(
            selectedPackage
        );
    }

    // ========================================
    // MOSTRAR HISTORIAS
    // ========================================

    public void ShowStories()
    {
        HideAllPanels();

        if (storiesPanel != null)
        {
            storiesPanel.SetActive(true);
        }

        ClearContainer(storiesContainer);

        int totalStories = 0;

        foreach (LoadedPackageModel package in loadedPackages)
        {
            if (package == null ||
                package.historias == null)
            {
                continue;
            }

            foreach (StoryModel story in package.historias)
            {
                if (story == null)
                {
                    continue;
                }

                CreateStoryCard(package, story);

                totalStories++;
            }
        }

        Debug.Log(
            $"Historias disponibles: {totalStories}"
        );
    }

    // ========================================
    // OCULTAR TODOS LOS PANELES
    // ========================================

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

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        // NUEVOS PANELES VOCACIONALES

        if (vocationalTestPanel != null)
        {
            vocationalTestPanel.SetActive(false);
        }

        if (vocationalResultPanel != null)
        {
            vocationalResultPanel.SetActive(false);
        }
    }

    // ========================================
    // CREAR TARJETA DE CUESTIONARIO
    // ========================================

    private void CreateQuizCard(
        LoadedPackageModel package,
        QuizModel quiz)
    {
        if (contentCardPrefab == null)
        {
            Debug.LogError(
                "ContentCardPrefab no está asignado."
            );

            return;
        }

        if (testsContainer == null)
        {
            Debug.LogError(
                "TestsContainer no está asignado."
            );

            return;
        }

        GameObject cardObject = Instantiate(
            contentCardPrefab,
            testsContainer
        );

        ContentCardView card =
            cardObject.GetComponent<ContentCardView>();

        if (card == null)
        {
            Debug.LogError(
                "El prefab no tiene ContentCardView."
            );

            Destroy(cardObject);

            return;
        }

        if (card.TitleText != null)
        {
            card.TitleText.text = quiz.nombre;
        }

        Sprite sprite = null;

        if (!string.IsNullOrWhiteSpace(quiz.imagen))
        {
            sprite = imageLoader.LoadSprite(
                package.rootPath,
                quiz.imagen
            );
        }

        card.SetImage(sprite);

        if (card.Button != null)
        {
            card.Button.onClick.AddListener(
                () => OpenQuiz(package, quiz)
            );
        }
    }

    // ========================================
    // CREAR TARJETA DE HISTORIA
    // ========================================

    private void CreateStoryCard(
        LoadedPackageModel package,
        StoryModel story)
    {
        if (contentCardPrefab == null)
        {
            Debug.LogError(
                "ContentCardPrefab no está asignado."
            );

            return;
        }

        if (storiesContainer == null)
        {
            Debug.LogError(
                "StoriesContainer no está asignado."
            );

            return;
        }

        GameObject cardObject = Instantiate(
            contentCardPrefab,
            storiesContainer
        );

        ContentCardView card =
            cardObject.GetComponent<ContentCardView>();

        if (card == null)
        {
            Debug.LogError(
                "El prefab no tiene ContentCardView."
            );

            Destroy(cardObject);

            return;
        }

        if (card.TitleText != null)
        {
            card.TitleText.text = story.nombre;
        }

        string imagePath = GetFirstStoryImage(story);

        Sprite sprite = null;

        if (!string.IsNullOrWhiteSpace(imagePath))
        {
            sprite = imageLoader.LoadSprite(
                package.rootPath,
                imagePath
            );
        }

        card.SetImage(sprite);

        if (card.Button != null)
        {
            card.Button.onClick.AddListener(
                () => OpenStory(package, story)
            );
        }
    }

    // ========================================
    // OBTENER PRIMERA IMAGEN DE HISTORIA
    // ========================================

    private string GetFirstStoryImage(StoryModel story)
    {
        if (story == null ||
            story.escenas == null ||
            story.escenas.Count == 0)
        {
            return null;
        }

        StorySceneModel firstWithImage = null;

        foreach (StorySceneModel scene in story.escenas)
        {
            if (scene == null ||
                string.IsNullOrWhiteSpace(scene.imagen))
            {
                continue;
            }

            if (firstWithImage == null ||
                scene.orden < firstWithImage.orden)
            {
                firstWithImage = scene;
            }
        }

        return firstWithImage != null
            ? firstWithImage.imagen
            : null;
    }

    // ========================================
    // ABRIR HISTORIA INDEPENDIENTE
    // ========================================

    private void OpenStory(
        LoadedPackageModel package,
        StoryModel story)
    {
        if (AppController.Instance == null)
        {
            Debug.LogError(
                "AppController no está activo."
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

        AppController.Instance.SelectPackage(package);

        Debug.Log(
            $"Abriendo historia: {story.nombre}"
        );

        storyController.OpenStandaloneStory(
            package,
            story
        );
    }

    // ========================================
    // ABRIR CUESTIONARIO NORMAL
    // ========================================

    private void OpenQuiz(
        LoadedPackageModel package,
        QuizModel quiz)
    {
        if (AppController.Instance == null)
        {
            Debug.LogError(
                "AppController no está activo."
            );

            return;
        }

        if (sequenceController == null)
        {
            Debug.LogError(
                "SequenceController no está asignado."
            );

            return;
        }

        AppController.Instance.SelectPackage(package);

        AppController.Instance.StartQuiz(quiz);

        Debug.Log(
            $"Iniciando cuestionario: {quiz.nombre}"
        );

        sequenceController.StartSequence();
    }

    // ========================================
    // ELIMINAR TARJETAS ANTERIORES
    // ========================================

    private void ClearContainer(Transform container)
    {
        if (container == null)
        {
            return;
        }

        for (int i = container.childCount - 1;
             i >= 0;
             i--)
        {
            Destroy(
                container.GetChild(i).gameObject
            );
        }
    }

    // ========================================
    // RECARGAR PAQUETES
    // ========================================

    public void ReloadContent()
    {
        if (packageLoader == null)
        {
            packageLoader = new PackageLoaderService();
        }

        if (imageLoader == null)
        {
            imageLoader = new ImageLoaderService();
        }

        LoadPackages();

        ShowMainMenu();

        Debug.Log(
            "Contenido STEM recargado correctamente."
        );
    }
}