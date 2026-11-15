using System.Collections.Generic;

public class LoadedPackageModel
{
    public string rootPath;

    public StemPackageModel manifest;

    // Cuestionarios
    public List<QuizModel> cuestionarios =
        new List<QuizModel>();

    // Historias
    public List<StoryModel> historias =
        new List<StoryModel>();

    // Videos
    public List<VideoModel> videos =
        new List<VideoModel>();

    // NUEVO: test vocacional
    public VocationalTestModel testVocacional;

    public QuizModel GetQuiz(string id)
    {
        return cuestionarios.Find(
            quiz => quiz.id == id
        );
    }

    public StoryModel GetStory(string id)
    {
        return historias.Find(
            story => story.id == id
        );
    }

    public VideoModel GetVideo(string id)
    {
        return videos.Find(
            video => video.id == id
        );
    }

    public VocationalTestModel GetVocationalTest()
    {
        return testVocacional;
    }
}