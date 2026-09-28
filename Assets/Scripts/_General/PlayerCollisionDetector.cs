using System;
using UnityEngine;

public class PlayerCollisionDetector : MonoBehaviour
{
    public DialogueInterpreter dialogueInterpreter;

    void OnControllerColliderHit(ControllerColliderHit hit)
    {        
        GameObject target = hit.collider.gameObject;
        if (target != null) {
            ObjectIdentifier objectIdentifier = target.GetComponentInParent<ObjectIdentifier>();
            if (objectIdentifier != null)
            {
                print(String.Format("A collision was found! name={0}", target.name));
                if (dialogueInterpreter != null)
                {
                    dialogueInterpreter.OnObjectTouched(target.name);
                }
            }
        }        
    }
}
