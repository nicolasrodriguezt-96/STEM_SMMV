using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuizResultView : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;

    [Header("Cinco estrellas del Canvas, de izquierda a derecha")]
    [SerializeField] private Image[] stars = new Image[5];

    [Header("Aspecto de las estrellas")]
    [Tooltip("Imagen de la estrella a color. Se reutiliza en las cinco Image.")]
    [SerializeField] private Sprite colorStarSprite;
    [Tooltip("Imagen de la misma estrella en blanco y negro. Recomendado para un gris real.")]
    [SerializeField] private Sprite grayscaleStarSprite;
    [SerializeField] private Color activeStarTint = Color.white;
    [SerializeField] private Color inactiveStarTint = Color.gray;

    [Header("Trofeo y textos")]
    [SerializeField] private Image trophyImage;
    [SerializeField] private TMP_Text quizNameText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text percentageText;
    [SerializeField] private TMP_Text elapsedTimeText;

    [Header("Navegación")]
    [SerializeField] private Button backToTestsButton;
    [SerializeField] private Button backToMainMenuButton;

    public Button BackToTestsButton => backToTestsButton;
    public Button BackToMainMenuButton => backToMainMenuButton;

    public void Show()
    {
        if (panel != null) panel.SetActive(true);
        else gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
        else gameObject.SetActive(false);
    }

    public void Render(
        string quizName,
        int correctAnswers,
        int totalQuestions,
        float elapsedSeconds,
        Sprite trophy,
        string title,
        string message)
    {
        int safeTotal = Mathf.Max(0, totalQuestions);
        int safeCorrect = Mathf.Clamp(correctAnswers, 0, safeTotal);

        float percentage = safeTotal > 0
            ? safeCorrect * 100f / safeTotal
            : 0f;

        // Tramos completos de 20%. Ej.: 5/10 -> 2 estrellas; 6/10 -> 3.
        int activeStars = safeTotal > 0
            ? Mathf.Clamp(Mathf.FloorToInt(safeCorrect * 5f / safeTotal), 0, 5)
            : 0;

        if (stars != null)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                Image star = stars[i];
                if (star == null) continue;

                bool isActive = i < activeStars;
                Sprite chosenSprite = isActive ? colorStarSprite : grayscaleStarSprite;
                if (chosenSprite != null)
                    star.sprite = chosenSprite;

                star.color = isActive ? activeStarTint : inactiveStarTint;
                star.enabled = true;
            }
        }

        if (trophyImage != null)
        {
            trophyImage.sprite = trophy;
            trophyImage.enabled = trophy != null;
            trophyImage.color = Color.white;
            trophyImage.preserveAspect = true;
        }

        if (quizNameText != null) quizNameText.text = quizName;
        if (titleText != null) titleText.text = title;
        if (messageText != null) messageText.text = message;
        if (scoreText != null) scoreText.text = $"Respuestas correctas: {safeCorrect} de {safeTotal}";
        if (percentageText != null) percentageText.text = $"{percentage:0.#}%";
        if (elapsedTimeText != null)
        {
            int duration = Mathf.Max(0, Mathf.FloorToInt(elapsedSeconds));
            elapsedTimeText.text = $"Tiempo: {duration / 60:00}:{duration % 60:00}";
        }
    }
}
