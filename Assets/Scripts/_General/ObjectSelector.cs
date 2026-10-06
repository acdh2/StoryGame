using System;
using TransformHandles;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class ObjectSelector : MonoBehaviour
{
    private TransformHandleManager _manager;
    public TransformHandleSettings _settings;

    [SerializeField] private LayerMask selectableObjectsLayer;
    [SerializeField] private ProjectSettings projectSettings;
    [SerializeField] private EditorCameraController editorCameraController;

    public event Action OnTransformComplete;
    public event Action OnSelectObject;
    
    private Handle _globalHandle;
    private Transform _currentTarget;
    private bool _isDraggingHandle;
    private LayerMask _handleLayer;

    private int currentHandleType = 0;
    private bool isEnabled = true;

    public GameObject SelectedObject => _currentTarget != null ? _currentTarget.gameObject : null;

    private GameObject lastSelected = null;

    void Start()
    {
        _manager = TransformHandleManager.Instance;
        _manager.Settings = _settings;
        _manager.BlockWhenPointerOverUI = true;

        _handleLayer = LayerMask.GetMask("TransformHandle");
    }

    public void SetEnabled(bool enable)
    {
        if (enable)
        {
            SelectObject(lastSelected);
        } else
        {
            lastSelected = SelectedObject;
            ClearHandleTarget();
        }
        isEnabled = enable;
    }

    private float _lastClickTime;
    private const float DoubleClickThreshold = 0.3f; // Maximale tijd in seconden tussen de klikken

    void Update()
    {
        if (isEnabled) 
        {
            if (_currentTarget == null && _globalHandle != null)
            {
                ForceClearHandleTarget();
            }

            if (Input.GetMouseButtonDown(0))
            {
                TrySelectObject();
                _lastClickTime = Time.time;
            }
        }
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

    private void TrySelectObject()
    {
        if (_isDraggingHandle) return;
        if (IsPointerOverUI()) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (_handleLayer != 0 && Physics.Raycast(ray, Mathf.Infinity, _handleLayer))
        {
            return;
        }

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, selectableObjectsLayer))
        {
            SelectObject(hit.collider.gameObject);
        }
        else
        {
            ClearHandleTarget();
        }
    }

    public void SelectObject(GameObject targetObject) 
    {
        OnSelectObject?.Invoke();

        if (targetObject == null)
        {
            ClearHandleTarget();
            return;
        }

        Transform targetTransform = targetObject.transform;
        if (_currentTarget == targetTransform) return;
        SetHandleTarget(targetTransform);
    }

    public void SetHandleType(int handleType)
    {
        if (currentHandleType != handleType) {
            currentHandleType = handleType;
            ApplyCurrentType();
        }
    }

    private void ApplyCurrentType() {
        if (_globalHandle != null) {
            switch (currentHandleType) {
                case 1:
                    TransformHandleManager.ChangeHandleType(_globalHandle, HandleType.Rotation);
                    break;
                case 2: 
                    TransformHandleManager.ChangeHandleType(_globalHandle, HandleType.Scale);
                    break;
                default:
                    TransformHandleManager.ChangeHandleType(_globalHandle, HandleType.Position);
                    break;
            }
        }
    }

    private void SetHandleTarget(Transform newTarget)
    {
        if (_globalHandle == null)
        {
            _globalHandle = _manager.CreateHandle(newTarget);
            
            if (_globalHandle != null)
            {
                foreach (CapsuleCollider collider in _globalHandle.GetComponentsInChildren<CapsuleCollider>())
                {
                    collider.radius = 0.15f;
                }

                _globalHandle.OnInteractionStartEvent += OnHandleStartInteraction;
                _globalHandle.OnInteractionEndEvent   += OnHandleEndInteraction;
            }
        }
        else
        {
            if (_currentTarget != null)
            {
                _manager.AddTarget(newTarget, _globalHandle);
                _manager.RemoveTarget(_currentTarget, _globalHandle);
            }
            else
            {
                _manager.AddTarget(newTarget, _globalHandle);
            }
        }

        _currentTarget = newTarget;
        ApplyCurrentType();
    }

    private void ClearHandleTarget()
    {
        if (_currentTarget != null && _globalHandle != null)
        {
            ForceClearHandleTarget();
        }
    }

    private void ForceClearHandleTarget() 
    {
        _globalHandle.OnInteractionStartEvent -= OnHandleStartInteraction;
        _globalHandle.OnInteractionEndEvent   -= OnHandleEndInteraction;

        _manager.RemoveTarget(_currentTarget, _globalHandle);
        
        _globalHandle = null;
        _currentTarget = null;
        _isDraggingHandle = false;
    }

    private void OnHandleStartInteraction(Handle handle)
    {
        _isDraggingHandle = true;
        if (editorCameraController != null)
        {
            editorCameraController.NotifyDragStart();
        }
        if (projectSettings != null) {
            _globalHandle.PositionSnap = projectSettings.PositionSnap;
            _globalHandle.RotationSnap = projectSettings.RotationSnap;
            _globalHandle.ScaleSnap = projectSettings.ScaleSnap;
        }
    }

    private void OnHandleEndInteraction(Handle handle)
    {
        _isDraggingHandle = false;
        if (editorCameraController != null)
        {
            editorCameraController.NotifyDragEnd();
        }
        if (_currentTarget != null)
        {
            OnTransformComplete?.Invoke();
        }
    }
}