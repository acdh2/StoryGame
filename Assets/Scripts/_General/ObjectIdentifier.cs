using UnityEngine;

public class ObjectIdentifier : MonoBehaviour
{
    [SerializeField] private string prefabId;

    public string PrefabId => prefabId;

    public void SetPrefabId(string id)
    {
        prefabId = id;
    }
}