using System;
using UnityEngine;

public class TestLocalDatabaseController : MonoBehaviour
{
    [Header("Prueba SQLite")]

    [SerializeField]
    private bool createTestRecord = true;

    private void Start()
    {
        try
        {
            using (LocalDatabaseService database =
                   new LocalDatabaseService())
            {
                if (createTestRecord)
                {
                    CreateTestResult(database);
                }

                int total = database.CountResults();

                int pending = database
                    .GetPendingResults()
                    .Count;

                Debug.Log(
                    "RESULTADOS LOCALES: " + total
                );

                Debug.Log(
                    "PENDIENTES DE SINCRONIZAR: " + pending
                );

                Debug.Log(
                    "RUTA SQLITE: " +
                    database.DatabasePath
                );
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "Error probando SQLite:\n" + ex
            );
        }
    }

    private void CreateTestResult(
        LocalDatabaseService database)
    {
        DateTime finishedAt = DateTime.UtcNow;

        DateTime startedAt =
            finishedAt.AddSeconds(-185);

        LocalResultRecord result =
            new LocalResultRecord
            {
                Id = Guid.NewGuid().ToString(),

                DeviceId = "STEM-TEST-001",

                ResultType = "QUIZ",

                PackageId = "paquete_prueba",

                ContentId = "quiz_prueba",

                ContentName = "Cuestionario de prueba",

                StartedAtUtc = startedAt.ToString("o"),

                FinishedAtUtc = finishedAt.ToString("o"),

                DurationSeconds = 185,

                CorrectAnswers = 8,

                TotalQuestions = 10,

                ScorePercentage = 80,

                SyncStatus = 0
            };

        database.SaveResult(result);
    }
}