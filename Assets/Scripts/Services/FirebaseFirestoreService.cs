using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class FirebaseFirestoreService
{
    private readonly string projectId;

    private const string BaseUrl =
        "https://firestore.googleapis.com/v1/projects/";

    // ========================================
    // MODELOS DE FIRESTORE
    // ========================================

    [Serializable]
    private class StringValue
    {
        public string stringValue;

        public StringValue(string value)
        {
            stringValue = value ?? "";
        }
    }

    [Serializable]
    private class IntegerValue
    {
        public string integerValue;

        public IntegerValue(int value)
        {
            integerValue = value.ToString(
                System.Globalization.CultureInfo.InvariantCulture
            );
        }
    }

    [Serializable]
    private class DoubleValue
    {
        public double doubleValue;

        public DoubleValue(double value)
        {
            doubleValue = value;
        }
    }

    [Serializable]
    private class FirestoreFields
    {
        public StringValue id;
        public StringValue ownerUid;
        public StringValue deviceId;
        public StringValue resultType;

        public StringValue packageId;
        public StringValue contentId;
        public StringValue contentName;

        public StringValue startedAtUtc;
        public StringValue finishedAtUtc;

        public DoubleValue durationSeconds;

        public IntegerValue correctAnswers;
        public IntegerValue totalQuestions;

        public DoubleValue scorePercentage;

        public StringValue vocationalScoresJson;
        public StringValue recommendedCareersJson;
    }

    [Serializable]
    private class FirestoreDocument
    {
        public FirestoreFields fields;
    }

    // ========================================
    // CONSTRUCTOR
    // ========================================

    public FirebaseFirestoreService(string firebaseProjectId)
    {
        if (string.IsNullOrWhiteSpace(firebaseProjectId))
        {
            throw new ArgumentException(
                "El ID del proyecto Firebase está vacío."
            );
        }

        projectId = firebaseProjectId.Trim();
    }

    // ========================================
    // OBTENER RUTA DE LA COLECCIÓN
    // ========================================

    private string GetCollectionUrl()
    {
        return BaseUrl +
               projectId +
               "/databases/(default)/documents/results";
    }

    // ========================================
    // ENVIAR RESULTADO
    // ========================================

    public IEnumerator UploadResult(
        LocalResultRecord result,
        string idToken,
        string ownerUid,
        Action onSuccess,
        Action<string> onError)
    {
        if (result == null)
        {
            onError?.Invoke(
                "El resultado es nulo."
            );

            yield break;
        }

        if (string.IsNullOrWhiteSpace(result.Id))
        {
            onError?.Invoke(
                "El resultado no tiene ID."
            );

            yield break;
        }

        if (string.IsNullOrWhiteSpace(idToken) ||
            string.IsNullOrWhiteSpace(ownerUid))
        {
            onError?.Invoke(
                "No existe una sesión Firebase válida."
            );

            yield break;
        }

        // =====================================
        // 1. PREPARAR DOCUMENTO
        // =====================================

        FirestoreDocument document =
            BuildDocument(result, ownerUid);

        string json =
            JsonUtility.ToJson(document);

        // =====================================
        // 2. CONSTRUIR URL
        // =====================================

        string url =
            GetCollectionUrl() +
            "?documentId=" +
            UnityWebRequest.EscapeURL(result.Id);

        // =====================================
        // 3. REALIZAR SOLICITUD
        // =====================================

        bool created = false;
        bool alreadyExists = false;

        string error = "";

        using (UnityWebRequest request =
            new UnityWebRequest(
                url,
                UnityWebRequest.kHttpVerbPOST
            ))
        {
            request.uploadHandler =
                new UploadHandlerRaw(
                    Encoding.UTF8.GetBytes(json)
                );

            request.downloadHandler =
                new DownloadHandlerBuffer();

            request.SetRequestHeader(
                "Content-Type",
                "application/json"
            );

            request.SetRequestHeader(
                "Authorization",
                "Bearer " + idToken
            );

            request.timeout = 25;

            yield return request.SendWebRequest();

            if (request.result ==
                UnityWebRequest.Result.Success)
            {
                created = true;
            }
            else if (request.responseCode == 409)
            {
                // El documento podría haberse guardado
                // anteriormente, pero Unity no recibió
                // la confirmación.
                alreadyExists = true;
            }
            else
            {
                error =
                    $"HTTP: {request.responseCode}\n" +
                    $"Error: {request.error}\n" +
                    $"Respuesta: {request.downloadHandler.text}";
            }
        }

        // =====================================
        // 4. COMPROBAR RESULTADO
        // =====================================

        if (created)
        {
            onSuccess?.Invoke();

            yield break;
        }

        if (alreadyExists)
        {
            // No aceptamos automáticamente un 409.
            // Primero verificamos el documento existente.

            yield return VerifyExistingResult(
                result,
                idToken,
                ownerUid,
                onSuccess,
                onError
            );

            yield break;
        }

        onError?.Invoke(error);
    }

    // ========================================
    // CREAR DOCUMENTO FIRESTORE
    // ========================================

    private FirestoreDocument BuildDocument(
        LocalResultRecord result,
        string ownerUid)
    {
        FirestoreFields fields =
            new FirestoreFields
            {
                id = new StringValue(result.Id),

                ownerUid = new StringValue(ownerUid),

                deviceId = new StringValue(
                    result.DeviceId
                ),

                resultType = new StringValue(
                    result.ResultType
                ),

                packageId = new StringValue(
                    result.PackageId
                ),

                contentId = new StringValue(
                    result.ContentId
                ),

                contentName = new StringValue(
                    result.ContentName
                ),

                startedAtUtc = new StringValue(
                    result.StartedAtUtc
                ),

                finishedAtUtc = new StringValue(
                    result.FinishedAtUtc
                ),

                durationSeconds = new DoubleValue(
                    result.DurationSeconds
                ),

                correctAnswers = new IntegerValue(
                    result.CorrectAnswers
                ),

                totalQuestions = new IntegerValue(
                    result.TotalQuestions
                ),

                scorePercentage = new DoubleValue(
                    result.ScorePercentage
                ),

                vocationalScoresJson = new StringValue(
                    result.VocationalScoresJson
                ),

                recommendedCareersJson = new StringValue(
                    result.RecommendedCareersJson
                )
            };

        return new FirestoreDocument
        {
            fields = fields
        };
    }

    // ========================================
    // VERIFICAR DOCUMENTO EXISTENTE
    // ========================================

    private IEnumerator VerifyExistingResult(
        LocalResultRecord result,
        string idToken,
        string ownerUid,
        Action onSuccess,
        Action<string> onError)
    {
        string url =
            GetCollectionUrl() +
            "/" +
            UnityWebRequest.EscapeURL(result.Id);

        using (UnityWebRequest request =
            UnityWebRequest.Get(url))
        {
            request.SetRequestHeader(
                "Authorization",
                "Bearer " + idToken
            );

            request.timeout = 25;

            yield return request.SendWebRequest();

            if (request.result !=
                UnityWebRequest.Result.Success)
            {
                onError?.Invoke(
                    "El documento ya existe, pero no " +
                    "fue posible verificarlo.\n" +
                    $"HTTP: {request.responseCode}\n" +
                    request.error
                );

                yield break;
            }

            FirestoreDocument existingDocument;

            try
            {
                existingDocument =
                    JsonUtility.FromJson<FirestoreDocument>(
                        request.downloadHandler.text
                    );
            }
            catch (Exception ex)
            {
                onError?.Invoke(
                    "Error interpretando el documento existente: " +
                    ex.Message
                );

                yield break;
            }

            if (!MatchesExistingResult(
                    existingDocument,
                    result,
                    ownerUid))
            {
                onError?.Invoke(
                    "El documento existente no coincide " +
                    "con el resultado local."
                );

                yield break;
            }

            Debug.Log(
                "Resultado encontrado y verificado en Firebase: " +
                result.Id
            );

            onSuccess?.Invoke();
        }
    }

    // ========================================
    // COMPARAR RESULTADOS
    // ========================================

    private bool MatchesExistingResult(
        FirestoreDocument document,
        LocalResultRecord result,
        string ownerUid)
    {
        if (document == null ||
            document.fields == null)
        {
            return false;
        }

        FirestoreFields fields = document.fields;

        return
            Matches(fields.id, result.Id) &&

            Matches(fields.ownerUid, ownerUid) &&

            Matches(fields.deviceId, result.DeviceId) &&

            Matches(fields.resultType, result.ResultType) &&

            Matches(fields.packageId, result.PackageId) &&

            Matches(fields.contentId, result.ContentId) &&

            Matches(fields.contentName, result.ContentName) &&

            Matches(fields.startedAtUtc, result.StartedAtUtc) &&

            Matches(fields.finishedAtUtc, result.FinishedAtUtc) &&

            Matches(
                fields.vocationalScoresJson,
                result.VocationalScoresJson
            ) &&

            Matches(
                fields.recommendedCareersJson,
                result.RecommendedCareersJson
            );
    }

    private bool Matches(
        StringValue value,
        string expected)
    {
        return value != null &&
               string.Equals(
                   value.stringValue,
                   expected ?? "",
                   StringComparison.Ordinal
               );
    }
}