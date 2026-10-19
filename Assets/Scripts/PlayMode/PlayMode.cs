using System.Collections;
using UnityEngine;
using StarterAssets;
using UnityEngine.UIElements;
using System.Linq;

[RequireComponent(typeof(DialogueInterpreter))]
public class PlayMode : UIControllerBase
{
    [SerializeField]private GameObject editorCamera;

    [SerializeField]private GameConfiguration gameConfiguration;

    [SerializeField]private SceneManager sceneManager;

    private DialogueInterpreter dialogueInterpreter;
    private GameObject player;
    private DynamicFaceFocus dynamicFaceFocus;

    private bool isActive = false;
    
    protected override void Awake()
    {
        base.Awake();
        dialogueInterpreter = GetComponent<DialogueInterpreter>();
    }

    protected override void OnUIEnabled(VisualElement root)
    {
        sceneManager?.LockState();
        DisableEditorCamera();
        StartStory(root);
    }

    protected override void OnUIDisabled()
    {
        EndStory();
        EnableEditorCamera();
        sceneManager?.UnlockState();

        isActive = false;
    }

    private void RestartStory()
    {
        if (dialogueInterpreter != null) {
            dialogueInterpreter.RestartDialogue();
        }        
    }

    private void StartStory(VisualElement root)
    {   
        CreatePlayer();
        HandleScreenShown();//state starts in unlocked mode
        if (dialogueInterpreter != null) {
            dialogueInterpreter.StartDialogue(root, sceneManager);
        }        
    }

    private void EndStory() {
        dialogueInterpreter.StopDialogue();
        DestroyPlayer();
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
            RespawnBehaviour respawnBehaviour = player.GetComponentInChildren<RespawnBehaviour>();
            if (respawnBehaviour != null) respawnBehaviour.OnPlayerDied += RestartStory;
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
            RespawnBehaviour respawnBehaviour = player.GetComponentInChildren<RespawnBehaviour>();
            if (respawnBehaviour != null) respawnBehaviour.OnPlayerDied -= RestartStory;
            if (gameConfiguration != null) gameConfiguration.Unapply();
            Destroy(dynamicFaceFocus);
            dialogueInterpreter.OnCameraChangeRequested -= HandleCameraChange;
            dialogueInterpreter.OnScreenShown -= HandleScreenShown;
            dialogueInterpreter.OnScreenHidden -= HandleScreenHidden;
            PlayerControlBlocker playerControlBlocker = player.GetComponentInChildren<PlayerControlBlocker>();
            playerControlBlocker?.SetControlsActive(false);
            Destroy(player);
        }
    }

    // void Update()
    // {
    //     if (UnityEngine.Cursor.visible && isActive)
    //     {
    //         isActive = false;
    //         foreach (IKeyEventReceiver keyReceiver in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IKeyEventReceiver>().ToArray())
    //         {
    //             keyReceiver.OnKeyEvent("StopGame");
    //         }
    //     }
    // }

    private void HandleScreenShown()
    {
        isActive = false;
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
        isActive = true;
    }


    private void HandleCameraChange(bool mode, string speaker)
    {
        if (dynamicFaceFocus != null)
        {
            if (mode) dynamicFaceFocus.Apply(speaker);
            else dynamicFaceFocus.Unapply(speaker);
        }        
    }

}