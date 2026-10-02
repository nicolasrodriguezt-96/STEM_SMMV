using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class PackageValidatorService
{
    public void ValidatePackage(
        string rootPath,
        StemPackageModel package)
    {
        if (package == null)
        {
            throw new Exception("El paquete es nulo.");
        }

        Dictionary<string, StoryModel> stories =
            LoadStories(rootPath, package);

        Dictionary<string, VideoModel> videos =
            LoadVideos(rootPath, package);

        if (package.cuestionarios == null ||
            package.cuestionarios.Count == 0)
        {
            throw new Exception(
                "El paquete debe contener al menos un cuestionario."
            );
        }

        foreach (string quizRelativePath in package.cuestionarios)
        {
            ValidateQuiz(
                rootPath,
                quizRelativePath,
                stories,
                videos
            );
        }
    }

    private Dictionary<string, StoryModel> LoadStories(
        string rootPath,
        StemPackageModel package)
    {
        Dictionary<string, StoryModel> stories =
            new Dictionary<string, StoryModel>();

        if (package.historias == null)
        {
            return stories;
        }

        foreach (string relativePath in package.historias)
        {
            string fullPath =
                GetSafeFullPath(rootPath, relativePath);

            if (!File.Exists(fullPath))
            {
                throw new Exception(
                    $"No existe la historia: {relativePath}"
                );
            }

            string json = File.ReadAllText(fullPath);

            StoryModel story =
                JsonUtility.FromJson<StoryModel>(json);

            ValidateStory(
                rootPath,
                story,
                relativePath
            );

            if (stories.ContainsKey(story.id))
            {
                throw new Exception(
                    $"Hay una historia con ID duplicado: {story.id}"
                );
            }

            stories.Add(story.id, story);
        }

        return stories;
    }

    private Dictionary<string, VideoModel> LoadVideos(
        string rootPath,
        StemPackageModel package)
    {
        Dictionary<string, VideoModel> videos =
            new Dictionary<string, VideoModel>();

        if (package.videos == null)
        {
            return videos;
        }

        foreach (string relativePath in package.videos)
        {
            string fullPath =
                GetSafeFullPath(rootPath, relativePath);

            if (!File.Exists(fullPath))
            {
                throw new Exception(
                    $"No existe la definición del video: {relativePath}"
                );
            }

            string json = File.ReadAllText(fullPath);

            VideoModel video =
                JsonUtility.FromJson<VideoModel>(json);

            ValidateVideo(
                rootPath,
                video,
                relativePath
            );

            if (videos.ContainsKey(video.id))
            {
                throw new Exception(
                    $"Hay un video con ID duplicado: {video.id}"
                );
            }

            videos.Add(video.id, video);
        }

        return videos;
    }

    private void ValidateQuiz(
        string rootPath,
        string relativePath,
        Dictionary<string, StoryModel> stories,
        Dictionary<string, VideoModel> videos)
    {
        string fullPath =
            GetSafeFullPath(rootPath, relativePath);

        if (!File.Exists(fullPath))
        {
            throw new Exception(
                $"No existe el cuestionario: {relativePath}"
            );
        }

        string json = File.ReadAllText(fullPath);

        QuizModel quiz =
            JsonUtility.FromJson<QuizModel>(json);

        if (quiz == null)
        {
            throw new Exception(
                $"No fue posible leer el cuestionario: {relativePath}"
            );
        }

        if (string.IsNullOrWhiteSpace(quiz.id))
        {
            throw new Exception(
                $"El cuestionario {relativePath} no tiene ID."
            );
        }

        if (string.IsNullOrWhiteSpace(quiz.nombre))
        {
            throw new Exception(
                $"El cuestionario {quiz.id} no tiene nombre."
            );
        }

        if (!string.IsNullOrWhiteSpace(quiz.imagen))
        {
            ValidateExistingFile(
                rootPath,
                quiz.imagen,
                $"imagen del cuestionario {quiz.nombre}"
            );
        }

        ValidateQuestions(
            rootPath,
            quiz
        );

        ValidateSequence(
            quiz,
            stories,
            videos
        );
    }

    private void ValidateQuestions(
        string rootPath,
        QuizModel quiz)
    {
        if (quiz.preguntas == null ||
            quiz.preguntas.Count == 0)
        {
            throw new Exception(
                $"El cuestionario {quiz.nombre} no tiene preguntas."
            );
        }

        HashSet<string> questionIds =
            new HashSet<string>();

        foreach (QuestionModel question in quiz.preguntas)
        {
            if (question == null)
            {
                throw new Exception(
                    $"El cuestionario {quiz.nombre} contiene una pregunta nula."
                );
            }

            if (string.IsNullOrWhiteSpace(question.id))
            {
                throw new Exception(
                    $"El cuestionario {quiz.nombre} contiene una pregunta sin ID."
                );
            }

            if (!questionIds.Add(question.id))
            {
                throw new Exception(
                    $"El cuestionario {quiz.nombre} tiene una pregunta con ID duplicado: {question.id}"
                );
            }

            if (string.IsNullOrWhiteSpace(question.texto))
            {
                throw new Exception(
                    $"La pregunta {question.id} no tiene texto."
                );
            }

            if (!string.IsNullOrWhiteSpace(question.imagen))
            {
                ValidateExistingFile(
                    rootPath,
                    question.imagen,
                    $"imagen de la pregunta {question.id}"
                );
            }

            if (question.respuestas == null ||
                question.respuestas.Count != 4)
            {
                throw new Exception(
                    $"La pregunta {question.id} debe tener exactamente 4 respuestas."
                );
            }

            int correctAnswers =
                question.respuestas.Count(
                    answer => answer != null && answer.correcta
                );

            if (correctAnswers != 1)
            {
                throw new Exception(
                    $"La pregunta {question.id} debe tener exactamente una respuesta correcta."
                );
            }

            foreach (AnswerModel answer in question.respuestas)
            {
                if (answer == null)
                {
                    throw new Exception(
                        $"La pregunta {question.id} contiene una respuesta nula."
                    );
                }

                if (string.IsNullOrWhiteSpace(answer.texto))
                {
                    throw new Exception(
                        $"La pregunta {question.id} contiene una respuesta sin texto."
                    );
                }
            }
        }
    }

    private void ValidateSequence(
        QuizModel quiz,
        Dictionary<string, StoryModel> stories,
        Dictionary<string, VideoModel> videos)
    {
        if (quiz.secuencia == null ||
            quiz.secuencia.Count == 0)
        {
            throw new Exception(
                $"El cuestionario {quiz.nombre} no tiene secuencia."
            );
        }

        HashSet<int> usedOrders =
            new HashSet<int>();

        foreach (SequenceItemModel item in quiz.secuencia)
        {
            if (item == null)
            {
                throw new Exception(
                    $"El cuestionario {quiz.nombre} contiene un elemento de secuencia nulo."
                );
            }

            if (!usedOrders.Add(item.orden))
            {
                throw new Exception(
                    $"El cuestionario {quiz.nombre} tiene el orden duplicado: {item.orden}"
                );
            }

            if (string.IsNullOrWhiteSpace(item.tipo))
            {
                throw new Exception(
                    $"El cuestionario {quiz.nombre} contiene un elemento sin tipo."
                );
            }

            if (string.IsNullOrWhiteSpace(item.id))
            {
                throw new Exception(
                    $"El cuestionario {quiz.nombre} contiene un elemento sin ID."
                );
            }

            switch (item.tipo.ToLowerInvariant())
            {
                case "pregunta":
                    if (!quiz.preguntas.Any(
                        question => question.id == item.id))
                    {
                        throw new Exception(
                            $"La secuencia del cuestionario {quiz.nombre} referencia una pregunta inexistente: {item.id}"
                        );
                    }
                    break;

                case "historia":
                    if (!stories.ContainsKey(item.id))
                    {
                        throw new Exception(
                            $"La secuencia del cuestionario {quiz.nombre} referencia una historia inexistente: {item.id}"
                        );
                    }
                    break;

                case "video":
                    if (!videos.ContainsKey(item.id))
                    {
                        throw new Exception(
                            $"La secuencia del cuestionario {quiz.nombre} referencia un video inexistente: {item.id}"
                        );
                    }
                    break;

                case "minijuego":
                    ValidateMinigame(item.id, quiz.nombre);
                    break;

                default:
                    throw new Exception(
                        $"Tipo de secuencia desconocido en {quiz.nombre}: {item.tipo}"
                    );
            }
        }
    }

    private void ValidateStory(
        string rootPath,
        StoryModel story,
        string relativePath)
    {
        if (story == null)
        {
            throw new Exception(
                $"No fue posible leer la historia: {relativePath}"
            );
        }

        if (string.IsNullOrWhiteSpace(story.id))
        {
            throw new Exception(
                $"La historia {relativePath} no tiene ID."
            );
        }

        if (string.IsNullOrWhiteSpace(story.nombre))
        {
            throw new Exception(
                $"La historia {story.id} no tiene nombre."
            );
        }

        if (story.escenas == null ||
            story.escenas.Count == 0)
        {
            throw new Exception(
                $"La historia {story.nombre} no tiene escenas."
            );
        }

        HashSet<int> orders =
            new HashSet<int>();

        foreach (StorySceneModel scene in story.escenas)
        {
            if (scene == null)
            {
                throw new Exception(
                    $"La historia {story.nombre} contiene una escena nula."
                );
            }

            if (!orders.Add(scene.orden))
            {
                throw new Exception(
                    $"La historia {story.nombre} tiene el orden de escena duplicado: {scene.orden}"
                );
            }

            if (string.IsNullOrWhiteSpace(scene.texto))
            {
                throw new Exception(
                    $"La escena {scene.orden} de {story.nombre} no tiene texto."
                );
            }

            if (!string.IsNullOrWhiteSpace(scene.imagen))
            {
                ValidateExistingFile(
                    rootPath,
                    scene.imagen,
                    $"imagen de la historia {story.nombre}"
                );
            }

            if (!string.IsNullOrWhiteSpace(scene.audio))
            {
                ValidateExistingFile(
                    rootPath,
                    scene.audio,
                    $"audio de la historia {story.nombre}"
                );
            }
        }
    }

    private void ValidateVideo(
        string rootPath,
        VideoModel video,
        string relativePath)
    {
        if (video == null)
        {
            throw new Exception(
                $"No fue posible leer el video: {relativePath}"
            );
        }

        if (string.IsNullOrWhiteSpace(video.id))
        {
            throw new Exception(
                $"La definición de video {relativePath} no tiene ID."
            );
        }

        if (string.IsNullOrWhiteSpace(video.nombre))
        {
            throw new Exception(
                $"El video {video.id} no tiene nombre."
            );
        }

        if (string.IsNullOrWhiteSpace(video.archivo))
        {
            throw new Exception(
                $"El video {video.nombre} no tiene archivo."
            );
        }

        ValidateExistingFile(
            rootPath,
            video.archivo,
            $"archivo de video {video.nombre}"
        );
    }

    private void ValidateMinigame(
        string minigameId,
        string quizName)
    {
        string[] validMinigames =
        {
            "circuito_electrico",
            "robotica",
            "programacion",
            "espacio"
        };

        if (!validMinigames.Contains(minigameId))
        {
            throw new Exception(
                $"El cuestionario {quizName} referencia un minijuego desconocido: {minigameId}"
            );
        }
    }

    private void ValidateExistingFile(
        string rootPath,
        string relativePath,
        string description)
    {
        string fullPath =
            GetSafeFullPath(rootPath, relativePath);

        if (!File.Exists(fullPath))
        {
            throw new Exception(
                $"No existe {description}: {relativePath}"
            );
        }
    }

    private string GetSafeFullPath(
        string rootPath,
        string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new Exception(
                "Se encontró una ruta vacía dentro del paquete."
            );
        }

        string fullRootPath =
            Path.GetFullPath(rootPath);

        string fullPath =
            Path.GetFullPath(
                Path.Combine(rootPath, relativePath)
            );

        string rootWithSeparator =
            fullRootPath.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar
            ) + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(
                rootWithSeparator,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception(
                $"Ruta no permitida dentro del paquete: {relativePath}"
            );
        }

        return fullPath;
    }
}