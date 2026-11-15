using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestFirebaseFirestoreController : MonoBehaviour
{
    [Header("Firebase")]
    [SerializeField]
    private FirebaseSessionController sessionController;

    [SerializeField]
    private string firebaseProjectId = "uaofb-a3d66";

    [Header("Prueba")]
    [SerializeField]
    private bool uploadOneOnStart = true;

    [SerializeField]
    private float waitForAuthenticationSeconds = 30f;

    private bool uploading;

    // ========================================
    // INICIO
    // ========================================

    private IEnumerator Start()
    {
        if (uploadOneOnStart)
        {
            yield return UploadOneCoroutine();
        }
    }

    // ========================================
    // EJECUTAR MANUALMENTE
    // ========================================

    public void UploadOne()
    {
        if (!uploading)
        {
            StartCoroutine(
                UploadOneCoroutine()
            );
        }
    }

    // ========================================
    // SINCRONIZAR UN RESULTADO
    // ========================================

    private IEnumerator UploadOneCoroutine()
    {
        if (uploading)
        {
            yield break;
        }

        uploading = true;

        // =====================================
        // 1. ESPERAR AUTENTICACIÓN
        // =====================================

        if (sessionController == null)
        {
            Debug.LogError(
                "FirebaseSessionController no está asignado."
            );

            uploading = false;
            yield break;
        }

        float startWait = Time.realtimeSinceStartup;

        while (!sessionController.IsAuthenticated &&
               Time.realtimeSinceStartup - startWait <
               waitForAuthenticationSeconds)
        {
            yield return null;
        }

        if (!sessionController.IsAuthenticated)
        {
            Debug.LogWarning(
                "Firebase todavía no está autenticado. " +
                "Los resultados permanecen pendientes."
            );

            uploading = false;
            yield break;
        }

        // =====================================
        // 2. BUSCAR RESULTADO PENDIENTE
        // =====================================

        LocalResultRecord result =
            FindPendingResult();

        if (result == null)
        {
            Debug.Log(
                "No se encontraron resultados reales " +
                "pendientes para esta instalación."
            );

            uploading = false;
            yield break;
        }

        Debug.Log(
            "INICIANDO SINCRONIZACIÓN\n" +
            $"ID: {result.Id}\n" +
            $"Tipo: {result.ResultType}\n" +
            $"Nombre: {result.ContentName}"
        );

        // =====================================
        // 3. ENVIAR A FIREBASE
        // =====================================

        bool uploaded = false;
        string uploadError = "";

        FirebaseFirestoreService firestore =
            new FirebaseFirestoreService(
                firebaseProjectId
            );

        yield return StartCoroutine(
            firestore.UploadResult(
                result,

                sessionController.CurrentSession.idToken,

                sessionController.FirebaseUid,

                () =>
                {
                    uploaded = true;
                },

                error =>
                {
                    uploadError = error;
                }
            )
        );

        // =====================================
        // 4. COMPROBAR RESULTADO
        // =====================================

        if (uploaded)
        {
            try
            {
                using (LocalDatabaseService database =
                    new LocalDatabaseService())
                {
                    database.MarkAsSynced(
                        result.Id
                    );
                }

                Debug.Log(
                    "================================\n" +
                    "SINCRONIZACIÓN EXITOSA\n" +
                    "================================\n" +
                    $"ID: {result.Id}\n" +
                    $"Nombre: {result.ContentName}\n" +
                    "Firebase: GUARDADO\n" +
                    "SQLite: SINCRONIZADO\n" +
                    "================================"
                );
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "Firebase recibió el resultado, " +
                    "pero no se pudo actualizar SQLite.\n" +
                    "El registro seguirá pendiente y " +
                    "se verificará en el próximo intento.\n" +
                    ex
                );
            }
        }
        else
        {
            Debug.LogError(
                "ERROR DE SINCRONIZACIÓN\n" +
                uploadError + "\n" +
                "El resultado permanece pendiente en SQLite."
            );
        }

        uploading = false;
    }

    // ========================================
    // OBTENER RESULTADO PENDIENTE
    // ========================================

    private LocalResultRecord FindPendingResult()
    {
        try
        {
            using (LocalDatabaseService database =
                new LocalDatabaseService())
            {
                List<LocalResultRecord> pending =
                    database.GetPendingResults(100);

                // Filtramos los datos de prueba antiguos.
                // Solo enviaremos resultados de esta instalación.

                string currentDeviceId =
                    PlayerPrefs.GetString(
                        "STEM_INSTALLATION_DEVICE_ID",
                        ""
                    );

                foreach (LocalResultRecord result in pending)
                {
                    if (result == null)
                    {
                        continue;
                    }

                    if (result.DeviceId != currentDeviceId)
                    {
                        continue;
                    }

                    if (result.ResultType != "QUIZ" &&
                        result.ResultType != "VOCATIONAL")
                    {
                        continue;
                    }

                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "Error consultando SQLite:\n" +
                ex
            );
        }

        return null;
    }
}