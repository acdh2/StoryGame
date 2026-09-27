using System;

[Serializable]
public class CommandData
{
    public string CommandType;
    public string Argument;
}

[Serializable]
public class CommandDataList
{
    public System.Collections.Generic.List<CommandData> Commands = new System.Collections.Generic.List<CommandData>();
}