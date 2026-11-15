using SQLite;

[Table("result_executions")]
public class LocalResultRecord
{
    [PrimaryKey]
    public string Id { get; set; }

    [Indexed]
    public string DeviceId { get; set; }

    // QUIZ o VOCATIONAL
    [Indexed]
    public string ResultType { get; set; }

    public string PackageId { get; set; }

    public string ContentId { get; set; }

    public string ContentName { get; set; }

    public string StartedAtUtc { get; set; }

    public string FinishedAtUtc { get; set; }

    public double DurationSeconds { get; set; }

    // Para cuestionarios normales
    public int CorrectAnswers { get; set; }

    public int TotalQuestions { get; set; }

    public double ScorePercentage { get; set; }

    // Para tests vocacionales
    public string VocationalScoresJson { get; set; }

    public string RecommendedCareersJson { get; set; }

    // 0 = pendiente
    // 1 = sincronizado
    [Indexed]
    public int SyncStatus { get; set; }

    public string SyncedAtUtc { get; set; }
}