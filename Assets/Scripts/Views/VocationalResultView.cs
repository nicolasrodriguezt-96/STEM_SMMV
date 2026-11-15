using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VocationalResultView : MonoBehaviour
{
    [Header("Panel principal")]
    [SerializeField] private GameObject panel;

    [Header("Título")]
    [SerializeField] private TMP_Text resultTitle;

    [Header("Primera carrera")]
    [SerializeField] private GameObject firstCareerGroup;
    [SerializeField] private Image firstCareerImage;
    [SerializeField] private TMP_Text firstCareerNameText;
    [SerializeField] private TMP_Text firstCareerDescriptionText;

    [Header("Segunda carrera")]
    [SerializeField] private GameObject secondCareerGroup;
    [SerializeField] private Image secondCareerImage;
    [SerializeField] private TMP_Text secondCareerNameText;
    [SerializeField] private TMP_Text secondCareerDescriptionText;

    public void Show()
    {
        if (panel != null)
            panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    public void Render(
        VocationalCareerModel firstCareer,
        Sprite firstSprite,
        VocationalCareerModel secondCareer,
        Sprite secondSprite)
    {
        bool hasSecondCareer =
            secondCareer != null &&
            secondSprite != null;

        if (resultTitle != null)
        {
            resultTitle.text = hasSecondCareer
                ? "¡Tienes dos talentos STEM!"
                : "¡Descubre tu futuro STEM!";
        }

        // Primera carrera
        if (firstCareerGroup != null)
        {
            firstCareerGroup.SetActive(
                firstCareer != null
            );
        }

        if (firstCareerImage != null)
        {
            firstCareerImage.sprite = firstSprite;
            firstCareerImage.enabled = firstSprite != null;
        }

        if (firstCareerNameText != null)
        {
            firstCareerNameText.text =
                firstCareer != null
                    ? firstCareer.nombre
                    : "";
        }

        if (firstCareerDescriptionText != null)
        {
            firstCareerDescriptionText.text =
                firstCareer != null
                    ? firstCareer.descripcion
                    : "";
        }

        // Segunda carrera
        if (secondCareerGroup != null)
        {
            secondCareerGroup.SetActive(
                hasSecondCareer
            );
        }

        if (secondCareerImage != null)
        {
            secondCareerImage.sprite =
                hasSecondCareer ? secondSprite : null;

            secondCareerImage.enabled =
                hasSecondCareer;
        }

        if (secondCareerNameText != null)
        {
            secondCareerNameText.text =
                hasSecondCareer
                    ? secondCareer.nombre
                    : "";
        }

        if (secondCareerDescriptionText != null)
        {
            secondCareerDescriptionText.text =
                hasSecondCareer
                    ? secondCareer.descripcion
                    : "";
        }
    }
}