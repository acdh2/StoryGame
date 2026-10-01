using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class RespawnBehaviour : MonoBehaviour
{
    public GameObject spawnPoint = null;
    private Transform respawnPoint;

    private Transform teleportationTarget = null;
    private float teleportationTargetHeight = 0f;

    public float fallThreshold = -10f;

    private bool shouldReset = false;
    private Vector3 resetPosition = Vector3.zero;
    private Quaternion resetRotation = Quaternion.identity;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();

        CinemachineCamera cinemachineCamera = GetComponentInParent<CinemachineCamera>();
        if (spawnPoint != null) {
            SetRespawnPoint(spawnPoint);
            Respawn();
        } else {
            var spawns = GameObject.FindGameObjectsWithTag("SpawnPoint");
            if (spawns.Length > 0)
            {
                var id = Random.Range(0, spawns.Length);
                SetRespawnPoint(spawns[id]);
                Respawn();
            }
        }
        resetPosition = transform.position;
        resetRotation = transform.rotation;
    }

    void Update()
    {
        if (transform.position.y < fallThreshold)
        {
            Respawn();
        }
    }

    void LateUpdate() 
    {
        if (teleportationTarget != null) {
            Vector3 finalPosition = teleportationTarget.position;
            finalPosition.y += teleportationTargetHeight;

            TeleportImmediatelyTo(finalPosition, teleportationTarget.rotation);
            teleportationTarget = null;
        }
        if (shouldReset)
        {
            TeleportImmediatelyTo(resetPosition, resetRotation);
            shouldReset = false;
        }
    }

    public void SetRespawnPoint(GameObject spawnPoint)
    {
        respawnPoint = spawnPoint.transform;
    }

    public void Respawn()
    {
        if (respawnPoint != null) {
            TeleportTo(respawnPoint);
        } else
        {
            shouldReset = true;
        }
    }

    public void Teleport(GameObject target) {
        TeleportTo(target.transform);
    }

    public void TeleportByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return;

        string searchName = name.Trim().ToLowerInvariant();
        var transforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (var t in transforms)
        {
            if (t.name.Trim().ToUpperInvariant() == searchName.ToUpperInvariant())
            {
                TeleportTo(t);
                break;
            }
        }
    }

    public void TeleportTo(Transform targetTransform)
    {
        teleportationTarget = targetTransform;
        teleportationTargetHeight = 0f;

        // Bereken de hoogte op basis van de Collider van het target object
        Collider targetCollider = targetTransform.GetComponent<Collider>();
        if (targetCollider != null)
        {
            teleportationTargetHeight = targetCollider.bounds.extents.y;
        }
        else
        {
            // Fallback op Renderer bounds als er geen Collider is
            Renderer targetRenderer = targetTransform.GetComponent<Renderer>();
            if (targetRenderer != null)
            {
                teleportationTargetHeight = targetRenderer.bounds.extents.y;
            }
        }

        // Voeg de helft van de hoogte van de eigen CharacterController toe zodat het karakter er echt bovenop staat in plaats van erin verzonden te worden
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            teleportationTargetHeight += cc.height / 2f;
        }
    }

    private void TeleportImmediatelyTo(Vector3 targetPosition, Quaternion targetRotation)
    {
        var characterController = GetComponent<CharacterController>();
        if (characterController != null) {
            CinemachineCamera cinemachineCamera = GetComponentInParent<CinemachineCamera>();

            var currentCameraPosition = Vector3.zero;
            if (cinemachineCamera != null) {
                currentCameraPosition = cinemachineCamera.Follow.transform.position;
            }

            characterController.enabled = false;
            transform.position = targetPosition;
            transform.rotation = targetRotation;
            characterController.enabled = true;

            if (cinemachineCamera != null) {
                cinemachineCamera.OnTargetObjectWarped(cinemachineCamera.Follow, cinemachineCamera.Follow.transform.position - currentCameraPosition);
            }
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider.CompareTag("SpawnPoint")) {
            SetRespawnPoint(hit.gameObject);
        }
        
        Renderer rend = hit.collider.GetComponent<Renderer>();
        if (rend != null && rend.sharedMaterial != null)
        {
            if (rend.sharedMaterial.name.Contains("lava"))
            {
                AudioClip clip = Resources.Load<AudioClip>("resetsound");

                if (clip != null)
                {
                    audioSource.pitch = Random.Range(1.3f, 1.55f);
                    audioSource.PlayOneShot(clip);
                }
                Respawn();
            }
        }
    }
}
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using Unity.Cinemachine;

// public class RespawnBehaviour : MonoBehaviour
// {
//     public GameObject spawnPoint = null;
//     private Transform respawnPoint;

//     private Transform teleportationTarget = null;

//     //public CinemachineCamera cinemachineCamera;

//     public float fallThreshold = -10f;

//     private bool shouldReset = false;
//     private Vector3 resetPosition = Vector3.zero;
//     private Quaternion resetRotation = Quaternion.identity;

//     private AudioSource audioSource;

//     void Start()
//     {
//         audioSource = gameObject.AddComponent<AudioSource>();

//         CinemachineCamera cinemachineCamera = GetComponentInParent<CinemachineCamera>();
//         if (spawnPoint != null) {
//             SetRespawnPoint(spawnPoint);
//             Respawn();
//         } else {
//             var spawns = GameObject.FindGameObjectsWithTag("SpawnPoint");
//             if (spawns.Length > 0)
//             {
//                 var id = Random.Range(0, spawns.Length);
//                 SetRespawnPoint(spawns[id]);
//                 Respawn();
//             }
//         }
//         resetPosition = transform.position;
//         resetRotation = transform.rotation;
//     }

//     void Update()
//     {
//         if (transform.position.y < fallThreshold)
//         {
//             Respawn();
//         }
//     }

//     void LateUpdate() 
//     {
//         if (teleportationTarget != null) {
//             TeleportImmediatelyTo(teleportationTarget.position, teleportationTarget.rotation);
//             teleportationTarget = null;
//         }
//         if (shouldReset)
//         {
//             TeleportImmediatelyTo(resetPosition, resetRotation);
//             shouldReset = false;
//         }
//     }

//     public void SetRespawnPoint(GameObject spawnPoint)
//     {
//         respawnPoint = spawnPoint.transform;
//     }

//     public void Respawn()
//     {
//         if (respawnPoint != null) {
//             TeleportTo(respawnPoint);
//         } else
//         {
//             shouldReset = true;
//         }
//     }

//     public void Teleport(GameObject target) {
//         TeleportTo(target.transform);
//     }

//     public void TeleportByName(string name)
//     {
//         if (string.IsNullOrEmpty(name)) return;

//         string searchName = name.Trim().ToLowerInvariant();
//         var transforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
//         foreach (var t in transforms)
//         {
//             if (t.name.Trim().ToUpperInvariant() == searchName.ToUpperInvariant())
//             {
//                 TeleportTo(t);
//                 break;
//             }
//         }
//     }

//     public void TeleportTo(Transform targetTransform)
//     {
//         teleportationTarget = targetTransform;
//     }

//     private void TeleportImmediatelyTo(Vector3 targetPosition, Quaternion targetRotation)
//     {
//         var characterController = GetComponent<CharacterController>();
//         if (characterController != null) {
//             CinemachineCamera cinemachineCamera = GetComponentInParent<CinemachineCamera>();

//             var currentCameraPosition = Vector3.zero;
//             if (cinemachineCamera != null) {
//                 currentCameraPosition = cinemachineCamera.Follow.transform.position;
//             }

//             characterController.enabled = false;
//             transform.position = targetPosition;
//             transform.rotation = targetRotation;
//             characterController.enabled = true;

//             if (cinemachineCamera != null) {
//                 cinemachineCamera.OnTargetObjectWarped(cinemachineCamera.Follow, cinemachineCamera.Follow.transform.position - currentCameraPosition);
//             }
//         }
//     }

//     void OnControllerColliderHit(ControllerColliderHit hit)
//     {
//         if (hit.collider.CompareTag("SpawnPoint")) {
//             SetRespawnPoint(hit.gameObject);
//         }
        
//         Renderer rend = hit.collider.GetComponent<Renderer>();
//         if (rend != null && rend.sharedMaterial != null)
//         {
//             if (rend.sharedMaterial.name.Contains("lava"))
//             {
//                 AudioClip clip = Resources.Load<AudioClip>("resetsound");

//                 if (clip != null)
//                 {
//                     audioSource.pitch = Random.Range(1.3f, 1.55f);
//                     audioSource.PlayOneShot(clip);
//                 }
//                 Respawn();
//             }
//         }
//     }
// }
