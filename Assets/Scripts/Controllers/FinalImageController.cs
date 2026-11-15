using System;
using System.IO;
using SimpleFileBrowser;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controlador de UNA imagen final para STEM SMMV (Windows).
/// Debe estar en un GameObject activo llamado FinalImageManager,
/// FUERA de FinalImagePanel.
///
/// - Oculta FinalImagePanel al iniciar Unity.
/// - Permite elegir una imagen PNG/JPG con Simple File Browser.
/// - Guarda una unica imagen en Application.persistentDataPath.
/// - Muestra la imagen al pulsar los botones de salida de resultados
///   mediante FinalImageController.ShowIfAvailable().
/// - Continuar solo oculta la imagen; el menu de destino ya se ha abierto
///   por el controlador que llamo a ShowIfAvailable().
/// </summary>
public class FinalImageController : MonoBehaviour
{
    [Header("Panel de imagen final (NO el Canvas principal)")]
    [SerializeField] private GameObject finalImagePanel;

    [Header("Componentes del panel")]
    [SerializeField] private RawImage finalImageView;
    [SerializeField] private AspectRatioFitter imageAspectFitter;
    [SerializeField] private Button continueButton;

    [Header("Limites")]
    [SerializeField, Min(1)] private int maxFileSizeMB = 15;
    [SerializeField, Min(256)] private int maxImageDimension = 4096;

    private static FinalImageController instance;
    private Texture2D savedTexture;
    private string savedImagePath;

    public static bool HasImage => instance != null && instance.savedTexture != null;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("Ya existe un FinalImageController. Destruyendo duplicado.", this);
            Destroy(gameObject);
            return;
        }

        instance = this;
        savedImagePath = Path.Combine(
            Application.persistentDataPath, "STEM", "Data", "final_image.png");

        // Proteccion: JAMAS desactivar un contenedor de la app por error.
        // Se exige el panel concreto y que sus componentes vivan dentro de el.
        if (!IsSafeFinalPanel())
        {
            Debug.LogError(
                "FinalImageController: referencias incorrectas. No se ocultara ningun " +
                "objeto para evitar desactivar historias, tests o minijuegos.", this);
            return;
        }

        HideFinalImage(); // Solo FinalImagePanel, desde Awake.

        continueButton.onClick.AddListener(CloseFinalImage);
        LoadSavedImage();
    }

    /// <summary>
    /// Rechaza accidentalmente asignar Canvas, MainPanel o un contenedor
    /// general. FinalImagePanel debe ser un objeto UI independiente dentro
    /// del Canvas principal, con su RawImage y boton como descendientes.
    /// </summary>
    private bool IsSafeFinalPanel()
    {
        if (finalImagePanel == null)
        {
            Debug.LogError("Asigna FinalImagePanel en el Inspector de FinalImageManager.", this);
            return false;
        }

        if (finalImagePanel.name != "FinalImagePanel")
        {
            Debug.LogError(
                "No se ocultara '" + finalImagePanel.name +
                "'. En Final Image Panel asigna el objeto llamado FinalImagePanel.", this);
            return false;
        }

        if (finalImagePanel.GetComponent<Canvas>() != null)
        {
            Debug.LogError(
                "Final Image Panel apunta a un objeto Canvas. " +
                "Asigna el Panel interno, nunca el Canvas.", this);
            return false;
        }

        if (transform == finalImagePanel.transform ||
            transform.IsChildOf(finalImagePanel.transform))
        {
            Debug.LogError("FinalImageManager debe estar FUERA de FinalImagePanel.", this);
            return false;
        }

        if (finalImageView == null ||
            !finalImageView.transform.IsChildOf(finalImagePanel.transform))
        {
            Debug.LogError(
                "Final Image View debe ser un RawImage dentro de FinalImagePanel.", this);
            return false;
        }

        if (continueButton == null ||
            !continueButton.transform.IsChildOf(finalImagePanel.transform))
        {
            Debug.LogError(
                "Continue Button debe ser un Button dentro de FinalImagePanel.", this);
            return false;
        }

        return true;
    }

    /// <summary>
    /// OnClick de "Cargar imagen final" en MainPanel.
    /// </summary>
    public void SelectFinalImage()
    {
        if (FileBrowser.IsOpen)
        {
            Debug.LogWarning("Ya hay un selector de archivos abierto.", this);
            return;
        }

        // Se conserva el filtro ZIP para no afectar al selector del instalador.
        FileBrowser.SetFilters(
            true,
            new FileBrowser.Filter("Imagenes", ".png", ".jpg", ".jpeg"),
            new FileBrowser.Filter("Paquetes ZIP", ".zip")
        );
        FileBrowser.SetDefaultFilter(".png");

        bool shown = FileBrowser.ShowLoadDialog(
            OnFileSelected,
            () => Debug.Log("Seleccion de imagen cancelada."),
            FileBrowser.PickMode.Files,
            false,
            null,
            null,
            "Seleccionar imagen final (PNG o JPG)",
            "Seleccionar"
        );

        if (!shown)
            Debug.LogWarning("No se pudo abrir el selector de imagen.", this);
    }

    private void OnFileSelected(string[] paths)
    {
        if (paths == null || paths.Length == 0 || string.IsNullOrWhiteSpace(paths[0]))
            return;

        string sourcePath = paths[0];
        string extension = Path.GetExtension(sourcePath).ToLowerInvariant();

        if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
        {
            Debug.LogError("Selecciona una imagen PNG, JPG o JPEG.", this);
            return;
        }

        Texture2D newTexture = null;
        try
        {
            long maxBytes = (long)maxFileSizeMB * 1024L * 1024L;
            if (FileBrowserHelpers.GetFilesize(sourcePath) > maxBytes)
                throw new InvalidOperationException(
                    "La imagen supera el limite de " + maxFileSizeMB + " MB.");

            byte[] bytes = FileBrowserHelpers.ReadBytesFromFile(sourcePath);
            if (bytes == null || bytes.Length == 0 || bytes.LongLength > maxBytes)
                throw new InvalidOperationException("La imagen esta vacia o es demasiado grande.");

            newTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(newTexture, bytes, false))
                throw new InvalidOperationException("El archivo no es una imagen PNG/JPG valida.");

            if (newTexture.width > maxImageDimension || newTexture.height > maxImageDimension)
                throw new InvalidOperationException(
                    "La imagen supera " + maxImageDimension + " pixeles en alguno de sus lados.");

            byte[] pngBytes = newTexture.EncodeToPNG();
            if (pngBytes == null || pngBytes.Length == 0)
                throw new InvalidOperationException("No se pudo convertir a PNG.");

            Directory.CreateDirectory(Path.GetDirectoryName(savedImagePath));
            File.WriteAllBytes(savedImagePath, pngBytes);

            SetSavedTexture(newTexture);
            newTexture = null;

            Debug.Log("IMAGEN FINAL GUARDADA CORRECTAMENTE: " + savedImagePath);

            // Mostrar vista previa solo tras la seleccion, nunca al inicio.
            if (!ShowIfAvailable())
                Debug.LogWarning("Imagen guardada, pero no se pudo mostrar. Revisa las referencias.");
        }
        catch (Exception ex)
        {
            Debug.LogError("No se pudo guardar la imagen final: " + ex.Message, this);
        }
        finally
        {
            if (newTexture != null)
                Destroy(newTexture);
        }
    }

    private void LoadSavedImage()
    {
        if (!File.Exists(savedImagePath))
        {
            Debug.Log("No hay imagen final guardada todavia.");
            return;
        }

        Texture2D loadedTexture = null;
        try
        {
            byte[] bytes = File.ReadAllBytes(savedImagePath);
            loadedTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(loadedTexture, bytes, false))
                throw new InvalidOperationException("La imagen guardada no es valida.");

            SetSavedTexture(loadedTexture);
            loadedTexture = null;
            Debug.Log("Imagen final recuperada desde disco: " + savedImagePath);

            // Deliberadamente NO se llama ShowIfAvailable() aqui.
        }
        catch (Exception ex)
        {
            Debug.LogError("No se pudo recuperar la imagen final: " + ex.Message, this);
        }
        finally
        {
            if (loadedTexture != null)
                Destroy(loadedTexture);
        }
    }

    private void SetSavedTexture(Texture2D texture)
    {
        if (finalImageView != null)
            finalImageView.texture = texture;

        if (imageAspectFitter != null && texture != null)
            imageAspectFitter.aspectRatio = (float)texture.width / texture.height;

        if (savedTexture != null && savedTexture != texture)
            Destroy(savedTexture);

        savedTexture = texture;
    }

    private void HideFinalImage()
    {
        if (IsSafeFinalPanel())
            finalImagePanel.SetActive(false);
    }

    /// <summary>
    /// Lo llaman QuizResultController y FinalImageNavigationController.
    /// Primero debe prepararse el menu de destino; despues se activa el panel.
    /// </summary>
    public static bool ShowIfAvailable()
    {
        if (instance == null || !instance.IsSafeFinalPanel() || instance.savedTexture == null)
        {
            Debug.LogWarning(
                "IMAGEN FINAL NO DISPONIBLE. Verifica FinalImageManager activo, " +
                "FinalImagePanel asignado e imagen guardada.");
            return false;
        }

        instance.finalImagePanel.SetActive(true);
        instance.finalImagePanel.transform.SetAsLastSibling();
        Debug.Log("IMAGEN FINAL MOSTRADA");
        return true;
    }

    /// <summary>
    /// OnClick del boton Continuar (se registra automaticamente en Awake).
    /// </summary>
    public void CloseFinalImage()
    {
        HideFinalImage();
        Debug.Log("Imagen final cerrada.");
    }

    /// <summary>
    /// Vista previa opcional desde un boton de prueba.
    /// </summary>
    public void PreviewFinalImage()
    {
        if (!ShowIfAvailable())
            Debug.LogWarning("Carga primero una imagen final.");
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        if (continueButton != null)
            continueButton.onClick.RemoveListener(CloseFinalImage);

        if (savedTexture != null)
            Destroy(savedTexture);

        instance = null;
    }
}
