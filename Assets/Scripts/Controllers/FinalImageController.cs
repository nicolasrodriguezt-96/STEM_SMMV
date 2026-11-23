using System;
using System.IO;
using SimpleFileBrowser;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gestiona una sola imagen de cierre compartida por todas las actividades STEM.
/// En esta etapa, se configura y prueba en Windows.
/// No depende de Firebase ni de los paquetes ZIP.
/// </summary>
public class FinalImageController : MonoBehaviour
{
    [Header("Pantalla de cierre")]
    [SerializeField] private GameObject finalImagePanel;
    [SerializeField] private RawImage finalImageView;
    [SerializeField] private AspectRatioFitter imageAspectFitter;
    [SerializeField] private Button continueButton;

    [Header("Limites de la imagen")]
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
            Debug.LogWarning("Ya existe un FinalImageController en la escena. Se elimina el duplicado.");
            Destroy(gameObject);
            return;
        }

        instance = this;
        savedImagePath = Path.Combine(
            Application.persistentDataPath, "STEM", "Data", "final_image.png");

        // Es importante que el panel sea independiente de MainPanel y ResultPanel.
        if (finalImagePanel != null)
            finalImagePanel.SetActive(false);

        if (continueButton != null)
            continueButton.onClick.AddListener(CloseFinalImage);

        LoadSavedImage();
    }

    /// <summary>
    /// Asignar desde el OnClick del boton "Cargar imagen final".
    /// Mantiene visibles imagenes y ZIP para no bloquear el instalador.
    /// </summary>
    public void SelectFinalImage()
    {
        if (FileBrowser.IsOpen)
        {
            Debug.LogWarning("Ya hay un explorador de archivos abierto.");
            return;
        }

        // Los filtros del plugin son globales. Incluimos tambien ZIP para
        // que el instalador de paquetes siga pudiendo seleccionar sus archivos.
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
            Debug.LogWarning("No fue posible abrir el explorador de imagenes.");
    }

    private void OnFileSelected(string[] paths)
    {
        if (paths == null || paths.Length == 0 || string.IsNullOrWhiteSpace(paths[0]))
            return;

        string sourcePath = paths[0];
        string extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
        {
            Debug.LogError("Selecciona un archivo PNG, JPG o JPEG.");
            return;
        }

        Texture2D newTexture = null;
        try
        {
            long maxBytes = (long)maxFileSizeMB * 1024 * 1024;
            if (FileBrowserHelpers.GetFilesize(sourcePath) > maxBytes)
                throw new InvalidOperationException("La imagen supera el limite de " + maxFileSizeMB + " MB.");

            byte[] bytes = FileBrowserHelpers.ReadBytesFromFile(sourcePath);
            if (bytes == null || bytes.Length == 0 || bytes.LongLength > maxBytes)
                throw new InvalidOperationException("El archivo esta vacio o es demasiado grande.");

            newTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(newTexture, bytes, false))
                throw new InvalidOperationException("El archivo no contiene una imagen PNG/JPG valida.");

            if (newTexture.width > maxImageDimension || newTexture.height > maxImageDimension)
                throw new InvalidOperationException(
                    "La imagen supera la dimension maxima de " + maxImageDimension + " pixeles por lado.");

            // Se normaliza todo a UN archivo PNG persistente. Una nueva imagen
            // reemplaza el archivo anterior sin acumular imagenes.
            byte[] pngBytes = newTexture.EncodeToPNG();
            if (pngBytes == null || pngBytes.Length == 0)
                throw new InvalidOperationException("No se pudo convertir la imagen a PNG.");

            Directory.CreateDirectory(Path.GetDirectoryName(savedImagePath));
            File.WriteAllBytes(savedImagePath, pngBytes);

            SetSavedTexture(newTexture);
            newTexture = null; // La textura ahora pertenece al controlador.

            Debug.Log("IMAGEN FINAL GUARDADA CORRECTAMENTE");
            Debug.Log("Ruta: " + savedImagePath);
            ShowIfAvailable(); // Vista previa inmediata de la imagen elegida.
            Debug.Log("Se mostrara al finalizar quiz, test vocacional o historia, una vez conectados sus controladores.");
        }
        catch (Exception ex)
        {
            Debug.LogError("No se pudo guardar la imagen final: " + ex.Message);
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
                throw new InvalidOperationException("El PNG guardado es invalido.");

            SetSavedTexture(loadedTexture);
            loadedTexture = null;
            Debug.Log("Imagen final recuperada desde disco: " + savedImagePath);
        }
        catch (Exception ex)
        {
            Debug.LogError("No se pudo recuperar la imagen final: " + ex.Message);
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

    /// <summary>
    /// Para la fase de integracion: llamar al finalizar cada actividad.
    /// Devuelve false si no existe una imagen configurada.
    /// </summary>
    public static bool ShowIfAvailable()
    {
        if (instance == null || instance.savedTexture == null || instance.finalImagePanel == null)
            return false;

        instance.finalImagePanel.SetActive(true);
        instance.finalImagePanel.transform.SetAsLastSibling();
        return true;
    }

    /// <summary>
    /// Asignar al ContinueButton si no esta conectado mediante el Inspector.
    /// El listener se registra automaticamente en Awake cuando hay referencia.
    /// </summary>
    public void CloseFinalImage()
    {
        if (finalImagePanel != null)
            finalImagePanel.SetActive(false);
    }

    // Permite probar la pantalla sin terminar un cuestionario.
    public void PreviewFinalImage()
    {
        if (!ShowIfAvailable())
            Debug.LogWarning("Primero selecciona una imagen final.");
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
