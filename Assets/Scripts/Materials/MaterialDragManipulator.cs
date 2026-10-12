using UnityEngine;
using UnityEngine.UIElements;

public class MaterialDragManipulator : PointerManipulator
{
    private readonly Material _material;
    private readonly Texture2D _thumbnail;
    private readonly MaterialPalette _palette;
    private readonly EditorCameraController cameraController;

    private bool _isDragging;
    private Vector3 _startPointerPosition;
    private Vector3 _lastPointerPosition;
    private ScrollView _scrollView;
    private VisualElement _panelRoot;
    private int _pointerId = -1;

    public MaterialDragManipulator(Material material, Texture2D thumbnail, MaterialPalette palette, EditorCameraController cameraController)
    {
        _material = material;
        _thumbnail = thumbnail;
        _palette = palette;
        this.cameraController = cameraController;
        activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<PointerDownEvent>(OnPointerDown);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        UnregisterRootCallbacks();
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (_pointerId != -1 || !CanStartManipulation(evt)) return;
        cameraController?.NotifyDragStart();

        _pointerId = evt.pointerId;
        _isDragging = false;
        _startPointerPosition = evt.position;
        _lastPointerPosition = evt.position;

        _panelRoot = target.panel?.visualTree;
        if (_panelRoot == null) return;

        _scrollView = target.GetFirstAncestorOfType<ScrollView>();

        _panelRoot.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        _panelRoot.RegisterCallback<PointerUpEvent>(OnPointerUp);
        _panelRoot.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (evt.pointerId != _pointerId) return;

        VisualElement scrollContainer = _scrollView != null ? (VisualElement)_scrollView : target.parent;
        Rect bounds = scrollContainer.worldBound;
        bool isOutsideContainer = evt.position.y < bounds.y;

        if (!_isDragging)
        {
            if (isOutsideContainer)
            {
                _isDragging = true;
                evt.StopPropagation();

                _panelRoot.CapturePointer(_pointerId);
                _palette.StartDragPreview(_thumbnail, evt.position);
            }
            else if (_scrollView != null)
            {
                Vector3 delta = evt.position - _lastPointerPosition;
                _scrollView.scrollOffset -= new Vector2(delta.x, delta.y);
                evt.StopPropagation();
            }

            _lastPointerPosition = evt.position;
            return;
        }

        evt.StopPropagation();
        _palette.UpdateDragPreview(evt.position);
        _lastPointerPosition = evt.position;
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (evt.pointerId != _pointerId) return;
        cameraController?.NotifyDragEnd();

        VisualElement scrollContainer = _scrollView != null ? (VisualElement)_scrollView : target.parent;
        Rect bounds = scrollContainer.worldBound;
        bool isOutsideContainer = evt.position.y < bounds.y;

        if (_isDragging)
        {
            evt.StopPropagation();

            if (_panelRoot != null && _panelRoot.HasPointerCapture(_pointerId))
            {
                _panelRoot.ReleasePointer(_pointerId);
            }

            if (isOutsideContainer)
            {
                _palette.EndDragAndApply(_material);
            }
            else
            {
                _palette.CancelDragPreview();
            }
        }

        CleanupAndStop();
    }

    private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
    {
        if (evt.pointerId != _pointerId) return;
        cameraController?.NotifyDragEnd();

        if (_isDragging)
        {
            _palette.CancelDragPreview();
        }

        CleanupAndStop();
    }

    private void CleanupAndStop()
    {
        _isDragging = false;
        UnregisterRootCallbacks();
        _pointerId = -1;
        _scrollView = null;
    }

    private void UnregisterRootCallbacks()
    {
        if (_panelRoot == null) return;

        _panelRoot.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        _panelRoot.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        _panelRoot.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        _panelRoot = null;
    }
}