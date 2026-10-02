using System.Collections;
using UnityEngine;
using StarterAssets;
using UnityEngine.UIElements;

[RequireComponent(typeof(DialogueInterpreter))]
public class PlayMode : UIControllerBase
{
    [SerializeField]private GameObject editorCamera;

    [SerializeField]private GameConfiguration gameConfiguration;

    [SerializeField]private SceneManager sceneManager;

    private DialogueInterpreter dialogueInterpreter;
    private GameObject player;
    private DynamicFaceFocus dynamicFaceFocus;
    
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

    protected override void OnUIDisabled()
    {
        EndStory();
        EnableEditorCamera();
        DestroyPlayer();
    }

    private void StartStory(VisualElement root)
    {   
        if (sceneManager != null)
        {
            sceneManager.PushSceneState();
        }
        if (dialogueInterpreter != null) {
            dialogueInterpreter.StartDialogue(root);
        }        
    }

    private void EndStory() {
        if (sceneManager != null)
        {
            sceneManager.PopSceneState();
        }
    }

    private void DisableEditorCamera() 
    {
        if (editorCamera != null)
            editorCamera.SetActive(false);
    }

    private void EnableEditorCamera()
    {
        if (editorCamera != null) {
            editorCamera.SetActive(true);
        }
    }

    private void CreatePlayer()
    {
        GameObject playerPrefab = gameConfiguration.GetPlayerPrefab();
        if (player == null && playerPrefab != null) {
            player = Instantiate(playerPrefab);
            dialogueInterpreter.OnScreenShown += HandleScreenShown;
            dialogueInterpreter.OnScreenHidden += HandleScreenHidden;
            dialogueInterpreter.OnCameraChangeRequested += HandleCameraChange;
            if (gameConfiguration != null) gameConfiguration.Apply();
            AddCollisionDetector(player);
            dynamicFaceFocus = player.AddComponent<DynamicFaceFocus>();
        }      
    }

    private void AddCollisionDetector(GameObject player)
    {
        CharacterController characterController = player.GetComponentInChildren<CharacterController>();
        if (characterController != null) {
            var playerCollisionDetector = characterController.gameObject.AddComponent<PlayerCollisionDetector>();
            playerCollisionDetector.dialogueInterpreter = dialogueInterpreter;
        }
    }

    private void DestroyPlayer()
    {
        if (player != null) {
            if (gameConfiguration != null) gameConfiguration.Unapply();
            Destroy(dynamicFaceFocus);
            dialogueInterpreter.OnCameraChangeRequested -= HandleCameraChange;
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


    private void HandleCameraChange(bool mode)
    {
        if (dynamicFaceFocus != null)
        {
            if (mode) dynamicFaceFocus.Apply();
            else dynamicFaceFocus.Unapply();
        }        
    }

}