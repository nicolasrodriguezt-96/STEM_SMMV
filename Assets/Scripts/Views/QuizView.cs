using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuizView : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField]
    private GameObject panel;

    [Header("Texts")]
    [SerializeField]
    private TMP_Text titleText;

    [SerializeField]
    private TMP_Text questionText;

    [SerializeField]
    private TMP_Text progressText;

    [Header("Image")]
    [SerializeField]
    private Image questionImage;

    [Header("Answers")]
    [SerializeField]
    private Button answerAButton;

    [SerializeField]
    private Button answerBButton;

    [SerializeField]
    private Button answerCButton;

    [SerializeField]
    private Button answerDButton;

    [Header("Navigation")]
    [SerializeField]
    private Button exitButton;

    public Button AnswerAButton => answerAButton;
    public Button AnswerBButton => answerBButton;
    public Button AnswerCButton => answerCButton;
    public Button AnswerDButton => answerDButton;
    public Button ExitButton => exitButton;

    public void Show()
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }
    }

    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    public void SetTitle(string title)
    {
        if (titleText != null)
        {
            titleText.text = title;
        }
    }

    public void SetQuestion(string question)
    {
        if (questionText != null)
        {
            questionText.text = question;
        }
    }

    public void SetProgress(
        int current,
        int total)
    {
        if (progressText != null)
        {
            progressText.text =
                $"{current} / {total}";
        }
    }

    public void SetImage(Sprite sprite)
    {
        if (questionImage == null)
        {
            return;
        }

        if (sprite == null)
        {
            questionImage.sprite = null;
            questionImage.enabled = false;

            return;
        }

        questionImage.enabled = true;
        questionImage.sprite = sprite;
        questionImage.preserveAspect = true;
    }

    public void SetAnswerText(
        int index,
        string text)
    {
        Button button =
            GetAnswerButton(index);

        if (button == null)
        {
            return;
        }

        TMP_Text tmp =
            button.GetComponentInChildren<TMP_Text>();

        if (tmp != null)
        {
            tmp.text = text;
        }
    }

    public void SetAnswersInteractable(
        bool interactable)
    {
        if (answerAButton != null)
            answerAButton.interactable = interactable;

        if (answerBButton != null)
            answerBButton.interactable = interactable;

        if (answerCButton != null)
            answerCButton.interactable = interactable;

        if (answerDButton != null)
            answerDButton.interactable = interactable;
    }

    private Button GetAnswerButton(
        int index)
    {
        switch (index)
        {
            case 0:
                return answerAButton;

            case 1:
                return answerBButton;

            case 2:
                return answerCButton;

            case 3:
                return answerDButton;

            default:
                return null;
        }
    }
}