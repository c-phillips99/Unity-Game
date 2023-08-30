using System.Collections;
using System.Collections.Generic;
using TarodevController;
using UnityEngine;

public class NPCInteract : MonoBehaviour, IInteractable
{
    [SerializeField] private TextAsset inkDialogueJSON;

    public void Interact(PlayerController playerController)
    {
        if (!DialogueManager.Instance.DialogueIsPlaying) DialogueManager.Instance.StartDialogue(inkDialogueJSON);
    }
}
