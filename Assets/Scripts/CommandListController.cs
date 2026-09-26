using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class CommandListController : MonoBehaviour
{
    [Header("UI Templates & Setup")]
    public VisualTreeAsset itemTemplate; // Sleep hier CommandItem.uxml in
    public int initialItemCount = 24;

    private UIDocument uiDocument;
    private ScrollView scrollView;
    private VisualElement container;

    // Drag and Drop state
    private VisualElement draggedElement = null;
    private VisualElement placeholder = null;
    private Vector2 dragStartPosition;
    private bool isDragging = false;

    // Double click detection
    private float lastClickTime = 0f;
    private const float DOUBLE_CLICK_THRESHOLD = 0.3f;

    private readonly List<string> defaultOptions = new List<string> { "say", "option", "label", "jump", "jumpif", "set", "print" };

    private void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        VisualElement root = uiDocument.rootVisualElement;

        scrollView = root.Q<ScrollView>("list-scroll-view");
        if (scrollView == null) return;

        container = scrollView.contentContainer;

        // Maak ruimte vrij onderaan door een lege onder-padding toe te voegen aan het scrollvenster
        container.style.paddingBottom = 300; // Maakt 'over-scroll' onderaan mogelijk

        // Luister naar dubbelklik op het lege deel van het scroll-gebied
        scrollView.RegisterCallback<MouseDownEvent>(OnScrollViewMouseDown);

        PopulateInitialList();
    }

    private void PopulateInitialList()
    {
        container.Clear();
        for (int i = 0; i < initialItemCount; i++)
        {
            AddItemToList();
        }
    }

    private void AddItemToList()
    {
        if (itemTemplate == null)
        {
            Debug.LogError("[CommandListController] Geen itemTemplate toegewezen!");
            return;
        }

        TemplateContainer itemInstance = itemTemplate.Instantiate();
        VisualElement itemRoot = itemInstance.Q<VisualElement>("command-item-root") ?? itemInstance;

        // Vul de opties in de dropdown
        DropdownField dropdown = itemRoot.Q<DropdownField>("command-dropdown");
        if (dropdown != null)
        {
            dropdown.choices = defaultOptions;
            dropdown.value = defaultOptions[0];
        }

        // Voeg Drag & Drop logica toe aan het item
        RegisterDragEvents(itemRoot);

        container.Add(itemRoot);
    }

    // ==========================================
    // DOUBLE CLICK LOGIC
    // ==========================================
    private void OnScrollViewMouseDown(MouseDownEvent evt)
    {
        // Alleen linker muisknop
        if (evt.button != 0) return;

        // Controleer of de gebruiker op de lege achtergrond klikt en niet op een item
        if (evt.target == scrollView || evt.target == container)
        {
            if (Time.time - lastClickTime < DOUBLE_CLICK_THRESHOLD)
            {
                AddItemToList();
                evt.StopPropagation();
            }
            lastClickTime = Time.time;
        }
    }

    private void RegisterDragEvents(VisualElement element)
    {
        element.RegisterCallback<MouseDownEvent>(evt =>
        {
            if (evt.button != 0) return;

            // Controleer of de gebruiker specifiek op de drag-handle heeft geklikt
            if (evt.target is VisualElement targetVE)
            {
                VisualElement dragHandle = element.Q<VisualElement>("drag-handle");
                
                // Als de klik NIET op de drag-handle (of een child daarvan) was, negeer de drag
                if (dragHandle == null || (targetVE != dragHandle && !dragHandle.Contains(targetVE)))
                {
                    return;
                }
            }

            isDragging = true;
            draggedElement = element;
            dragStartPosition = evt.mousePosition;

            placeholder = new VisualElement();
            placeholder.style.height = element.resolvedStyle.height;
            placeholder.style.width = element.resolvedStyle.width;
            placeholder.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.4f);

            int index = container.IndexOf(element);
            container.Insert(index, placeholder);

            draggedElement.style.position = Position.Absolute;
            draggedElement.style.width = placeholder.resolvedStyle.width;
            draggedElement.CaptureMouse();

            UpdateDraggedElementPosition(evt.mousePosition);
            evt.StopPropagation();
        });

        element.RegisterCallback<MouseMoveEvent>(evt =>
        {
            if (!isDragging || draggedElement != element) return;

            UpdateDraggedElementPosition(evt.mousePosition);

            float draggedY = draggedElement.worldBound.center.y;
            int placeholderIndex = container.IndexOf(placeholder);

            foreach (var child in container.Children())
            {
                // Negeer de placeholder en het gesleepte element zelf bij het bepalen van de nieuwe plek
                if (child == placeholder || child == draggedElement) continue;

                int childIndex = container.IndexOf(child);

                // Als we omhoog slepen en boven het midden van een element komen
                if (draggedY < child.worldBound.center.y && placeholderIndex > childIndex)
                {
                    container.Insert(childIndex, placeholder);
                    break;
                }
                // Als we omlaag slepen en onder het midden van een element komen
                else if (draggedY > child.worldBound.center.y && placeholderIndex < childIndex)
                {
                    container.Insert(childIndex, placeholder);
                    break;
                }
            }

            evt.StopPropagation();
        });

element.RegisterCallback<MouseUpEvent>(evt =>
{
    if (!isDragging || draggedElement != element) return;

    draggedElement.ReleaseMouse();
    isDragging = false;

    Rect containerBounds = scrollView.worldBound;
    if (!containerBounds.Contains(evt.mousePosition))
    {
        // Buiten het scherm gesleept -> Verwijderen
        container.Remove(draggedElement);
        container.Remove(placeholder);
    }
    else
    {
        int oldIndex = container.IndexOf(draggedElement);
        int targetIndex = container.IndexOf(placeholder);

        // Als het element van een lagere naar een hogere index schuift,
        // compenseer voor de index-verschuiving na het verwijderen
        if (oldIndex < targetIndex)
        {
            targetIndex--;
        }

        container.Remove(placeholder);
        container.Remove(draggedElement);

        // Reset styling naar normale layout
        draggedElement.style.position = Position.Relative;
        draggedElement.style.top = StyleKeyword.Null;
        draggedElement.style.left = StyleKeyword.Null;
        draggedElement.style.width = StyleKeyword.Null;

        container.Insert(targetIndex, draggedElement);
    }

    draggedElement = null;
    placeholder = null;
    evt.StopPropagation();
});
    }

    private void UpdateDraggedElementPosition(Vector2 mousePosition)
    {
        if (draggedElement == null) return;
        Vector2 localPos = container.WorldToLocal(mousePosition);
        draggedElement.style.left = localPos.x - (draggedElement.resolvedStyle.width / 2);
        draggedElement.style.top = localPos.y - (draggedElement.resolvedStyle.height / 2);
    }
}