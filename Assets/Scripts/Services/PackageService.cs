using System;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using UnityEngine;

public class PackageService
{
    private readonly string packagesPath;
    private readonly string tempRootPath;

    // ============================================
    // CONSTRUCTOR
    // ============================================

    public PackageService()
    {
        string stemPath = Path.Combine(
            Application.persistentDataPath,
            "STEM"
        );

        packagesPath = Path.Combine(
            stemPath,
            "Packages"
        );

        tempRootPath = Path.Combine(
            stemPath,
            "Temp"
        );

        Directory.CreateDirectory(packagesPath);
        Directory.CreateDirectory(tempRootPath);
    }

    // ============================================
    // INSTALAR PAQUETE ZIP
    // ============================================

    public string InstallPackage(string zipPath)
    {
        if (string.IsNullOrWhiteSpace(zipPath))
            throw new Exception("La ruta del ZIP está vacía.");

        if (!File.Exists(zipPath))
            throw new Exception("No se encontró el archivo ZIP: " + zipPath);

        if (!string.Equals(Path.GetExtension(zipPath), ".zip",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("El archivo seleccionado debe ser un ZIP.");
        }

        // La extracción y la validación se hacen ANTES de tocar los
        // paquetes instalados, para no perder datos ante un ZIP inválido.
        string stagingPath = Path.Combine(
            tempRootPath, "incoming_" + Guid.NewGuid().ToString("N"));

        // El respaldo se guarda en STEM/Temp, fuera de STEM/Packages.
        // Ambos directorios están en el mismo volumen: Directory.Move
        // conserva la posibilidad de restaurarlos sin copiarlos.
        string backupPath = Path.Combine(
            tempRootPath, "packages_backup_" + Guid.NewGuid().ToString("N"));

        string destinationPath = null;
        bool previousPackagesBackedUp = false;
        bool installationCommitted = false;

        Directory.CreateDirectory(stagingPath);

        try
        {
            // 1. Extraer y comprobar el ZIP COMPLETO.
            Debug.Log("Extrayendo paquete: " + Path.GetFileName(zipPath));
            ZipFile.ExtractToDirectory(zipPath, stagingPath);

            string manifestPath = Path.Combine(stagingPath, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                throw new Exception(
                    "El ZIP no contiene manifest.json en su carpeta raíz.");
            }

            StemPackageModel package = JsonUtility.FromJson<StemPackageModel>(
                File.ReadAllText(manifestPath));

            ValidateManifest(package);
            ValidatePackageFiles(stagingPath, package);

            PackageValidatorService validator = new PackageValidatorService();
            validator.ValidatePackage(stagingPath, package);

            Debug.Log("Paquete nuevo validado: " + package.nombre);
            destinationPath = Path.Combine(packagesPath, package.id);

            // 2. Poner a salvo TODOS los paquetes anteriores.
            // El ZIP nuevo todavía está íntegro en STEM/Temp.
            if (Directory.Exists(packagesPath))
            {
                Directory.Move(packagesPath, backupPath);
                previousPackagesBackedUp = true;
            }

            // 3. Crear la carpeta vacía de paquetes e instalar SOLO el nuevo.
            Directory.CreateDirectory(packagesPath);
            Directory.Move(stagingPath, destinationPath);
            installationCommitted = true;

            // 4. Solo tras instalar correctamente se retiran los anteriores.
            if (previousPackagesBackedUp && Directory.Exists(backupPath))
            {
                try
                {
                    Directory.Delete(backupPath, true);
                    Debug.Log("Paquetes anteriores eliminados correctamente.");
                }
                catch (Exception cleanupException)
                {
                    // Se conserva el respaldo si el SO no permite eliminarlo.
                    Debug.LogWarning(
                        "Nuevo paquete instalado, pero no se pudo eliminar " +
                        "el respaldo temporal de paquetes anteriores: " +
                        backupPath + " | " + cleanupException.Message);
                }
            }

            Debug.Log(
                "Instalación por reemplazo completada. " +
                "Solo está activo el paquete: " + package.nombre +
                " | ID: " + package.id + " | Ruta: " + destinationPath);

            return destinationPath;
        }
        catch (Exception installException)
        {
            // Si la operación falló ANTES del respaldo, los paquetes
            // existentes nunca se tocaron. Si falló DESPUÉS, se restauran.
            if (previousPackagesBackedUp && !installationCommitted)
            {
                try
                {
                    // El contenido nuevo es descartable: el respaldo no.
                    if (Directory.Exists(packagesPath))
                        Directory.Delete(packagesPath, true);

                    Directory.Move(backupPath, packagesPath);
                    Debug.LogWarning(
                        "La instalación falló. Se restauraron TODOS los paquetes anteriores.");
                }
                catch (Exception restoreException)
                {
                    Debug.LogError(
                        "ATENCIÓN: no se pudo restaurar automáticamente " +
                        "la carpeta de paquetes anteriores. El respaldo " +
                        "permanece en: " + backupPath + "\n" +
                        restoreException);
                }
            }

            Debug.LogError(
                "No se instaló el ZIP nuevo: " + installException.Message);
            throw;
        }
        finally
        {
            // La extracción temporal se borra, sin tocar el respaldo.
            if (Directory.Exists(stagingPath))
            {
                try
                {
                    Directory.Delete(stagingPath, true);
                }
                catch (Exception cleanupException)
                {
                    Debug.LogWarning(
                        "No se pudo limpiar la extracción temporal: " +
                        cleanupException.Message);
                }
            }
        }
    }

    // ============================================
    // VALIDAR MANIFEST
    // ============================================

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

        // Evitar que el ID se utilice como una ruta.
        if (!Regex.IsMatch(
            package.id,
            @"^[A-Za-z0-9_-]+$"))
        {
            throw new Exception(
                "El ID del paquete solamente puede " +
                "contener letras, números, guiones y guiones bajos."
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

    // ============================================
    // VALIDAR ARCHIVOS REFERENCIADOS
    // ============================================

    private void ValidatePackageFiles(
        string rootPath,
        StemPackageModel package)
    {
        // ====================================
        // CUESTIONARIOS
        // ====================================

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

        // ====================================
        // HISTORIAS
        // ====================================

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

        // ====================================
        // VIDEOS
        // ====================================

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

        // ====================================
        // PORTADA
        // ====================================

        if (!string.IsNullOrWhiteSpace(
            package.portada))
        {
            ValidateReferencedFile(
                rootPath,
                package.portada,
                "portada"
            );
        }

        // ====================================
        // NUEVO: TEST VOCACIONAL
        // ====================================

        if (!string.IsNullOrWhiteSpace(
            package.testVocacional))
        {
            ValidateReferencedFile(
                rootPath,
                package.testVocacional,
                "test vocacional"
            );
        }
    }

    // ============================================
    // VALIDAR EXISTENCIA DE ARCHIVO
    // ============================================

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

        string fullPath = GetSafeFullPath(
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

    // ============================================
    // VALIDAR RUTAS SEGURAS
    // ============================================

    private string GetSafeFullPath(
        string rootPath,
        string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new Exception(
                "La ruta del archivo está vacía."
            );
        }

        if (Path.IsPathRooted(relativePath) ||
            relativePath.Contains(":"))
        {
            throw new Exception(
                "No se permiten rutas absolutas: " +
                relativePath
            );
        }

        string normalizedPath = relativePath
            .Replace(
                '\\',
                Path.DirectorySeparatorChar
            )
            .Replace(
                '/',
                Path.DirectorySeparatorChar
            );

        string rootFullPath = Path.GetFullPath(
            rootPath
        );

        string rootWithSeparator =
            rootFullPath.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar
            ) + Path.DirectorySeparatorChar;

        string fullPath = Path.GetFullPath(
            Path.Combine(
                rootFullPath,
                normalizedPath
            )
        );

        StringComparison comparison =
            Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

        if (!fullPath.StartsWith(
            rootWithSeparator,
            comparison))
        {
            throw new Exception(
                "Ruta no permitida dentro del paquete: " +
                relativePath
            );
        }

        return fullPath;
    }

    // ============================================
    // OBTENER RUTA DE PAQUETES
    // ============================================

    public string GetPackagesPath()
    {
        return packagesPath;
    }
}