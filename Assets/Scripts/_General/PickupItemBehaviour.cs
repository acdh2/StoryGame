using UnityEngine;
using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor;

public class PickupItemBehaviour : MonoBehaviour
{
    public List<string> inventory = new List<string>();
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

    public bool Has(string itemName)
    {
        if (itemName == "") return false;
        return inventory.Contains(itemName);
    }  

    public void Drop(string itemName)
    {
        if (itemName == "") return;
        if (itemName != null)
        {
            while (inventory.Contains(itemName)) {
                inventory.Remove(itemName);
            };
        }
    }

    public void Pickup(string itemName)
    {
        if (itemName == "") return;
        if (!inventory.Contains(itemName)) inventory.Add(itemName);

        GameObject target = GameObject.Find(itemName);

        ThirdPersonController[] playerControllers = FindObjectsByType<ThirdPersonController>(FindObjectsSortMode.None);
        foreach (var playerController in playerControllers)
        {
            if (playerController != null && target != null)
            {
                Transform hand = FindDescendant(playerController.transform, "Right_Hand");
                if (hand)
                {
                    if (currentItem) {
                        Destroy(currentItem);
                        currentItem = null;
                    }

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

        if (target != null) Destroy(target);
    }

}
