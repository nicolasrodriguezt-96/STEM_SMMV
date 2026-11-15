using System;
using System.Collections.Generic;

public class VocationalScoringService
{
    private VocationalTestModel currentTest;

    private readonly Dictionary<string, int> scores =
        new Dictionary<string, int>();

    private int answeredQuestions;

    public void StartTest(VocationalTestModel test)
    {
        if (test == null ||
            test.carreras == null ||
            test.carreras.Count != 4)
        {
            throw new ArgumentException(
                "El test vocacional debe contener exactamente 4 carreras."
            );
        }

        currentTest = test;

        scores.Clear();
        answeredQuestions = 0;

        foreach (VocationalCareerModel career in test.carreras)
        {
            if (career == null ||
                string.IsNullOrWhiteSpace(career.id) ||
                scores.ContainsKey(career.id))
            {
                throw new ArgumentException(
                    "Hay carreras inválidas o repetidas."
                );
            }

            scores.Add(career.id, 0);
        }
    }

    public void RegisterAnswer(VocationalAnswerModel answer)
    {
        if (currentTest == null ||
            answer == null ||
            string.IsNullOrWhiteSpace(answer.carreraId) ||
            !scores.ContainsKey(answer.carreraId))
        {
            throw new ArgumentException(
                "La respuesta contiene una carrera inválida."
            );
        }

        if (answeredQuestions >= GetTotalQuestions())
        {
            throw new InvalidOperationException(
                "El test ya tiene todas sus respuestas."
            );
        }

        scores[answer.carreraId]++;

        answeredQuestions++;
    }

    public List<VocationalCareerModel> GetRecommendations()
    {
        List<VocationalCareerModel> recommendations =
            new List<VocationalCareerModel>();

        if (currentTest == null || answeredQuestions == 0)
        {
            return recommendations;
        }

        int maxScore = 0;

        foreach (int score in scores.Values)
        {
            if (score > maxScore)
            {
                maxScore = score;
            }
        }

        foreach (VocationalCareerModel career in currentTest.carreras)
        {
            if (scores[career.id] == maxScore)
            {
                recommendations.Add(career);
            }
        }

        return recommendations;
    }

    public int GetScore(string careerId)
    {
        if (string.IsNullOrWhiteSpace(careerId))
        {
            return 0;
        }

        return scores.ContainsKey(careerId)
            ? scores[careerId]
            : 0;
    }

    public int GetAnsweredQuestions()
    {
        return answeredQuestions;
    }

    public int GetTotalQuestions()
    {
        if (currentTest == null ||
            currentTest.preguntas == null)
        {
            return 0;
        }

        return currentTest.preguntas.Count;
    }
}