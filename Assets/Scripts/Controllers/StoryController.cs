using System;
using UnityEngine;

public class StoryController : MonoBehaviour
{
    // ========================================
    // VISTAS
    // ========================================

    [Header("Vistas")]

    [SerializeField]
    private StoryView storyView;

    [SerializeField]
    private MainMenuView mainMenuView;

    // ========================================
    // SONIDO DE CAMBIO DE PÁGINA
    // ========================================

    [Header("Sonido de cambio de página")]

    [SerializeField]
    private AudioSource pageTurnAudioSource;

    [SerializeField]
    private AudioClip pageTurnSound;

    [Range(0f, 1f)]
    [SerializeField]
    private float pageTurnVolume = 1f;

    // ========================================
    // SERVICIOS
    // ========================================

    private ImageLoaderService imageLoader;

    // ========================================
    // DATOS DE LA HISTORIA
    // ========================================

    private LoadedPackageModel currentPackage;

    private StoryModel currentStory;

    private int currentSceneIndex;

    // ========================================
    // CONTROL DE NAVEGACIÓN
    // ========================================

    private bool openedFromQuiz;

    private Action onStoryFinished;

    // ========================================
    // INICIALIZACIÓN
    // ========================================

    private void Awake()
    {
        // Inicializar servicio de imágenes
        imageLoader = new ImageLoaderService();

        // Validar vista
        if (storyView == null)
        {
            Debug.LogError(
                "StoryView no está asignado.",
                this
            );

            return;
        }

        // Configurar botón anterior
        if (storyView.PreviousButton != null)
        {
            storyView.PreviousButton.onClick.AddListener(
                PreviousScene
            );
        }

        // Configurar botón siguiente
        if (storyView.NextButton != null)
        {
            storyView.NextButton.onClick.AddListener(
                NextScene
            );
        }

        // Configurar botón salir
        if (storyView.ExitButton != null)
        {
            storyView.ExitButton.onClick.AddListener(
                ExitStory
            );
        }

        // Configurar audio
        if (pageTurnAudioSource != null)
        {
            pageTurnAudioSource.playOnAwake = false;

            pageTurnAudioSource.loop = false;

            pageTurnAudioSource.spatialBlend = 0f;
        }

        // Ocultar historia al iniciar
        storyView.Hide();
    }

    // ========================================
    // ABRIR HISTORIA INDEPENDIENTE
    // ========================================

    public void OpenStandaloneStory(
        LoadedPackageModel package,
        StoryModel story)
    {
        OpenStory(
            package,
            story,
            false,
            null
        );
    }

    // ========================================
    // ABRIR HISTORIA DESDE UN QUIZ
    // ========================================

    public void OpenStoryFromQuiz(
        LoadedPackageModel package,
        StoryModel story,
        Action finishedCallback)
    {
        OpenStory(
            package,
            story,
            true,
            finishedCallback
        );
    }

    // ========================================
    // ABRIR HISTORIA
    // ========================================

    public void OpenStory(
        LoadedPackageModel package,
        StoryModel story)
    {
        OpenStandaloneStory(
            package,
            story
        );
    }

    // ========================================
    // ABRIR HISTORIA CON CONFIGURACIÓN
    // ========================================

    public void OpenStory(
        LoadedPackageModel package,
        StoryModel story,
        bool fromQuiz,
        Action finishedCallback)
    {
        if (storyView == null)
        {
            Debug.LogError(
                "No es posible abrir la historia: " +
                "StoryView no está asignado."
            );

            return;
        }

        if (package == null || story == null)
        {
            Debug.LogError(
                "No fue posible abrir la historia."
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

        // Validar escenas
        foreach (StorySceneModel scene in story.escenas)
        {
            if (scene == null)
            {
                Debug.LogError(
                    "La historia contiene una escena inválida."
                );

                return;
            }
        }

        // Guardar datos
        currentPackage = package;

        currentStory = story;

        openedFromQuiz = fromQuiz;

        onStoryFinished = finishedCallback;

        // Iniciar desde la primera página
        currentSceneIndex = 0;

        // Garantizar el orden de las escenas
        currentStory.escenas.Sort(
            (a, b) => a.orden.CompareTo(b.orden)
        );

        // Ocultar los demás paneles
        if (mainMenuView != null)
        {
            mainMenuView.HideAllPanels();
        }

        // Mostrar panel de historia
        storyView.Show();

        // Mostrar primera escena
        ShowCurrentScene();

        Debug.Log(
            "Historia iniciada: " +
            currentStory.nombre
        );
    }

    // ========================================
    // MOSTRAR ESCENA ACTUAL
    // ========================================

    private void ShowCurrentScene()
    {
        if (currentStory == null ||
            currentPackage == null)
        {
            return;
        }

        if (currentSceneIndex < 0 ||
            currentSceneIndex >= currentStory.escenas.Count)
        {
            Debug.LogError(
                "Índice de escena fuera de rango."
            );

            return;
        }

        StorySceneModel scene =
            currentStory.escenas[currentSceneIndex];

        // ========================================
        // MOSTRAR TÍTULO
        // ========================================

        storyView.SetTitle(
            currentStory.nombre
        );

        // ========================================
        // MOSTRAR TEXTO
        // ========================================

        storyView.SetText(
            scene.texto
        );

        // ========================================
        // MOSTRAR PROGRESO
        // ========================================

        storyView.SetProgress(
            currentSceneIndex + 1,
            currentStory.escenas.Count
        );

        // ========================================
        // CONFIGURAR BOTÓN ANTERIOR
        // ========================================

        storyView.SetPreviousEnabled(
            currentSceneIndex > 0
        );

        // ========================================
        // CONFIGURAR BOTÓN SIGUIENTE
        // ========================================

        bool isLastScene =
            currentSceneIndex ==
            currentStory.escenas.Count - 1;

        if (isLastScene)
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

        // ========================================
        // CARGAR IMAGEN
        // ========================================

        Sprite sprite = null;

        if (!string.IsNullOrWhiteSpace(scene.imagen))
        {
            sprite = imageLoader.LoadSprite(
                currentPackage.rootPath,
                scene.imagen
            );
        }

        // Mostrar imagen
        storyView.SetImage(sprite);

        // ========================================
        // REGISTRO EN CONSOLA
        // ========================================

        Debug.Log(
            $"Historia: {currentStory.nombre} - " +
            $"Página {currentSceneIndex + 1}/" +
            $"{currentStory.escenas.Count}"
        );
    }

    // ========================================
    // SIGUIENTE PÁGINA
    // ========================================

    private void NextScene()
    {
        if (currentStory == null)
        {
            return;
        }

        // Comprobar si existen más páginas
        if (currentSceneIndex <
            currentStory.escenas.Count - 1)
        {
            // Avanzar
            currentSceneIndex++;

            // Reproducir sonido
            PlayPageTurnSound();

            // Mostrar nueva página
            ShowCurrentScene();

            return;
        }

        // Si estamos en la última página,
        // finalizar la historia.
        FinishStory();
    }

    // ========================================
    // PÁGINA ANTERIOR
    // ========================================

    private void PreviousScene()
    {
        if (currentStory == null)
        {
            return;
        }

        // No retroceder antes de la primera página
        if (currentSceneIndex <= 0)
        {
            return;
        }

        // Retroceder
        currentSceneIndex--;

        // Reproducir sonido
        PlayPageTurnSound();

        // Mostrar página anterior
        ShowCurrentScene();
    }

    // ========================================
    // REPRODUCIR SONIDO DE PÁGINA
    // ========================================

    private void PlayPageTurnSound()
    {
        if (pageTurnAudioSource == null ||
            pageTurnSound == null)
        {
            return;
        }

        // Detener el efecto anterior si
        // el usuario cambia rápidamente de página.
        pageTurnAudioSource.Stop();

        // Reproducir sonido configurado
        // desde el Inspector.
        pageTurnAudioSource.PlayOneShot(
            pageTurnSound,
            pageTurnVolume
        );
    }

    // ========================================
    // FINALIZAR HISTORIA
    // ========================================

    private void FinishStory()
    {
        if (currentStory == null)
        {
            return;
        }

        Debug.Log(
            "Historia finalizada: " +
            currentStory.nombre
        );

        // Guardar información antes de limpiarla
        bool wasOpenedFromQuiz = openedFromQuiz;

        Action finishedCallback = onStoryFinished;

        // Ocultar panel
        storyView.Hide();

        // Limpiar datos
        ClearStory();

        if (wasOpenedFromQuiz)
        {
            // Continuar secuencia del quiz
            // después de limpiar la historia anterior.
            if (finishedCallback != null)
            {
                finishedCallback.Invoke();
            }
            else if (mainMenuView != null)
            {
                mainMenuView.ShowMainMenu();
            }
        }
        else
        {
            // Historia independiente:
            // regresar al listado de historias.
            if (mainMenuView != null)
            {
                mainMenuView.ShowStories();
            }
        }
    }

    // ========================================
    // SALIR DE LA HISTORIA
    // ========================================

    private void ExitStory()
    {
        if (currentStory == null)
        {
            return;
        }

        bool wasOpenedFromQuiz = openedFromQuiz;

        Debug.Log(
            "Saliendo de la historia: " +
            currentStory.nombre
        );

        // Ocultar panel
        storyView.Hide();

        // Limpiar datos
        ClearStory();

        if (mainMenuView == null)
        {
            return;
        }

        if (wasOpenedFromQuiz)
        {
            // Al abandonar una historia dentro
            // de un quiz, volver al menú principal.
            mainMenuView.ShowMainMenu();
        }
        else
        {
            // Si era independiente, volver
            // al listado de historias.
            mainMenuView.ShowStories();
        }
    }

    // ========================================
    // LIMPIAR HISTORIA ACTUAL
    // ========================================

    private void ClearStory()
    {
        currentPackage = null;

        currentStory = null;

        currentSceneIndex = 0;

        openedFromQuiz = false;

        onStoryFinished = null;
    }

    // ========================================
    // LIMPIAR EVENTOS
    // ========================================

    private void OnDestroy()
    {
        if (storyView == null)
        {
            return;
        }

        if (storyView.PreviousButton != null)
        {
            storyView.PreviousButton.onClick.RemoveListener(
                PreviousScene
            );
        }

        if (storyView.NextButton != null)
        {
            storyView.NextButton.onClick.RemoveListener(
                NextScene
            );
        }

        if (storyView.ExitButton != null)
        {
            storyView.ExitButton.onClick.RemoveListener(
                ExitStory
            );
        }
    }
}