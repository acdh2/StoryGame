using UnityEngine;
using UnityEngine.UIElements;

public class AutoScrollViewButtons : MonoBehaviour
{
    [Tooltip("Aantal pixels dat per klik wordt verschoven.")]
    public float scrollStep = 100f;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>()?.rootVisualElement;
        if (root == null) return;

        var scrollViews = root.Query<ScrollView>().ToList();

        foreach (var sv in scrollViews)
        {
            if (sv.parent != null && sv.parent.ClassListContains("auto-scroll-wrapper"))
                continue;

            var originalParent = sv.parent;
            int originalIndex = originalParent.IndexOf(sv);

            var wrapper = new VisualElement();
            wrapper.AddToClassList("auto-scroll-wrapper");
            wrapper.style.flexDirection = FlexDirection.Row;
            wrapper.style.alignItems = Align.Center;
            wrapper.style.width = sv.style.width;
            wrapper.style.height = sv.style.height;
            wrapper.style.maxWidth = sv.style.maxWidth;
            wrapper.style.maxHeight = sv.style.maxHeight;
            wrapper.style.minWidth = sv.style.minWidth;
            wrapper.style.minHeight = sv.style.minHeight;

            sv.style.width = StyleKeyword.Auto;
            sv.style.height = new Length(100, LengthUnit.Percent);
            sv.style.flexGrow = 1;

            var leftBtn = new Button(() => Scroll(sv, -scrollStep)) { text = "‹" };
            var rightBtn = new Button(() => Scroll(sv, scrollStep)) { text = "›" };

            leftBtn.AddToClassList("auto-scroll-btn");
            rightBtn.AddToClassList("auto-scroll-btn");

            ApplyModernButtonStyle(leftBtn);
            ApplyModernButtonStyle(rightBtn);

            originalParent.Insert(originalIndex, wrapper);
            originalParent.Remove(sv);

            wrapper.Add(leftBtn);
            wrapper.Add(sv);
            wrapper.Add(rightBtn);
        }
    }

    private void ApplyModernButtonStyle(Button btn)
    {
        btn.style.width = 32;
        btn.style.height = 32;
        btn.style.alignSelf = Align.Center;
        btn.style.marginLeft = 6;
        btn.style.marginRight = 6;
        btn.style.borderTopLeftRadius = 16;
        btn.style.borderTopRightRadius = 16;
        btn.style.borderBottomLeftRadius = 16;
        btn.style.borderBottomRightRadius = 16;
        btn.style.backgroundColor = new Color(0f, 0f, 0f, 0.35f);
        btn.style.color = Color.white;
        btn.style.fontSize = 20;
        btn.style.unityFontStyleAndWeight = FontStyle.Bold;
        btn.style.borderTopWidth = 0;
        btn.style.borderBottomWidth = 0;
        btn.style.borderLeftWidth = 0;
        btn.style.borderRightWidth = 0;
    }

    private void Scroll(ScrollView sv, float amount)
    {
        Vector2 offset = sv.scrollOffset;
        offset.x += amount;
        sv.scrollOffset = offset;
    }
}