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

    // private bool isLoaded = false;

    public event Action OnDataChanged;

    private static readonly Dictionary<string, Func<string, string>> commandRegistry = new Dictionary<string, Func<string, string>>(StringComparer.OrdinalIgnoreCase)
    {
        { "say", arg => $"say(\"{arg}\")" },
        { "option", arg => $"option(\"{arg}\")" },
        { "label", arg => $"label(\"{arg}\")" },
        { "jump", arg => $"jump(\"{arg}\")" },
        { "jumpif", arg => $"check_if(\"{arg}\")" },
        { "set", arg => $"set(\"{arg}\")" },
        { "print", arg => $"print(\"{arg}\")" }
    };

    public IEnumerable<string> AvailableCommandTypes => commandRegistry.Keys;

    public IReadOnlyList<CommandData> Commands
    {
        get
        {
            // EnsureLoaded();
            return commands.AsReadOnly();
        }
    }

    // private void EnsureLoaded()
    // {
    //     if (!isLoaded)
    //     {
    //         Load();
    //     }
    // }

    public void SetCommands(List<CommandData> newCommands)
    {
        commands = newCommands ?? new List<CommandData>();
        // isLoaded = true;
        OnDataChanged?.Invoke();
    }

    public void AddCommand(string commandType = "say", string argument = "")
    {
        // EnsureLoaded();
        commands.Add(new CommandData { CommandType = commandType, Argument = argument });
        OnDataChanged?.Invoke();
    }

    public void InsertCommand(int index, string commandType = "say", string argument = "")
    {
        // EnsureLoaded();
        commands.Insert(index, new CommandData { CommandType = commandType, Argument = argument });
        OnDataChanged?.Invoke();
    }

    public void RemoveCommand(int index)
    {
        // EnsureLoaded();
        if (index >= 0 && index < commands.Count)
        {
            commands.RemoveAt(index);
            OnDataChanged?.Invoke();
        }
    }

    public void MoveCommand(int oldIndex, int newIndex)
    {
        // EnsureLoaded();
        // if (oldIndex < 0 || oldIndex >= commands.Count || newIndex < 0 || newIndex > commands.Count) return;

        var item = commands[oldIndex];
        commands.RemoveAt(oldIndex);
        
        if (newIndex > oldIndex) newIndex--;
        commands.Insert(newIndex, item);
        
        OnDataChanged?.Invoke();
    }

    public void UpdateCommand(int index, string commandType, string argument)
    {
        // EnsureLoaded();
        if (index >= 0 && index < commands.Count)
        {
            commands[index].CommandType = commandType;
            commands[index].Argument = argument;
        }
    }

    public string SerializeToJson()
    {
        // EnsureLoaded();
        CommandDataListWrapper wrapper = new CommandDataListWrapper { Commands = commands };
        return JsonUtility.ToJson(wrapper, true);
    }

    public void DeserializeFromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        CommandDataListWrapper wrapper = JsonUtility.FromJson<CommandDataListWrapper>(json);
        if (wrapper != null && wrapper.Commands != null)
        {
            commands = wrapper.Commands;

            // isLoaded = true;
            OnDataChanged?.Invoke();
        }
    }

    // public void Save()
    // {
    //     string json = SerializeToJson();
    //     PlayerPrefs.SetString(SAVE_KEY, json);
    //     PlayerPrefs.Save();
    // }

    // public void Load()
    // {
    //     commands.Clear();

    //     if (PlayerPrefs.HasKey(SAVE_KEY))
    //     {
    //         string json = PlayerPrefs.GetString(SAVE_KEY);
    //         DeserializeFromJson(json);
    //         if (commands.Count > 0) return;
    //     }

    //     isLoaded = true;
    //     //Save();
    //     OnDataChanged?.Invoke();
    // }

    public string ToLuaScript()
    {
        // EnsureLoaded();
        StringBuilder sb = new StringBuilder();

        foreach (var cmd in commands)
        {
            if (string.IsNullOrEmpty(cmd.CommandType)) continue;

            string arg = cmd.Argument ?? "";
            try { arg = Regex.Unescape(arg); } catch { }
            arg = arg.Replace("\"", "\\\"");

            if (commandRegistry.TryGetValue(cmd.CommandType, out var generator))
            {
                sb.AppendLine(generator(arg));
            }
            else
            {
                sb.AppendLine($"{cmd.CommandType}(\"{arg}\")");
            }
        }

        return sb.ToString();
    }
}