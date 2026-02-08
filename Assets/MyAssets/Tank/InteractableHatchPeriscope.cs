using UnityEngine;

public class InteractableHatchPeriscope : Interactable
{
    [SerializeField] private Commander commander;
    [SerializeField] private Camera _camera;
    private RenderTexture outputTexture;
    public bool _activeCamera = false;

    private void Start()
    {
        outputTexture = _camera.targetTexture;
    }

    public override void Interact()
    {
        _activeCamera = !_activeCamera;
        commander.LookIntoPeriscope(_camera);

        if (_activeCamera)
            _camera.targetTexture = null;
        else
            _camera.targetTexture = outputTexture;

        commander.StartInteractionCooldown();
    }

    public override bool CanInteract()
    {
        return commander.currentPosition == Commander.Position.Inside;
    }
}
