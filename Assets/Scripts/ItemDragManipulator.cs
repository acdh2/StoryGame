using UnityEngine;
using UnityEngine.UIElements;

public class ItemDragManipulator : PointerManipulator
{
    private readonly LevelObjectPalette.LevelItemData itemData;
    private readonly LevelObjectPalette manager;
    
    private bool isTracking;
    private bool isDragging;
    private int pointerId = -1;
    
    private Vector3 startPosition;
    private VisualElement panelRoot;

    // Drempel in pixels voordat we een beslissing nemen
    private const float DirectionThreshold = 10f; 

    public ItemDragManipulator(LevelObjectPalette.LevelItemData data, LevelObjectPalette manager)
    {
        this.itemData = data;
        this.manager = manager;
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
        if (isTracking || !CanStartManipulation(evt)) return;

        pointerId = evt.pointerId;
        isTracking = true;
        isDragging = false;
        startPosition = evt.position;

        panelRoot = target.panel?.visualTree;
        if (panelRoot == null) return;

        // Luister op de root om de beweging te volgen
        panelRoot.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        panelRoot.RegisterCallback<PointerUpEvent>(OnPointerUp);
        panelRoot.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!isTracking || evt.pointerId != pointerId) return;

        // Als de drag-and-drop nog niet gestart is, bepalen we de richting
        if (!isDragging)
        {
            Vector2 delta = evt.position - startPosition;

            if (delta.magnitude >= DirectionThreshold)
            {
                // Is de beweging voornamelijk horizontaal (opzij uit de toolbar)?
                if (Mathf.Abs(delta.y) > DirectionThreshold) //Mathf.Abs(delta.x))
                {
                    // Start Drag and Drop
                    isDragging = true;
                    evt.StopPropagation();

                    // Neem de touch over van de ScrollView
                    panelRoot.CapturePointer(pointerId);
                    manager.StartDragPreview(itemData, evt.position, evt.currentTarget as VisualElement);
                }
                else
                {
                    // Beweging is voornamelijk verticaal -> Gebruiker wil scrollen.
                    // Laat de ScrollView het overnemen en stop met tracken.
                    CleanupAndStop();
                }
            }
            return;
        }

        // Indien Drag and Drop actief is: blokkeer ScrollView en update preview
        evt.StopPropagation();
        manager.UpdateDragPreview(evt.position, evt.currentTarget as VisualElement);
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (!isTracking || evt.pointerId != pointerId) return;

        if (isDragging)
        {
            evt.StopPropagation();

            if (panelRoot != null && panelRoot.HasPointerCapture(pointerId))
            {
                panelRoot.ReleasePointer(pointerId);
            }

            manager.EndDragAndSpawn(itemData);
        }

        CleanupAndStop();
    }

    private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
    {
        if (!isTracking || evt.pointerId != pointerId) return;

        if (isDragging)
        {
            manager.CancelDragPreview();
        }

        CleanupAndStop();
    }

    private void CleanupAndStop()
    {
        isTracking = false;
        isDragging = false;
        UnregisterRootCallbacks();
        pointerId = -1;
    }

    private void UnregisterRootCallbacks()
    {
        if (panelRoot == null) return;

        panelRoot.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        panelRoot.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        panelRoot.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        panelRoot = null;
    }
}
