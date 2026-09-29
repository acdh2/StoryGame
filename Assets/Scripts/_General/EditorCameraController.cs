using UnityEngine;

public class EditorCameraController : MonoBehaviour
{
    [SerializeField] private ObjectSelector objectSelector = null;

    [Header("Movement")]
    public float moveSpeed = 10f;

    [Header("Look")]
    public float lookSensitivity = 2f;

    private Transform cameraTransform;
    private float verticalRotation = 0f;

    private Vector3 originalPosition = Vector3.zero;
    private Quaternion originalRotation = Quaternion.identity;

    public void ResetView()
    {
        if (objectSelector != null)
        {
            GameObject selectedObject = objectSelector.SelectedObject;
            if (selectedObject)
            {
                Renderer renderer = selectedObject.GetComponentInChildren<Renderer>();
                Vector3 targetCenter = renderer != null ? renderer.bounds.center : selectedObject.transform.position;
                float objectSize = renderer != null ? renderer.bounds.size.z : 2f;
                
                float distance = Mathf.Max(objectSize * 2f, 3f);
                
                transform.position = targetCenter - Vector3.forward * distance;
                transform.LookAt(targetCenter);
                return;
            }
        }
        
        transform.SetPositionAndRotation(originalPosition, originalRotation);
    }
    void Awake()
    {
        transform.GetPositionAndRotation(out originalPosition, out originalRotation);
    }

    void Start()
    {
        // Zoek de child camera op
        Camera cam = GetComponentInChildren<Camera>();
        if (cam != null)
        {
            cameraTransform = cam.transform;
        }
        else
        {
            Debug.LogError("Geen Camera gevonden als child van " + gameObject.name);
        }
    }

    void Update()
    {
        HandleLook();
        HandleMovement();
    }

    void HandleLook()
    {
        // Alleen rondkijken wanneer de rechtermuisknop ingedrukt is
        if (Input.GetMouseButton(1))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

            // Horizontal rotatie: draai de Player op de Y-as
            transform.Rotate(Vector3.up * mouseX);

            // Verticale rotatie: kantel alleen de Camera op de X-as
            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);

            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
            }
        }
        else if (Input.GetMouseButtonUp(1))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

void HandleMovement()
    {
        float inputX = Input.GetAxisRaw("Horizontal"); // A/D
        float inputZ = Input.GetAxisRaw("Vertical");   // W/S

        float inputY = 0f;
        if (Input.GetKey(KeyCode.E)) inputY += 1f; // Omhoog
        if (Input.GetKey(KeyCode.Q)) inputY -= 1f; // Omlaag

        // Horizontale beweging op XZ-plane
        Vector3 horizontalMove = (transform.right * inputX + transform.forward * inputZ).normalized;

        // Verticale beweging op wereld Y-as
        Vector3 verticalMove = Vector3.up * inputY;

        // Gecombineerde beweging
        Vector3 moveDirection = (horizontalMove + verticalMove).normalized;

        transform.position += moveDirection * moveSpeed * Time.deltaTime;
    }
}