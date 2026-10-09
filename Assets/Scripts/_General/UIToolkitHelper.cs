using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class UIToolkitHelper : MonoBehaviour
{
    public static bool IsPointerOverUI()
    {
        Vector2 mousePos = GetMousePosition();
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

    public static Vector2 GetMousePosition()
    {
        return Mouse.current.position.ReadValue();//Input.mousePosition;
    }

    public static bool GetMouseButtonDown()
    {
        return Mouse.current.leftButton.wasPressedThisFrame;//Input.GetMouseButtonDown(0);
    }
}
