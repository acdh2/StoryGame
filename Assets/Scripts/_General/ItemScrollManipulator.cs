using UnityEngine;
using UnityEngine.UIElements;

public class ItemScrollManipulator : PointerManipulator
{
    private readonly ScrollView _scrollView;
    private bool _isDragging;
    private Vector3 _startPosition;
    private int _pointerId = -1;
    private float _dragThreshold = 64f;

    public ItemScrollManipulator(ScrollView scrollView)
    {
        _scrollView = scrollView;
        activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<PointerDownEvent>(OnPointerDown);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        UnregisterCallbacks();
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (_pointerId != -1 || !CanStartManipulation(evt)) return;

        VisualElement current = evt.target as VisualElement;
        while (current != null && current != target)
        {
            if (current.ClassListContains("unity-scroller") || current == _scrollView.contentContainer)
            {
                break;
            }
            if (current != _scrollView && current.childCount > 0 && current != evt.target)
            {
                return; 
            }
            current = current.parent;
        }

        _pointerId = evt.pointerId;
        _isDragging = false;
        _startPosition = evt.position;

        target.CapturePointer(_pointerId);

        target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        target.RegisterCallback<PointerUpEvent>(OnPointerUp);
        target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (evt.pointerId != _pointerId) return;

        Vector3 delta = evt.position - _startPosition;

        if (!_isDragging && delta.magnitude > _dragThreshold)
        {
            _isDragging = true;
            _startPosition = evt.position;
        }

        if (_isDragging)
        {
            _scrollView.scrollOffset -= new Vector2(delta.x, delta.y);
            _startPosition = evt.position;
            evt.StopPropagation();
        }
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (evt.pointerId != _pointerId) return;

        bool wasDragging = _isDragging;
        StopDragging();

        if (wasDragging)
        {
            evt.StopPropagation();
        }
    }

    private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
    {
        if (evt.pointerId != _pointerId) return;
        StopDragging();
    }

    private void StopDragging()
    {
        _isDragging = false;
        if (target.HasPointerCapture(_pointerId))
        {
            target.ReleasePointer(_pointerId);
        }
        UnregisterCallbacks();
        _pointerId = -1;
    }

    private void UnregisterCallbacks()
    {
        target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
    }
}