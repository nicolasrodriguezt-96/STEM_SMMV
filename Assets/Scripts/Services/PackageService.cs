using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;

public class PackageService
{
    private readonly string packagesPath;

    public PackageService()
    {
        packagesPath = Path.Combine(
            Application.persistentDataPath,
            "STEM",
            "Packages"
        );

        if (!Directory.Exists(packagesPath))
        {
            Directory.CreateDirectory(packagesPath);
        }
    }

    public string InstallPackage(string zipPath)
    {
        if (string.IsNullOrWhiteSpace(zipPath))
        {
            throw new Exception("La ruta del ZIP está vacía.");
        }

        if (!File.Exists(zipPath))
        {
            throw new Exception(
                "No se encontró el archivo ZIP: " + zipPath
            );
        }

        string tempPath = Path.Combine(
            Application.persistentDataPath,
            "STEM",
            "Temp",
            Guid.NewGuid().ToString()
        );

        Directory.CreateDirectory(tempPath);

        try
        {
            ZipFile.ExtractToDirectory(zipPath, tempPath);

            string manifestPath =
                Path.Combine(tempPath, "manifest.json");

            if (!File.Exists(manifestPath))
            {
                throw new Exception(
                    "El paquete no contiene manifest.json."
                );
            }

            string manifestJson =
                File.ReadAllText(manifestPath);

            StemPackageModel package =
                JsonUtility.FromJson<StemPackageModel>(
                    manifestJson
                );

            ValidateManifest(package);

            ValidatePackageFiles(
                tempPath,
                package
            );

            PackageValidatorService validator =
                new PackageValidatorService();

            validator.ValidatePackage(
                tempPath,
                package
            );

            string destinationPath =
                Path.Combine(
                    packagesPath,
                    package.id
                );

            if (Directory.Exists(destinationPath))
            {
                Directory.Delete(
                    destinationPath,
                    true
                );
            }

            Directory.Move(
                tempPath,
                destinationPath
            );

            Debug.Log(
                $"Paquete instalado: {package.nombre}"
            );

            Debug.Log(
                $"Ruta: {destinationPath}"
            );

            return destinationPath;
        }
        catch
        {
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(
                    tempPath,
                    true
                );
            }

            throw;
        }
    }

    private void ValidateManifest(
        StemPackageModel package)
    {
        if (package == null)
        {
            throw new Exception(
                "No fue posible leer manifest.json."
            );
        }

        if (string.IsNullOrWhiteSpace(package.id))
        {
            throw new Exception(
                "El paquete no tiene ID."
            );
        }

        if (string.IsNullOrWhiteSpace(package.nombre))
        {
            throw new Exception(
                "El paquete no tiene nombre."
            );
        }

        if (package.version <= 0)
        {
            throw new Exception(
                "La versión del paquete no es válida."
            );
        }
    }

    private void ValidatePackageFiles(
        string rootPath,
        StemPackageModel package)
    {
        if (package.cuestionarios != null)
        {
            foreach (string questionnaire
                     in package.cuestionarios)
            {
                ValidateReferencedFile(
                    rootPath,
                    questionnaire,
                    "cuestionario"
                );
            }
        }

        if (package.historias != null)
        {
            foreach (string story
                     in package.historias)
            {
                ValidateReferencedFile(
                    rootPath,
                    story,
                    "historia"
                );
            }
        }

        if (package.videos != null)
        {
            foreach (string video
                     in package.videos)
            {
                ValidateReferencedFile(
                    rootPath,
                    video,
                    "video"
                );
            }
        }

        if (!string.IsNullOrWhiteSpace(
                package.portada))
        {
            ValidateReferencedFile(
                rootPath,
                package.portada,
                "portada"
            );
        }
    }

    private void ValidateReferencedFile(
        string rootPath,
        string relativePath,
        string type)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new Exception(
                $"Existe una referencia de {type} vacía."
            );
        }

        string fullPath =
            Path.Combine(
                rootPath,
                relativePath
            );

        if (!File.Exists(fullPath))
        {
            throw new Exception(
                $"No existe el archivo de {type}: " +
                relativePath
            );
        }
    }

    public string GetPackagesPath()
    {
        return packagesPath;
    }
}