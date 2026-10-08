using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class EditorCameraController : MonoBehaviour, IKeyEventReceiver
{
    private bool isDragging = false;
    public void NotifyDragStart() { isDragging = true; }
    public void NotifyDragEnd() { isDragging = false; }

    [SerializeField] private ObjectSelector objectSelector = null;
    [SerializeField] private InputSystem_Actions inputActions;

    [Header("Settings")]
    public float lookSensitivity = 0.5f;
    public float zoomSpeed = 5f;

    private float verticalRotation = 0f;
    private Vector3 originalPosition = Vector3.zero;
    private Quaternion originalRotation = Quaternion.identity;

    private Vector3 velocity = Vector3.zero;
    private float cooldown = 0f;
    private bool isRotatingAllowed = false;

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
        inputActions.Editor.MousePress.performed += OnMousePress;
        inputActions.Editor.Look.performed += OnLookPerformed;
        inputActions.Editor.Zoom.performed += OnZoomPerformed;
        inputActions.Editor.Move.performed += OnMovePerformed;
        inputActions.Editor.Move.canceled += OnMoveCanceled;
        inputActions.Editor.Lift.performed += OnLiftPerformed;
        inputActions.Editor.Lift.canceled += OnLiftCanceled;
        if (objectSelector != null) objectSelector.OnSelectObject += OnSelectObject;
    }

    private void OnDisable()
    {
        inputActions.Editor.MousePress.performed -= OnMousePress;
        inputActions.Editor.Look.performed -= OnLookPerformed;
        inputActions.Editor.Zoom.performed -= OnZoomPerformed;
        inputActions.Editor.Move.performed -= OnMovePerformed;
        inputActions.Editor.Move.canceled -= OnMoveCanceled;
        inputActions.Editor.Lift.performed -= OnLiftPerformed;
        inputActions.Editor.Lift.canceled -= OnLiftCanceled;
        if (objectSelector != null) objectSelector.OnSelectObject -= OnSelectObject;
        inputActions.Editor.Disable();
    }

    private void Start()
    {
    }

    private void OnLiftPerformed(InputAction.CallbackContext context)
    {
        Vector2 moveInput = context.ReadValue<Vector2>();
        velocity = new Vector3(velocity.x, moveInput.y, velocity.z);
    }

    private void OnLiftCanceled(InputAction.CallbackContext context)
    {
        Vector2 moveInput = context.ReadValue<Vector2>();
        velocity = new Vector3(velocity.x, moveInput.y, velocity.z);
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        Vector2 moveInput = context.ReadValue<Vector2>();
        velocity = new Vector3(moveInput.x, velocity.y, moveInput.y);
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        Vector2 moveInput = context.ReadValue<Vector2>();
        velocity = new Vector3(moveInput.x, velocity.y, moveInput.y);
    }    

    public void OnKeyEvent(string identifier)
    {
        if (!gameObject.activeSelf) return;
        
        if (identifier == "ResetView")
        {
            ResetView();
        }
    }


    void Update()
    {
        if (cooldown > 0f)
        {
            cooldown -= Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        if (velocity.magnitude > 0.01f) {
            transform.Translate(velocity, Space.Self);
            //velocity *= 0.9f;
        }
    }

    private void OnSelectObject()
    {
        isRotatingAllowed = false;
    }

    private void OnMousePress(InputAction.CallbackContext context)
    {
        isRotatingAllowed = !IsPointerOverUI();
    }

    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        // Als de actie boven UI begon, negeer alle vervolgbewegingen
        if (!isRotatingAllowed) return;
        if (IsPointerOverUI()) return;

        Vector2 lookValue = context.ReadValue<Vector2>();
        if (lookValue == Vector2.zero) return;

        float mouseX = lookValue.x * lookSensitivity;
        float mouseY = lookValue.y * lookSensitivity;

        transform.Rotate(Vector3.up * mouseX, Space.World);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);

        Vector3 currentEuler = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(verticalRotation, currentEuler.y, 0f);
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
        if (isDragging) return true;

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
        if (objectSelector != null && cooldown < 0.1f)
        {
            GameObject selectedObject = objectSelector.SelectedObject;
            if (selectedObject != null)
            {
                cooldown = 1f;
                Renderer renderer = selectedObject.GetComponentInChildren<Renderer>();
                Vector3 targetCenter = renderer != null ? renderer.bounds.center : selectedObject.transform.position;
                float objectSize = renderer != null ? renderer.bounds.size.z : 2f;
                float distance = Mathf.Max(objectSize * 2f, 3f);
                
                Vector3 newPosition = targetCenter - transform.forward * distance;
                if (newPosition.y < 0.5f) newPosition.y = 0.5f;
                transform.position = newPosition;
                transform.LookAt(selectedObject.transform);
                return;
            }
        }
        
        transform.SetPositionAndRotation(originalPosition, originalRotation);
        verticalRotation = transform.rotation.eulerAngles.x;
        cooldown = 0f;
    }    

}