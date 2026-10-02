using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PackageLoaderService
{
    private readonly string packagesPath;

    public PackageLoaderService()
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

    public List<LoadedPackageModel> LoadAllPackages()
    {
        List<LoadedPackageModel> packages =
            new List<LoadedPackageModel>();

        string[] directories =
            Directory.GetDirectories(packagesPath);

        foreach (string directory in directories)
        {
            try
            {
                LoadedPackageModel package =
                    LoadPackage(directory);

                packages.Add(package);
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"No fue posible cargar el paquete " +
                    $"{directory}: {ex.Message}"
                );
            }
        }

        return packages;
    }

    public LoadedPackageModel LoadPackage(
        string packagePath)
    {
        string manifestPath =
            Path.Combine(
                packagePath,
                "manifest.json"
            );

        if (!File.Exists(manifestPath))
        {
            throw new Exception(
                $"No existe manifest.json en: {packagePath}"
            );
        }

        string manifestJson =
            File.ReadAllText(manifestPath);

        StemPackageModel manifest =
            JsonUtility.FromJson<StemPackageModel>(
                manifestJson
            );

        if (manifest == null)
        {
            throw new Exception(
                "No fue posible leer manifest.json."
            );
        }

        LoadedPackageModel loadedPackage =
            new LoadedPackageModel
            {
                rootPath = packagePath,
                manifest = manifest
            };

        LoadQuizzes(loadedPackage);
        LoadStories(loadedPackage);
        LoadVideos(loadedPackage);

        return loadedPackage;
    }

    private void LoadQuizzes(
        LoadedPackageModel package)
    {
        if (package.manifest.cuestionarios == null)
        {
            return;
        }

        foreach (
            string relativePath
            in package.manifest.cuestionarios)
        {
            string fullPath =
                Path.Combine(
                    package.rootPath,
                    relativePath
                );

            if (!File.Exists(fullPath))
            {
                throw new Exception(
                    $"No existe el cuestionario: {relativePath}"
                );
            }

            string json =
                File.ReadAllText(fullPath);

            QuizModel quiz =
                JsonUtility.FromJson<QuizModel>(
                    json
                );

            if (quiz == null)
            {
                throw new Exception(
                    $"No fue posible leer el cuestionario: {relativePath}"
                );
            }

            package.cuestionarios.Add(quiz);
        }
    }

    private void LoadStories(
        LoadedPackageModel package)
    {
        if (package.manifest.historias == null)
        {
            return;
        }

        foreach (
            string relativePath
            in package.manifest.historias)
        {
            string fullPath =
                Path.Combine(
                    package.rootPath,
                    relativePath
                );

            if (!File.Exists(fullPath))
            {
                throw new Exception(
                    $"No existe la historia: {relativePath}"
                );
            }

            string json =
                File.ReadAllText(fullPath);

            StoryModel story =
                JsonUtility.FromJson<StoryModel>(
                    json
                );

            if (story == null)
            {
                throw new Exception(
                    $"No fue posible leer la historia: {relativePath}"
                );
            }

            package.historias.Add(story);
        }
    }

    private void LoadVideos(
        LoadedPackageModel package)
    {
        if (package.manifest.videos == null)
        {
            return;
        }

        foreach (
            string relativePath
            in package.manifest.videos)
        {
            string fullPath =
                Path.Combine(
                    package.rootPath,
                    relativePath
                );

            if (!File.Exists(fullPath))
            {
                throw new Exception(
                    $"No existe la definición del video: {relativePath}"
                );
            }

            string json =
                File.ReadAllText(fullPath);

            VideoModel video =
                JsonUtility.FromJson<VideoModel>(
                    json
                );

            if (video == null)
            {
                throw new Exception(
                    $"No fue posible leer el video: {relativePath}"
                );
            }

            package.videos.Add(video);
        }
    }

    public string GetPackagesPath()
    {
        return packagesPath;
    }
}