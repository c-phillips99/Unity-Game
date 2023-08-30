using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ink.Runtime;
using TarodevController;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    private Story _currentStory;
    private bool _dialogueIsPlaying = false;
    public bool DialogueIsPlaying { get { return _dialogueIsPlaying; } private set { _dialogueIsPlaying = value; } }

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Update()
    {
        if (!DialogueIsPlaying) return;

        if (PlayerInput.FrameInput.Interact) ContinueStory();
    }
    public void StartDialogue(TextAsset inkDialogueJSON)
    {
        Debug.Log("Conversation started");
        _currentStory = new Story(inkDialogueJSON.text);
        DialogueIsPlaying = true;
    }
    private void ContinueStory()
    {
        if (_currentStory.canContinue) Debug.Log(_currentStory.Continue());
        else EndDialogue();
        
    }

    private void EndDialogue()
    {
        Debug.Log("Conversation ended");
        DialogueIsPlaying = false;
    }
}
