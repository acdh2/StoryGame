using UnityEngine;
using StarterAssets;

public class PlayMode : MonoBehaviour
{
    public GameObject playerPrefab;
    public GameObject editorCamera;
    public StarterAssetsInputs starterAssetsInputs;

    private void OnEnable()
    {
        if (editorCamera != null)
            editorCamera.SetActive(false);

        if (playerPrefab != null) 
            playerPrefab.SetActive(true);

        if (starterAssetsInputs != null) 
            starterAssetsInputs.cursorInputForLook = true;
    }

    private void OnDisable()
    {
        if (editorCamera != null)
            editorCamera.SetActive(true);

        if (playerPrefab != null) 
            playerPrefab.SetActive(false);

        if (starterAssetsInputs != null) 
            starterAssetsInputs.cursorInputForLook = false;
    }
}
