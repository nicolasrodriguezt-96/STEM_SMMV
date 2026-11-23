
using UnityEngine;

public class MostrarPanelButton : MonoBehaviour
{
    [Header("Panel que se mostrará u ocultará")]
    [SerializeField] private GameObject panel;

    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    public void MostrarPanel()
    {
        if (panel != null)
            panel.SetActive(true);
    }

    public void CerrarPanel()
    {
        if (panel != null)
            panel.SetActive(false);
    }
}
