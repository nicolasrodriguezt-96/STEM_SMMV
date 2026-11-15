using System;
using System.Collections;
using UnityEngine;

public class FirebaseSessionController : MonoBehaviour
{
    [Header("Firebase")]
    [SerializeField]
    private string firebaseApiKey;

    [Header("Inicialización")]
    [SerializeField]
    private bool connectOnStart = true;

    private FirebaseAuthService authService;

    private FirebaseSessionStorageService storageService;

    private bool connecting;

    public FirebaseAuthSession CurrentSession
    {
        get;
        private set;
    }

    public string FirebaseUid
    {
        get
        {
            return CurrentSession != null
                ? CurrentSession.localId
                : "";
        }
    }

    public bool IsAuthenticated
    {
        get
        {
            return CurrentSession != null &&
                   !string.IsNullOrWhiteSpace(
                       CurrentSession.idToken
                   );
        }
    }

    private void Awake()
    {
        storageService =
            new FirebaseSessionStorageService();
    }

    private void Start()
    {
        if (connectOnStart)
        {
            Connect();
        }
    }

    // ========================================
    // CONECTAR
    // ========================================

    public void Connect()
    {
        if (connecting)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(firebaseApiKey))
        {
            Debug.LogError(
                "Debes configurar la API Key de Firebase."
            );

            return;
        }

        authService =
            new FirebaseAuthService(firebaseApiKey);

        StartCoroutine(ConnectCoroutine());
    }

    private IEnumerator ConnectCoroutine()
    {
        connecting = true;

        FirebaseSessionRecord savedSession = null;

        try
        {
            savedSession =
                storageService.LoadSession();
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "No fue posible leer la sesión Firebase almacenada:\n" +
                ex
            );

            connecting = false;

            yield break;
        }

        if (savedSession != null &&
            !string.IsNullOrWhiteSpace(
                savedSession.RefreshToken))
        {
            Debug.Log(
                "Sesión Firebase encontrada. Renovando autenticación..."
            );

            string expectedUid =
                savedSession.UserId;

            yield return StartCoroutine(
                authService.RefreshSession(
                    savedSession.RefreshToken,

                    session =>
                    {
                        if (session.localId != expectedUid)
                        {
                            Debug.LogError(
                                "El UID renovado no coincide con " +
                                "la sesión almacenada."
                            );

                            return;
                        }

                        OnAuthenticationSuccess(session);
                    },

                    OnAuthenticationError
                )
            );
        }
        else
        {
            Debug.Log(
                "Primera autenticación Firebase del dispositivo..."
            );

            yield return StartCoroutine(
                authService.SignInAnonymously(
                    OnAuthenticationSuccess,
                    OnAuthenticationError
                )
            );
        }

        connecting = false;
    }

    // ========================================
    // AUTENTICACIÓN CORRECTA
    // ========================================

    private void OnAuthenticationSuccess(
        FirebaseAuthSession session)
    {
        try
        {
            // Primero guardamos la sesión.
            storageService.SaveSession(
                session.localId,
                session.refreshToken
            );

            // Después la dejamos disponible.
            CurrentSession = session;

            Debug.Log(
                "================================\n" +
                "FIREBASE SESSION OK\n" +
                "================================\n" +
                $"UID: {session.localId}\n" +
                "Sesión: GUARDADA EN SQLITE\n" +
                "Estado: AUTENTICADO\n" +
                "================================"
            );
        }
        catch (Exception ex)
        {
            CurrentSession = null;

            Debug.LogError(
                "La autenticación funcionó, pero " +
                "no fue posible guardar la sesión:\n" +
                ex
            );
        }
    }

    // ========================================
    // ERROR DE AUTENTICACIÓN
    // ========================================

    private void OnAuthenticationError(
        string error)
    {
        Debug.LogError(
            "FIREBASE SESSION ERROR\n" +
            error
        );

        // No borramos la sesión guardada.
        // Si no hay conexión, podremos reintentar después.
    }
}