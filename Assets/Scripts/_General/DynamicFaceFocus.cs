using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using TransformHandles.Utils;

public class DynamicFaceFocus : MonoBehaviour
{
    private Transform headTransform;
    private GameObject faceCamObj;
    private CinemachineCamera previousActiveCamera;
    private Camera mainCamera;
    private Vector3 originalCamPos;
    private Quaternion originalCamRot;
    private Transform originalCamParent;

    private bool isApplied = false;

    public void Apply()
    {
        if (isApplied) return;
        isApplied = true;

        foreach (Transform t in transform.root.GetComponentsInChildren<Transform>())
        {
            if (t.name.Equals("Head", System.StringComparison.OrdinalIgnoreCase) || t.name.Equals("Hfd", System.StringComparison.OrdinalIgnoreCase))
            {
                headTransform = t;
                break;
            }
        }

        if (headTransform == null) return;

        var brain = FindFirstObjectByType<CinemachineBrain>();
        brain.enabled = false;

        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            originalCamPos = mainCamera.transform.position;
            originalCamRot = mainCamera.transform.rotation;
            originalCamParent = mainCamera.transform.parent;

            mainCamera.transform.position = headTransform.TransformPoint(0, 0, 3f);
            mainCamera.transform.LookAt(headTransform);

            hideBlockingObjects(mainCamera);
        }

    }

    private List<MeshRenderer> hiddenRenderers = null;

    void hideBlockingObjects(Camera camObj)
    {
        hiddenRenderers = new List<MeshRenderer>();
        Vector3 direction = camObj.transform.position - headTransform.position;
        float distance = direction.magnitude;

        if (Physics.Raycast(headTransform.position, direction.normalized, out RaycastHit hit, distance))
        {
            MeshRenderer renderer = hit.collider.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
                hiddenRenderers.Add(renderer);
            }
        }
    }

    public void Unapply()
    {
        if (!isApplied) return;

        if (mainCamera != null)
        {
            if (hiddenRenderers != null) {
                foreach (var renderer in hiddenRenderers)
                {
                    if (renderer != null) renderer.enabled = true;
                }        
            }

            mainCamera.transform.SetParent(originalCamParent);
            mainCamera.transform.position = originalCamPos;
            mainCamera.transform.rotation = originalCamRot;
        }

        var brain = FindFirstObjectByType<CinemachineBrain>();
        brain.enabled = true;

        isApplied = false;
    }
}