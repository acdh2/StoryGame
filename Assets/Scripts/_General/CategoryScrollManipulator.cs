using UnityEngine;
using UnityEngine.UIElements;

public class CategoryButtonScrollManipulator : PointerManipulator
{
    private readonly ScrollView _scrollView;
    private bool _isDragging;
    private Vector3 _startPosition;
    private int _pointerId = -1;
    private float _dragThreshold = 8f;

    public CategoryButtonScrollManipulator(ScrollView scrollView)
    {
        _scrollView = scrollView;
        activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
        target.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
        target.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
        target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut, TrickleDown.TrickleDown);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
        target.UnregisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
        target.UnregisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
        target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut, TrickleDown.TrickleDown);
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (_pointerId != -1 || !CanStartManipulation(evt)) return;

        _pointerId = evt.pointerId;
        _isDragging = false;
        _startPosition = evt.position;
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (evt.pointerId != _pointerId) return;

        Vector3 totalDelta = evt.position - _startPosition;

        if (!_isDragging)
        {
            if (totalDelta.magnitude > _dragThreshold)
            {
                _isDragging = true;
                _startPosition = evt.position;
                target.CapturePointer(_pointerId);
            }
            else
            {
                return;
            }
        }

        Vector3 delta = evt.position - _startPosition;
        _scrollView.scrollOffset -= new Vector2(delta.x, delta.y);
        _startPosition = evt.position;
        evt.StopPropagation();
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
        _pointerId = -1;
    }
}