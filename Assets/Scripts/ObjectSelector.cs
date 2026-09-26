using TransformHandles;
using UnityEngine;
using UnityEngine.EventSystems;

public class ObjectSelector : MonoBehaviour
{
    private TransformHandleManager _manager;
    public TransformHandleSettings _settings;

    [SerializeField] private LayerMask selectableObjectsLayer;
    [SerializeField] private ProjectSettings projectSettings;

    private Handle _globalHandle;
    private Transform _currentTarget;
    private bool _isDraggingHandle;
    private LayerMask _handleLayer;

    void Start()
    {
        _manager = TransformHandleManager.Instance;
        _manager.Settings = _settings;
        _manager.BlockWhenPointerOverUI = true;

        // Haal de layer-mask op die de TransformHandleManager gebruikt
        _handleLayer = LayerMask.GetMask("TransformHandle");
    }

    void Update() 
    {
        if (Input.GetMouseButtonDown(0)) 
        {
            TrySelectObject();
        }
    }

    private void TrySelectObject()
    {
        // 1. Als de gebruiker momenteel de handle sleept, doe niks
        if (_isDraggingHandle) return;

        // 2. Als er op UI wordt geklikt, blokkeer selectie
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // 3. Check of de klik op een onderdeel van de transform handle zelf is
        if (_handleLayer != 0 && Physics.Raycast(ray, Mathf.Infinity, _handleLayer))
        {
            // Klik is op de handle -> breek af zodat de handle de input kan verwerken
            return;
        }

        // 4. Raycast naar selecteerbare 3D objecten
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, selectableObjectsLayer))
        {
            SelectObject(hit.collider.gameObject);
        }
        else
        {
            // Klik op lege 3D ruimte -> Verwijder target
            ClearHandleTarget();
        }
    }

    public void SelectObject(GameObject targetObject) 
    {
        if (targetObject == null)
        {
            ClearHandleTarget();
            return;
        }

        Transform targetTransform = targetObject.transform;
        if (_currentTarget == targetTransform) return;
        SetHandleTarget(targetTransform);
    }

    private void SetHandleTarget(Transform newTarget)
    {
        if (_globalHandle == null)
        {
            _globalHandle = _manager.CreateHandle(newTarget);
            
            if (_globalHandle != null)
            {
                _globalHandle.OnInteractionStartEvent += OnHandleStartInteraction;
                _globalHandle.OnInteractionEndEvent   += OnHandleEndInteraction;
            }
        }
        else
        {
            if (_currentTarget != null)
            {
                // Eerst nieuwe toevoegen, daarna oude verwijderen om te voorkomen dat
                // RemoveTarget de handle vernietigt omdat de lijst even leeg is
                _manager.AddTarget(newTarget, _globalHandle);
                _manager.RemoveTarget(_currentTarget, _globalHandle);
            }
            else
            {
                _manager.AddTarget(newTarget, _globalHandle);
            }
        }

        _currentTarget = newTarget;
    }

    private void ClearHandleTarget()
    {
        if (_currentTarget != null && _globalHandle != null)
        {
            _globalHandle.OnInteractionStartEvent -= OnHandleStartInteraction;
            _globalHandle.OnInteractionEndEvent   -= OnHandleEndInteraction;

            _manager.RemoveTarget(_currentTarget, _globalHandle);
            
            _globalHandle = null;
            _currentTarget = null;
            _isDraggingHandle = false;
        }
    }

    private void OnHandleStartInteraction(Handle handle)
    {
        _isDraggingHandle = true;
        if (projectSettings != null) {
            _globalHandle.PositionSnap = projectSettings.PositionSnap;
            _globalHandle.RotationSnap = projectSettings.RotationSnap;
            _globalHandle.ScaleSnap = projectSettings.ScaleSnap;
        }
    }

    private void OnHandleEndInteraction(Handle handle)
    {
        _isDraggingHandle = false;
    }
}