using System;
using UnityEngine;

public class AppController : MonoBehaviour
{
    public static AppController Instance { get; private set; }

    public LoadedPackageModel CurrentPackage { get; private set; }
    public QuizModel CurrentQuiz { get; private set; }

    public int CurrentSequenceIndex { get; private set; }
    public int Score { get; private set; }

    public DateTime QuizStartTime { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SelectPackage(LoadedPackageModel package)
    {
        CurrentPackage = package;
        CurrentQuiz = null;

        CurrentSequenceIndex = 0;
        Score = 0;

        Debug.Log(
            $"Paquete seleccionado: {package.manifest.nombre}"
        );
    }

    public void StartQuiz(QuizModel quiz)
    {
        if (CurrentPackage == null)
        {
            Debug.LogError(
                "No hay un paquete seleccionado."
            );

            return;
        }

        CurrentQuiz = quiz;

        CurrentSequenceIndex = 0;
        Score = 0;

        QuizStartTime = DateTime.Now;

        Debug.Log(
            $"Quiz iniciado: {quiz.nombre}"
        );
    }

    public SequenceItemModel GetCurrentSequenceItem()
    {
        if (CurrentQuiz == null)
        {
            return null;
        }

        if (CurrentQuiz.secuencia == null)
        {
            return null;
        }

        if (CurrentSequenceIndex < 0 ||
            CurrentSequenceIndex >= CurrentQuiz.secuencia.Count)
        {
            return null;
        }

        return CurrentQuiz.secuencia[
            CurrentSequenceIndex
        ];
    }

    public bool MoveNext()
    {
        if (CurrentQuiz == null ||
            CurrentQuiz.secuencia == null)
        {
            return false;
        }

        CurrentSequenceIndex++;

        return CurrentSequenceIndex <
               CurrentQuiz.secuencia.Count;
    }

    public void AddPoint()
    {
        Score++;
    }

    public void ResetApplication()
    {
        CurrentPackage = null;
        CurrentQuiz = null;

        CurrentSequenceIndex = 0;
        Score = 0;
    }
}