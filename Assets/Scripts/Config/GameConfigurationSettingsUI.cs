using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(GameConfiguration))]
public class GameConfigurationSettingsUI : UIControllerBase
{
    [SerializeField] private GameConfiguration gameConfiguration;

    private Toggle fogToggle;
    private Slider fogDensitySlider;
    private Slider timeHoursSlider;
    private DropdownField musicDropdown;
    private DropdownField playerDropdown;

    private List<Button> colorButtons = new List<Button>();

    protected override void OnUIEnabled(VisualElement root)
    {
        if (gameConfiguration == null)
        {
            gameConfiguration = GetComponent<GameConfiguration>();
        }

        if (gameConfiguration != null) {
            gameConfiguration.OnInvalidateSettings += ApplyConfigurationToUI;
        }

        fogToggle = root.Q<Toggle>("toggle-fog");
        if (fogToggle != null)
        {
            fogToggle.RegisterValueChangedCallback(evt => gameConfiguration.fogEnabled = evt.newValue);
        }

        colorButtons = root.Query<Button>().Where(b => b.name != null && b.name.StartsWith("color-")).ToList();

        root.RegisterCallback<GeometryChangedEvent>(_ => RefreshColorButtons());

        foreach (var btn in colorButtons)
        {
            btn.clicked += () =>
            {
                Color btnColor = btn.resolvedStyle.backgroundColor;
                gameConfiguration.fogColor = btnColor;
                UpdateColorSelection(btnColor);
            };
        }

        fogDensitySlider = root.Q<Slider>("slider-fog-density");
        if (fogDensitySlider != null)
        {
            var dragContainer = fogDensitySlider.Q("unity-drag-container");
            dragContainer.RegisterCallback<PointerUpEvent>(evt =>
            {
                gameConfiguration.fogDensity = fogDensitySlider.value * fogDensitySlider.value;
            });
        }

        timeHoursSlider = root.Q<Slider>("slider-time");
        if (timeHoursSlider != null)
        {
            var dragContainer = timeHoursSlider.Q("unity-drag-container");
            dragContainer.RegisterCallback<PointerUpEvent>(evt =>
            {
                gameConfiguration.timeHours = timeHoursSlider.value;
            });
        }

        musicDropdown = root.Q<DropdownField>("dropdown-music");
        if (musicDropdown != null && gameConfiguration.musicTracks != null)
        {
            List<string> musicOptions = new List<string> { "No music" };
            musicOptions.AddRange(gameConfiguration.musicTracks.Select(t => t != null ? t.name : "Onbekend"));
            musicDropdown.choices = musicOptions;

            musicDropdown.RegisterValueChangedCallback(evt =>
            {
                gameConfiguration.selectedMusicIndex = musicDropdown.index - 1;
            });
        }

        playerDropdown = root.Q<DropdownField>("dropdown-player");
        if (playerDropdown != null && gameConfiguration.playerModels != null)
        {
            List<string> playerOptions = gameConfiguration.playerModels.Select(p => p != null ? p.name : "Onbekend").ToList();
            playerDropdown.choices = playerOptions;

            playerDropdown.RegisterValueChangedCallback(evt =>
            {
                gameConfiguration.selectedPlayerModelIndex = playerDropdown.index;
            });
        }

        ApplyConfigurationToUI();
    }

    public void ApplyConfigurationToUI()
    {
        if (gameConfiguration == null) return;

        if (fogToggle != null)
        {
            fogToggle.SetValueWithoutNotify(gameConfiguration.fogEnabled);
        }

        RefreshColorButtons();

        if (fogDensitySlider != null)
        {
            fogDensitySlider.SetValueWithoutNotify(Mathf.Sqrt(gameConfiguration.fogDensity));
        }

        if (timeHoursSlider != null)
        {
            timeHoursSlider.SetValueWithoutNotify(gameConfiguration.timeHours);
        }

        if (musicDropdown != null && musicDropdown.choices.Count > 0)
        {
            int targetIndex = Mathf.Clamp(gameConfiguration.selectedMusicIndex + 1, 0, musicDropdown.choices.Count - 1);
            musicDropdown.SetValueWithoutNotify(musicDropdown.choices[targetIndex]);
        }

        if (playerDropdown != null && playerDropdown.choices.Count > 0)
        {
            int targetIndex = Mathf.Clamp(gameConfiguration.selectedPlayerModelIndex, 0, playerDropdown.choices.Count - 1);
            playerDropdown.SetValueWithoutNotify(playerDropdown.choices[targetIndex]);
        }
    }

    private void RefreshColorButtons()
    {
        if (gameConfiguration == null) return;
        UpdateColorSelection(gameConfiguration.fogColor);
    }

    private void UpdateColorSelection(Color targetColor)
    {
        foreach (var b in colorButtons)
        {
            Color btnColor = b.resolvedStyle.backgroundColor;
            if (ColorsApproximatelyEqual(targetColor, btnColor))
            {
                HighlightSelectedColorButton(b);
            }
            else
            {
                b.style.borderTopWidth = 0;
                b.style.borderBottomWidth = 0;
                b.style.borderLeftWidth = 0;
                b.style.borderRightWidth = 0;
            }
        }
    }

    private bool ColorsApproximatelyEqual(Color c1, Color c2, float tolerance = 0.05f)
    {
        return Mathf.Abs(c1.r - c2.r) < tolerance &&
               Mathf.Abs(c1.g - c2.g) < tolerance &&
               Mathf.Abs(c1.b - c2.b) < tolerance;
    }

    private void HighlightSelectedColorButton(Button btn)
    {
        btn.style.borderTopWidth = 2;
        btn.style.borderBottomWidth = 2;
        btn.style.borderLeftWidth = 2;
        btn.style.borderRightWidth = 2;
        btn.style.borderTopColor = Color.cyan;
        btn.style.borderBottomColor = Color.cyan;
        btn.style.borderLeftColor = Color.cyan;
        btn.style.borderRightColor = Color.cyan;
    }

    protected override void OnUIDisabled()
    {
        if (gameConfiguration != null) {
            gameConfiguration.OnInvalidateSettings -= ApplyConfigurationToUI;
        }
        fogToggle = null;
        fogDensitySlider = null;
        timeHoursSlider = null;
        musicDropdown = null;
        playerDropdown = null;
        colorButtons.Clear();
    }
}