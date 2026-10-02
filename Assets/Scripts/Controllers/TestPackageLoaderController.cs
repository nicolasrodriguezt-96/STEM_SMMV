using System.Collections.Generic;
using UnityEngine;

public class TestPackageLoaderController : MonoBehaviour
{
    private void Start()
    {
        PackageLoaderService loader =
            new PackageLoaderService();

        Debug.Log(
            "Buscando paquetes en: " +
            loader.GetPackagesPath()
        );

        List<LoadedPackageModel> packages =
            loader.LoadAllPackages();

        Debug.Log(
            $"Paquetes encontrados: {packages.Count}"
        );

        foreach (
            LoadedPackageModel package
            in packages)
        {
            Debug.Log(
                $"PAQUETE: {package.manifest.nombre}"
            );

            Debug.Log(
                $"ID: {package.manifest.id}"
            );

            Debug.Log(
                $"Cuestionarios: {package.cuestionarios.Count}"
            );

            Debug.Log(
                $"Historias: {package.historias.Count}"
            );

            Debug.Log(
                $"Videos: {package.videos.Count}"
            );

            foreach (
                QuizModel quiz
                in package.cuestionarios)
            {
                int cantidadPreguntas =
                    quiz.preguntas != null
                        ? quiz.preguntas.Count
                        : 0;

                Debug.Log(
                    $"Quiz: {quiz.nombre} - " +
                    $"{cantidadPreguntas} preguntas"
                );
            }
        }
    }
}