using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableCommanderPeri : Interactable
{
    [SerializeField] private Commander commander;

    public override bool CanInteract()
    {
        return commander.currentPosition == Commander.Position.Inside;
    }

    public override void Interact()
    {
        commander.StartInteractionCooldown();
        commander.TogglePeriView(!commander.commanderPeriscope_Active);
    }
}
