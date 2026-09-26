using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

[Serializable]
public class CommandDataListWrapper
{
    public List<CommandData> Commands = new List<CommandData>();
}

public class StoryDataStore : MonoBehaviour
{
    private const string SAVE_KEY = "StoryEditor_SavedCommands";

    [SerializeField]
    private List<CommandData> commands = new List<CommandData>();

    private bool isLoaded = false;

    public event Action OnDataChanged;

    public IReadOnlyList<CommandData> Commands
    {
        get
        {
            EnsureLoaded();
            return commands.AsReadOnly();
        }
    }

    private void EnsureLoaded()
    {
        if (!isLoaded)
        {
            Load();
        }
    }

    public void SetCommands(List<CommandData> newCommands)
    {
        commands = newCommands ?? new List<CommandData>();
        isLoaded = true;
        OnDataChanged?.Invoke();
    }

    public void AddCommand(string commandType = "say", string argument = "")
    {
        EnsureLoaded();
        commands.Add(new CommandData { CommandType = commandType, Argument = argument });
        OnDataChanged?.Invoke();
    }

    public void InsertCommand(int index, string commandType = "say", string argument = "")
    {
        EnsureLoaded();
        commands.Insert(index, new CommandData { CommandType = commandType, Argument = argument });
        OnDataChanged?.Invoke();
    }

    public void RemoveCommand(int index)
    {
        EnsureLoaded();
        if (index >= 0 && index < commands.Count)
        {
            commands.RemoveAt(index);
            OnDataChanged?.Invoke();
        }
    }

    public void MoveCommand(int oldIndex, int newIndex)
    {
        EnsureLoaded();
        if (oldIndex < 0 || oldIndex >= commands.Count || newIndex < 0 || newIndex > commands.Count) return;

        var item = commands[oldIndex];
        commands.RemoveAt(oldIndex);
        
        if (newIndex > oldIndex) newIndex--;
        commands.Insert(newIndex, item);
        
        OnDataChanged?.Invoke();
    }

    public void UpdateCommand(int index, string commandType, string argument)
    {
        EnsureLoaded();
        if (index >= 0 && index < commands.Count)
        {
            commands[index].CommandType = commandType;
            commands[index].Argument = argument;
        }
    }

    public void Save()
    {
        EnsureLoaded();
        CommandDataListWrapper wrapper = new CommandDataListWrapper { Commands = commands };
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    public void Load()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(SAVE_KEY);
            CommandDataListWrapper wrapper = JsonUtility.FromJson<CommandDataListWrapper>(json);
            if (wrapper != null && wrapper.Commands.Count > 0)
            {
                commands = wrapper.Commands;
                isLoaded = true;
                OnDataChanged?.Invoke();
                return;
            }
        }

        commands.Clear();
        for (int i = 0; i < 24; i++)
        {
            commands.Add(new CommandData { CommandType = "say", Argument = "" });
        }
        isLoaded = true;
        OnDataChanged?.Invoke();
    }

    public string ToLuaScript()
    {
        EnsureLoaded();
        StringBuilder sb = new StringBuilder();

        foreach (var cmd in commands)
        {
            if (string.IsNullOrEmpty(cmd.CommandType)) continue;

            string arg = cmd.Argument ?? "";
            try { arg = Regex.Unescape(arg); } catch { }
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
                    sb.AppendLine($"{cmd.CommandType}(\"{arg}\")");
                    break;
            }
        }

        return sb.ToString();
    }
}