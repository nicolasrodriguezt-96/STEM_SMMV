using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Permite ejecutar tests vocacionales procedentes de varios paquetes ZIP.
/// Mantiene OpenVocationalTest() como método público para el botón actual.
/// Si solo hay un test, lo inicia directamente (comportamiento anterior).
/// Si hay varios, presenta una pantalla de selección.
/// </summary>
public class VocationalMenuController : MonoBehaviour
{
    [Header("Controladores")]
    [SerializeField] private MainMenuView mainMenuView;
    [SerializeField] private VocationalTestController vocationalTestController;
    [SerializeField] private VocationalResultController vocationalResultController;

    [Header("Paneles vocacionales")]
    [SerializeField] private GameObject vocationalTestPanel;
    [SerializeField] private GameObject vocationalResultPanel;

    [Header("Selector de múltiples tests (nuevo)")]
    [Tooltip("Panel que muestra la lista de tests. Debe ser hermano de MainPanel en el Canvas.")]
    [SerializeField] private GameObject vocationalSelectionPanel;

    [Tooltip("Contenedor de los botones, dentro de VocationalSelectionPanel.")]
    [SerializeField] private Transform vocationalButtonsContainer;

    [Tooltip("Prefab de un botón UI, con un texto TMP o Text en su interior.")]
    [SerializeField] private Button vocationalButtonPrefab;

    [Tooltip("Opcional: botón Volver del selector; este script le configura el clic.")]
    [SerializeField] private Button closeSelectionButton;

    private bool initialViewApplied;

    private void Awake()
    {
        HideVocationalPanels();

        if (closeSelectionButton != null)
            closeSelectionButton.onClick.AddListener(ReturnToMainMenu);
    }

    private void LateUpdate()
    {
        // Se conserva la inicialización del controlador original.
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

        if (vocationalSelectionPanel != null)
            vocationalSelectionPanel.SetActive(false);
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

        // Compatibilidad con MainMenuView que no conoce estos paneles.
        HideVocationalPanels();
    }

    /// <summary>
    /// Se mantiene el mismo método para el botón ya existente.
    /// </summary>
    public void OpenVocationalTest()
    {
        if (vocationalTestController == null)
        {
            Debug.LogError("Falta asignar VocationalTestController.", this);
            return;
        }

        List<LoadedPackageModel> packages = FindVocationalPackages();

        if (packages.Count == 0)
        {
            Debug.LogWarning("No se encontró ningún test vocacional instalado.");
            return;
        }

        if (packages.Count == 1)
        {
            OpenSelectedTest(packages[0]);
            return;
        }

        if (vocationalSelectionPanel == null ||
            vocationalButtonsContainer == null ||
            vocationalButtonPrefab == null)
        {
            Debug.LogError(
                "Hay " + packages.Count + " tests vocacionales instalados, " +
                "pero no está configurado el selector. Asigna " +
                "VocationalSelectionPanel, VocationalButtonsContainer " +
                "y VocationalButtonPrefab en el Inspector.", this);
            return;
        }

        PopulateSelectionButtons(packages);
        HideVocationalPanels();

        if (mainMenuView != null)
            mainMenuView.HideAllPanels();

        vocationalSelectionPanel.SetActive(true);
        vocationalSelectionPanel.transform.SetAsLastSibling();
        Debug.Log("Mostrando " + packages.Count + " tests vocacionales disponibles.");
    }

    private void PopulateSelectionButtons(List<LoadedPackageModel> packages)
    {
        // Desactivar inmediatamente antes de Destroy para evitar botones
        // duplicados durante este frame si se abre el selector varias veces.
        for (int i = vocationalButtonsContainer.childCount - 1; i >= 0; i--)
        {
            GameObject child = vocationalButtonsContainer.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }

        foreach (LoadedPackageModel package in packages)
        {
            LoadedPackageModel selectedPackage = package;
            Button button = Instantiate(vocationalButtonPrefab, vocationalButtonsContainer);
            button.gameObject.SetActive(true);

            string packageName = package.manifest != null &&
                                 !string.IsNullOrWhiteSpace(package.manifest.nombre)
                ? package.manifest.nombre
                : "Paquete STEM";

            string testName = !string.IsNullOrWhiteSpace(package.testVocacional.nombre)
                ? package.testVocacional.nombre
                : "Test vocacional";

            string label = testName + "  |  " + packageName;
            button.gameObject.name = "Test_" +
                (package.manifest != null ? package.manifest.id : testName);

            TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
                tmp.text = label;
            else
            {
                Text legacy = button.GetComponentInChildren<Text>(true);
                if (legacy != null)
                    legacy.text = label;
                else
                    Debug.LogWarning("El prefab del botón no contiene texto TMP ni Text.");
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OpenSelectedTest(selectedPackage));
        }
    }

    private void OpenSelectedTest(LoadedPackageModel package)
    {
        if (package == null || package.testVocacional == null)
        {
            Debug.LogError("El test seleccionado no es válido.");
            return;
        }

        HideVocationalPanels();
        Debug.Log("Abriendo test vocacional: " + package.testVocacional.nombre);
        vocationalTestController.StartTest(package);
    }

    /// <summary>
    /// Se consultan los ZIP instalados sin imponer una exclusividad global.
    /// No se requiere cambiar VocationalTestController.
    /// </summary>
    private List<LoadedPackageModel> FindVocationalPackages()
    {
        List<LoadedPackageModel> results = new List<LoadedPackageModel>();
        string packagesPath = Path.Combine(
            Application.persistentDataPath, "STEM", "Packages");

        if (!Directory.Exists(packagesPath))
        {
            Debug.LogWarning("No existe la carpeta de paquetes: " + packagesPath);
            return results;
        }

        foreach (string packagePath in Directory.GetDirectories(packagesPath))
        {
            try
            {
                string manifestPath = Path.Combine(packagePath, "manifest.json");
                if (!File.Exists(manifestPath))
                    continue;

                StemPackageModel manifest = JsonUtility.FromJson<StemPackageModel>(
                    File.ReadAllText(manifestPath));

                if (manifest == null ||
                    string.IsNullOrWhiteSpace(manifest.testVocacional))
                    continue;

                string testPath = GetSafeFilePath(packagePath, manifest.testVocacional);
                if (!File.Exists(testPath))
                {
                    Debug.LogError("No existe el JSON vocacional: " + testPath);
                    continue;
                }

                VocationalTestModel test = JsonUtility.FromJson<VocationalTestModel>(
                    File.ReadAllText(testPath));

                if (test == null || test.carreras == null || test.carreras.Count != 4 ||
                    test.preguntas == null || test.preguntas.Count == 0)
                {
                    Debug.LogError("Test vocacional inválido en paquete: " + manifest.nombre);
                    continue;
                }

                bool invalidQuestion = false;
                foreach (VocationalQuestionModel question in test.preguntas)
                {
                    if (question == null || question.respuestas == null ||
                        question.respuestas.Count != 4)
                    {
                        invalidQuestion = true;
                        break;
                    }
                }
                if (invalidQuestion)
                {
                    Debug.LogError("El test contiene preguntas sin cuatro opciones: " + manifest.nombre);
                    continue;
                }

                results.Add(new LoadedPackageModel
                {
                    rootPath = packagePath,
                    manifest = manifest,
                    testVocacional = test
                });
            }
            catch (Exception ex)
            {
                Debug.LogError("Error leyendo test vocacional en '" + packagePath +
                               "': " + ex.Message);
            }
        }

        results.Sort((a, b) => string.Compare(
            a.testVocacional.nombre, b.testVocacional.nombre,
            StringComparison.CurrentCultureIgnoreCase));

        return results;
    }

    private static string GetSafeFilePath(string rootPath, string relativePath)
    {
        string root = Path.GetFullPath(rootPath).TrimEnd(
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;

        string normalized = relativePath.Replace('/', Path.DirectorySeparatorChar)
                                        .Replace('\\', Path.DirectorySeparatorChar);

        if (Path.IsPathRooted(normalized) || normalized.Contains(":"))
            throw new IOException("Ruta inválida en testVocacional.");

        string fullPath = Path.GetFullPath(Path.Combine(root, normalized));
        StringComparison comparison = Path.DirectorySeparatorChar == '\\'
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (!fullPath.StartsWith(root, comparison))
            throw new IOException("El archivo está fuera del paquete.");

        return fullPath;
    }

    private void OnDestroy()
    {
        if (closeSelectionButton != null)
            closeSelectionButton.onClick.RemoveListener(ReturnToMainMenu);
    }
}
