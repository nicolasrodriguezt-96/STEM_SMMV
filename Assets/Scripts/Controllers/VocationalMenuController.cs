using System;
using System.IO;
using UnityEngine;

public class VocationalMenuController : MonoBehaviour
{
    [Header("Controladores")]
    [SerializeField] private MainMenuView mainMenuView;
    [SerializeField] private VocationalTestController vocationalTestController;
    [SerializeField] private VocationalResultController vocationalResultController;

    [Header("Paneles vocacionales")]
    [SerializeField] private GameObject vocationalTestPanel;
    [SerializeField] private GameObject vocationalResultPanel;

    private bool initialViewApplied;

    private void Awake()
    {
        HideVocationalPanels();
    }

    private void LateUpdate()
    {
        // Se ejecuta una sola vez, después de los Start()
        // de los demás componentes.
        if (initialViewApplied)
            return;

        initialViewApplied = true;
        ReturnToMainMenu();
    }

    public void HideVocationalPanels()
    {
        if (vocationalTestPanel != null)
            vocationalTestPanel.SetActive(false);

        if (vocationalResultPanel != null)
            vocationalResultPanel.SetActive(false);
    }

    public void ReturnToMainMenu()
    {
        if (vocationalTestController != null)
            vocationalTestController.HideTest();

        if (vocationalResultController != null)
            vocationalResultController.HideResult();

        HideVocationalPanels();

        if (mainMenuView != null)
            mainMenuView.ShowMainMenu();

        // El MainMenuView antiguo puede no conocer
        // los nuevos paneles: los ocultamos de nuevo.
        HideVocationalPanels();
    }

    public void OpenVocationalTest()
    {
        if (vocationalTestController == null)
        {
            Debug.LogError(
                "Falta asignar VocationalTestController."
            );
            return;
        }

        LoadedPackageModel package = FindVocationalPackage();

        if (package == null)
        {
            Debug.LogWarning(
                "No se encontró un test vocacional instalado."
            );
            return;
        }

        HideVocationalPanels();

        vocationalTestController.StartTest(package);
    }

    private LoadedPackageModel FindVocationalPackage()
    {
        string packagesPath = Path.Combine(
            Application.persistentDataPath,
            "STEM",
            "Packages"
        );

        if (!Directory.Exists(packagesPath))
        {
            Debug.LogWarning(
                "No existe la carpeta de paquetes: " +
                packagesPath
            );
            return null;
        }

        LoadedPackageModel result = null;

        foreach (string packagePath in
                 Directory.GetDirectories(packagesPath))
        {
            try
            {
                string manifestPath = Path.Combine(
                    packagePath,
                    "manifest.json"
                );

                if (!File.Exists(manifestPath))
                    continue;

                string manifestJson =
                    File.ReadAllText(manifestPath);

                StemPackageModel manifest =
                    JsonUtility.FromJson<StemPackageModel>(
                        manifestJson
                    );

                if (manifest == null ||
                    string.IsNullOrWhiteSpace(
                        manifest.testVocacional))
                {
                    continue;
                }

                string vocationalPath = GetSafeFilePath(
                    packagePath,
                    manifest.testVocacional
                );

                if (!File.Exists(vocationalPath))
                {
                    Debug.LogError(
                        "No existe el JSON vocacional: " +
                        vocationalPath
                    );
                    continue;
                }

                string testJson =
                    File.ReadAllText(vocationalPath);

                VocationalTestModel test =
                    JsonUtility.FromJson<VocationalTestModel>(
                        testJson
                    );

                if (test == null)
                    continue;

                if (result != null)
                {
                    Debug.LogError(
                        "Hay más de un test vocacional instalado. " +
                        "Revisa tus paquetes."
                    );

                    return null;
                }

                result = new LoadedPackageModel
                {
                    rootPath = packagePath,
                    testVocacional = test
                };
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "Error leyendo el paquete vocacional: " +
                    ex.Message
                );
            }
        }

        return result;
    }

    private string GetSafeFilePath(
        string rootPath,
        string relativePath)
    {
        string root = Path.GetFullPath(rootPath)
            .TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar
            ) + Path.DirectorySeparatorChar;

        string normalized = relativePath
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        if (Path.IsPathRooted(normalized) ||
            normalized.Contains(":"))
        {
            throw new IOException(
                "Ruta inválida en testVocacional."
            );
        }

        string fullPath = Path.GetFullPath(
            Path.Combine(root, normalized)
        );

        if (!fullPath.StartsWith(
                root,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException(
                "El archivo está fuera del paquete."
            );
        }

        return fullPath;
    }
}