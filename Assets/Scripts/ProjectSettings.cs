using UnityEngine;

[CreateAssetMenu(fileName = "ProjectSettings", menuName = "Scriptable Objects/ProjectSettings")]
public class ProjectSettings : ScriptableObject
{
    [Header("Snapping Settings")]
    [SerializeField] private Vector3 positionSnap = new Vector3(0.5f, 0.5f, 0.5f);
    [SerializeField] private float rotationSnap = 15f;
    [SerializeField] private Vector3 scaleSnap = new Vector3(0.1f, 0.1f, 0.1f);

    public Vector3 PositionSnap => positionSnap;
    public float RotationSnap => rotationSnap;
    public Vector3 ScaleSnap => scaleSnap;
}