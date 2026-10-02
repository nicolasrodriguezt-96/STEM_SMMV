using UnityEngine;

public class TestPackageController : MonoBehaviour
{
    private void Start()
    {
        try
        {
            PackageService packageService =
                new PackageService();

            string zipPath =
                @"C:\STEM_TEST\paquete_valido.zip";

            string installedPath =
                packageService.InstallPackage(zipPath);

            Debug.Log(
                "VALIDACIÓN COMPLETA CORRECTA"
            );

            Debug.Log(
                "Paquete instalado en: " +
                installedPath
            );
        }
        catch (System.Exception ex)
        {
            Debug.LogError(
                "PAQUETE RECHAZADO: " +
                ex.Message
            );
        }
    }
}