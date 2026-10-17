using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ContentCardView : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private Image image;

    [SerializeField]
    private TMP_Text titleText;

    [SerializeField]
    private Button button;

    [SerializeField]
    private AspectRatioFitter aspectRatioFitter;

    public Image Image => image;

    public TMP_Text TitleText => titleText;

    public Button Button => button;

    public void SetImage(Sprite sprite)
    {
        if (image == null)
        {
            Debug.LogWarning(
                "No hay un componente Image asignado en ContentCardView."
            );

            return;
        }

        if (sprite == null)
        {
            image.sprite = null;
            image.enabled = false;

            return;
        }

        image.enabled = true;
        image.sprite = sprite;

        image.type = Image.Type.Simple;

        if (aspectRatioFitter != null)
        {
            float width =
                sprite.rect.width;

            float height =
                sprite.rect.height;

            if (height > 0)
            {
                aspectRatioFitter.aspectRatio =
                    width / height;
            }
        }
    }
}