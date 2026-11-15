using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VocationalTestView : MonoBehaviour
{
    // ========================================
    // PANEL
    // ========================================

    [Header("Panel")]

    [SerializeField]
    private GameObject panel;

    // ========================================
    // TEXTOS
    // ========================================

    [Header("Textos")]

    [SerializeField]
    private TMP_Text titleText;

    [SerializeField]
    private TMP_Text questionText;

    [SerializeField]
    private TMP_Text progressText;

    // ========================================
    // BOTONES
    // ========================================

    [Header("Botones de respuestas")]

    [SerializeField]
    private Button answerA;

    [SerializeField]
    private Button answerB;

    [SerializeField]
    private Button answerC;

    [SerializeField]
    private Button answerD;

    // ========================================
    // MOSTRAR PANEL
    // ========================================

    public void Show()
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }
    }

    // ========================================
    // OCULTAR PANEL
    // ========================================

    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    // ========================================
    // MOSTRAR TÍTULO
    // ========================================

    public void SetTitle(string title)
    {
        if (titleText != null)
        {
            titleText.text = title ?? "";
        }
    }

    // ========================================
    // MOSTRAR PREGUNTA
    // ========================================

    public void SetQuestion(string question)
    {
        if (questionText != null)
        {
            questionText.text = question ?? "";
        }
    }

    // ========================================
    // MOSTRAR PROGRESO
    // ========================================

    public void SetProgress(int current, int total)
    {
        if (progressText != null)
        {
            progressText.text =
                $"Pregunta {current} de {total}";
        }
    }

    // ========================================
    // MOSTRAR TEXTO DE RESPUESTA
    // ========================================

    public void SetAnswerText(int index, string text)
    {
        Button button = GetAnswerButton(index);

        if (button == null)
        {
            Debug.LogWarning(
                $"No existe el botón de respuesta {index}"
            );

            return;
        }

        TMP_Text buttonText =
            button.GetComponentInChildren<TMP_Text>(true);

        if (buttonText != null)
        {
            char letter = (char)('A' + index);

            buttonText.text =
                $"{letter}. {text}";
        }
    }

    // ========================================
    // HABILITAR / DESHABILITAR RESPUESTAS
    // ========================================

    public void SetAnswersInteractable(bool interactable)
    {
        for (int i = 0; i < 4; i++)
        {
            Button button = GetAnswerButton(i);

            if (button != null)
            {
                button.interactable = interactable;
            }
        }
    }

    // ========================================
    // OBTENER BOTÓN
    // ========================================

    private Button GetAnswerButton(int index)
    {
        switch (index)
        {
            case 0:
                return answerA;

            case 1:
                return answerB;

            case 2:
                return answerC;

            case 3:
                return answerD;

            default:
                return null;
        }
    }
}