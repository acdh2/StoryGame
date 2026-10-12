using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MaterialPalette : UIControllerBase
{
    [System.Serializable]
    public struct MaterialCategory
    {
        public string categoryName;
        public List<Material> materials;
    }

    [Header("UI Settings")]
    [SerializeField] private LayerMask targetLayerMask;
    [SerializeField] private SceneManager sceneManager;
    [SerializeField] private EditorCameraController cameraController;

    private Vector2 itemSize = new Vector2(48, 48);

    [Header("Data")]
    [SerializeField] private Texture2D defaultIcon;

    [SerializeField] private List<MaterialCategory> categories = new List<MaterialCategory>();

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
            categoryBar.contentContainer.style.paddingRight = 16;
        }

        scrollView = root.Q<ScrollView>("ItemBar");
        if (scrollView != null)
        {
            scrollView.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
            scrollView.contentContainer.style.paddingRight = 16;
            scrollView.AddManipulator(new ItemScrollManipulator(scrollView));
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
        if (categoryBar != null)
        {
            categoryBar.Clear();
            categoryBar = null;
        }

        if (scrollView != null)
        {
            scrollView.Clear();
            scrollView = null;
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

            Button categoryBtn = new Button();
            categoryBtn.clicked += () => SelectCategory(categoryIndex);
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
            categoryBtn.style.flexShrink = 0;

            if (defaultIcon != null)
            {
                VisualElement icon = new VisualElement();
                icon.style.backgroundImage = new StyleBackground(defaultIcon);
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

            categoryBtn.AddManipulator(new CategoryButtonScrollManipulator(categoryBar));

            categoryBar.Add(categoryBtn);
            categoryButtons.Add(categoryBtn);
        }

        categoryBar.RegisterCallback<GeometryChangedEvent>(OnScrollViewGeometryChanged);
        categoryBar.MarkDirtyRepaint();
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
            itemCard.style.flexShrink = 0;
            
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
            itemCard.AddManipulator(new MaterialDragManipulator(mat, thumbnail, this, cameraController));

            scrollView.Add(itemCard);
        }
        scrollView.scrollOffset = Vector2.zero;
        scrollView.RegisterCallback<GeometryChangedEvent>(OnScrollViewGeometryChanged);
        scrollView.MarkDirtyRepaint();
    }

    private void OnScrollViewGeometryChanged(GeometryChangedEvent evt)
    {
        var target = evt.target as ScrollView;
        if (target != null)
        {
            target.UnregisterCallback<GeometryChangedEvent>(OnScrollViewGeometryChanged);
            target.MarkDirtyRepaint();
        }
    }

    public void StartDragPreview(Texture2D thumbnail, Vector2 screenPos)
    {
        if (dragPreview == null || root == null) return;
        dragPreview.style.backgroundImage = new StyleBackground(thumbnail);
        dragPreview.style.display = DisplayStyle.Flex;
        UpdateDragPreview(screenPos);
    }

    public void UpdateDragPreview(Vector2 screenPos)
    {
        if (dragPreview == null || root == null) return;

        Vector2 localPos = root.WorldToLocal(screenPos);

        float previewWidth = dragPreview.resolvedStyle.width > 0 ? dragPreview.resolvedStyle.width : itemSize.x;
        float previewHeight = dragPreview.resolvedStyle.height > 0 ? dragPreview.resolvedStyle.height : itemSize.y;

        dragPreview.style.left = localPos.x - (previewWidth / 2f);
        dragPreview.style.top = localPos.y - (previewHeight / 2f);
    }

    public void EndDragAndApply(Material material)
    {
        if (dragPreview != null) dragPreview.style.display = DisplayStyle.None;

        if (UIToolkitHelper.IsPointerOverUI()) return;
        if (UIToolkitHelper.IsPointerOutsideScreen()) return;
        
        TryApplyMaterial(material, UIToolkitHelper.GetMousePosition());
    }

    public void CancelDragPreview()
    {
        if (dragPreview != null) dragPreview.style.display = DisplayStyle.None;
    }

    private void TryApplyMaterial(Material material, Vector2 mouseScreenPos)
    {
        if (Camera.main == null || material == null || sceneManager == null) return;

        Ray ray = Camera.main.ScreenPointToRay(mouseScreenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, targetLayerMask))
        {
            sceneManager.ApplyMaterialToTarget(hit.collider.gameObject, material);
        }
    }

    public Material FindMaterialByName(string materialName)
    {
        foreach (var category in categories)
        {
            foreach (var mat in category.materials)
            {
                if (mat != null && mat.name == materialName)
                {
                    return mat;
                }
            }
        }
        return null;
    }    
}
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UIElements;

// public class MaterialPalette : UIControllerBase
// {
//     [System.Serializable]
//     public struct MaterialCategory
//     {
//         public string categoryName;
//         public List<Material> materials;
//     }

//     [Header("UI Settings")]
//     [SerializeField] private LayerMask targetLayerMask;
//     [SerializeField] private SceneManager sceneManager;
//     [SerializeField] private EditorCameraController cameraController;

//     private Vector2 itemSize = new Vector2(48, 48);

//     [Header("Data")]
//     [SerializeField] private Texture2D defaultIcon;

//     [SerializeField] private List<MaterialCategory> categories = new List<MaterialCategory>();

//     private VisualElement root;
//     private ScrollView categoryBar;
//     private ScrollView scrollView;
//     private VisualElement dragPreview;

//     private int currentCategoryIndex = 0;
//     private readonly List<Button> categoryButtons = new List<Button>();

//     protected override void OnUIEnabled(VisualElement root)
//     {
//         this.root = root;

//         categoryBar = root.Q<ScrollView>("CategoryBar");
//         if (categoryBar != null)
//         {
//             categoryBar.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
//             categoryBar.RegisterCallback<PointerDownEvent>(OnCategoryBarPointerDown);
//         }

//         scrollView = root.Q<ScrollView>("ItemBar");
//         if (scrollView != null)
//         {
//             scrollView.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
//             scrollView.AddManipulator(new ItemScrollManipulator(scrollView));
//         }

//         SetupDragPreview();
//         BuildCategoryTabs();

//         if (categories.Count > 0)
//         {
//             SelectCategory(0);
//         }

//         if (categoryBar != null)
//         {
//             categoryBar.AddManipulator(new CategoryScrollManipulator(categoryBar));
//         }        
//     }

//     protected override void OnUIDisabled()
//     {
//         if (categoryBar != null)
//         {
//             categoryBar.UnregisterCallback<PointerDownEvent>(OnCategoryBarPointerDown);
//             categoryBar.Clear();
//             categoryBar = null;
//         }

//         if (scrollView != null)
//         {
//             scrollView.Clear();
//             scrollView = null;
//         }

//         categoryButtons.Clear();
//         dragPreview = null;
//         root = null;
//     }

//     private void OnCategoryBarPointerDown(PointerDownEvent evt)
//     {
//         if (evt.button != (int)MouseButton.LeftMouse) return;

//         var panelRoot = root?.panel?.visualTree;
//         if (panelRoot == null) return;

//         int pointerId = evt.pointerId;
//         Vector3 lastPosition = evt.position;

//         void OnMove(PointerMoveEvent moveEvt)
//         {
//             if (moveEvt.pointerId != pointerId) return;
//             Vector3 delta = moveEvt.position - lastPosition;
//             categoryBar.scrollOffset -= new Vector2(delta.x, delta.y);
//             lastPosition = moveEvt.position;
//             moveEvt.StopPropagation();
//         }

//         void OnUp(PointerUpEvent upEvt)
//         {
//             if (upEvt.pointerId != pointerId) return;
//             panelRoot.UnregisterCallback<PointerMoveEvent>(OnMove);
//             panelRoot.UnregisterCallback<PointerUpEvent>(OnUp);
//             panelRoot.UnregisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
//             upEvt.StopPropagation();
//         }

//         void OnCaptureOut(PointerCaptureOutEvent capEvt)
//         {
//             if (capEvt.pointerId != pointerId) return;
//             panelRoot.UnregisterCallback<PointerMoveEvent>(OnMove);
//             panelRoot.UnregisterCallback<PointerUpEvent>(OnUp);
//             panelRoot.UnregisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
//         }

//         panelRoot.RegisterCallback<PointerMoveEvent>(OnMove);
//         panelRoot.RegisterCallback<PointerUpEvent>(OnUp);
//         panelRoot.RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
//     }

//     private void SetupDragPreview()
//     {
//         if (root == null) return;

//         dragPreview = new VisualElement();
//         dragPreview.style.position = Position.Absolute;
//         dragPreview.style.width = itemSize.x;
//         dragPreview.style.height = itemSize.y;
//         dragPreview.style.display = DisplayStyle.None;
//         dragPreview.pickingMode = PickingMode.Ignore;
//         dragPreview.style.borderTopLeftRadius = 12;
//         dragPreview.style.borderTopRightRadius = 12;
//         dragPreview.style.borderBottomLeftRadius = 12;
//         dragPreview.style.borderBottomRightRadius = 12;
//         root.Add(dragPreview);
//     }

//     private void BuildCategoryTabs()
//     {
//         if (categoryBar == null) return;

//         categoryBar.Clear();
//         categoryButtons.Clear();

//         for (int i = 0; i < categories.Count; i++)
//         {
//             int categoryIndex = i;
//             var category = categories[i];

//             Button categoryBtn = new Button(() => SelectCategory(categoryIndex));
//             categoryBtn.style.flexDirection = FlexDirection.Row;
//             categoryBtn.style.alignItems = Align.Center;
//             categoryBtn.style.justifyContent = Justify.Center;
//             categoryBtn.style.height = 32;
//             categoryBtn.style.paddingLeft = 16;
//             categoryBtn.style.paddingRight = 16;
//             categoryBtn.style.marginLeft = 4;
//             categoryBtn.style.marginRight = 4;
//             categoryBtn.style.borderTopWidth = 0;
//             categoryBtn.style.borderBottomWidth = 0;
//             categoryBtn.style.borderLeftWidth = 0;
//             categoryBtn.style.borderRightWidth = 0;
//             categoryBtn.style.borderTopLeftRadius = 6;
//             categoryBtn.style.borderTopRightRadius = 6;
//             categoryBtn.style.borderBottomLeftRadius = 6;
//             categoryBtn.style.borderBottomRightRadius = 6;
//             categoryBtn.style.flexShrink = 0;

//             if (defaultIcon != null)
//             {
//                 VisualElement icon = new VisualElement();
//                 icon.style.backgroundImage = new StyleBackground(defaultIcon);
//                 icon.style.width = 20;
//                 icon.style.height = 20;
//                 icon.style.marginRight = 8;
//                 categoryBtn.Add(icon);
//             }

//             Label label = new Label(string.IsNullOrEmpty(category.categoryName) ? $"Cat {i + 1}" : category.categoryName.ToUpper());
//             label.style.fontSize = 12;
//             label.style.unityFontStyleAndWeight = FontStyle.Bold;
//             label.style.color = Color.black;
//             categoryBtn.Add(label);

//             categoryBar.Add(categoryBtn);
//             categoryButtons.Add(categoryBtn);
//         }
//     }

//     public void SelectCategory(int index)
//     {
//         if (index < 0 || index >= categories.Count) return;

//         currentCategoryIndex = index;

//         for (int i = 0; i < categoryButtons.Count; i++)
//         {
//             categoryButtons[i].style.backgroundColor = (i == index) 
//                 ? new StyleColor(new Color(0.8f, 0.8f, 0.8f)) 
//                 : new StyleColor(Color.clear);
//         }

//         PopulateItems(categories[currentCategoryIndex].materials);
//     }

//     private void PopulateItems(List<Material> materials)
//     {
//         if (scrollView == null) return;

//         scrollView.Clear();
//         if (materials == null) return;

//         foreach (var mat in materials)
//         {
//             if (mat == null) continue;

//             VisualElement itemCard = new VisualElement();

//             itemCard.style.width = itemSize.x;
//             itemCard.style.height = itemSize.y;
//             itemCard.style.marginLeft = 6;
//             itemCard.style.marginRight = 6;
//             itemCard.style.backgroundColor = new StyleColor(new Color(0.95f, 0.95f, 0.95f));
//             itemCard.style.flexShrink = 0;
            
//             itemCard.style.borderTopLeftRadius = 12;
//             itemCard.style.borderTopRightRadius = 12;
//             itemCard.style.borderBottomLeftRadius = 12;
//             itemCard.style.borderBottomRightRadius = 12;

//             itemCard.style.borderTopWidth = 1;
//             itemCard.style.borderBottomWidth = 1;
//             itemCard.style.borderLeftWidth = 1;
//             itemCard.style.borderRightWidth = 1;
//             itemCard.style.borderTopColor = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
//             itemCard.style.borderBottomColor = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
//             itemCard.style.borderLeftColor = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
//             itemCard.style.borderRightColor = new StyleColor(new Color(0.7f, 0.7f, 0.7f));

//             Texture2D thumbnail = MaterialPreviewGenerator.CreatePreview(mat);

//             if (thumbnail != null)
//             {
//                 VisualElement icon = new VisualElement();
//                 icon.style.backgroundImage = new StyleBackground(thumbnail);
//                 icon.style.width = Length.Percent(80);
//                 icon.style.height = Length.Percent(80);
//                 icon.style.alignSelf = Align.Center;
//                 icon.style.marginTop = Length.Percent(10);
//                 itemCard.Add(icon);
//             }

//             itemCard.tooltip = mat.name;
//             itemCard.AddManipulator(new MaterialDragManipulator(mat, thumbnail, this, cameraController));

//             scrollView.Add(itemCard);
//         }
//         scrollView.scrollOffset = Vector2.zero;
//         scrollView.RegisterCallback<GeometryChangedEvent>(OnScrollViewGeometryChanged);
//         scrollView.MarkDirtyRepaint();
//         scrollView.contentContainer.style.paddingRight = 16;
//     }

//     private void OnScrollViewGeometryChanged(GeometryChangedEvent evt)
//     {
//         var target = evt.target as ScrollView;
//         if (target != null)
//         {
//             target.UnregisterCallback<GeometryChangedEvent>(OnScrollViewGeometryChanged);
//             target.MarkDirtyRepaint();
//         }
//     }

//     public void StartDragPreview(Texture2D thumbnail, Vector2 screenPos)
//     {
//         if (dragPreview == null || root == null) return;
//         dragPreview.style.backgroundImage = new StyleBackground(thumbnail);
//         dragPreview.style.display = DisplayStyle.Flex;
//         UpdateDragPreview(screenPos);
//     }

//     public void UpdateDragPreview(Vector2 screenPos)
//     {
//         if (dragPreview == null || root == null) return;

//         Vector2 localPos = root.WorldToLocal(screenPos);

//         float previewWidth = dragPreview.resolvedStyle.width > 0 ? dragPreview.resolvedStyle.width : itemSize.x;
//         float previewHeight = dragPreview.resolvedStyle.height > 0 ? dragPreview.resolvedStyle.height : itemSize.y;

//         dragPreview.style.left = localPos.x - (previewWidth / 2f);
//         dragPreview.style.top = localPos.y - (previewHeight / 2f);
//     }

//     public void EndDragAndApply(Material material)
//     {
//         if (dragPreview != null) dragPreview.style.display = DisplayStyle.None;

//         if (UIToolkitHelper.IsPointerOverUI()) return;
//         if (UIToolkitHelper.IsPointerOutsideScreen()) return;
        
//         TryApplyMaterial(material, UIToolkitHelper.GetMousePosition());
//     }

//     public void CancelDragPreview()
//     {
//         if (dragPreview != null) dragPreview.style.display = DisplayStyle.None;
//     }

//     private void TryApplyMaterial(Material material, Vector2 mouseScreenPos)
//     {
//         if (Camera.main == null || material == null || sceneManager == null) return;

//         Ray ray = Camera.main.ScreenPointToRay(mouseScreenPos);

//         if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, targetLayerMask))
//         {
//             sceneManager.ApplyMaterialToTarget(hit.collider.gameObject, material);
//         }
//     }

//     public Material FindMaterialByName(string materialName)
//     {
//         foreach (var category in categories)
//         {
//             foreach (var mat in category.materials)
//             {
//                 if (mat != null && mat.name == materialName)
//                 {
//                     return mat;
//                 }
//             }
//         }
//         return null;
//     }    
// }

// // using System.Collections.Generic;
// // using UnityEngine;
// // using UnityEngine.UIElements;

// // public class MaterialPalette : UIControllerBase
// // {
// //     [System.Serializable]
// //     public struct MaterialCategory
// //     {
// //         public string categoryName;
// //         public List<Material> materials;
// //     }

// //     [Header("UI Settings")]
// //     [SerializeField] private LayerMask targetLayerMask;
// //     [SerializeField] private SceneManager sceneManager;
// //     [SerializeField] private EditorCameraController cameraController;

// //     private Vector2 itemSize = new Vector2(48, 48);

// //     [Header("Data")]
// //     [SerializeField] private Texture2D defaultIcon;

// //     [SerializeField] private List<MaterialCategory> categories = new List<MaterialCategory>();

// //     private VisualElement root;
// //     private ScrollView categoryBar;
// //     private ScrollView scrollView;
// //     private VisualElement dragPreview;

// //     private int currentCategoryIndex = 0;
// //     private readonly List<Button> categoryButtons = new List<Button>();

// //     protected override void OnUIEnabled(VisualElement root)
// //     {
// //         this.root = root;

// //         categoryBar = root.Q<ScrollView>("CategoryBar");
// //         if (categoryBar != null)
// //         {
// //             categoryBar.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
// //         }

// //         scrollView = root.Q<ScrollView>("ItemBar");
// //         if (scrollView != null)
// //         {
// //             scrollView.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
// //         }

// //         SetupDragPreview();
// //         BuildCategoryTabs();

// //         if (categories.Count > 0)
// //         {
// //             SelectCategory(0);
// //         }
// //     }

// //     protected override void OnUIDisabled()
// //     {
// //         if (scrollView != null)
// //         {
// //             scrollView.Clear();
// //             scrollView = null;
// //         }

// //         if (categoryBar != null)
// //         {
// //             categoryBar.Clear();
// //             categoryBar = null;
// //         }

// //         categoryButtons.Clear();
// //         dragPreview = null;
// //         root = null;
// //     }

// //     private void SetupDragPreview()
// //     {
// //         if (root == null) return;

// //         dragPreview = new VisualElement();
// //         dragPreview.style.position = Position.Absolute;
// //         dragPreview.style.width = itemSize.x;
// //         dragPreview.style.height = itemSize.y;
// //         dragPreview.style.display = DisplayStyle.None;
// //         dragPreview.pickingMode = PickingMode.Ignore;
// //         dragPreview.style.borderTopLeftRadius = 12;
// //         dragPreview.style.borderTopRightRadius = 12;
// //         dragPreview.style.borderBottomLeftRadius = 12;
// //         dragPreview.style.borderBottomRightRadius = 12;
// //         root.Add(dragPreview);
// //     }

// //     private void BuildCategoryTabs()
// //     {
// //         if (categoryBar == null) return;

// //         categoryBar.Clear();
// //         categoryButtons.Clear();

// //         for (int i = 0; i < categories.Count; i++)
// //         {
// //             int categoryIndex = i;
// //             var category = categories[i];

// //             Button categoryBtn = new Button(() => SelectCategory(categoryIndex));
// //             categoryBtn.style.flexDirection = FlexDirection.Row;
// //             categoryBtn.style.alignItems = Align.Center;
// //             categoryBtn.style.justifyContent = Justify.Center;
// //             categoryBtn.style.height = 32;
// //             categoryBtn.style.paddingLeft = 16;
// //             categoryBtn.style.paddingRight = 16;
// //             categoryBtn.style.marginLeft = 4;
// //             categoryBtn.style.marginRight = 4;
// //             categoryBtn.style.borderTopWidth = 0;
// //             categoryBtn.style.borderBottomWidth = 0;
// //             categoryBtn.style.borderLeftWidth = 0;
// //             categoryBtn.style.borderRightWidth = 0;
// //             categoryBtn.style.borderTopLeftRadius = 6;
// //             categoryBtn.style.borderTopRightRadius = 6;
// //             categoryBtn.style.borderBottomLeftRadius = 6;
// //             categoryBtn.style.borderBottomRightRadius = 6;
// //             categoryBtn.style.flexShrink = 0;

// //             if (defaultIcon != null)
// //             {
// //                 VisualElement icon = new VisualElement();
// //                 icon.style.backgroundImage = new StyleBackground(defaultIcon);
// //                 icon.style.width = 20;
// //                 icon.style.height = 20;
// //                 icon.style.marginRight = 8;
// //                 categoryBtn.Add(icon);
// //             }

// //             Label label = new Label(string.IsNullOrEmpty(category.categoryName) ? $"Cat {i + 1}" : category.categoryName.ToUpper());
// //             label.style.fontSize = 12;
// //             label.style.unityFontStyleAndWeight = FontStyle.Bold;
// //             label.style.color = Color.black;
// //             categoryBtn.Add(label);

// //             categoryBar.Add(categoryBtn);
// //             categoryButtons.Add(categoryBtn);
// //         }
// //     }

// //     public void SelectCategory(int index)
// //     {
// //         if (index < 0 || index >= categories.Count) return;

// //         currentCategoryIndex = index;

// //         for (int i = 0; i < categoryButtons.Count; i++)
// //         {
// //             categoryButtons[i].style.backgroundColor = (i == index) 
// //                 ? new StyleColor(new Color(0.8f, 0.8f, 0.8f)) 
// //                 : new StyleColor(Color.clear);
// //         }

// //         PopulateItems(categories[currentCategoryIndex].materials);
// //     }

// //     private void PopulateItems(List<Material> materials)
// //     {
// //         if (scrollView == null) return;

// //         scrollView.Clear();
// //         if (materials == null) return;

// //         foreach (var mat in materials)
// //         {
// //             if (mat == null) continue;

// //             VisualElement itemCard = new VisualElement();

// //             itemCard.style.width = itemSize.x;
// //             itemCard.style.height = itemSize.y;
// //             itemCard.style.marginLeft = 6;
// //             itemCard.style.marginRight = 6;
// //             itemCard.style.backgroundColor = new StyleColor(new Color(0.95f, 0.95f, 0.95f));
// //             itemCard.style.flexShrink = 0;
            
// //             itemCard.style.borderTopLeftRadius = 12;
// //             itemCard.style.borderTopRightRadius = 12;
// //             itemCard.style.borderBottomLeftRadius = 12;
// //             itemCard.style.borderBottomRightRadius = 12;

// //             itemCard.style.borderTopWidth = 1;
// //             itemCard.style.borderBottomWidth = 1;
// //             itemCard.style.borderLeftWidth = 1;
// //             itemCard.style.borderRightWidth = 1;
// //             itemCard.style.borderTopColor = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
// //             itemCard.style.borderBottomColor = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
// //             itemCard.style.borderLeftColor = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
// //             itemCard.style.borderRightColor = new StyleColor(new Color(0.7f, 0.7f, 0.7f));

// //             Texture2D thumbnail = MaterialPreviewGenerator.CreatePreview(mat);

// //             if (thumbnail != null)
// //             {
// //                 VisualElement icon = new VisualElement();
// //                 icon.style.backgroundImage = new StyleBackground(thumbnail);
// //                 icon.style.width = Length.Percent(80);
// //                 icon.style.height = Length.Percent(80);
// //                 icon.style.alignSelf = Align.Center;
// //                 icon.style.marginTop = Length.Percent(10);
// //                 itemCard.Add(icon);
// //             }

// //             itemCard.tooltip = mat.name;
// //             itemCard.AddManipulator(new MaterialDragManipulator(mat, thumbnail, this, cameraController));

// //             scrollView.Add(itemCard);
// //         }
// //         scrollView.scrollOffset = Vector2.zero;
// //         scrollView.RegisterCallback<GeometryChangedEvent>(OnScrollViewGeometryChanged);
// //         scrollView.MarkDirtyRepaint();
// //         scrollView.contentContainer.style.paddingRight = 16;
// //     }

// //     private void OnScrollViewGeometryChanged(GeometryChangedEvent evt)
// //     {
// //         var target = evt.target as ScrollView;
// //         if (target != null)
// //         {
// //             target.UnregisterCallback<GeometryChangedEvent>(OnScrollViewGeometryChanged);
// //             target.MarkDirtyRepaint();
// //         }
// //     }

// //     public void StartDragPreview(Texture2D thumbnail, Vector2 screenPos)
// //     {
// //         if (dragPreview == null || root == null) return;
// //         dragPreview.style.backgroundImage = new StyleBackground(thumbnail);
// //         dragPreview.style.display = DisplayStyle.Flex;
// //         UpdateDragPreview(screenPos);
// //     }

// //     public void UpdateDragPreview(Vector2 screenPos)
// //     {
// //         if (dragPreview == null || root == null) return;

// //         Vector2 localPos = root.WorldToLocal(screenPos);

// //         float previewWidth = dragPreview.resolvedStyle.width > 0 ? dragPreview.resolvedStyle.width : itemSize.x;
// //         float previewHeight = dragPreview.resolvedStyle.height > 0 ? dragPreview.resolvedStyle.height : itemSize.y;

// //         dragPreview.style.left = localPos.x - (previewWidth / 2f);
// //         dragPreview.style.top = localPos.y - (previewHeight / 2f);
// //     }

// //     public void EndDragAndApply(Material material)
// //     {
// //         if (dragPreview != null) dragPreview.style.display = DisplayStyle.None;

// //         if (UIToolkitHelper.IsPointerOverUI()) return;
// //         if (UIToolkitHelper.IsPointerOutsideScreen()) return;

// //         TryApplyMaterial(material, UIToolkitHelper.GetMousePosition());
// //     }

// //     public void CancelDragPreview()
// //     {
// //         if (dragPreview != null) dragPreview.style.display = DisplayStyle.None;
// //     }

// //     private void TryApplyMaterial(Material material, Vector2 mouseScreenPos)
// //     {
// //         if (Camera.main == null || material == null || sceneManager == null) return;

// //         Ray ray = Camera.main.ScreenPointToRay(mouseScreenPos);

// //         if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, targetLayerMask))
// //         {
// //             sceneManager.ApplyMaterialToTarget(hit.collider.gameObject, material);
// //         }
// //     }

// //     public Material FindMaterialByName(string materialName)
// //     {
// //         foreach (var category in categories)
// //         {
// //             foreach (var mat in category.materials)
// //             {
// //                 if (mat != null && mat.name == materialName)
// //                 {
// //                     return mat;
// //                 }
// //             }
// //         }
// //         return null;
// //     }    
// // }