using UnityEngine;
using System.Reflection;

public class Trampoline : MonoBehaviour
{
    public float bounceForce = 15f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            MonoBehaviour controller = other.GetComponent("ThirdPersonController") as MonoBehaviour;
            if (controller != null)
            {
                FieldInfo velocityField = controller.GetType().GetField("_verticalVelocity", BindingFlags.NonPublic | BindingFlags.Instance);
                if (velocityField != null)
                {
                    velocityField.SetValue(controller, bounceForce);
                }
            }
        }
    }
}