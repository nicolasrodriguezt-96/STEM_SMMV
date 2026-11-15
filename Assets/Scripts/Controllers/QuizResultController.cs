using System;
using UnityEngine;

// ============================================
// CONFIGURACIÓN DE CADA NIVEL DE RECOMPENSA
// ============================================

[Serializable]
public class QuizResultTier
{
    [Header("Imagen del trofeo")]
    public Sprite trophy;

    [Header("Textos")]
    public string title;

    [TextArea(2, 5)]
    public string message;

    [Header("Sonido")]
    public AudioClip sound;
}

// ============================================
// CONTROLADOR DE RESULTADOS
// ============================================

[RequireComponent(typeof(AudioSource))]
public class QuizResultController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField]
    private QuizResultView resultView;

    [SerializeField]
    private MainMenuView mainMenuView;

    [SerializeField]
    private AudioSource audioSource;

    // ========================================
    // CONFIGURACIÓN DE NIVELES
    // ========================================

    [Header("Nivel 1: 0% a 50%")]
    [SerializeField]
    private QuizResultTier tier1 = new QuizResultTier();

    [Header("Nivel 2: más de 50% hasta 70%")]
    [SerializeField]
    private QuizResultTier tier2 = new QuizResultTier();

    [Header("Nivel 3: más de 70% hasta 90%")]
    [SerializeField]
    private QuizResultTier tier3 = new QuizResultTier();

    [Header("Nivel 4: más de 90% hasta 100%")]
    [SerializeField]
    private QuizResultTier tier4 = new QuizResultTier();

    [Header("Configuración de audio")]
    [SerializeField, Range(0f, 1f)]
    private float soundVolume = 1f;

    // ========================================
    // INICIALIZACIÓN
    // ========================================

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }

        if (resultView == null)
        {
            Debug.LogError(
                "QuizResultView no está asignado.",
                this
            );

            return;
        }

        // Botón para volver a los cuestionarios
        if (resultView.BackToTestsButton != null)
        {
            resultView.BackToTestsButton.onClick
                .AddListener(BackToTests);
        }

        // Botón para volver al menú principal
        if (resultView.BackToMainMenuButton != null)
        {
            resultView.BackToMainMenuButton.onClick
                .AddListener(BackToMainMenu);
        }

        resultView.Hide();
    }

    // ========================================
    // MOSTRAR RESULTADO DEL CUESTIONARIO
    // ========================================

    public void ShowResult(
        string quizName,
        int correctAnswers,
        int totalQuestions,
        float elapsedSeconds)
    {
        if (resultView == null)
        {
            Debug.LogError(
                "No se puede mostrar el resultado: falta QuizResultView.",
                this
            );

            if (mainMenuView != null)
            {
                mainMenuView.ShowTests();
            }

            return;
        }

        // Evitar valores inválidos
        int safeTotal = Mathf.Max(
            0,
            totalQuestions
        );

        int safeCorrect = Mathf.Clamp(
            correctAnswers,
            0,
            safeTotal
        );

        // Calcular porcentaje
        float percentage = safeTotal > 0
            ? safeCorrect * 100f / safeTotal
            : 0f;

        // Determinar nivel de recompensa
        QuizResultTier selectedTier =
            GetTier(percentage);

        // Ocultar pantallas anteriores
        if (mainMenuView != null)
        {
            mainMenuView.HideAllPanels();
        }

        // Mostrar información
        resultView.Render(
            quizName,
            safeCorrect,
            safeTotal,
            elapsedSeconds,
            selectedTier.trophy,
            selectedTier.title,
            selectedTier.message
        );

        // Activar panel
        resultView.Show();

        // Reproducir sonido del nivel
        PlayResultSound(selectedTier.sound);

        Debug.Log(
            $"RESULTADO: {quizName} | " +
            $"Correctas: {safeCorrect}/{safeTotal} | " +
            $"Porcentaje: {percentage:0.#}%"
        );
    }

    // ========================================
    // SELECCIONAR NIVEL SEGÚN PORCENTAJE
    // ========================================

    private QuizResultTier GetTier(float percentage)
    {
        if (percentage <= 50f)
        {
            return tier1;
        }

        if (percentage <= 70f)
        {
            return tier2;
        }

        if (percentage <= 90f)
        {
            return tier3;
        }

        return tier4;
    }

    // ========================================
    // REPRODUCCIÓN DE AUDIO
    // ========================================

    private void PlayResultSound(AudioClip clip)
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.Stop();

        if (clip != null)
        {
            audioSource.PlayOneShot(
                clip,
                soundVolume
            );
        }
    }

    // ========================================
    // VOLVER A LOS CUESTIONARIOS
    // ========================================

    public void BackToTests()
    {
        CloseResult();

        if (mainMenuView != null)
        {
            mainMenuView.ShowTests();
        }
    }

    // ========================================
    // VOLVER AL MENÚ PRINCIPAL
    // ========================================

    public void BackToMainMenu()
    {
        CloseResult();

        if (mainMenuView != null)
        {
            mainMenuView.ShowMainMenu();
        }
    }

    // ========================================
    // CERRAR RESULTADOS
    // ========================================

    private void CloseResult()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        if (resultView != null)
        {
            resultView.Hide();
        }
    }
}
