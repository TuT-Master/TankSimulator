using UnityEngine;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    [Header("General")]
    public bool canInteract = true;

    [Header("Raycast")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float maxRange = 5f;
    [SerializeField] private LayerMask interactableLayerMask;

    [Header("Hold Interaction")]
    [SerializeField] private float holdDuration = 0.35f; // how long to hold LMB
    [SerializeField] private bool requireHold = true;

    [Header("Graphics")]
    [SerializeField] private Image cursor;
    public bool showCursorWhenHolding = true;

    // Runtime state
    private Interactable currentTarget;
    private float holdTimer;
    private bool hasInteractedThisHold;

    // Optional: expose progress for UI (0..1)
    public float HoldProgress01 => (currentTarget == null || holdDuration <= 0f) ? 0f : Mathf.Clamp01(holdTimer / holdDuration);
    public Interactable CurrentTarget => currentTarget;




    private void Update()
    {
        if (!canInteract)
        {
            ResetHold();
            return;
        }

        // 1) Find what we are looking at this frame
        bool _ = TryGetInteractableObject(out Interactable lookedAt);

        // 2) If target changed, reset hold and notify focus changes
        if (lookedAt != currentTarget)
        {
            if (currentTarget != null)
                currentTarget.OnFocusExit();

            currentTarget = lookedAt;

            if (currentTarget != null)
                currentTarget.OnFocusEnter();

            ResetHold(keepTarget: true); // keep target, reset timer
        }

        // 3) If no target, nothing to do
        if (currentTarget == null)
        {
            ResetHold();
            return;
        }

        // 4) Handle interaction input
        if (!requireHold)
        {
            // simple click interact
            if (Input.GetMouseButtonDown(0))
                currentTarget.Interact();

            return;
        }

        // Hold-to-interact
        if (Input.GetMouseButton(0))
        {
            holdTimer += Time.deltaTime;

            // Optional: tell target progress (for audio/animation/UI)
            currentTarget.OnHoldProgress(HoldProgress01);
            cursor.enabled = showCursorWhenHolding;
            cursor.fillAmount = HoldProgress01;

            if (!hasInteractedThisHold && holdTimer >= holdDuration)
            {
                hasInteractedThisHold = true;
                currentTarget.Interact();
            }
        }
        else
        {
            // released
            currentTarget.OnHoldProgress(0f);
            ResetHold(keepTarget: true);
        }
    }
    private void ResetHold(bool keepTarget = false)
    {
        holdTimer = 0f;
        hasInteractedThisHold = false;

        cursor.enabled = false;
        cursor.fillAmount = 0f;

        if (!keepTarget)
            currentTarget = null;
    }
    private bool TryGetInteractableObject(out Interactable interactable)
    {
        Vector3 origin = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRange, interactableLayerMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.gameObject.TryGetComponent(out interactable))
                return true;
        }

        interactable = null;
        return false;
    }
}
