using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// ========================================
// SESIÓN FIREBASE
// ========================================

[Serializable]
public class FirebaseAuthSession
{
    public string idToken;
    public string refreshToken;
    public string expiresIn;
    public string localId;
}

// ========================================
// RESPUESTA DE RENOVACIÓN
// ========================================

[Serializable]
public class FirebaseRefreshResponse
{
    public string id_token;
    public string refresh_token;
    public string expires_in;
    public string user_id;
}

// ========================================
// SERVICIO FIREBASE AUTH
// ========================================

public class FirebaseAuthService
{
    private readonly string apiKey;

    private const string SignUpEndpoint =
        "https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=";

    private const string RefreshEndpoint =
        "https://securetoken.googleapis.com/v1/token?key=";

    public FirebaseAuthService(string firebaseApiKey)
    {
        if (string.IsNullOrWhiteSpace(firebaseApiKey))
        {
            throw new ArgumentException(
                "La API Key de Firebase está vacía."
            );
        }

        apiKey = firebaseApiKey.Trim();
    }

    // ========================================
    // AUTENTICACIÓN ANÓNIMA
    // ========================================

    public IEnumerator SignInAnonymously(
        Action<FirebaseAuthSession> onSuccess,
        Action<string> onError)
    {
        string url =
            SignUpEndpoint +
            UnityWebRequest.EscapeURL(apiKey);

        string body =
            "{\"returnSecureToken\":true}";

        using (UnityWebRequest request =
            CreatePostRequest(
                url,
                body,
                "application/json"
            ))
        {
            yield return request.SendWebRequest();

            if (request.result !=
                UnityWebRequest.Result.Success)
            {
                onError?.Invoke(
                    GetRequestError(request)
                );

                yield break;
            }

            FirebaseAuthSession session;

            try
            {
                session =
                    JsonUtility.FromJson<FirebaseAuthSession>(
                        request.downloadHandler.text
                    );
            }
            catch (Exception ex)
            {
                onError?.Invoke(
                    "Respuesta de autenticación inválida: " +
                    ex.Message
                );

                yield break;
            }

            if (!IsValidSession(session))
            {
                onError?.Invoke(
                    "Firebase devolvió una sesión incompleta."
                );

                yield break;
            }

            onSuccess?.Invoke(session);
        }
    }

    // ========================================
    // RENOVAR SESIÓN EXISTENTE
    // ========================================

    public IEnumerator RefreshSession(
        string refreshToken,
        Action<FirebaseAuthSession> onSuccess,
        Action<string> onError)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            onError?.Invoke(
                "No existe un refreshToken para renovar la sesión."
            );

            yield break;
        }

        string url =
            RefreshEndpoint +
            UnityWebRequest.EscapeURL(apiKey);

        string body =
            "grant_type=refresh_token" +
            "&refresh_token=" +
            UnityWebRequest.EscapeURL(refreshToken);

        using (UnityWebRequest request =
            CreatePostRequest(
                url,
                body,
                "application/x-www-form-urlencoded"
            ))
        {
            yield return request.SendWebRequest();

            if (request.result !=
                UnityWebRequest.Result.Success)
            {
                onError?.Invoke(
                    GetRequestError(request)
                );

                yield break;
            }

            FirebaseRefreshResponse response;

            try
            {
                response =
                    JsonUtility.FromJson<FirebaseRefreshResponse>(
                        request.downloadHandler.text
                    );
            }
            catch (Exception ex)
            {
                onError?.Invoke(
                    "Respuesta de renovación inválida: " +
                    ex.Message
                );

                yield break;
            }

            if (response == null)
            {
                onError?.Invoke(
                    "Firebase no devolvió datos de renovación."
                );

                yield break;
            }

            FirebaseAuthSession session =
                new FirebaseAuthSession
                {
                    idToken = response.id_token,
                    refreshToken = response.refresh_token,
                    expiresIn = response.expires_in,
                    localId = response.user_id
                };

            if (!IsValidSession(session))
            {
                onError?.Invoke(
                    "Firebase devolvió una sesión renovada incompleta."
                );

                yield break;
            }

            onSuccess?.Invoke(session);
        }
    }

    // ========================================
    // CREAR SOLICITUD HTTP
    // ========================================

    private UnityWebRequest CreatePostRequest(
        string url,
        string body,
        string contentType)
    {
        UnityWebRequest request =
            new UnityWebRequest(
                url,
                UnityWebRequest.kHttpVerbPOST
            );

        request.uploadHandler =
            new UploadHandlerRaw(
                Encoding.UTF8.GetBytes(body)
            );

        request.downloadHandler =
            new DownloadHandlerBuffer();

        request.SetRequestHeader(
            "Content-Type",
            contentType
        );

        request.timeout = 20;

        return request;
    }

    // ========================================
    // VALIDACIONES
    // ========================================

    private bool IsValidSession(
        FirebaseAuthSession session)
    {
        return session != null &&
               !string.IsNullOrWhiteSpace(session.idToken) &&
               !string.IsNullOrWhiteSpace(session.refreshToken) &&
               !string.IsNullOrWhiteSpace(session.localId);
    }

    private string GetRequestError(
        UnityWebRequest request)
    {
        return
            $"HTTP: {request.responseCode}\n" +
            $"Error: {request.error}\n" +
            $"Respuesta: {request.downloadHandler.text}";
    }
}