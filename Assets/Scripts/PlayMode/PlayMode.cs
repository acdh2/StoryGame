using System.Collections;
using UnityEngine;
using StarterAssets;
using UnityEngine.UIElements;

[RequireComponent(typeof(DialogueInterpreter))]
public class PlayMode : UIControllerBase
{
    public GameObject editorCamera;
    public ObjectSelector objectSelector;

    public GameConfiguration gameConfiguration;

    private DialogueInterpreter dialogueInterpreter;
    private GameObject player;
    
    protected override void Awake()
    {
        base.Awake();
        dialogueInterpreter = GetComponent<DialogueInterpreter>();
    }

    protected override void OnUIEnabled(VisualElement root)
    {
        DisableEditorCamera();
        CreatePlayer();
        StartStory(root);
    }

    private void StartStory(VisualElement root)
    {   
        if (dialogueInterpreter != null) {
            dialogueInterpreter.StartDialogue(root);
        }        
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
        GameObject playerPrefab = gameConfiguration.GetPlayerPrefab();
        if (player == null && playerPrefab != null) {
            player = Instantiate(playerPrefab);
            dialogueInterpreter.OnScreenShown += HandleScreenShown;
            dialogueInterpreter.OnScreenHidden += HandleScreenHidden;
            if (gameConfiguration != null) gameConfiguration.Apply();
        }      
    }

    private void DestroyPlayer()
    {
        if (player != null) {
            if (gameConfiguration != null) gameConfiguration.Unapply();
            dialogueInterpreter.OnScreenShown -= HandleScreenShown;
            dialogueInterpreter.OnScreenHidden -= HandleScreenHidden;
            Destroy(player);
        }
    }

    private void HandleScreenShown()
    {
        if (player != null)
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