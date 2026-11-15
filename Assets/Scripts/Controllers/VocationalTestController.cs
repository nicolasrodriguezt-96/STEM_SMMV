using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class VocationalTestController : MonoBehaviour
{
    // ========================================
    // VISTAS
    // ========================================

    [Header("Vistas")]
    [SerializeField] private VocationalTestView vocationalView;
    [SerializeField] private MainMenuView mainMenuView;

    // ========================================
    // CONTROLADORES
    // ========================================

    [Header("Controladores")]
    [SerializeField] private VocationalMenuController menuController;
    [SerializeField] private VocationalResultController resultController;

    // ========================================
    // BOTONES
    // ========================================

    [Header("Botones de respuestas")]
    [SerializeField] private Button answerAButton;
    [SerializeField] private Button answerBButton;
    [SerializeField] private Button answerCButton;
    [SerializeField] private Button answerDButton;

    [Header("Navegación")]
    [SerializeField] private Button exitButton;

    // ========================================
    // SERVICIOS
    // ========================================

    private VocationalScoringService scoringService;

    // ========================================
    // DATOS DEL TEST
    // ========================================

    private LoadedPackageModel currentPackage;
    private VocationalTestModel currentTest;

    private int currentQuestionIndex;
    private bool testRunning;

    // ========================================
    // NUEVO: CONTROL DE TIEMPO
    // ========================================

    private DateTime testStartTimeUtc;

    private const string DeviceIdKey =
        "STEM_INSTALLATION_DEVICE_ID";

    // ========================================
    // MODELOS PARA SERIALIZAR RESULTADOS
    // ========================================

    [Serializable]
    private class CareerScoreData
    {
        public string id;
        public string nombre;
        public int puntos;
    }

    [Serializable]
    private class VocationalScoresData
    {
        public List<CareerScoreData> carreras =
            new List<CareerScoreData>();
    }

    [Serializable]
    private class VocationalRecommendationsData
    {
        public List<string> carreraIds =
            new List<string>();
    }

    // ========================================
    // INICIALIZACIÓN
    // ========================================

    private void Awake()
    {
        scoringService = new VocationalScoringService();

        if (vocationalView == null)
        {
            Debug.LogError(
                "VocationalTestView no está asignado.",
                this
            );

            return;
        }

        if (answerAButton != null)
        {
            answerAButton.onClick.AddListener(
                () => SelectAnswer(0)
            );
        }
        else
        {
            Debug.LogWarning(
                "Answer A Button no está asignado."
            );
        }

        if (answerBButton != null)
        {
            answerBButton.onClick.AddListener(
                () => SelectAnswer(1)
            );
        }
        else
        {
            Debug.LogWarning(
                "Answer B Button no está asignado."
            );
        }

        if (answerCButton != null)
        {
            answerCButton.onClick.AddListener(
                () => SelectAnswer(2)
            );
        }
        else
        {
            Debug.LogWarning(
                "Answer C Button no está asignado."
            );
        }

        if (answerDButton != null)
        {
            answerDButton.onClick.AddListener(
                () => SelectAnswer(3)
            );
        }
        else
        {
            Debug.LogWarning(
                "Answer D Button no está asignado."
            );
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(
                ExitTest
            );
        }

        vocationalView.Hide();

        testRunning = false;

        Debug.Log(
            "VocationalTestController inicializado."
        );
    }

    // ========================================
    // INICIAR TEST
    // ========================================

    public void StartTest(LoadedPackageModel package)
    {
        if (vocationalView == null)
        {
            Debug.LogError(
                "No se puede iniciar: falta VocationalTestView."
            );

            return;
        }

        if (resultController == null)
        {
            Debug.LogError(
                "No se puede iniciar: falta VocationalResultController."
            );

            return;
        }

        if (package == null ||
            package.testVocacional == null)
        {
            Debug.LogError(
                "El paquete no contiene un test vocacional."
            );

            return;
        }

        VocationalTestModel test =
            package.testVocacional;

        if (test.preguntas == null ||
            test.preguntas.Count == 0)
        {
            Debug.LogError(
                "El test vocacional no tiene preguntas."
            );

            return;
        }

        foreach (VocationalQuestionModel question
                 in test.preguntas)
        {
            if (question == null ||
                question.respuestas == null ||
                question.respuestas.Count != 4)
            {
                Debug.LogError(
                    "Todas las preguntas deben tener " +
                    "exactamente cuatro respuestas."
                );

                return;
            }
        }

        try
        {
            if (scoringService == null)
            {
                scoringService =
                    new VocationalScoringService();
            }

            scoringService.StartTest(test);
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "Error iniciando el test: " +
                ex.Message
            );

            return;
        }

        currentPackage = package;
        currentTest = test;

        currentQuestionIndex = 0;
        testRunning = true;

        // NUEVO: registrar inicio del test.
        testStartTimeUtc = DateTime.UtcNow;

        if (mainMenuView != null)
        {
            mainMenuView.HideAllPanels();
        }

        resultController.HideResult();

        vocationalView.Show();

        ShowCurrentQuestion();

        Debug.Log(
            "Test vocacional iniciado: " +
            currentTest.nombre
        );
    }

    // ========================================
    // MOSTRAR PREGUNTA
    // ========================================

    private void ShowCurrentQuestion()
    {
        if (!testRunning || currentTest == null)
        {
            return;
        }

        if (currentQuestionIndex < 0 ||
            currentQuestionIndex >= currentTest.preguntas.Count)
        {
            Debug.LogError(
                "Índice de pregunta fuera de rango."
            );

            return;
        }

        VocationalQuestionModel question =
            currentTest.preguntas[currentQuestionIndex];

        vocationalView.SetTitle(
            currentTest.nombre
        );

        vocationalView.SetQuestion(
            question.texto
        );

        vocationalView.SetProgress(
            currentQuestionIndex + 1,
            currentTest.preguntas.Count
        );

        for (int i = 0; i < 4; i++)
        {
            vocationalView.SetAnswerText(
                i,
                question.respuestas[i].texto
            );
        }

        vocationalView.SetAnswersInteractable(true);

        Debug.Log(
            $"Pregunta {currentQuestionIndex + 1}/" +
            $"{currentTest.preguntas.Count}"
        );
    }

    // ========================================
    // SELECCIONAR RESPUESTA
    // ========================================

    private void SelectAnswer(int answerIndex)
    {
        if (!testRunning || currentTest == null)
        {
            return;
        }

        if (answerIndex < 0 || answerIndex > 3)
        {
            Debug.LogError(
                "Índice de respuesta inválido."
            );

            return;
        }

        vocationalView.SetAnswersInteractable(false);

        VocationalQuestionModel question =
            currentTest.preguntas[currentQuestionIndex];

        VocationalAnswerModel selectedAnswer =
            question.respuestas[answerIndex];

        try
        {
            scoringService.RegisterAnswer(
                selectedAnswer
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "Error registrando respuesta: " +
                ex.Message
            );

            vocationalView.SetAnswersInteractable(true);

            return;
        }

        Debug.Log(
            $"Respuesta seleccionada: {selectedAnswer.texto}"
        );

        Debug.Log(
            $"Carrera: {selectedAnswer.carreraId}"
        );

        Debug.Log(
            "Puntuación actual: " +
            scoringService.GetScore(
                selectedAnswer.carreraId
            )
        );

        currentQuestionIndex++;

        if (currentQuestionIndex >=
            currentTest.preguntas.Count)
        {
            FinishTest();
        }
        else
        {
            ShowCurrentQuestion();
        }
    }

    // ========================================
    // FINALIZAR TEST
    // ========================================

    private void FinishTest()
    {
        if (!testRunning)
        {
            return;
        }

        testRunning = false;

        // NUEVO: registrar finalización.
        DateTime testEndTimeUtc = DateTime.UtcNow;

        vocationalView.Hide();

        List<VocationalCareerModel> recommendations =
            scoringService.GetRecommendations();

        if (recommendations == null ||
            recommendations.Count == 0)
        {
            Debug.LogError(
                "No fue posible calcular las recomendaciones."
            );

            ExitTest();
            return;
        }

        Debug.Log(
            "Test vocacional finalizado."
        );

        Debug.Log(
            "Preguntas respondidas: " +
            scoringService.GetAnsweredQuestions()
        );

        foreach (VocationalCareerModel career
                 in recommendations)
        {
            Debug.Log(
                $"Carrera recomendada: {career.nombre} " +
                $"({scoringService.GetScore(career.id)} puntos)"
            );
        }

        // NUEVO: guardar antes de mostrar resultados.
        SaveVocationalResult(
            recommendations,
            testEndTimeUtc
        );

        // La pantalla de resultados sigue funcionando igual.
        resultController.ShowResult(
            recommendations
        );
    }

    // ========================================
    // NUEVO: GUARDAR EN SQLITE
    // ========================================

    private void SaveVocationalResult(
        List<VocationalCareerModel> recommendations,
        DateTime testEndTimeUtc)
    {
        try
        {
            if (currentTest == null ||
                currentPackage == null)
            {
                Debug.LogError(
                    "No hay datos del test vocacional para guardar."
                );

                return;
            }

            // =====================================
            // 1. OBTENER ID DEL PAQUETE
            // =====================================

            string packageId = "";

            if (!string.IsNullOrWhiteSpace(
                    currentPackage.rootPath))
            {
                string cleanPath =
                    currentPackage.rootPath.TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar
                    );

                packageId = Path.GetFileName(
                    cleanPath
                );
            }

            // =====================================
            // 2. PREPARAR PUNTUACIONES
            // =====================================

            VocationalScoresData scoresData =
                new VocationalScoresData();

            foreach (VocationalCareerModel career
                     in currentTest.carreras)
            {
                CareerScoreData score =
                    new CareerScoreData
                    {
                        id = career.id,
                        nombre = career.nombre,
                        puntos = scoringService.GetScore(
                            career.id
                        )
                    };

                scoresData.carreras.Add(score);
            }

            string scoresJson =
                JsonUtility.ToJson(scoresData);

            // =====================================
            // 3. PREPARAR RECOMENDACIONES
            // =====================================

            VocationalRecommendationsData recommendationsData =
                new VocationalRecommendationsData();

            foreach (VocationalCareerModel career
                     in recommendations)
            {
                recommendationsData.carreraIds.Add(
                    career.id
                );
            }

            string recommendationsJson =
                JsonUtility.ToJson(
                    recommendationsData
                );

            // =====================================
            // 4. CALCULAR TIEMPO
            // =====================================

            double durationSeconds =
                (testEndTimeUtc - testStartTimeUtc)
                .TotalSeconds;

            durationSeconds = Math.Max(
                0,
                durationSeconds
            );

            // =====================================
            // 5. CREAR REGISTRO
            // =====================================

            LocalResultRecord result =
                new LocalResultRecord
                {
                    Id = Guid.NewGuid().ToString("N"),

                    // Usamos el mismo ID que los cuestionarios.
                    DeviceId = GetOrCreateDeviceId(),

                    ResultType = "VOCATIONAL",

                    PackageId = packageId,

                    // Solo hay un test vocacional por paquete.
                    ContentId = "vocacional",

                    ContentName = currentTest.nombre,

                    StartedAtUtc =
                        testStartTimeUtc.ToString("o"),

                    FinishedAtUtc =
                        testEndTimeUtc.ToString("o"),

                    DurationSeconds = durationSeconds,

                    // No aplica el concepto de respuestas
                    // correctas en un test vocacional.
                    CorrectAnswers = 0,

                    TotalQuestions =
                        scoringService.GetTotalQuestions(),

                    // No es una calificación porcentual.
                    ScorePercentage = 0,

                    // Datos específicos del test vocacional.
                    VocationalScoresJson = scoresJson,

                    RecommendedCareersJson =
                        recommendationsJson,

                    // Pendiente de enviar a Firebase.
                    SyncStatus = 0,

                    SyncedAtUtc = null
                };

            // =====================================
            // 6. GUARDAR EN SQLITE
            // =====================================

            using (LocalDatabaseService database =
                new LocalDatabaseService())
            {
                database.SaveResult(result);
            }

            // =====================================
            // 7. MOSTRAR CONFIRMACIÓN
            // =====================================

            Debug.Log(
                "TEST VOCACIONAL GUARDADO EN SQLITE\n" +
                $"ID: {result.Id}\n" +
                $"Nombre: {result.ContentName}\n" +
                $"Preguntas: {scoringService.GetAnsweredQuestions()}\n" +
                $"Tiempo: {durationSeconds:0.##} segundos\n" +
                $"Puntuaciones: {scoresJson}\n" +
                $"Recomendaciones: {recommendationsJson}\n" +
                "Sincronización: PENDIENTE"
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "ERROR GUARDANDO EL TEST VOCACIONAL EN SQLITE:\n" +
                ex
            );
        }
    }

    // ========================================
    // NUEVO: IDENTIFICADOR DE INSTALACIÓN
    // ========================================

    private string GetOrCreateDeviceId()
    {
        string deviceId = PlayerPrefs.GetString(
            DeviceIdKey,
            ""
        );

        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            return deviceId;
        }

        deviceId = Guid.NewGuid().ToString("N");

        PlayerPrefs.SetString(
            DeviceIdKey,
            deviceId
        );

        PlayerPrefs.Save();

        return deviceId;
    }

    // ========================================
    // OCULTAR TEST
    // ========================================

    public void HideTest()
    {
        testRunning = false;

        if (vocationalView != null)
        {
            vocationalView.Hide();
        }
    }

    // ========================================
    // SALIR DEL TEST
    // ========================================

    public void ExitTest()
    {
        HideTest();

        if (menuController != null)
        {
            menuController.ReturnToMainMenu();
        }
        else if (mainMenuView != null)
        {
            mainMenuView.ShowMainMenu();
        }
        else
        {
            Debug.LogError(
                "No hay un controlador para regresar al menú."
            );
        }
    }

    // ========================================
    // REINICIAR TEST
    // ========================================

    public void RestartTest()
    {
        if (currentPackage == null)
        {
            Debug.LogWarning(
                "No hay un test para reiniciar."
            );

            return;
        }

        if (resultController != null)
        {
            resultController.HideResult();
        }

        StartTest(currentPackage);
    }

    // ========================================
    // CONSULTAR PUNTOS
    // ========================================

    public int GetCareerScore(string careerId)
    {
        if (scoringService == null)
        {
            return 0;
        }

        return scoringService.GetScore(
            careerId
        );
    }

    // ========================================
    // CONSULTAR PREGUNTAS RESPONDIDAS
    // ========================================

    public int GetAnsweredQuestions()
    {
        if (scoringService == null)
        {
            return 0;
        }

        return scoringService.GetAnsweredQuestions();
    }

    // ========================================
    // CONSULTAR TOTAL DE PREGUNTAS
    // ========================================

    public int GetTotalQuestions()
    {
        if (scoringService == null)
        {
            return 0;
        }

        return scoringService.GetTotalQuestions();
    }
}