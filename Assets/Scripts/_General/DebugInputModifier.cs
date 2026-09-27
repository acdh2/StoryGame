using UnityEngine;
using UnityEngine.InputSystem;

public class DebugInputModifier : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;

    private void Awake()
    {
#if UNITY_EDITOR        
        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();

        InputAction lookAction = playerInput.actions.FindAction("Look");

        if (lookAction != null)
        {
            // Overschrijft de processor voor de binding naar ScaleVector2(x=1,y=1)
            lookAction.ApplyBindingOverride(new InputBinding
            {
                overrideProcessors = "ScaleVector2(x=1,y=0.05)"
            });
        }
#endif
    }
}