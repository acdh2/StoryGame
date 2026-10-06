using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandDataListWrapper
{
    public List<CommandData> Commands = new List<CommandData>();
}

public class StoryDataStore : MonoBehaviour
{
    [SerializeField]
    private List<CommandData> commands = new List<CommandData>();

    public event Action OnDataChanged;
    public event Action OnDataInvalidated;

    private static readonly List<string> availableCommands = new List<string>
    {
        "text", "choice", "touched", "teleport", "show", "hide", "say", "unblock", "chapter", "goto", "create_var", "set_var", "var_is_set", "var_is_not_set", "code" // "unset", "block", 
    };

    public IEnumerable<string> AvailableCommandTypes => availableCommands;

    public IReadOnlyList<CommandData> Commands => commands.AsReadOnly();

    public void SetCommands(List<CommandData> newCommands)
    {
        commands = newCommands ?? new List<CommandData>();
        OnDataChanged?.Invoke();
    }

    public void AddCommand(string commandType = "text", string argument = "")
    {
        commands.Add(new CommandData { CommandType = commandType, Argument = argument });
        OnDataChanged?.Invoke();
    }

    public void InsertCommand(int index, string commandType = "text", string argument = "")
    {
        commands.Insert(index, new CommandData { CommandType = commandType, Argument = argument });
        OnDataChanged?.Invoke();
    }

    public void RemoveCommand(int index)
    {
        if (index >= 0 && index < commands.Count)
        {
            commands.RemoveAt(index);
            OnDataChanged?.Invoke();
        }
    }

    public void MoveCommand(int oldIndex, int newIndex)
    {
        var item = commands[oldIndex];
        commands.RemoveAt(oldIndex);
        
        if (newIndex > oldIndex) newIndex--;
        commands.Insert(newIndex, item);
        
        OnDataChanged?.Invoke();
    }

    public void UpdateCommand(int index, string commandType, string argument, bool invokeEvent = true)
    {
        if (index >= 0 && index < commands.Count)
        {
            commands[index].CommandType = commandType;
            commands[index].Argument = argument;
            if (invokeEvent)
            {
                OnDataChanged?.Invoke();
            }
        }
    }


    public string SerializeToJson()
    {
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
            OnDataInvalidated?.Invoke();
        }
    }
}