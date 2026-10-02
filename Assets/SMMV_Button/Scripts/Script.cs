using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class SMMVButton : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Identificación")]
    [SerializeField] private string buttonID = "A";

    [Header("Texto del botón")]
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private string buttonLabel = "A";

    [Header("Teclado - Modo Prueba")]
    [SerializeField] private Key keyboardKey = Key.R;

    [Header("Sprites")]
    [SerializeField] private Image buttonImage;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite pressedSprite;
    [SerializeField] private Sprite disabledSprite;

    [Header("Estado")]
    [SerializeField] private bool interactable = true;

    [Header("Acción")]
    [SerializeField] private UnityEvent onPressed;

    private bool isPressed = false;


// =========================================================
// UNITY
// =========================================================

    private void Awake()
    {
        UpdateButtonText();
        UpdateVisual();
    }

    private void OnValidate()
    {
        UpdateButtonText();
        UpdateVisual();
    }

    private void Update()
    {
        // Si el botón está deshabilitado, no recibe entradas
        if (!interactable)
            return;

        // Comprobar que existe un teclado
        if (Keyboard.current == null)
            return;

        // Tecla presionada
        if (Keyboard.current[keyboardKey].wasPressedThisFrame)
        {
            Press();
        }

        // Tecla liberada
        if (Keyboard.current[keyboardKey].wasReleasedThisFrame)
        {
            Release();
        }
    }

// =========================================================
// MOUSE / UI
// =========================================================

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!interactable)
            return;

        Press();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }


// =========================================================
// BOTÓN
// =========================================================

    public void Press()
    {
        if (!interactable)
            return;

        if (isPressed)
            return;

        isPressed = true;

        // Cambiar al sprite presionado
        UpdateVisual();

        // Ejecutar acción configurada en el Inspector
        onPressed?.Invoke();
    }

    public void Release()
    {
        if (!isPressed)
            return;

        isPressed = false;

        // Volver al sprite normal
        UpdateVisual();
    }


// =========================================================
// ESTADO
// =========================================================

    public void SetInteractable(bool value)
    {
        interactable = value;

        if (!value)
        {
            isPressed = false;
        }

        UpdateVisual();
    }

    public bool IsInteractable()
    {
        return interactable;
    }


// =========================================================
// TEXTO
// =========================================================

    public void SetButtonText(string newText)
    {
        buttonLabel = newText;

        UpdateButtonText();
    }

    private void UpdateButtonText()
    {
        if (buttonText != null)
        {
            buttonText.text = buttonLabel;
        }
    }


// =========================================================
// VISUAL
// =========================================================

    private void UpdateVisual()
    {
        if (buttonImage == null)
            return;

        // DESHABILITADO
        if (!interactable)
        {
            if (disabledSprite != null)
            {
                buttonImage.sprite = disabledSprite;
            }

            return;
        }

        // PRESIONADO
        if (isPressed)
        {
            if (pressedSprite != null)
            {
                buttonImage.sprite = pressedSprite;
            }
        }
        // NORMAL
        else
        {
            if (normalSprite != null)
            {
                buttonImage.sprite = normalSprite;
            }
        }
    }


// =========================================================
// INFORMACIÓN
// =========================================================

    public string GetButtonID()
    {
        return buttonID;
    }
}