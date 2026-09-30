using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class EditorCameraController : MonoBehaviour
{
    [SerializeField] private ObjectSelector objectSelector = null;
    [SerializeField] private InputSystem_Actions inputActions;

    [Header("Settings")]
    public float lookSensitivity = 0.5f;
    public float zoomSpeed = 5f;

    private float verticalRotation = 0f;
    private Vector3 originalPosition = Vector3.zero;
    private Quaternion originalRotation = Quaternion.identity;

    private bool isLookBlockedByUI = false;

    private void Awake()
    {
        transform.GetPositionAndRotation(out originalPosition, out originalRotation);
        if (inputActions == null)
        {
            inputActions = new InputSystem_Actions();
        }
    }

    private void OnEnable()
    {
        inputActions.Editor.Enable();
        inputActions.Editor.Look.performed += OnLookPerformed;
        inputActions.Editor.Zoom.performed += OnZoomPerformed;
        inputActions.Editor.ResetView.performed += OnResetViewPerformed;
    }

    private void OnDisable()
    {
        inputActions.Editor.Look.performed -= OnLookPerformed;
        inputActions.Editor.Zoom.performed -= OnZoomPerformed;
        inputActions.Editor.ResetView.performed -= OnResetViewPerformed;
        inputActions.Editor.Disable();
    }

    private void Start()
    {
    }

    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        // if (isLookBlockedByUI) return;

        // Vector2 lookValue = context.ReadValue<Vector2>();
        // if (lookValue == Vector2.zero) return;

        // float mouseX = lookValue.x * lookSensitivity;
        // float mouseY = lookValue.y * lookSensitivity;

        // transform.Rotate(Vector3.up * mouseX, Space.World);

        // verticalRotation -= mouseY;
        // verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);

        // Vector3 currentEuler = transform.eulerAngles;
        // transform.rotation = Quaternion.Euler(verticalRotation, currentEuler.y, 0f);
    }    

    private void OnResetViewPerformed(InputAction.CallbackContext context)
    {
        ResetView();
    }    

    private void Update()
    {
        HandleContinuousZoom();
        HandleContinuousRotation();
    }

    private void HandleContinuousRotation()
    {
        if (IsPointerOverUI()) return;

        //Vector2 zoomValue = inputActions.Editor.Zoom.ReadValue<Vector2>();
        Vector2 lookValue = inputActions.Editor.Look.ReadValue<Vector2>();
        if (lookValue != Vector2.zero) {

            float mouseX = lookValue.x * lookSensitivity;
            float mouseY = lookValue.y * lookSensitivity;

            transform.Rotate(Vector3.up * mouseX, Space.World);

            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);

            Vector3 currentEuler = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(verticalRotation, currentEuler.y, 0f);        
        }
    }

    private void HandleContinuousZoom()
    {
        if (IsPointerOverUI()) return;

        Vector2 zoomValue = inputActions.Editor.Zoom.ReadValue<Vector2>();
        if (zoomValue != Vector2.zero)
        {
            Vector3 moveDirection = new Vector3(zoomValue.x, 0f, zoomValue.y);
            transform.Translate(moveDirection * (zoomSpeed * 0.1f * Time.deltaTime), Space.Self);
        }

        Vector2 liftValue = inputActions.Editor.Lift.ReadValue<Vector2>();
        if (liftValue != Vector2.zero)
        {
            Vector3 moveDirection = new Vector3(0f, liftValue.y, 0f);
            transform.Translate(moveDirection * (zoomSpeed * 0.1f * Time.deltaTime), Space.Self);
        }
    }    

    private void OnZoomPerformed(InputAction.CallbackContext context)
    {
        if (IsPointerOverUI()) return;

        Vector2 zoomValue = context.ReadValue<Vector2>();
        Vector3 moveDirection = new Vector3(zoomValue.x, 0f, zoomValue.y);

        transform.Translate(moveDirection * (zoomSpeed * Time.deltaTime), Space.Self);
    }    

    private bool IsPointerOverUI()
    {
        Vector2 mousePos = Input.mousePosition;
        Vector2 pointerPosition = new Vector2(mousePos.x, Screen.height - mousePos.y);

        UIDocument[] uiDocuments = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
        foreach (var uiDoc in uiDocuments)
        {
            if (uiDoc != null && uiDoc.rootVisualElement != null)
            {
                if (uiDoc.rootVisualElement.style.display != DisplayStyle.None)
                {
                    Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(uiDoc.rootVisualElement.panel, pointerPosition);
                    VisualElement picked = uiDoc.rootVisualElement.panel.Pick(panelPosition);
                    if (picked != null && picked != uiDoc.rootVisualElement)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

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
                
                transform.position = targetCenter - transform.forward * distance;
                return;
            }
        }
        
        transform.SetPositionAndRotation(originalPosition, originalRotation);

        verticalRotation = transform.rotation.eulerAngles.x;
    }    

}