using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuizView : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text progressText;

    [Header("Image")]
    [SerializeField] private Image questionImage;

    [Header("Answers")]
    [SerializeField] private Button answerAButton;
    [SerializeField] private Button answerBButton;
    [SerializeField] private Button answerCButton;
    [SerializeField] private Button answerDButton;

    [Header("Navigation")]
    [SerializeField] private Button exitButton;

    private Button[] answerButtons;
    private Image[] answerImages;
    private Color[] initialColors;
    private Selectable.Transition[] initialTransitions;
    private bool initialized;

    public Button AnswerAButton => answerAButton;
    public Button AnswerBButton => answerBButton;
    public Button AnswerCButton => answerCButton;
    public Button AnswerDButton => answerDButton;
    public Button ExitButton => exitButton;

    public void Show()
    {
        if (panel != null) panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void SetTitle(string title)
    {
        if (titleText != null) titleText.text = title;
    }

    public void SetQuestion(string question)
    {
        if (questionText != null) questionText.text = question;
    }

    public void SetProgress(int current, int total)
    {
        if (progressText != null) progressText.text = $"{current} / {total}";
    }

    public void SetImage(Sprite sprite)
    {
        if (questionImage == null) return;
        questionImage.sprite = sprite;
        questionImage.enabled = sprite != null;
        questionImage.preserveAspect = true;
    }

    public void SetAnswerText(int index, string text)
    {
        Button button = GetAnswerButton(index);
        if (button == null) return;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        if (label != null) label.text = text;
    }

    public void SetAnswersInteractable(bool interactable)
    {
        CacheButtons();
        foreach (Button button in answerButtons)
        {
            if (button != null) button.interactable = interactable;
        }
    }

    // Rojo para la opción incorrecta; verde para la correcta.
    public void ShowAnswerFeedback(int selectedIndex, int correctIndex,
        Color correctColor, Color incorrectColor)
    {
        CacheButtons();

        for (int i = 0; i < answerButtons.Length; i++)
        {
            Button button = answerButtons[i];
            if (button == null) continue;

            // Desactiva temporalmente el tint automático del botón.
            button.transition = Selectable.Transition.None;
            if (button.targetGraphic != null)
                button.targetGraphic.CrossFadeColor(Color.white, 0f, true, true);

            if (answerImages[i] != null)
            {
                answerImages[i].color = i == correctIndex
                    ? correctColor
                    : (i == selectedIndex ? incorrectColor : initialColors[i]);
            }

            button.interactable = false;
        }
    }

    // Devuelve colores, transiciones y botones a su estado original.
    public void ResetAnswerStyles()
    {
        CacheButtons();

        for (int i = 0; i < answerButtons.Length; i++)
        {
            Button button = answerButtons[i];
            if (button == null) continue;

            button.transition = Selectable.Transition.None;

            if (answerImages[i] != null)
                answerImages[i].color = initialColors[i];

            if (button.targetGraphic != null)
                button.targetGraphic.CrossFadeColor(Color.white, 0f, true, true);

            button.transition = initialTransitions[i];
            button.interactable = true;
        }
    }

    private Button GetAnswerButton(int index)
    {
        CacheButtons();
        return index >= 0 && index < answerButtons.Length
            ? answerButtons[index]
            : null;
    }

    private void CacheButtons()
    {
        if (initialized) return;

        answerButtons = new[]
        {
            answerAButton, answerBButton, answerCButton, answerDButton
        };

        answerImages = new Image[answerButtons.Length];
        initialColors = new Color[answerButtons.Length];
        initialTransitions = new Selectable.Transition[answerButtons.Length];

        for (int i = 0; i < answerButtons.Length; i++)
        {
            Button button = answerButtons[i];
            if (button == null) continue;

            answerImages[i] = button.targetGraphic as Image;
            if (answerImages[i] == null)
                answerImages[i] = button.GetComponent<Image>();

            initialColors[i] = answerImages[i] != null
                ? answerImages[i].color
                : Color.white;
            initialTransitions[i] = button.transition;
        }

        initialized = true;
    }
}
