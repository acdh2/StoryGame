using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public class StoryEditor : MonoBehaviour
{
    public CommandListController commandListController;
    private const string SAVE_KEY = "StoryEditor_SavedCommands";

    [TextArea(10, 20)]
    public string CurrentLuaCode;

    private void OnEnable()
    {
        if (commandListController == null)
            commandListController = GetComponent<CommandListController>();

        LoadEditorState();
    }

    private void OnDisable()
    {
        SaveEditorState();
    }

    public void SaveEditorState()
    {
        if (commandListController == null) return;

        CommandDataList wrapper = new CommandDataList();
        wrapper.Commands = commandListController.GetCommandDataList();

        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();

        // Update direct het gegenereerde Lua-script
        CurrentLuaCode = ToLuaScript();
    }

    public void LoadEditorState()
    {
        if (commandListController == null) return;

        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(SAVE_KEY);
            CommandDataList wrapper = JsonUtility.FromJson<CommandDataList>(json);
            if (wrapper != null)
            {
                commandListController.LoadFromDataList(wrapper.Commands);
                CurrentLuaCode = ToLuaScript();
                return;
            }
        }

        commandListController.PopulateInitialList();
        CurrentLuaCode = ToLuaScript();
    }

    public string ToLuaScript()
    {
        if (commandListController == null) return string.Empty;

        var commandList = commandListController.GetCommandDataList();
        StringBuilder sb = new StringBuilder();

        foreach (var cmd in commandList)
        {
            if (string.IsNullOrEmpty(cmd.CommandType)) continue;

            string arg = cmd.Argument ?? "";

            // 1. Unescape ge ge-escapede karakters uit de input (bijv. \n -> echte newline)
            try
            {
                arg = Regex.Unescape(arg);
            }
            catch
            {
                // Val terug op originele string als Regex.Unescape faalt op ongeldige formatting
            }

            // 2. Escape dubbele quotes zodat de Lua-string niet breekt
            arg = arg.Replace("\"", "\\\"");

            switch (cmd.CommandType.ToLower())
            {
                case "say":
                    sb.AppendLine($"say(\"{arg}\")");
                    break;
                case "option":
                    sb.AppendLine($"option(\"{arg}\")");
                    break;
                case "label":
                    sb.AppendLine($"label(\"{arg}\")");
                    break;
                case "jump":
                    sb.AppendLine($"jump(\"{arg}\")");
                    break;
                case "jumpif":
                    sb.AppendLine($"check_if(\"{arg}\")");
                    break;
                case "set":
                    sb.AppendLine($"set(\"{arg}\")");
                    break;
                case "print":
                    sb.AppendLine($"print(\"{arg}\")");
                    break;
                default:
                    if (!string.IsNullOrEmpty(cmd.CommandType))
                    {
                        sb.AppendLine($"{cmd.CommandType}(\"{arg}\")");
                    }
                    break;
            }
        }

        return sb.ToString();
    }
}