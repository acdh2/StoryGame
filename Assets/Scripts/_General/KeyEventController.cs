using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

public class KeyEventController : MonoBehaviour
{
    private InputSystem_Actions inputActions;
    private IKeyEventReceiver[] receivers;

    private void Awake()
    {
        receivers = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                  .OfType<IKeyEventReceiver>()
                  .ToArray();
    }

    private void OnEnable()
    {
        inputActions = InputManager.Instance.Actions;
        InputManager.Instance.EnableMap("KeyboardShortcuts");

        var map = inputActions.asset.FindActionMap("KeyboardShortcuts");
        foreach (var action in map.actions)
        {
            action.performed += OnActionPerformed;
        }
    }

    private void OnDisable()
    {
        var map = inputActions.asset.FindActionMap("KeyboardShortcuts");
        foreach (var action in map.actions)
        {
            action.performed -= OnActionPerformed;
        }

        InputManager.Instance?.DisableMap("KeyboardShortcuts");
    }

    private void OnActionPerformed(InputAction.CallbackContext context)
    {
        if (!UIToolkitHelper.IsAnyTextFieldFocused()) {
            for (int i = 0; i < receivers.Length; i++)
            {
                receivers[i].OnKeyEvent(context.action.name);
            }
        }
    }
}