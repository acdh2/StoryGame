using UnityEngine;
using StarterAssets;
using Yarn.Unity;

public class PlayMode : MonoBehaviour
{
    public GameObject playerPrefab;
    public GameObject editorCamera;
    public StarterAssetsInputs starterAssetsInputs;
    public GameObject dialogueRunnerPrefab;
    private DialogueRunner dialogueRunner;

    private bool firstRun = true;

    private void OnEnable()
    {
        if (firstRun) {
            firstRun = false;
            return;
        }

        if (editorCamera != null)
            editorCamera.SetActive(false);

        if (playerPrefab != null) 
            playerPrefab.SetActive(true);

        if (starterAssetsInputs != null) 
            starterAssetsInputs.cursorInputForLook = true;

        StartDialogue();
    }

    private void StartDialogue() 
    {
        if (dialogueRunner == null) {
            GameObject dialogGameObject = GameObject.Instantiate(dialogueRunnerPrefab);
            dialogueRunner = dialogGameObject.GetComponent<DialogueRunner>();
            dialogueRunner.StartDialogue("Start");
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

    private void OnDisable()
    {
        if (editorCamera != null)
            editorCamera.SetActive(true);

        if (playerPrefab != null) 
            playerPrefab.SetActive(false);

        if (starterAssetsInputs != null) 
            starterAssetsInputs.cursorInputForLook = false;

        EndDialogue();
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