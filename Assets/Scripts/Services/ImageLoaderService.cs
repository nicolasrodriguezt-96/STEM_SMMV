using System.IO;
using UnityEngine;

public class ImageLoaderService
{
    public Sprite LoadSprite(
        string packageRootPath,
        string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        string fullPath =
            Path.Combine(
                packageRootPath,
                relativePath
            );

        if (!File.Exists(fullPath))
        {
            Debug.LogWarning(
                "No se encontró la imagen: " +
                fullPath
            );

            return null;
        }

        byte[] bytes =
            File.ReadAllBytes(fullPath);

        Texture2D texture =
            new Texture2D(2, 2);

        bool loaded =
            texture.LoadImage(bytes);

        if (!loaded)
        {
            Debug.LogWarning(
                "No fue posible cargar la imagen: " +
                fullPath
            );

            Object.Destroy(texture);

            return null;
        }

        Sprite sprite =
            Sprite.Create(
                texture,
                new Rect(
                    0,
                    0,
                    texture.width,
                    texture.height
                ),
                new Vector2(
                    0.5f,
                    0.5f
                )
            );

        return sprite;
    }
}