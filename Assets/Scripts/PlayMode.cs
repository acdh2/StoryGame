using UnityEngine;
using StarterAssets;
using Yarn.Unity;

public class PlayMode : MonoBehaviour
{
    public GameObject editorCamera;
    public GameObject dialogueRunnerPrefab;
    public GameObject playerPrefab;
    public ObjectSelector objectSelector;

    private DialogueRunner dialogueRunner;
    private GameObject player;

    private bool firstRun = true;

    private void OnEnable()
    {
        if (firstRun) {
            firstRun = false;
            return;
        }

        DisableEditorCamera();
        CreateDialogue();
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

            if (dialogueRunner != null)
            {
                dialogueRunner.onDialogueStart.AddListener(HandleDialogueStart);
                dialogueRunner.onDialogueComplete.AddListener(HandleDialogueComplete);
            }      
        }      
    }

    private void DestroyPlayer()
    {
        if (player != null) {
            Destroy(player);

            if (dialogueRunner != null)
            {
                dialogueRunner.onDialogueStart.RemoveListener(HandleDialogueStart);
                dialogueRunner.onDialogueComplete.RemoveListener(HandleDialogueComplete);
            }            
        }
    }

    private void HandleDialogueStart()
    {
        if (player != null) {
            PlayerControlBlocker blocker = player.GetComponentInChildren<PlayerControlBlocker>();
            print(blocker);
            blocker.DisableControls();
            print("disabled");
        }
        print("dialogue started");
    }

    private void HandleDialogueComplete()
    {
        if (player != null) {
            PlayerControlBlocker blocker = player.GetComponentInChildren<PlayerControlBlocker>();
            blocker.EnableControls();
        }
        print("dialogue ended");
    }    

    private void CreateDialogue() 
    {
        if (dialogueRunner == null) {
            GameObject dialogGameObject = GameObject.Instantiate(dialogueRunnerPrefab);
            dialogueRunner = dialogGameObject.GetComponent<DialogueRunner>();
        }
    }

    private void EndDialogue()
    {
        if (dialogueRunner != null)
        {
            dialogueRunner.Stop();
            Destroy(dialogueRunner.gameObject);
            dialogueRunner = null;
        }
    }

    private void StartDialogue() 
    {
        if (dialogueRunner != null) {
            dialogueRunner.StartDialogue("Start");
        }
    }

}

public class YarnDebugCommands : MonoBehaviour
{
    [YarnCommand("print")]
    public static void PrintDebug(string message)
    {
        Debug.Log($"[Yarn Debug] {message}");
    }
}