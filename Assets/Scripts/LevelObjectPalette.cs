using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class LevelObjectPalette : UIControllerBase
{
    [System.Serializable]
    public struct LevelItemData
    {
        public string itemName;
        public Texture2D thumbnail;
        public GameObject prefab;
    }

    [System.Serializable]
    public struct LevelItemCategory
    {
        public string categoryName;
        public Texture2D categoryIcon;
        public List<LevelItemData> items;
    }

    [SerializeField] private ObjectSelector objectSelector;

    [Header("UI Settings")]
    [SerializeField] private LayerMask baseplateLayer;
    [SerializeField] private ProjectSettings projectSettings;

    private Vector2 itemSize = new Vector2(48, 48);

    [Header("Data")]
    [SerializeField] private List<LevelItemCategory> categories = new List<LevelItemCategory>();

    private VisualElement root;
    private ScrollView categoryBar;
    private ScrollView scrollView;
    private VisualElement dragPreview;

    private int currentCategoryIndex = 0;
    private readonly List<Button> categoryButtons = new List<Button>();

    protected override void OnUIEnabled(VisualElement root)
    {
        this.root = root;

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

    protected override void OnUIDisabled()
    {
        if (scrollView != null)
        {
            scrollView.Clear();
            scrollView = null;
        }

        if (categoryBar != null)
        {
            categoryBar.Clear();
            categoryBar = null;
        }

        categoryButtons.Clear();
        dragPreview = null;
        root = null;
    }

    private void SetupDragPreview()
    {
        if (root == null) return;
        
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

        PopulateItems(categories[currentCategoryIndex].items);
    }

    private void PopulateItems(List<LevelItemData> items)
    {
        if (scrollView == null) return;

        scrollView.Clear();
        if (items == null) return;

        foreach (var item in items)
        {
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

            if (item.thumbnail != null)
            {
                VisualElement icon = new VisualElement();
                icon.style.backgroundImage = new StyleBackground(item.thumbnail);
                icon.style.width = Length.Percent(80);
                icon.style.height = Length.Percent(80);
                icon.style.alignSelf = Align.Center;
                icon.style.marginTop = Length.Percent(10);
                itemCard.Add(icon);
            }

            itemCard.tooltip = item.itemName;
            itemCard.AddManipulator(new ItemDragManipulator(item, this));

            scrollView.Add(itemCard);
        }
    }

    public void StartDragPreview(LevelItemData item, Vector2 localPos, VisualElement target)
    {
        if (dragPreview == null) return;
        dragPreview.style.backgroundImage = new StyleBackground(item.thumbnail);
        dragPreview.style.display = DisplayStyle.Flex;
        UpdateDragPreview(localPos, target);
    }

    public void UpdateDragPreview(Vector2 localPos, VisualElement target)
    {
        if (target == null || dragPreview == null || root == null) return;

        Vector2 panelPos = target.ChangeCoordinatesTo(root, localPos);

        float previewWidth = dragPreview.resolvedStyle.width > 0 ? dragPreview.resolvedStyle.width : itemSize.x;
        float previewHeight = dragPreview.resolvedStyle.height > 0 ? dragPreview.resolvedStyle.height : itemSize.y;

        dragPreview.style.left = panelPos.x - (previewWidth / 2f);
        dragPreview.style.top = panelPos.y - (previewHeight / 2f);
    }

    public void EndDragAndSpawn(LevelItemData item)
    {
        if (dragPreview != null) dragPreview.style.display = DisplayStyle.None;
        TrySpawnPrefab(item, Input.mousePosition);
    }

    public void CancelDragPreview()
    {
        if (dragPreview != null) dragPreview.style.display = DisplayStyle.None;
    }

    private Vector3 SnapPosition(Vector3 position, Vector3 snap)
    {
        return new Vector3(
            snap.x > 0 ? Mathf.Round(position.x / snap.x) * snap.x : position.x,
            snap.y > 0 ? Mathf.Round(position.y / snap.y) * snap.y : position.y,
            snap.z > 0 ? Mathf.Round(position.z / snap.z) * snap.z : position.z
        );
    }   

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }     

    private void TrySpawnPrefab(LevelItemData item, Vector2 mouseScreenPos)
    {
        if (Camera.main == null || item.prefab == null) return;

        Ray ray = Camera.main.ScreenPointToRay(mouseScreenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, baseplateLayer))
        {
            Vector3 spawnPosition = hit.point;
            if (projectSettings != null)
            {
                spawnPosition = SnapPosition(spawnPosition, projectSettings.PositionSnap);
            }
            GameObject newObject = Instantiate(item.prefab, spawnPosition, Quaternion.identity);
            SetLayerRecursively(newObject, LayerMask.NameToLayer("SelectableObjects"));

            objectSelector?.SelectObject(newObject);
        }
    }
}