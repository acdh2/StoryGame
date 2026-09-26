using UnityEngine;
using StarterAssets;

public class PlayMode : MonoBehaviour
{
    public GameObject editorCamera;
    public GameObject playerPrefab;
    public ObjectSelector objectSelector;

    private GameObject player;

    private void OnEnable()
    {
        DisableEditorCamera();
        CreatePlayer();
        StartDialogue();
    }

    private void OnDisable()
    {
        EnableEditorCamera();
        EndDialogue();
        DestroyPlayer();

    }

    private void DisableEditorCamera() 
    {
        if (editorCamera != null)
            editorCamera.SetActive(false);

        if (objectSelector != null) {
            objectSelector.SelectObject(null);
            objectSelector.gameObject.SetActive(false);
        }
    }

    private void EnableEditorCamera()
    {
        if (editorCamera != null) {
            editorCamera.SetActive(true);
        }

        if (objectSelector != null) {
            objectSelector.gameObject.SetActive(true);
        }
    }

    private void CreatePlayer()
    {
        if (player == null) {
            player = GameObject.Instantiate(playerPrefab);
        }      
    }

    private void DestroyPlayer()
    {
        if (player != null) {
            Destroy(player);
        }
    }

    private void StartDialogue() 
    {
    }

    private void EndDialogue()
    {
    }

}

