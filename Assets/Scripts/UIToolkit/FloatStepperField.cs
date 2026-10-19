using System;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class FloatStepperField : BaseField<float>
{
    float m_Min = 0f, m_Max = 100f, m_Step = 0.5f;
    int m_Decimals = 1;
    readonly Label m_ValueLabel = new Label();

    [UxmlAttribute] public float min { get => m_Min; set { m_Min = value; Refresh(); } }
    [UxmlAttribute] public float max { get => m_Max; set { m_Max = value; Refresh(); } }
    [UxmlAttribute] public float step
    {
        get => m_Step;
        set
        {
            m_Step = Mathf.Max(0.0001f, value);
            m_Decimals = CountDecimals(m_Step);
            Refresh();
        }
    }

    // Eerst snappen op het raster, dan afronden, dan clampen
    float Normalize(float v)
    {
        double steps = Math.Round((v - m_Min) / (double)m_Step);
        double snapped = m_Min + steps * m_Step;
        snapped = Math.Round(snapped, Mathf.Clamp(m_Decimals, 0, 15));
        return Mathf.Clamp((float)snapped, m_Min, m_Max);
    }

    static int CountDecimals(float s)
    {
        // decimal-conversie ronde 0.5f netjes af naar 0.5 i.p.v. 0.5000000xxx
        decimal d = (decimal)s;
        return (decimal.GetBits(d)[3] >> 16) & 0xFF;
    }

    public override float value
    {
        get => base.value;
        set => base.value = Normalize(value);
    }

    public override void SetValueWithoutNotify(float newValue)
    {
        base.SetValueWithoutNotify(Normalize(newValue));
        m_ValueLabel.text = base.value.ToString("F" + m_Decimals);
    }

    public FloatStepperField() : this(null) { }

    public FloatStepperField(string label) : base(label, new VisualElement())
    {
        var input = this.Q<VisualElement>(className: inputUssClassName);
        input.style.flexDirection = FlexDirection.Row;
        input.style.alignItems = Align.Center;

        var minus = new Button(() => value = base.value - m_Step) { text = "-" };
        var plus  = new Button(() => value = base.value + m_Step) { text = "+" };
        m_ValueLabel.style.minWidth = 50;
        m_ValueLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

        input.Add(minus);
        input.Add(m_ValueLabel);
        input.Add(plus);

        SetValueWithoutNotify(base.value);
    }

    void Refresh() => SetValueWithoutNotify(base.value);
}