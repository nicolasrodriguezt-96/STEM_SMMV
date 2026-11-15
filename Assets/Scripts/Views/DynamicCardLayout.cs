
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(GridLayoutGroup))]
[RequireComponent(typeof(ContentSizeFitter))]
public class DynamicCardLayout : MonoBehaviour
{
    [Header("Scroll")]
    [SerializeField]
    private ScrollRect scrollRect;

    [Header("Card Size")]
    [SerializeField, Min(100f)]
    private float maxCardWidth = 850f;

    [SerializeField, Min(0.1f)]
    private float cardAspectRatio = 1.5f;

    [Header("Spacing")]
    [SerializeField, Min(0)]
    private int horizontalPadding = 20;

    [SerializeField, Min(0)]
    private int verticalPadding = 20;

    [SerializeField, Min(0f)]
    private float verticalSpacing = 25f;

    private RectTransform contentRect;
    private RectTransform viewportRect;
    private GridLayoutGroup grid;
    private ContentSizeFitter sizeFitter;

    private float lastViewportWidth = -1f;
    private bool initialized;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        RefreshLayout();
    }

    private void Start()
    {
        RefreshLayout();
    }

    private void Initialize()
    {
        if (contentRect == null)
            contentRect = GetComponent<RectTransform>();

        if (grid == null)
            grid = GetComponent<GridLayoutGroup>();

        if (sizeFitter == null)
            sizeFitter = GetComponent<ContentSizeFitter>();

        if (scrollRect == null)
            scrollRect = GetComponentInParent<ScrollRect>();

        if (viewportRect == null)
            viewportRect = transform.parent as RectTransform;

        if (!initialized)
        {
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);

            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta =
                new Vector2(0f, contentRect.sizeDelta.y);

            initialized = true;
        }
    }

    public void RefreshLayout()
    {
        Initialize();

        if (viewportRect == null || scrollRect == null)
        {
            Debug.LogWarning(
                "DynamicCardLayout necesita estar dentro de un Scroll View.",
                this
            );

            return;
        }

        float viewportWidth = viewportRect.rect.width;

        if (viewportWidth <= 0f)
            return;

        // Una columna para evitar superposiciones.
        grid.constraint =
            GridLayoutGroup.Constraint.FixedColumnCount;

        grid.constraintCount = 1;

        grid.startCorner =
            GridLayoutGroup.Corner.UpperLeft;

        grid.startAxis =
            GridLayoutGroup.Axis.Horizontal;

        grid.childAlignment = TextAnchor.UpperCenter;

        // Espaciado.
        grid.padding.left = horizontalPadding;
        grid.padding.right = horizontalPadding;
        grid.padding.top = verticalPadding;
        grid.padding.bottom = verticalPadding;

        grid.spacing = new Vector2(0f, verticalSpacing);

        // Calcular tamaño adaptable.
        float availableWidth = Mathf.Max(
            1f,
            viewportWidth - horizontalPadding * 2f
        );

        float cardWidth = Mathf.Min(
            maxCardWidth,
            availableWidth
        );

        float cardHeight = cardWidth / cardAspectRatio;

        grid.cellSize = new Vector2(
            cardWidth,
            cardHeight
        );

        // El contenido crece verticalmente.
        sizeFitter.horizontalFit =
            ContentSizeFitter.FitMode.Unconstrained;

        sizeFitter.verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        // Configurar ScrollRect.
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;

        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        scrollRect.movementType =
            ScrollRect.MovementType.Clamped;

        lastViewportWidth = viewportWidth;

        LayoutRebuilder.MarkLayoutForRebuild(contentRect);
    }

    private void LateUpdate()
    {
        if (viewportRect == null)
            return;

        float currentWidth = viewportRect.rect.width;

        if (currentWidth > 0f &&
            Mathf.Abs(currentWidth - lastViewportWidth) > 1f)
        {
            RefreshLayout();
        }
    }

    private void OnTransformChildrenChanged()
    {
        if (contentRect != null)
        {
            LayoutRebuilder.MarkLayoutForRebuild(contentRect);
        }
    }
}
