using UnityEngine;
using StarterAssets;
using System.Collections;

public class PickupItemBehaviour : MonoBehaviour
{
    GameObject currentItem = null;

    void Start()
    {
        
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider.CompareTag("Pickup")) {
            HandlePlayerPickup(hit.collider.gameObject);
        }
    }


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

    public void DropItem()
    {
        GameObject item = currentItem;
        if (item == null) return;
        
        Transform playerTransform = transform;
        Collider playerCollider = GetComponent<Collider>();

        // 1. Ontkoppel van de speler
        item.transform.SetParent(null);

        // 2. Activeer Rigidbody en reset krachten
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 3. Negeer tijdelijk de botsing met de speler
        Collider itemCollider = item.GetComponent<Collider>();
        if (itemCollider != null && itemCollider is MeshCollider) (itemCollider as MeshCollider).convex = true;
        if (itemCollider != null && playerCollider != null)
        {
            Physics.IgnoreCollision(itemCollider, playerCollider, true);
            StartCoroutine(ResetCollision(itemCollider, playerCollider, 0.5f));            
        }
        Rigidbody itemBody = currentItem.GetComponent<Rigidbody>();
        if (itemBody == null) itemBody = currentItem.AddComponent<Rigidbody>();
        itemBody.isKinematic = false;

        // 4. Plaats net voor de speler
        item.transform.position = playerTransform.position + playerTransform.forward * 1.5f + Vector3.up * 0.5f;
    }

    private IEnumerator ResetCollision(Collider itemCol, Collider playerCol, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (itemCol != null && playerCol != null)
        {
            Physics.IgnoreCollision(itemCol, playerCol, false);
        }
    }    

    private void HandlePlayerPickup(GameObject target)
    {
        ThirdPersonController[] playerControllers = FindObjectsByType<ThirdPersonController>(FindObjectsSortMode.None);

        foreach (var playerController in playerControllers)
        {
            if (playerController != null && target != null)
            {
                Transform hand = FindDescendant(playerController.transform, "Right_Hand");
                if (hand)
                {
                    if (currentItem) DropItem();
                    if (target.TryGetComponent(out Rigidbody rb))
                    {
                        rb.isKinematic = true;
                    }
                    target.transform.SetParent(hand, false);
                    target.transform.localPosition = Vector3.zero;
                    target.transform.localRotation = Quaternion.identity;
                    currentItem = target;
                }

                break;    
            }
        }
    }

}
