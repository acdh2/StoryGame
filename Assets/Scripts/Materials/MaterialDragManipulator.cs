using UnityEngine;
using UnityEngine.UIElements;

public class MaterialDragManipulator : PointerManipulator
{
    private readonly Material _material;
    private readonly Texture2D _thumbnail;
    private readonly MaterialPalette _palette;
    private bool _isDragging;

    public MaterialDragManipulator(Material material, Texture2D thumbnail, MaterialPalette palette)
    {
        _material = material;
        _thumbnail = thumbnail;
        _palette = palette;
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
        _isDragging = true;
        target.CapturePointer(evt.pointerId);
        _palette.StartDragPreview(_thumbnail, evt.localPosition, target);
        evt.StopPropagation();
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!_isDragging || !target.HasPointerCapture(evt.pointerId)) return;
        _palette.UpdateDragPreview(evt.localPosition, target);
        evt.StopPropagation();
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (!_isDragging || !target.HasPointerCapture(evt.pointerId)) return;

        _isDragging = false;
        target.ReleasePointer(evt.pointerId);
        _palette.EndDragAndApply(_material);
        evt.StopPropagation();
    }

    private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
    {
        if (_isDragging)
        {
            _isDragging = false;
            _palette.CancelDragPreview();
        }
    }
}