using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class PackageValidatorService
{
    // Las 12 carreras permitidas por nuestra aplicación.
    private static readonly HashSet<string> AllowedCareers =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "fisica",
            "quimica",
            "matematicas",
            "biologia",
            "ing_mecatronica",
            "ing_multimedia",
            "ing_ambiental",
            "ing_biomedica",
            "ciencia_datos",
            "ciberseguridad",
            "devops",
            "inteligencia_artificial"
        };

    private static readonly HashSet<string> AllowedMinigames =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "circuito_electrico",
            "robotica",
            "programacion",
            "espacio"
        };

    // ========================================
    // VALIDAR PAQUETE COMPLETO
    // ========================================

    public void ValidatePackage(
        string rootPath,
        StemPackageModel package)
    {
        if (package == null)
            throw new Exception("El paquete es nulo.");

        bool hasQuizzes =
            package.cuestionarios != null &&
            package.cuestionarios.Count > 0;

        bool hasStories =
            package.historias != null &&
            package.historias.Count > 0;

        bool hasVocational =
            !string.IsNullOrWhiteSpace(
                package.testVocacional
            );

        if (!hasQuizzes && !hasStories && !hasVocational)
        {
            throw new Exception(
                "El paquete debe contener cuestionarios, " +
                "historias o un test vocacional."
            );
        }

        Dictionary<string, StoryModel> stories =
            LoadStories(rootPath, package);

        Dictionary<string, VideoModel> videos =
            LoadVideos(rootPath, package);

        if (hasQuizzes)
        {
            HashSet<string> quizIds = new HashSet<string>();

            foreach (string path in package.cuestionarios)
            {
                QuizModel quiz = ValidateQuiz(
                    rootPath,
                    path,
                    stories,
                    videos
                );

                if (!quizIds.Add(quiz.id))
                {
                    throw new Exception(
                        "ID de cuestionario duplicado: " + quiz.id
                    );
                }
            }
        }

        // NUEVO
        ValidateVocationalTest(rootPath, package);

        Debug.Log(
            "Validación completa del paquete STEM correcta."
        );
    }

    // ========================================
    // LEER ARCHIVO JSON
    // ========================================

    private T ReadJson<T>(
        string rootPath,
        string relativePath,
        string description) where T : class
    {
        string fullPath = GetSafeFullPath(
            rootPath,
            relativePath
        );

        if (!File.Exists(fullPath))
        {
            throw new Exception(
                $"No existe {description}: {relativePath}"
            );
        }

        try
        {
            T result = JsonUtility.FromJson<T>(
                File.ReadAllText(fullPath)
            );

            if (result == null)
            {
                throw new Exception(
                    "El JSON no contiene un objeto válido."
                );
            }

            return result;
        }
        catch (Exception ex)
        {
            throw new Exception(
                $"Error leyendo {description}: {relativePath}. " +
                ex.Message,
                ex
            );
        }
    }

    // ========================================
    // HISTORIAS
    // ========================================

    private Dictionary<string, StoryModel> LoadStories(
        string rootPath,
        StemPackageModel package)
    {
        Dictionary<string, StoryModel> stories =
            new Dictionary<string, StoryModel>();

        if (package.historias == null)
            return stories;

        foreach (string path in package.historias)
        {
            StoryModel story = ReadJson<StoryModel>(
                rootPath,
                path,
                "historia"
            );

            ValidateStory(rootPath, story);

            if (stories.ContainsKey(story.id))
            {
                throw new Exception(
                    "Historia duplicada: " + story.id
                );
            }

            stories.Add(story.id, story);
        }

        return stories;
    }

    private void ValidateStory(
        string rootPath,
        StoryModel story)
    {
        if (string.IsNullOrWhiteSpace(story.id) ||
            string.IsNullOrWhiteSpace(story.nombre))
        {
            throw new Exception(
                "Existe una historia sin ID o nombre."
            );
        }

        if (story.escenas == null ||
            story.escenas.Count == 0)
        {
            throw new Exception(
                $"La historia {story.nombre} no tiene escenas."
            );
        }

        HashSet<int> orders = new HashSet<int>();

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
                    $"Orden duplicado en {story.nombre}: {scene.orden}"
                );
            }

            if (string.IsNullOrWhiteSpace(scene.texto))
            {
                throw new Exception(
                    $"La escena {scene.orden} de {story.nombre} " +
                    "no tiene texto."
                );
            }

            if (!string.IsNullOrWhiteSpace(scene.imagen))
            {
                ValidateExistingFile(
                    rootPath,
                    scene.imagen,
                    "imagen de historia"
                );
            }

            if (!string.IsNullOrWhiteSpace(scene.audio))
            {
                ValidateExistingFile(
                    rootPath,
                    scene.audio,
                    "audio de historia"
                );
            }
        }
    }

    // ========================================
    // VIDEOS
    // ========================================

    private Dictionary<string, VideoModel> LoadVideos(
        string rootPath,
        StemPackageModel package)
    {
        Dictionary<string, VideoModel> videos =
            new Dictionary<string, VideoModel>();

        if (package.videos == null)
            return videos;

        foreach (string path in package.videos)
        {
            VideoModel video = ReadJson<VideoModel>(
                rootPath,
                path,
                "definición de video"
            );

            if (string.IsNullOrWhiteSpace(video.id) ||
                string.IsNullOrWhiteSpace(video.nombre) ||
                string.IsNullOrWhiteSpace(video.archivo))
            {
                throw new Exception(
                    $"Definición de video incompleta: {path}"
                );
            }

            ValidateExistingFile(
                rootPath,
                video.archivo,
                "archivo de video"
            );

            if (videos.ContainsKey(video.id))
            {
                throw new Exception(
                    "Video duplicado: " + video.id
                );
            }

            videos.Add(video.id, video);
        }

        return videos;
    }

    // ========================================
    // CUESTIONARIOS DE CONOCIMIENTOS
    // ========================================

    private QuizModel ValidateQuiz(
        string rootPath,
        string relativePath,
        Dictionary<string, StoryModel> stories,
        Dictionary<string, VideoModel> videos)
    {
        QuizModel quiz = ReadJson<QuizModel>(
            rootPath,
            relativePath,
            "cuestionario"
        );

        if (string.IsNullOrWhiteSpace(quiz.id) ||
            string.IsNullOrWhiteSpace(quiz.nombre))
        {
            throw new Exception(
                $"Cuestionario sin ID o nombre: {relativePath}"
            );
        }

        if (!string.IsNullOrWhiteSpace(quiz.imagen))
        {
            ValidateExistingFile(
                rootPath,
                quiz.imagen,
                "portada del cuestionario"
            );
        }

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
            if (question == null ||
                string.IsNullOrWhiteSpace(question.id) ||
                string.IsNullOrWhiteSpace(question.texto))
            {
                throw new Exception(
                    $"Pregunta incompleta en {quiz.nombre}."
                );
            }

            if (!questionIds.Add(question.id))
            {
                throw new Exception(
                    $"Pregunta duplicada: {question.id}"
                );
            }

            if (!string.IsNullOrWhiteSpace(question.imagen))
            {
                ValidateExistingFile(
                    rootPath,
                    question.imagen,
                    "imagen de pregunta"
                );
            }

            if (question.respuestas == null ||
                question.respuestas.Count != 4)
            {
                throw new Exception(
                    $"La pregunta {question.id} debe tener " +
                    "exactamente 4 respuestas."
                );
            }

            int correctCount = 0;

            foreach (AnswerModel answer in question.respuestas)
            {
                if (answer == null ||
                    string.IsNullOrWhiteSpace(answer.texto))
                {
                    throw new Exception(
                        $"Respuesta incompleta en {question.id}."
                    );
                }

                if (answer.correcta)
                    correctCount++;
            }

            if (correctCount != 1)
            {
                throw new Exception(
                    $"La pregunta {question.id} debe tener " +
                    "exactamente una respuesta correcta."
                );
            }
        }

        if (quiz.secuencia == null ||
            quiz.secuencia.Count == 0)
        {
            throw new Exception(
                $"El cuestionario {quiz.nombre} no tiene secuencia."
            );
        }

        HashSet<int> usedOrders = new HashSet<int>();

        foreach (SequenceItemModel item in quiz.secuencia)
        {
            if (item == null ||
                string.IsNullOrWhiteSpace(item.id) ||
                string.IsNullOrWhiteSpace(item.tipo))
            {
                throw new Exception(
                    $"Secuencia incompleta en {quiz.nombre}."
                );
            }

            if (!usedOrders.Add(item.orden))
            {
                throw new Exception(
                    $"Orden duplicado: {item.orden}"
                );
            }

            switch (item.tipo.ToLowerInvariant())
            {
                case "pregunta":

                    if (!questionIds.Contains(item.id))
                    {
                        throw new Exception(
                            $"Pregunta inexistente: {item.id}"
                        );
                    }
                    break;

                case "historia":

                    if (!stories.ContainsKey(item.id))
                    {
                        throw new Exception(
                            $"Historia inexistente: {item.id}"
                        );
                    }
                    break;

                case "video":

                    if (!videos.ContainsKey(item.id))
                    {
                        throw new Exception(
                            $"Video inexistente: {item.id}"
                        );
                    }
                    break;

                case "minijuego":

                    if (!AllowedMinigames.Contains(item.id))
                    {
                        throw new Exception(
                            $"Minijuego desconocido: {item.id}"
                        );
                    }
                    break;

                default:

                    throw new Exception(
                        $"Tipo de secuencia desconocido: {item.tipo}"
                    );
            }
        }

        return quiz;
    }

    // ========================================
    // NUEVO TEST VOCACIONAL
    // ========================================

    private void ValidateVocationalTest(
        string rootPath,
        StemPackageModel package)
    {
        if (string.IsNullOrWhiteSpace(
            package.testVocacional))
        {
            return;
        }

        VocationalTestModel test =
            ReadJson<VocationalTestModel>(
                rootPath,
                package.testVocacional,
                "test vocacional"
            );

        if (string.IsNullOrWhiteSpace(test.id) ||
            string.IsNullOrWhiteSpace(test.nombre))
        {
            throw new Exception(
                "El test vocacional debe tener ID y nombre."
            );
        }

        // Exactamente cuatro carreras por test.
        if (test.carreras == null ||
            test.carreras.Count != 4)
        {
            throw new Exception(
                "El test vocacional debe utilizar " +
                "exactamente 4 carreras STEM."
            );
        }

        HashSet<string> careerIds =
            new HashSet<string>(StringComparer.Ordinal);

        foreach (VocationalCareerModel career in test.carreras)
        {
            if (career == null ||
                string.IsNullOrWhiteSpace(career.id) ||
                string.IsNullOrWhiteSpace(career.nombre))
            {
                throw new Exception(
                    "Existe una carrera sin ID o nombre."
                );
            }

            if (!AllowedCareers.Contains(career.id))
            {
                throw new Exception(
                    "Carrera STEM no permitida: " + career.id
                );
            }

            if (!careerIds.Add(career.id))
            {
                throw new Exception(
                    "Carrera duplicada: " + career.id
                );
            }
        }

        if (test.preguntas == null ||
            test.preguntas.Count == 0)
        {
            throw new Exception(
                "El test vocacional no tiene preguntas."
            );
        }

        HashSet<string> questionIds =
            new HashSet<string>(StringComparer.Ordinal);

        foreach (VocationalQuestionModel question in test.preguntas)
        {
            if (question == null ||
                string.IsNullOrWhiteSpace(question.id) ||
                string.IsNullOrWhiteSpace(question.texto))
            {
                throw new Exception(
                    "Existe una pregunta vocacional incompleta."
                );
            }

            if (!questionIds.Add(question.id))
            {
                throw new Exception(
                    $"Pregunta vocacional duplicada: {question.id}"
                );
            }

            if (question.respuestas == null ||
                question.respuestas.Count != 4)
            {
                throw new Exception(
                    $"La pregunta vocacional {question.id} debe " +
                    "tener exactamente 4 respuestas."
                );
            }

            foreach (VocationalAnswerModel answer in question.respuestas)
            {
                if (answer == null ||
                    string.IsNullOrWhiteSpace(answer.texto) ||
                    string.IsNullOrWhiteSpace(answer.carreraId))
                {
                    throw new Exception(
                        $"Respuesta incompleta en {question.id}."
                    );
                }

                // La respuesta debe referenciar una de
                // las cuatro carreras del test.
                if (!careerIds.Contains(answer.carreraId))
                {
                    throw new Exception(
                        $"La pregunta {question.id} referencia " +
                        $"una carrera no incluida en el test: " +
                        answer.carreraId
                    );
                }
            }
        }

        Debug.Log(
            $"Test vocacional validado: {test.nombre} | " +
            $"Carreras: {test.carreras.Count} | " +
            $"Preguntas: {test.preguntas.Count}"
        );
    }

    // ========================================
    // VALIDACIÓN DE ARCHIVOS Y RUTAS
    // ========================================

    private void ValidateExistingFile(
        string rootPath,
        string relativePath,
        string description)
    {
        string fullPath = GetSafeFullPath(
            rootPath,
            relativePath
        );

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

        if (Path.IsPathRooted(relativePath) ||
            relativePath.Contains(":"))
        {
            throw new Exception(
                "Ruta absoluta no permitida: " + relativePath
            );
        }

        string normalized = relativePath
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);

        string fullRootPath = Path.GetFullPath(rootPath);

        string rootWithSeparator =
            fullRootPath.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar
            ) + Path.DirectorySeparatorChar;

        string fullPath = Path.GetFullPath(
            Path.Combine(fullRootPath, normalized)
        );

        StringComparison comparison =
            Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

        if (!fullPath.StartsWith(
            rootWithSeparator,
            comparison))
        {
            throw new Exception(
                "Ruta no permitida dentro del paquete: " +
                relativePath
            );
        }

        return fullPath;
    }
}