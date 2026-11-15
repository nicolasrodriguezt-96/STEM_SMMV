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
        {
            throw new Exception(
                "La ruta del ZIP está vacía."
            );
        }

        if (!File.Exists(zipPath))
        {
            throw new Exception(
                "No se encontró el archivo ZIP: " + zipPath
            );
        }

        if (!string.Equals(
            Path.GetExtension(zipPath),
            ".zip",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception(
                "El archivo seleccionado debe ser un ZIP."
            );
        }

        string tempPath = Path.Combine(
            tempRootPath,
            Guid.NewGuid().ToString("N")
        );

        Directory.CreateDirectory(tempPath);

        string destinationPath = null;
        string backupPath = null;

        bool previousPackageMoved = false;
        bool newPackageInstalled = false;

        try
        {
            // ====================================
            // 1. EXTRAER ZIP
            // ====================================

            Debug.Log(
                "Extrayendo paquete: " +
                Path.GetFileName(zipPath)
            );

            ZipFile.ExtractToDirectory(
                zipPath,
                tempPath
            );

            // ====================================
            // 2. BUSCAR MANIFEST
            // ====================================

            string manifestPath = Path.Combine(
                tempPath,
                "manifest.json"
            );

            if (!File.Exists(manifestPath))
            {
                throw new Exception(
                    "El ZIP no contiene manifest.json " +
                    "en su carpeta raíz."
                );
            }

            // ====================================
            // 3. LEER MANIFEST
            // ====================================

            string manifestJson =
                File.ReadAllText(manifestPath);

            StemPackageModel package =
                JsonUtility.FromJson<StemPackageModel>(
                    manifestJson
                );

            // ====================================
            // 4. VALIDAR MANIFEST
            // ====================================

            ValidateManifest(package);

            Debug.Log(
                "Manifest válido: " +
                package.nombre
            );

            // ====================================
            // 5. VALIDAR ARCHIVOS
            // ====================================

            ValidatePackageFiles(
                tempPath,
                package
            );

            // ====================================
            // 6. VALIDACIÓN PROFUNDA
            // ====================================

            PackageValidatorService validator =
                new PackageValidatorService();

            validator.ValidatePackage(
                tempPath,
                package
            );

            Debug.Log(
                "Validación del contenido completada."
            );

            // ====================================
            // 7. VALIDAR TEST VOCACIONAL ÚNICO
            // ====================================

            ValidateSingleVocationalTest(
                package
            );

            // ====================================
            // 8. PREPARAR DESTINO
            // ====================================

            destinationPath = Path.Combine(
                packagesPath,
                package.id
            );

            // ====================================
            // 9. RESPALDAR VERSIÓN ANTERIOR
            // ====================================

            if (Directory.Exists(destinationPath))
            {
                Debug.Log(
                    "Actualizando paquete existente: " +
                    package.id
                );

                backupPath = Path.Combine(
                    tempRootPath,
                    "backup_" +
                    Guid.NewGuid().ToString("N")
                );

                Directory.Move(
                    destinationPath,
                    backupPath
                );

                previousPackageMoved = true;
            }

            // ====================================
            // 10. INSTALAR PAQUETE VALIDADO
            // ====================================

            Directory.Move(
                tempPath,
                destinationPath
            );

            newPackageInstalled = true;

            // ====================================
            // 11. LIMPIAR RESPALDO ANTERIOR
            // ====================================

            if (previousPackageMoved &&
                Directory.Exists(backupPath))
            {
                try
                {
                    Directory.Delete(
                        backupPath,
                        true
                    );
                }
                catch (Exception cleanupException)
                {
                    Debug.LogWarning(
                        "El paquete se instaló, pero no fue " +
                        "posible eliminar el respaldo temporal: " +
                        cleanupException.Message
                    );
                }
            }

            // ====================================
            // 12. INSTALACIÓN CORRECTA
            // ====================================

            Debug.Log(
                "Paquete instalado: " +
                package.nombre
            );

            Debug.Log(
                "ID: " + package.id
            );

            Debug.Log(
                "Ruta: " + destinationPath
            );

            if (!string.IsNullOrWhiteSpace(
                package.testVocacional))
            {
                Debug.Log(
                    "Este paquete incluye el test vocacional STEM."
                );
            }

            return destinationPath;
        }
        catch
        {
            // ====================================
            // RECUPERAR PAQUETE ANTERIOR
            // ====================================

            if (previousPackageMoved &&
                !newPackageInstalled &&
                backupPath != null &&
                destinationPath != null &&
                Directory.Exists(backupPath) &&
                !Directory.Exists(destinationPath))
            {
                try
                {
                    Directory.Move(
                        backupPath,
                        destinationPath
                    );

                    Debug.LogWarning(
                        "Se restauró el paquete anterior."
                    );
                }
                catch (Exception restoreException)
                {
                    Debug.LogError(
                        "No se pudo restaurar el paquete anterior. " +
                        "El respaldo permanece en: " +
                        backupPath + "\n" +
                        restoreException.Message
                    );
                }
            }

            // ====================================
            // LIMPIAR EXTRACCIÓN TEMPORAL
            // ====================================

            if (Directory.Exists(tempPath))
            {
                try
                {
                    Directory.Delete(
                        tempPath,
                        true
                    );
                }
                catch (Exception cleanupException)
                {
                    Debug.LogWarning(
                        "No se pudo limpiar la carpeta temporal: " +
                        cleanupException.Message
                    );
                }
            }

            throw;
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
    // VALIDAR QUE SOLO EXISTA UN TEST VOCACIONAL
    // ============================================

    private void ValidateSingleVocationalTest(
        StemPackageModel incomingPackage)
    {
        // Si no trae test vocacional,
        // no necesita comprobar exclusividad.
        if (string.IsNullOrWhiteSpace(
            incomingPackage.testVocacional))
        {
            return;
        }

        string[] installedDirectories =
            Directory.GetDirectories(packagesPath);

        foreach (string directory in installedDirectories)
        {
            string installedPackageId =
                Path.GetFileName(directory);

            // Permitir actualizar el mismo paquete.
            if (string.Equals(
                installedPackageId,
                incomingPackage.id,
                StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string manifestPath = Path.Combine(
                directory,
                "manifest.json"
            );

            if (!File.Exists(manifestPath))
            {
                continue;
            }

            string json =
                File.ReadAllText(manifestPath);

            StemPackageModel installedPackage =
                JsonUtility.FromJson<StemPackageModel>(
                    json
                );

            if (installedPackage == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(
                installedPackage.testVocacional))
            {
                throw new Exception(
                    "Ya existe un test vocacional instalado " +
                    $"en el paquete '{installedPackage.nombre}'. " +
                    "Solo puede existir uno activo. " +
                    "Actualiza ese paquete o desinstálalo primero."
                );
            }
        }
    }

    // ============================================
    // OBTENER RUTA DE PAQUETES
    // ============================================

    public string GetPackagesPath()
    {
        return packagesPath;
    }
}