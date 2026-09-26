using UnityEngine;
using StarterAssets;
using UnityEngine.UIElements;

[RequireComponent(typeof(DialogueInterpreter))]
public class PlayMode : UIControllerBase
{
    public GameObject editorCamera;
    public GameObject playerPrefab;
    public ObjectSelector objectSelector;

    private DialogueInterpreter dialogueInterpreter;
    private GameObject player;

    protected override void Awake()
    {
        base.Awake();
        dialogueInterpreter = GetComponent<DialogueInterpreter>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) {
            dialogueInterpreter.StartDialogue();
        }
    }

    protected override void OnUIEnabled(VisualElement root)
    {
        DisableEditorCamera();
        CreatePlayer();
    }

    protected override void OnUIDisabled()
    {
        EnableEditorCamera();
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
        if (player == null && playerPrefab != null) {
            player = Instantiate(playerPrefab);
            dialogueInterpreter.OnScreenShown += HandleScreenShown;
            dialogueInterpreter.OnScreenHidden += HandleScreenHidden;
        }      
    }

    private void DestroyPlayer()
    {
        if (player != null) {
            dialogueInterpreter.OnScreenShown -= HandleScreenShown;
            dialogueInterpreter.OnScreenHidden -= HandleScreenHidden;
            Destroy(player);
        }
    }

    private void HandleScreenShown()
    {
        if (player)
        {
            PlayerControlBlocker playerControlBlocker = player.GetComponentInChildren<PlayerControlBlocker>();
            playerControlBlocker?.SetControlsActive(false);
        }
    }

    private void HandleScreenHidden()
    {
        if (player) {
            PlayerControlBlocker playerControlBlocker = player.GetComponentInChildren<PlayerControlBlocker>();
            playerControlBlocker?.SetControlsActive(true);
        }
    }

}