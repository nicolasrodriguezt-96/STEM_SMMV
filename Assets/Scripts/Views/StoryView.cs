using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoryView : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField]
    private GameObject panel;

    [Header("Texts")]
    [SerializeField]
    private TMP_Text titleText;

    [SerializeField]
    private TMP_Text storyText;

    [SerializeField]
    private TMP_Text progressText;

    [Header("Image")]
    [SerializeField]
    private Image storyImage;

    [Header("Buttons")]
    [SerializeField]
    private Button previousButton;

    [SerializeField]
    private Button nextButton;

    [SerializeField]
    private Button exitButton;

    public Button PreviousButton => previousButton;
    public Button NextButton => nextButton;
    public Button ExitButton => exitButton;

    public void Show()
    {
        panel.SetActive(true);
    }

    public void Hide()
    {
        panel.SetActive(false);
    }

    public void SetTitle(string title)
    {
        titleText.text = title;
    }

    public void SetText(string text)
    {
        storyText.text = text;
    }

    public void SetProgress(
        int current,
        int total)
    {
        progressText.text =
            $"{current} / {total}";
    }

    public void SetImage(Sprite sprite)
    {
        if (sprite == null)
        {
            storyImage.sprite = null;
            storyImage.enabled = false;

            return;
        }

        storyImage.enabled = true;
        storyImage.sprite = sprite;
        storyImage.preserveAspect = true;
    }

    public void SetPreviousEnabled(
        bool enabled)
    {
        previousButton.interactable =
            enabled;
    }

    public void SetNextText(
        string text)
    {
        TMP_Text buttonText =
            nextButton
                .GetComponentInChildren<TMP_Text>();

        if (buttonText != null)
        {
            buttonText.text = text;
        }
    }
}