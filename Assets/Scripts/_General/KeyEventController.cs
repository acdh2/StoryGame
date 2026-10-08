using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

public class KeyEventController : MonoBehaviour
{
    private InputSystem_Actions inputActions;
    private IKeyEventReceiver[] receivers;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        //receivers = GetComponents<IKeyEventReceiver>();
        receivers = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                  .OfType<IKeyEventReceiver>()
                  .ToArray();        
    }

    private void OnEnable()
    {
        inputActions.KeyboardShortcuts.Enable();

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

        inputActions.KeyboardShortcuts.Disable();
    }

    private void OnActionPerformed(InputAction.CallbackContext context)
    {
        for (int i = 0; i < receivers.Length; i++)
        {            
            receivers[i].OnKeyEvent(context.action.name);
        }
    }
}