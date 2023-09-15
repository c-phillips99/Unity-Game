using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState;

    public static event Action<GameState> OnGameStateChanged;

    public enum GameState
    {
        Menu,
        GameAlive,
        GameDead,
        GamePaused,
        Quit
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void UpdateGameState (GameState newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case GameState.Menu:
                break;
            case GameState.GameAlive:
                SceneManager.LoadScene(1);
                break;
            case GameState.GameDead:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }

        OnGameStateChanged?.Invoke(newState);
    }
}
