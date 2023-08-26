using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameState CurrentState;

    public static event Action<GameState> OnGameStateChanged;

    public enum GameState
    {
        Menu,
        GameAlive,
        GameDead
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        UpdateGameState(GameState.GameAlive);
    }

    public void UpdateGameState (GameState newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case GameState.Menu:
                break;
            case GameState.GameAlive:
                break;
            case GameState.GameDead:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }

        OnGameStateChanged?.Invoke(newState);
    }
}
