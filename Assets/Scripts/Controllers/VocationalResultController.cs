using System.Collections.Generic;
using UnityEngine;

public class VocationalResultController : MonoBehaviour
{
    [Header("Vista de resultados")]
    [SerializeField] private VocationalResultView resultView;

    [Header("Menú principal")]
    [SerializeField] private MainMenuView mainMenuView;

    [Header("Ciencias básicas")]
    [SerializeField] private Sprite fisica;
    [SerializeField] private Sprite quimica;
    [SerializeField] private Sprite matematicas;
    [SerializeField] private Sprite biologia;

    [Header("Ingenierías")]
    [SerializeField] private Sprite ingMecatronica;
    [SerializeField] private Sprite ingMultimedia;
    [SerializeField] private Sprite ingAmbiental;
    [SerializeField] private Sprite ingBiomedica;

    [Header("Tecnología")]
    [SerializeField] private Sprite cienciaDatos;
    [SerializeField] private Sprite ciberseguridad;
    [SerializeField] private Sprite devops;
    [SerializeField] private Sprite inteligenciaArtificial;

    private void Awake()
    {
        HideResult();
    }

    public void ShowResult(
        List<VocationalCareerModel> recommendedCareers)
    {
        if (resultView == null)
        {
            Debug.LogError(
                "Falta asignar VocationalResultView.",
                this
            );
            return;
        }

        if (recommendedCareers == null ||
            recommendedCareers.Count == 0)
        {
            Debug.LogError(
                "No hay carreras recomendadas."
            );
            return;
        }

        VocationalCareerModel firstCareer =
            recommendedCareers[0];

        VocationalCareerModel secondCareer =
            recommendedCareers.Count > 1
                ? recommendedCareers[1]
                : null;

        if (recommendedCareers.Count > 2)
        {
            Debug.LogWarning(
                "Empataron más de dos carreras. " +
                "Se mostrarán las dos primeras."
            );
        }

        Sprite firstSprite =
            GetCareerSprite(firstCareer.id);

        Sprite secondSprite =
            secondCareer != null
                ? GetCareerSprite(secondCareer.id)
                : null;

        if (firstSprite == null)
        {
            Debug.LogWarning(
                "Falta el sprite de la carrera: " +
                firstCareer.id
            );
        }

        if (secondCareer != null && secondSprite == null)
        {
            Debug.LogWarning(
                "Falta el sprite de la segunda carrera: " +
                secondCareer.id +
                ". Se ocultará su grupo."
            );
        }

        if (mainMenuView != null)
            mainMenuView.HideAllPanels();

        resultView.Render(
            firstCareer,
            firstSprite,
            secondCareer,
            secondSprite
        );

        resultView.Show();
    }

    public void HideResult()
    {
        if (resultView != null)
            resultView.Hide();
    }

    private Sprite GetCareerSprite(string careerId)
    {
        if (string.IsNullOrWhiteSpace(careerId))
            return null;

        switch (careerId)
        {
            case "fisica":
                return fisica;

            case "quimica":
                return quimica;

            case "matematicas":
                return matematicas;

            case "biologia":
                return biologia;

            case "ing_mecatronica":
                return ingMecatronica;

            case "ing_multimedia":
                return ingMultimedia;

            case "ing_ambiental":
                return ingAmbiental;

            case "ing_biomedica":
                return ingBiomedica;

            case "ciencia_datos":
                return cienciaDatos;

            case "ciberseguridad":
                return ciberseguridad;

            case "devops":
                return devops;

            case "inteligencia_artificial":
                return inteligenciaArtificial;

            default:
                Debug.LogWarning(
                    "No existe un sprite configurado para: " +
                    careerId
                );
                return null;
        }
    }
}