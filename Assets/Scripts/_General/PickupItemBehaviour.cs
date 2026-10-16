using UnityEngine;
using StarterAssets;

public class PickupItemBehaviour : MonoBehaviour
{
    GameObject currentItem = null;

    private Transform FindDescendant(Transform parent, string childName)
    {
        if (parent.name == childName) return parent;
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            Transform result = FindDescendant(child, childName);
            if (result != null) return result;
        }
        return null;
    }  

    public bool HasWeapon()
    {
        return currentItem != null;
    }

    public bool Has(string itemName)
    {
        if (itemName == "") return false;
        return currentItem != null && currentItem.name == itemName;
    }  

    public void Drop(string itemName)
    {
        if (itemName == "") return;
        if (currentItem != null && currentItem.name == itemName)
        {
            Destroy(currentItem);
            currentItem = null;
        }
    }

    public void Pickup(string itemName)
    {
        if (itemName == "") return;
        if (currentItem != null) {
            Destroy(currentItem);
            currentItem = null;
        }

        GameObject target = GameObject.Find(itemName);
        if (target != null) {

            ThirdPersonController[] playerControllers = FindObjectsByType<ThirdPersonController>(FindObjectsSortMode.None);
            foreach (var playerController in playerControllers)
            {
                if (playerController != null)
                {
                    Transform hand = FindDescendant(playerController.transform, "Right_Hand");
                    if (hand)
                    {
                        if (target.TryGetComponent(out Rigidbody rb))
                        {
                            rb.isKinematic = true;
                        }
                        target.transform.SetParent(hand, false);
                        target.transform.localPosition = Vector3.zero;
                        target.transform.localRotation = Quaternion.identity;
                        currentItem = target;
                    }

                    return;    
                }

            }            
        }
    }

}
