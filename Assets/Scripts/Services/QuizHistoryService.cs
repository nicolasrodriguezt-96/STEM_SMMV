using System;
using System.IO;
using UnityEngine;

public class QuizHistoryService
{
    private readonly string directoryPath;
    private readonly string filePath;

    public QuizHistoryService()
    {
        directoryPath = Path.Combine(
            Application.persistentDataPath,
            "STEM",
            "Data"
        );

        filePath = Path.Combine(
            directoryPath,
            "quiz_history.json"
        );

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    public QuizHistoryModel Load()
    {
        if (!File.Exists(filePath))
        {
            return new QuizHistoryModel();
        }

        string json = File.ReadAllText(filePath);

        if (string.IsNullOrWhiteSpace(json))
        {
            return new QuizHistoryModel();
        }

        QuizHistoryModel history =
            JsonUtility.FromJson<QuizHistoryModel>(json);

        return history ?? new QuizHistoryModel();
    }

    public void SaveExecution(
        string quizId,
        string quizNombre,
        DateTime fechaInicio,
        DateTime fechaFin,
        float tiempoSegundos,
        int puntosObtenidos,
        int puntosMaximos)
    {
        QuizHistoryModel history = Load();

        float porcentaje = 0f;

        if (puntosMaximos > 0)
        {
            porcentaje =
                ((float)puntosObtenidos / puntosMaximos) * 100f;
        }

        QuizExecutionModel execution = new QuizExecutionModel
        {
            quizId = quizId,
            quizNombre = quizNombre,

            fechaInicio = fechaInicio.ToString("yyyy-MM-ddTHH:mm:ss"),
            fechaFin = fechaFin.ToString("yyyy-MM-ddTHH:mm:ss"),

            tiempoSegundos = tiempoSegundos,

            puntosObtenidos = puntosObtenidos,
            puntosMaximos = puntosMaximos,

            porcentaje = porcentaje
        };

        history.ejecuciones.Add(execution);

        Save(history);

        Debug.Log(
            $"Quiz guardado: {quizNombre} - " +
            $"{puntosObtenidos}/{puntosMaximos} - " +
            $"{porcentaje:0.##}%"
        );
    }

    private void Save(QuizHistoryModel history)
    {
        string json = JsonUtility.ToJson(history, true);

        File.WriteAllText(filePath, json);
    }

    public string GetFilePath()
    {
        return filePath;
    }
}