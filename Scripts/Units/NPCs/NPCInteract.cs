using System.Collections;
using System.Collections.Generic;
using TarodevController;
using UnityEngine;

public class NPCInteract : MonoBehaviour, IInteractable
{
    public void Interact(PlayerController playerController)
    {
        Debug.Log("Interacted with NPC");
    }
}
