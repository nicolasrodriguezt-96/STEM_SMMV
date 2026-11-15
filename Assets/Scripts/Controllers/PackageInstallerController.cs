
using System;
using System.IO;
using UnityEngine;
using SimpleFileBrowser;

public class PackageInstallerController : MonoBehaviour
{
    [Header("Main Menu")]
    [SerializeField]
    private MainMenuView mainMenuView;

    private PackageService packageService;

    [Header("Tamaño del explorador")]
    [SerializeField, Min(380f)]
    private float browserWidth = 700f;

    [SerializeField, Min(300f)]
    private float browserHeight = 500f;

    // Recordar la última carpeta utilizada
    private string lastDirectory;

    private void Awake()
    {
        packageService = new PackageService();

        // Mostrar solamente archivos ZIP
        FileBrowser.SetFilters(
            false,
            new FileBrowser.Filter(
                "Paquetes STEM (*.zip)",
                ".zip"
            )
        );

        FileBrowser.SetDefaultFilter(".zip");
    }

    // ============================================
    // ABRIR EXPLORADOR DE ARCHIVOS
    // ============================================

    public void InstallPackage()
    {
        // Evitar abrir varios exploradores a la vez
        if (FileBrowser.IsOpen)
        {
            Debug.LogWarning(
                "Ya existe un explorador de archivos abierto."
            );

            return;
        }

        Debug.Log("Abriendo explorador de paquetes STEM...");

        bool opened = FileBrowser.ShowLoadDialog(
            OnFileSelected,
            OnSelectionCanceled,
            FileBrowser.PickMode.Files,
            false,
            lastDirectory,
            null,
            "Seleccionar paquete STEM",
            "INSTALAR"
        );

        if (!opened)
        {
            Debug.LogWarning(
                "No se pudo abrir el explorador de archivos."
            );
        }
    }

    // ============================================
    // ARCHIVO SELECCIONADO
    // ============================================

    private void OnFileSelected(string[] paths)
    {
        if (paths == null || paths.Length == 0)
        {
            Debug.LogWarning(
                "No se seleccionó ningún archivo."
            );

            return;
        }

        string selectedPath = paths[0];

        Debug.Log(
            "Archivo seleccionado: " + selectedPath
        );

        // Recordar carpeta
        lastDirectory = Path.GetDirectoryName(selectedPath);

        InstallSelectedZip(selectedPath);
    }

    // ============================================
    // INSTALAR ZIP
    // ============================================

    private void InstallSelectedZip(string selectedPath)
    {
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            Debug.LogError(
                "La ruta del archivo está vacía."
            );

            return;
        }

        if (!File.Exists(selectedPath))
        {
            Debug.LogError(
                "El archivo seleccionado no existe: " +
                selectedPath
            );

            return;
        }

        // Comprobar extensión
        string extension = Path.GetExtension(selectedPath);

        if (!string.Equals(
                extension,
                ".zip",
                StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError(
                "El archivo seleccionado no es un ZIP válido."
            );

            return;
        }

        try
        {
            Debug.Log(
                "Instalando paquete STEM: " +
                Path.GetFileName(selectedPath)
            );

            // Reutilizamos nuestro servicio actual
            packageService.InstallPackage(selectedPath);

            Debug.Log(
                "Proceso de instalación completado."
            );

            // Actualizar menú y contenido
            if (mainMenuView != null)
            {
                mainMenuView.ReloadContent();
            }
            else
            {
                Debug.LogWarning(
                    "MainMenuView no está asignado. " +
                    "El contenido se actualizará al reiniciar."
                );
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "Error instalando paquete STEM:\n" +
                ex
            );
        }
    }

    // ============================================
    // CANCELAR SELECCIÓN
    // ============================================

    private void OnSelectionCanceled()
    {
        Debug.Log(
            "Selección de paquete cancelada."
        );
    }
}
