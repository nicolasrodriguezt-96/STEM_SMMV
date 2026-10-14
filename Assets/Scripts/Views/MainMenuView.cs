using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuView : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField]
    private GameObject packagesPanel;

    [SerializeField]
    private GameObject quizzesPanel;

    [Header("Containers")]
    [SerializeField]
    private Transform packagesContainer;

    [SerializeField]
    private Transform quizzesContainer;

    [Header("Prefabs")]
    [SerializeField]
    private GameObject menuButtonPrefab;

    [Header("Texts")]
    [SerializeField]
    private TMP_Text quizzesTitle;

    private PackageLoaderService packageLoader;

    private List<LoadedPackageModel> loadedPackages;

    private void Start()
    {
        packageLoader = new PackageLoaderService();

        ShowPackages();
    }

    public void ShowPackages()
    {
        packagesPanel.SetActive(true);
        quizzesPanel.SetActive(false);

        ClearContainer(packagesContainer);

        loadedPackages =
            packageLoader.LoadAllPackages();

        if (loadedPackages.Count == 0)
        {
            CreateMessageButton(
                packagesContainer,
                "No hay paquetes instalados"
            );

            return;
        }

        foreach (LoadedPackageModel package
                 in loadedPackages)
        {
            CreatePackageButton(package);
        }
    }

    private void CreatePackageButton(
        LoadedPackageModel package)
    {
        GameObject buttonObject =
            Instantiate(
                menuButtonPrefab,
                packagesContainer
            );

        Button button =
            buttonObject.GetComponent<Button>();

        TMP_Text text =
            buttonObject.GetComponentInChildren<TMP_Text>();

        text.text =
            package.manifest.nombre;

        button.onClick.AddListener(
            () => SelectPackage(package)
        );
    }

    private void SelectPackage(
        LoadedPackageModel package)
    {
        AppController.Instance.SelectPackage(
            package
        );

        ShowQuizzes(package);
    }

    private void ShowQuizzes(
        LoadedPackageModel package)
    {
        packagesPanel.SetActive(false);
        quizzesPanel.SetActive(true);

        ClearContainer(quizzesContainer);

        if (quizzesTitle != null)
        {
            quizzesTitle.text =
                package.manifest.nombre;
        }

        if (package.cuestionarios == null ||
            package.cuestionarios.Count == 0)
        {
            CreateMessageButton(
                quizzesContainer,
                "Este paquete no contiene cuestionarios"
            );

            return;
        }

        foreach (QuizModel quiz
                 in package.cuestionarios)
        {
            CreateQuizButton(quiz);
        }
    }

    private void CreateQuizButton(
        QuizModel quiz)
    {
        GameObject buttonObject =
            Instantiate(
                menuButtonPrefab,
                quizzesContainer
            );

        Button button =
            buttonObject.GetComponent<Button>();

        TMP_Text text =
            buttonObject.GetComponentInChildren<TMP_Text>();

        text.text = quiz.nombre;

        button.onClick.AddListener(
            () => SelectQuiz(quiz)
        );
    }

    private void SelectQuiz(
        QuizModel quiz)
    {
        AppController.Instance.StartQuiz(
            quiz
        );

        Debug.Log(
            $"Quiz seleccionado: {quiz.nombre}"
        );

        SequenceItemModel firstItem =
            AppController.Instance
                .GetCurrentSequenceItem();

        if (firstItem != null)
        {
            Debug.Log(
                $"Primer elemento: " +
                $"{firstItem.tipo} - " +
                $"{firstItem.id}"
            );
        }
    }

    public void BackToPackages()
    {
        AppController.Instance.ResetApplication();

        ShowPackages();
    }

    private void ClearContainer(
        Transform container)
    {
        for (
            int i = container.childCount - 1;
            i >= 0;
            i--)
        {
            Destroy(
                container.GetChild(i).gameObject
            );
        }
    }

    private void CreateMessageButton(
        Transform container,
        string message)
    {
        GameObject buttonObject =
            Instantiate(
                menuButtonPrefab,
                container
            );

        TMP_Text text =
            buttonObject.GetComponentInChildren<TMP_Text>();

        Button button =
            buttonObject.GetComponent<Button>();

        text.text = message;

        button.interactable = false;
    }
}