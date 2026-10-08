using UnityEngine;

public class ObjectIdentifier : MonoBehaviour
{
    [SerializeField] private string prefabId;
    [SerializeField] private string materialId;

    public string PrefabId => prefabId;
    public string MaterialId => materialId;

    public void SetPrefabId(string id)
    {
        prefabId = id;
    }

    public void SetMaterialId(string id)
    {
        materialId = id;
    }
}
