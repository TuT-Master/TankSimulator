using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    [Header("Optional")]
    [SerializeField] private string prompt = "Interact";
    public string Prompt => prompt;

    public abstract void Interact();

    public virtual void OnFocusEnter() { }

    public virtual void OnFocusExit() { }

    public virtual void OnHoldProgress(float progress01) { }
}
