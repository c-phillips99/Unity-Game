using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuScript : MonoBehaviour
{
    [SerializeField] private UIDocument _document;
    public static event Action OnPlayGameClicked;

    private void Start()
    {
        var root = _document.rootVisualElement;
        var playButton = root.Query<Button>(className: "play-button").First();
        playButton.clicked += StartGame;
    }

    private void StartGame()
    {
        GameManager.Instance.UpdateGameState(GameManager.GameState.GameAlive);
    }
}
