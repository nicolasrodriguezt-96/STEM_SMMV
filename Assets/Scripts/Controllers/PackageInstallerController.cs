using System;
using UnityEngine;

public class PackageInstallerController : MonoBehaviour
{
    [Header("ZIP a instalar")]
    [SerializeField]
    private string zipPath;

    [Header("Main Menu")]
    [SerializeField]
    private MainMenuView mainMenuView;

    private PackageService packageService;

    private void Awake()
    {
        packageService =
            new PackageService();
    }

    public void InstallPackage()
    {
        if (string.IsNullOrWhiteSpace(zipPath))
        {
            Debug.LogError(
                "No se ha indicado la ruta del ZIP."
            );

            return;
        }

        try
        {
            Debug.Log(
                $"Intentando instalar paquete: {zipPath}"
            );

            packageService.InstallPackage(
                zipPath
            );

            Debug.Log(
                "Paquete instalado correctamente."
            );

            /*
             * Si el menú está funcionando,
             * recargamos el contenido para
             * que aparezca inmediatamente.
             */
            if (mainMenuView != null)
            {
                mainMenuView.ReloadContent();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "Error instalando paquete:\n" +
                ex
            );
        }
    }
}