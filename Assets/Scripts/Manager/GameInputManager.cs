using UnityEngine;
using UnityEngine.InputSystem;

public class GameInputManager : MonoBehaviour
{
    private GameInputActions inputActions;

    private void Awake()
    {
        inputActions = new GameInputActions();
    }

    private void OnEnable()
    {
        inputActions.Gameplay.Enable();

        inputActions.Gameplay.CancelTarget.performed +=
            OnCancelTarget;
    }

    private void OnDisable()
    {
        inputActions.Gameplay.CancelTarget.performed -=
            OnCancelTarget;

        inputActions.Gameplay.Disable();
    }

    private void OnCancelTarget(
        InputAction.CallbackContext context)
    {
        if (EffectTargetManager.Instance == null)
            return;

        if (!EffectTargetManager.Instance.IsSelectingTarget)
            return;

        EffectTargetManager.Instance
            .CancelTargetSelection();
    }
}