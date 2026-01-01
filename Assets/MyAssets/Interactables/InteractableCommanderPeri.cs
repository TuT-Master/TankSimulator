using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableCommanderPeri : Interactable
{
    [SerializeField] private Commander commander;


    public override void Interact()
    {
        Debug.Log("Interacted with Commander Peri.");

        commander.StartInteractionCooldown();
        commander.TogglePeriView(!commander.commanderPeriscope_Active);
    }
}
