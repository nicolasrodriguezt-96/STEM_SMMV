using UnityEngine;

/// <summary>
/// Opcional: conectar el boton Home de los RESULTADOS VOCACIONALES.
/// No modifica la logica ni el guardado del test vocacional.
/// Colocar el componente en un GameObject que permanezca activo.
/// </summary>
public class FinalImageNavigationController : MonoBehaviour
{
    [Header("Navegacion")]
    [SerializeField] private MainMenuView mainMenuView;

    [Header("Opcional: panel de resultados vocacionales a ocultar")]
    [SerializeField] private GameObject vocationalResultPanel;

    // Asignar al OnClick del boton Home en VocationalResultPanel.
    public void ReturnHomeFromVocational()
    {
        if (vocationalResultPanel != null)
            vocationalResultPanel.SetActive(false);

        if (mainMenuView != null)
            mainMenuView.ShowMainMenu();
        else
            Debug.LogWarning("FinalImageNavigationController: MainMenuView no asignado.");

        FinalImageController.ShowIfAvailable();
    }

    // Opcional, solo cuando una historia termina y su control ya ha concluido.
    // No conectar este metodo al boton de avanzar paginas.
    public void ReturnToStoriesAfterFinish()
    {
        if (mainMenuView != null)
            mainMenuView.ShowStories();
        else
            Debug.LogWarning("FinalImageNavigationController: MainMenuView no asignado.");

        FinalImageController.ShowIfAvailable();
    }
}
