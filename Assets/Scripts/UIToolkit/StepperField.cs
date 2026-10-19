using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class StepperField : BaseField<int>
{
    int m_Min = 0, m_Max = 100, m_Step = 1;
    readonly Label m_ValueLabel = new Label();

    [UxmlAttribute] public int min  { get => m_Min;  set { m_Min = value;  Refresh(); } }
    [UxmlAttribute] public int max  { get => m_Max;  set { m_Max = value;  Refresh(); } }
    [UxmlAttribute] public int step { get => m_Step; set { m_Step = Mathf.Max(1, value); } }

    // Zorgt dat de waarde altijd binnen de limiet blijft
    public override int value
    {
        get => base.value;
        set => base.value = Mathf.Clamp(value, m_Min, m_Max);
    }

    public override void SetValueWithoutNotify(int newValue)
    {
        base.SetValueWithoutNotify(Mathf.Clamp(newValue, m_Min, m_Max));
        m_ValueLabel.text = base.value.ToString();
    }

    public StepperField() : this(null) { }

    public StepperField(string label) : base(label, new VisualElement())
    {
        var input = this.Q<VisualElement>(className: inputUssClassName);
        input.style.flexDirection = FlexDirection.Row;
        input.style.alignItems = Align.Center;

        var minus = new Button(() => value -= m_Step) { text = "-" };
        var plus  = new Button(() => value += m_Step) { text = "+" };
        m_ValueLabel.style.minWidth = 40;
        m_ValueLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

        input.Add(minus);
        input.Add(m_ValueLabel);
        input.Add(plus);

        SetValueWithoutNotify(base.value);
    }

    void Refresh() => SetValueWithoutNotify(base.value);
}