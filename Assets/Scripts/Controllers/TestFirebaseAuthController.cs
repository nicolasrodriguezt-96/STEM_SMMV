using UnityEngine;

public class TestFirebaseAuthController : MonoBehaviour
{
    [Header("Configuración Firebase")]

    [SerializeField]
    private string firebaseApiKey;

    [Header("Prueba de conexión")]

    [SerializeField]
    private bool testOnStart = true;

    private FirebaseAuthService authService;

    private void Start()
    {
        if (testOnStart)
        {
            TestConnection();
        }
    }

    public void TestConnection()
    {
        if (string.IsNullOrWhiteSpace(firebaseApiKey))
        {
            Debug.LogError(
                "Debes configurar la API Key de Firebase."
            );

            return;
        }

        authService =
            new FirebaseAuthService(firebaseApiKey);

        Debug.Log(
            "Iniciando prueba de autenticación con Firebase..."
        );

        StartCoroutine(
            authService.SignInAnonymously(
                OnAuthenticationSuccess,
                OnAuthenticationError
            )
        );
    }

    private void OnAuthenticationSuccess(
        FirebaseAuthSession session)
    {
        Debug.Log(
            "================================\n" +
            "FIREBASE AUTH OK\n" +
            "================================\n" +
            $"UID: {session.localId}\n" +
            "Autenticación: ANÓNIMA\n" +
            "Estado: CONECTADO\n" +
            "================================"
        );

        // No imprimimos los tokens:
        // son credenciales de autenticación.
    }

    private void OnAuthenticationError(string error)
    {
        Debug.LogError(
            "FIREBASE AUTH ERROR\n" +
            error
        );
    }
}