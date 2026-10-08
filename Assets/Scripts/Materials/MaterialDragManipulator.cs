using UnityEngine;
using UnityEngine.UIElements;

public class MaterialDragManipulator : PointerManipulator
{
    private readonly Material _material;
    private readonly Texture2D _thumbnail;
    private readonly MaterialPalette _palette;
    private bool _isDragging;
    private Vector2 _startPointerPosition;
    private readonly EditorCameraController cameraController;


    public MaterialDragManipulator(Material material, Texture2D thumbnail, MaterialPalette palette, EditorCameraController cameraController)
    {
        _material = material;
        _thumbnail = thumbnail;
        _palette = palette;
        this.cameraController = cameraController;
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<PointerDownEvent>(OnPointerDown);
        target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        target.RegisterCallback<PointerUpEvent>(OnPointerUp);
        target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (evt.button != 0) return;
        cameraController?.NotifyDragStart();

        _isDragging = false;
        _startPointerPosition = evt.localPosition;
        target.CapturePointer(evt.pointerId);
        evt.StopPropagation();
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!target.HasPointerCapture(evt.pointerId)) return;

        if (!_isDragging && (evt.localPosition.y < 0f))
        {
            if (Vector2.Distance(_startPointerPosition, evt.localPosition) > 5f)
            {
                _isDragging = true;
                _palette.StartDragPreview(_thumbnail, evt.localPosition, target);
            }
        }
        else
        {
            _palette.UpdateDragPreview(evt.localPosition, target);
        }

        evt.StopPropagation();
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (!target.HasPointerCapture(evt.pointerId)) return;
        cameraController?.NotifyDragEnd();

        target.ReleasePointer(evt.pointerId);

        if (_isDragging && (evt.localPosition.y < 0f))
        {
            _isDragging = false;
            _palette.EndDragAndApply(_material);
        }
        else
        {
            _palette.CancelDragPreview();
        }

        evt.StopPropagation();
    }

    private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
    {
        cameraController?.NotifyDragEnd();
        if (_isDragging)
        {
            _isDragging = false;
            _palette.CancelDragPreview();
        }
    }
}