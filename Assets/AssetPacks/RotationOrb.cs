using UnityEngine;

public class RotationOrb : MonoBehaviour
{
    void LateUpdate()
    {
        //transform.Rotate(new Vector3(2f, 3f, 5f) * 0.05f);
        transform.rotation = Quaternion.identity;
    }
}
