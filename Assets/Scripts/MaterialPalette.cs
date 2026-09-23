using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MaterialPalette : MonoBehaviour
{
    [System.Serializable]
    public struct MaterialCategory
    {
        public string categoryName;
        public Texture2D categoryIcon;
        public List<Material> materials;
    }

    [Header("UI Settings")]
    [SerializeField] private LayerMask targetLayerMask;

    private Vector2 itemSize = new Vector2(48, 48);

    [Header("Data")]
    [SerializeField] private List<MaterialCategory> categories = new List<MaterialCategory>();

    private VisualElement root;
    private ScrollView categoryBar;
    private ScrollView scrollView;
    private VisualElement dragPreview;

    private int currentCategoryIndex = 0;
    private List<Button> categoryButtons = new List<Button>();

    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        categoryBar = root.Q<ScrollView>("CategoryBar");
        if (categoryBar != null)
        {
            categoryBar.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
        }

        scrollView = root.Q<ScrollView>("ItemBar");
        if (scrollView != null)
        {
            scrollView.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
        }

        SetupDragPreview();
        BuildCategoryTabs();

        if (categories.Count > 0)
        {
            SelectCategory(0);
        }
    }

    private void SetupDragPreview()
    {
        dragPreview = new VisualElement();
        dragPreview.style.position = Position.Absolute;
        dragPreview.style.width = itemSize.x;
        dragPreview.style.height = itemSize.y;
        dragPreview.style.display = DisplayStyle.None;
        dragPreview.pickingMode = PickingMode.Ignore;
        dragPreview.style.borderTopLeftRadius = 12;
        dragPreview.style.borderTopRightRadius = 12;
        dragPreview.style.borderBottomLeftRadius = 12;
        dragPreview.style.borderBottomRightRadius = 12;
        root.Add(dragPreview);
    }

    private void BuildCategoryTabs()
    {
        if (categoryBar == null) return;

        categoryBar.Clear();
        categoryButtons.Clear();

        for (int i = 0; i < categories.Count; i++)
        {
            int categoryIndex = i;
            var category = categories[i];

            Button categoryBtn = new Button(() => SelectCategory(categoryIndex));
            categoryBtn.style.flexDirection = FlexDirection.Row;
            categoryBtn.style.alignItems = Align.Center;
            categoryBtn.style.justifyContent = Justify.Center;
            categoryBtn.style.height = 32;
            categoryBtn.style.paddingLeft = 16;
            categoryBtn.style.paddingRight = 16;
            categoryBtn.style.marginLeft = 4;
            categoryBtn.style.marginRight = 4;
            categoryBtn.style.borderTopWidth = 0;
            categoryBtn.style.borderBottomWidth = 0;
            categoryBtn.style.borderLeftWidth = 0;
            categoryBtn.style.borderRightWidth = 0;
            categoryBtn.style.borderTopLeftRadius = 6;
            categoryBtn.style.borderTopRightRadius = 6;
            categoryBtn.style.borderBottomLeftRadius = 6;
            categoryBtn.style.borderBottomRightRadius = 6;

            if (category.categoryIcon != null)
            {
                VisualElement icon = new VisualElement();
                icon.style.backgroundImage = new StyleBackground(category.categoryIcon);
                icon.style.width = 20;
                icon.style.height = 20;
                icon.style.marginRight = 8;
                categoryBtn.Add(icon);
            }

            Label label = new Label(string.IsNullOrEmpty(category.categoryName) ? $"Cat {i + 1}" : category.categoryName.ToUpper());
            label.style.fontSize = 12;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = Color.black;
            categoryBtn.Add(label);

            categoryBar.Add(categoryBtn);
            categoryButtons.Add(categoryBtn);
        }
    }

    public void SelectCategory(int index)
    {
        if (index < 0 || index >= categories.Count) return;

        currentCategoryIndex = index;

        for (int i = 0; i < categoryButtons.Count; i++)
        {
            categoryButtons[i].style.backgroundColor = (i == index) 
                ? new StyleColor(new Color(0.8f, 0.8f, 0.8f)) 
                : new StyleColor(Color.clear);
        }

        PopulateItems(categories[currentCategoryIndex].materials);
    }

    private void PopulateItems(List<Material> materials)
    {
        if (scrollView == null) return;

        scrollView.Clear();
        if (materials == null) return;

        foreach (var mat in materials)
        {
            if (mat == null) continue;

            VisualElement itemCard = new VisualElement();

            itemCard.style.width = itemSize.x;
            itemCard.style.height = itemSize.y;
            itemCard.style.marginLeft = 6;
            itemCard.style.marginRight = 6;
            itemCard.style.backgroundColor = new StyleColor(new Color(0.95f, 0.95f, 0.95f));
            
            itemCard.style.borderTopLeftRadius = 12;
            itemCard.style.borderTopRightRadius = 12;
            itemCard.style.borderBottomLeftRadius = 12;
            itemCard.style.borderBottomRightRadius = 12;

            itemCard.style.borderTopWidth = 1;
            itemCard.style.borderBottomWidth = 1;
            itemCard.style.borderLeftWidth = 1;
            itemCard.style.borderRightWidth = 1;
            itemCard.style.borderTopColor = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
            itemCard.style.borderBottomColor = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
            itemCard.style.borderLeftColor = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
            itemCard.style.borderRightColor = new StyleColor(new Color(0.7f, 0.7f, 0.7f));

            // Genereer de live preview thumbnail van het materiaal
            Texture2D thumbnail = MaterialPreviewGenerator.CreatePreview(mat);

            if (thumbnail != null)
            {
                VisualElement icon = new VisualElement();
                icon.style.backgroundImage = new StyleBackground(thumbnail);
                icon.style.width = Length.Percent(80);
                icon.style.height = Length.Percent(80);
                icon.style.alignSelf = Align.Center;
                icon.style.marginTop = Length.Percent(10);
                itemCard.Add(icon);
            }

            itemCard.tooltip = mat.name;
            itemCard.AddManipulator(new MaterialDragManipulator(mat, thumbnail, this));

            scrollView.Add(itemCard);
        }
    }

    public void StartDragPreview(Texture2D thumbnail, Vector2 localPos, VisualElement target)
    {
        dragPreview.style.backgroundImage = new StyleBackground(thumbnail);
        dragPreview.style.display = DisplayStyle.Flex;
        UpdateDragPreview(localPos, target);
    }

    public void UpdateDragPreview(Vector2 localPos, VisualElement target)
    {
        if (target == null) return;

        Vector2 panelPos = target.ChangeCoordinatesTo(root, localPos);

        float previewWidth = dragPreview.resolvedStyle.width > 0 ? dragPreview.resolvedStyle.width : itemSize.x;
        float previewHeight = dragPreview.resolvedStyle.height > 0 ? dragPreview.resolvedStyle.height : itemSize.y;

        dragPreview.style.left = panelPos.x - (previewWidth / 2f);
        dragPreview.style.top = panelPos.y - (previewHeight / 2f);
    }

    public void EndDragAndApply(Material material)
    {
        dragPreview.style.display = DisplayStyle.None;
        TryApplyMaterial(material, Input.mousePosition);
    }

    public void CancelDragPreview()
    {
        dragPreview.style.display = DisplayStyle.None;
    }

    private void TryApplyMaterial(Material material, Vector2 mouseScreenPos)
    {
        if (Camera.main == null || material == null) return;

        Ray ray = Camera.main.ScreenPointToRay(mouseScreenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, targetLayerMask))
        {
            Renderer targetRenderer = hit.collider.GetComponent<Renderer>();
            if (targetRenderer != null)
            {
                targetRenderer.sharedMaterial = material;
            }
        }
    }
}