using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirebaseSyncController : MonoBehaviour
{
    // ========================================
    // FIREBASE
    // ========================================

    [Header("Firebase")]

    [SerializeField]
    private FirebaseSessionController sessionController;

    [SerializeField]
    private string firebaseApiKey;

    [SerializeField]
    private string firebaseProjectId = "uaofb-a3d66";

    // ========================================
    // SINCRONIZACIÓN
    // ========================================

    [Header("Sincronización automática")]

    [SerializeField]
    private bool syncAutomatically = true;

    [SerializeField]
    [Min(0f)]
    private float initialDelaySeconds = 5f;

    [SerializeField]
    [Min(10f)]
    private float syncIntervalSeconds = 60f;

    [SerializeField]
    [Range(1, 100)]
    private int maxResultsPerCycle = 20;

    [SerializeField]
    [Min(0f)]
    private float delayBetweenResultsSeconds = 0.2f;

    [SerializeField]
    [Min(5f)]
    private float authenticationWaitSeconds = 30f;

    // ========================================
    // ESTADO
    // ========================================

    private bool syncing;
    private bool loopRunning;

    private FirebaseAuthSession cachedSession;

    private DateTime tokenExpiresAtUtc =
        DateTime.MinValue;

    private FirebaseSessionStorageService
        sessionStorage;

    private FirebaseAuthService authService;

    private FirebaseFirestoreService firestoreService;

    // ========================================
    // INICIALIZACIÓN
    // ========================================

    private void Awake()
    {
        sessionStorage =
            new FirebaseSessionStorageService();
    }

    private IEnumerator Start()
    {
        if (!syncAutomatically)
        {
            yield break;
        }

        loopRunning = true;

        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, initialDelaySeconds)
        );

        while (loopRunning)
        {
            yield return SyncPendingResults();

            yield return new WaitForSecondsRealtime(
                Mathf.Max(10f, syncIntervalSeconds)
            );
        }
    }

    private void OnDisable()
    {
        loopRunning = false;
    }

    // ========================================
    // SINCRONIZACIÓN MANUAL
    // ========================================

    public void SyncNow()
    {
        if (syncing)
        {
            return;
        }

        StartCoroutine(
            SyncPendingResults()
        );
    }

    // ========================================
    // SINCRONIZACIÓN PRINCIPAL
    // ========================================

    private IEnumerator SyncPendingResults()
    {
        if (syncing)
        {
            yield break;
        }

        syncing = true;

        // =====================================
        // 1. COMPROBAR CONFIGURACIÓN
        // =====================================

        if (sessionController == null)
        {
            Debug.LogError(
                "FirebaseSyncController: falta FirebaseSessionController."
            );

            syncing = false;
            yield break;
        }

        if (string.IsNullOrWhiteSpace(firebaseApiKey))
        {
            Debug.LogError(
                "FirebaseSyncController: falta Firebase API Key."
            );

            syncing = false;
            yield break;
        }

        if (authService == null)
        {
            authService =
                new FirebaseAuthService(firebaseApiKey);
        }

        if (firestoreService == null)
        {
            firestoreService =
                new FirebaseFirestoreService(
                    firebaseProjectId
                );
        }

        // =====================================
        // 2. OBTENER RESULTADOS PENDIENTES
        // =====================================

        List<LocalResultRecord> pendingResults =
            GetPendingResults();

        if (pendingResults.Count == 0)
        {
            Debug.Log(
                "Firebase Sync: no hay resultados pendientes."
            );

            syncing = false;
            yield break;
        }

        Debug.Log(
            "FIREBASE SYNC\n" +
            $"Resultados pendientes para enviar: {pendingResults.Count}"
        );

        // =====================================
        // 3. ESPERAR AUTENTICACIÓN
        // =====================================

        if (!sessionController.IsAuthenticated)
        {
            sessionController.Connect();

            float waitStart =
                Time.realtimeSinceStartup;

            while (!sessionController.IsAuthenticated &&
                   Time.realtimeSinceStartup - waitStart <
                   authenticationWaitSeconds)
            {
                yield return null;
            }
        }

        if (!sessionController.IsAuthenticated)
        {
            Debug.LogWarning(
                "Firebase Sync: no hay sesión autenticada.\n" +
                "Los resultados seguirán pendientes."
            );

            syncing = false;
            yield break;
        }

        // =====================================
        // 4. OBTENER TOKEN ACTUAL
        // =====================================

        if (cachedSession == null)
        {
            CacheSession(
                sessionController.CurrentSession
            );
        }

        // =====================================
        // 5. RENOVAR TOKEN SI ES NECESARIO
        // =====================================

        if (DateTime.UtcNow >= tokenExpiresAtUtc)
        {
            FirebaseSessionRecord savedSession = null;

            try
            {
                savedSession =
                    sessionStorage.LoadSession();
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "Firebase Sync: error leyendo la sesión.\n" +
                    ex
                );
            }

            if (savedSession == null ||
                string.IsNullOrWhiteSpace(
                    savedSession.RefreshToken))
            {
                Debug.LogError(
                    "Firebase Sync: no existe un token " +
                    "de renovación guardado."
                );

                syncing = false;
                yield break;
            }

            bool refreshed = false;

            string refreshError = "";

            FirebaseAuthSession refreshedSession = null;

            yield return StartCoroutine(
                authService.RefreshSession(
                    savedSession.RefreshToken,

                    session =>
                    {
                        // Nunca sustituir silenciosamente
                        // la identidad del dispositivo.
                        if (session.localId != savedSession.UserId)
                        {
                            refreshError =
                                "El UID renovado no coincide " +
                                "con el UID almacenado.";

                            return;
                        }

                        refreshedSession = session;
                        refreshed = true;
                    },

                    error =>
                    {
                        refreshError = error;
                    }
                )
            );

            if (!refreshed)
            {
                Debug.LogWarning(
                    "Firebase Sync: no fue posible renovar la sesión.\n" +
                    refreshError +
                    "\nLos resultados permanecerán pendientes."
                );

                syncing = false;
                yield break;
            }

            try
            {
                sessionStorage.SaveSession(
                    refreshedSession.localId,
                    refreshedSession.refreshToken
                );

                CacheSession(refreshedSession);
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "Firebase Sync: no se pudo guardar " +
                    "la sesión renovada.\n" +
                    ex
                );

                syncing = false;
                yield break;
            }
        }

        // =====================================
        // 6. ENVIAR RESULTADOS
        // =====================================

        int synchronizedCount = 0;

        foreach (LocalResultRecord result
                 in pendingResults)
        {
            bool uploaded = false;

            string uploadError = "";

            yield return StartCoroutine(
                firestoreService.UploadResult(
                    result,

                    cachedSession.idToken,

                    cachedSession.localId,

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

            // =================================
            // SI FALLÓ, CONSERVAR PENDIENTE
            // =================================

            if (!uploaded)
            {
                Debug.LogWarning(
                    "Firebase Sync: no se pudo enviar.\n" +
                    $"ID: {result.Id}\n" +
                    uploadError +
                    "\nEl resultado permanece en SQLite."
                );

                // Si el servidor rechazó un token caducado,
                // forzaremos su renovación en el próximo ciclo.
                if (uploadError.Contains("HTTP: 401"))
                {
                    tokenExpiresAtUtc =
                        DateTime.MinValue;
                }

                // Detenemos este lote.
                // Los registros no confirmados permanecen pendientes.
                break;
            }

            // =================================
            // MARCAR COMO SINCRONIZADO
            // =================================

            bool markedAsSynced = false;

            try
            {
                using (LocalDatabaseService database =
                    new LocalDatabaseService())
                {
                    database.MarkAsSynced(
                        result.Id
                    );
                }

                markedAsSynced = true;
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "Firebase recibió el resultado, " +
                    "pero SQLite no pudo confirmar el estado.\n" +
                    ex
                );
            }

            if (!markedAsSynced)
            {
                // En el próximo intento, Firestore verificará
                // el documento existente antes de confirmarlo.
                break;
            }

            synchronizedCount++;

            Debug.Log(
                "Firebase Sync OK: " +
                result.ContentName +
                " | " +
                result.Id
            );

            if (delayBetweenResultsSeconds > 0)
            {
                yield return new WaitForSecondsRealtime(
                    delayBetweenResultsSeconds
                );
            }
        }

        // =====================================
        // 7. RESUMEN DEL LOTE
        // =====================================

        Debug.Log(
            "================================\n" +
            "FIREBASE SYNC FINALIZADO\n" +
            "================================\n" +
            $"Sincronizados en este ciclo: {synchronizedCount}\n" +
            $"Seleccionados para el ciclo: {pendingResults.Count}\n" +
            "================================"
        );

        syncing = false;
    }

    // ========================================
    // CONSULTAR PENDIENTES
    // ========================================

    private List<LocalResultRecord> GetPendingResults()
    {
        List<LocalResultRecord> results =
            new List<LocalResultRecord>();

        try
        {
            string currentDeviceId =
                PlayerPrefs.GetString(
                    "STEM_INSTALLATION_DEVICE_ID",
                    ""
                );

            if (string.IsNullOrWhiteSpace(currentDeviceId))
            {
                Debug.LogWarning(
                    "Firebase Sync: no hay un ID de instalación guardado."
                );

                return results;
            }

            using (LocalDatabaseService database =
                new LocalDatabaseService())
            {
                List<LocalResultRecord> pending =
                    database.GetPendingResults(2000);

                foreach (LocalResultRecord result in pending)
                {
                    if (result == null)
                    {
                        continue;
                    }

                    // No enviar registros de prueba
                    // pertenecientes a otras identidades.
                    if (result.DeviceId != currentDeviceId)
                    {
                        continue;
                    }

                    if (result.ResultType != "QUIZ" &&
                        result.ResultType != "VOCATIONAL")
                    {
                        continue;
                    }

                    results.Add(result);

                    if (results.Count >= maxResultsPerCycle)
                    {
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "Firebase Sync: error consultando SQLite.\n" +
                ex
            );
        }

        return results;
    }

    // ========================================
    // GUARDAR TOKEN EN MEMORIA
    // ========================================

    private void CacheSession(
        FirebaseAuthSession session)
    {
        if (session == null ||
            string.IsNullOrWhiteSpace(session.idToken))
        {
            cachedSession = null;

            tokenExpiresAtUtc =
                DateTime.MinValue;

            return;
        }

        cachedSession = session;

        int expiresIn = 3600;

        if (!string.IsNullOrWhiteSpace(session.expiresIn))
        {
            int parsed;

            if (int.TryParse(
                    session.expiresIn,
                    out parsed) &&
                parsed > 0)
            {
                expiresIn = parsed;
            }
        }

        // Dejamos un margen de seguridad
        // antes del vencimiento del token.
        int safeSeconds = Math.Max(
            10,
            expiresIn - 120
        );

        tokenExpiresAtUtc =
            DateTime.UtcNow.AddSeconds(
                safeSeconds
            );
    }

    // ========================================
    // REANUDAR APLICACIÓN
    // ========================================

    private void OnApplicationPause(bool paused)
    {
        if (!paused &&
            isActiveAndEnabled &&
            syncAutomatically)
        {
            SyncNow();
        }
    }
}