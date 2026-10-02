using System.Collections.Generic;

public class LoadedPackageModel
{
    public string rootPath;

    public StemPackageModel manifest;

    public List<QuizModel> cuestionarios =
        new List<QuizModel>();

    public List<StoryModel> historias =
        new List<StoryModel>();

    public List<VideoModel> videos =
        new List<VideoModel>();

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

    public QuizModel GetQuiz(string id)
    {
        return cuestionarios.Find(
            quiz => quiz.id == id
        );
    }
}