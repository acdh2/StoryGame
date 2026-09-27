using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

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
            gameConfiguration = FindFirstObjectByType<GameConfiguration>();
        }

        if (gameConfiguration == null) return;

        fogToggle = root.Q<Toggle>("toggle-fog");
        if (fogToggle != null)
        {
            fogToggle.value = gameConfiguration.fogEnabled;
            fogToggle.RegisterValueChangedCallback(evt => gameConfiguration.fogEnabled = evt.newValue);
        }

        colorButtons = root.Query<Button>().Where(b => b.name != null && b.name.StartsWith("color-")).ToList();

        foreach (var btn in colorButtons)
        {
            Color btnColor = btn.resolvedStyle.backgroundColor;

            if (ColorsApproximatelyEqual(gameConfiguration.fogColor, btnColor))
            {
                HighlightSelectedColorButton(btn);
            }

            btn.clicked += () =>
            {
                gameConfiguration.fogColor = btnColor;
                foreach (var b in colorButtons)
                {
                    b.style.borderTopWidth = 0;
                    b.style.borderBottomWidth = 0;
                    b.style.borderLeftWidth = 0;
                    b.style.borderRightWidth = 0;
                }
                HighlightSelectedColorButton(btn);
            };
        }

        fogDensitySlider = root.Q<Slider>("slider-fog-density");
        if (fogDensitySlider != null)
        {
            fogDensitySlider.value = Mathf.Sqrt(gameConfiguration.fogDensity);
            fogDensitySlider.RegisterValueChangedCallback(evt => gameConfiguration.fogDensity = evt.newValue * evt.newValue);
        }

        // fogDensitySlider = root.Q<Slider>("slider-fog-density");
        // if (fogDensitySlider != null)
        // {
        //     fogDensitySlider.value = gameConfiguration.fogDensity;
        //     fogDensitySlider.RegisterValueChangedCallback(evt => gameConfiguration.fogDensity = evt.newValue);
        // }

        timeHoursSlider = root.Q<Slider>("slider-time");
        if (timeHoursSlider != null)
        {
            timeHoursSlider.value = gameConfiguration.timeHours;
            timeHoursSlider.RegisterValueChangedCallback(evt => gameConfiguration.timeHours = evt.newValue);
        }

        musicDropdown = root.Q<DropdownField>("dropdown-music");
        if (musicDropdown != null && gameConfiguration.musicTracks != null)
        {
            List<string> musicOptions = new List<string> { "Geen muziek" };
            musicOptions.AddRange(gameConfiguration.musicTracks.Select(t => t != null ? t.name : "Onbekend"));
            musicDropdown.choices = musicOptions;

            int targetIndex = gameConfiguration.selectedMusicIndex + 1;
            musicDropdown.index = Mathf.Clamp(targetIndex, 0, musicOptions.Count - 1);

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

            playerDropdown.index = Mathf.Clamp(gameConfiguration.selectedPlayerModelIndex, 0, playerOptions.Count - 1);

            playerDropdown.RegisterValueChangedCallback(evt =>
            {
                gameConfiguration.selectedPlayerModelIndex = playerDropdown.index;
            });
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
        fogToggle = null;
        fogDensitySlider = null;
        timeHoursSlider = null;
        musicDropdown = null;
        playerDropdown = null;
        colorButtons.Clear();
    }
}