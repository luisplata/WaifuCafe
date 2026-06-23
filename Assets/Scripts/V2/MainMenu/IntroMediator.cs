using System;
using System.Collections.Generic;
using TMPro;
using PrimeTween;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntroMediator : MonoBehaviour
{
    public Action OnFinish;
    [Header("UI")] [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField] private RectTransform characterImage;
    [SerializeField] private GameObject[] elementsToShow;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Canvas canvas;

    [Header("Dialogue")] [SerializeField] private List<string> pages = new();

    private int currentPage;

    private void Start()
    {
        nextButton.gameObject.SetActive(false);
        nextButton.onClick.AddListener(Next);
        canvas.sortingOrder = -1;
        // canvas.gameObject.SetActive(false);
        fadeGroup.blocksRaycasts = false;
    }

    public void PlayIntro()
    {
        fadeGroup.blocksRaycasts = true;
        fadeGroup.alpha = 0;
        canvas.sortingOrder = 5;
        // canvas.gameObject.SetActive(true);

        characterImage.anchoredPosition =
            new Vector2(1400, characterImage.anchoredPosition.y);

        Sequence.Create()
            .Chain(
                Tween.Alpha(
                    fadeGroup,
                    endValue: 1,
                    duration: 1f))
            .Chain(
                Tween.UIAnchoredPositionX(
                    characterImage,
                    endValue: 400,
                    duration: 1f))
            .ChainCallback(ShowUI)
            .ChainCallback(StartDialogue);
    }

    private void ShowUI()
    {
        foreach (var element in elementsToShow)
        {
            element.SetActive(true);
        }
    }

    private void StartDialogue()
    {
        currentPage = 0;

        dialogueText.text = pages[currentPage];

        nextButton.gameObject.SetActive(true);
    }

    public void Next()
    {
        currentPage++;

        if (currentPage >= pages.Count)
        {
            Finish();
            return;
        }

        dialogueText.text = pages[currentPage];
    }

    private void Finish()
    {
        canvas.sortingOrder = -1;
        OnFinish?.Invoke();
    }
}