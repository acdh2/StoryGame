using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueInterpreter : MonoBehaviour
{
    public event Action OnDialogueStarted;
    public event Action OnDialogueEnded;
    public event Action OnScreenShown;
    public event Action OnScreenHidden;

    [Header("Script Input Source")]
    public StoryDataStore storyDataStore;

    private VisualElement rootElement;
    private Label characterNameLabel;
    private Label dialogueTextLabel;
    private VisualElement optionsContainer;
    private Button optionButtonTemplate;
    private Button continueButton;

    private bool advanceRequested = false;
    private string selectedOptionText = "";
    private bool touchedRequested = false;
    private string lastTouchedItem = "";
    private UnityEngine.Coroutine activeDialogueCoroutine = null;

    public class Instruction
    {
        public string Type;
        public string Argument;
    }

    private class ParsedLine
    {
        public int Indentation;
        public CommandData Command;
    }

    private List<Instruction> instructions = new List<Instruction>();

    private Dictionary<string, int> labels =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    private int autoLabelCounter = 0;

    private void ShowScreen()
    {
        if (rootElement != null &&
            rootElement.style.display != DisplayStyle.Flex)
        {
            rootElement.style.display = DisplayStyle.Flex;
            OnScreenShown?.Invoke();
        }
    }

    private void HideScreen()
    {
        if (rootElement != null &&
            rootElement.style.display != DisplayStyle.None)
        {
            rootElement.style.display = DisplayStyle.None;
            OnScreenHidden?.Invoke();
        }
    }

    public void StartDialogue(VisualElement root)
    {
        rootElement = root;

        if (root == null)
            return;

        BindUIElements(rootElement);

        StopDialogue();

        ShowScreen();

        ParseCommands();

        activeDialogueCoroutine =
            StartCoroutine(RunDialogueRoutine());

        OnDialogueStarted?.Invoke();
    }

    public void StopDialogue()
    {
        if (activeDialogueCoroutine != null)
        {
            StopCoroutine(activeDialogueCoroutine);
            activeDialogueCoroutine = null;
        }

        advanceRequested = false;
        selectedOptionText = "";
        touchedRequested = false;

        ClearOptionsUI();
        HideScreen();

        OnDialogueEnded?.Invoke();
    }

    private void BindUIElements(VisualElement root)
    {
        VisualElement dialogueRoot =
            root.Q<VisualElement>("dialogue-root");

        if (dialogueRoot == null)
            dialogueRoot = root;

        characterNameLabel =
            dialogueRoot.Q<Label>("character-name-label");

        dialogueTextLabel =
            dialogueRoot.Q<Label>("dialogue-text-label");

        optionsContainer =
            dialogueRoot.Q<VisualElement>("options-container");

        optionButtonTemplate =
            dialogueRoot.Q<Button>("option-button-template");

        continueButton =
            dialogueRoot.Q<Button>("continue-button");

        if (continueButton != null)
        {
            continueButton.clicked -= OnContinueClicked;
            continueButton.clicked += OnContinueClicked;
            continueButton.style.display =
                DisplayStyle.None;
        }

        if (optionButtonTemplate != null)
        {
            optionButtonTemplate.style.display =
                DisplayStyle.None;
        }
    }

    private void OnContinueClicked()
    {
        advanceRequested = true;
    }

    public void OnObjectTouched(string itemName)
    {
        if (touchedRequested &&
            string.Equals(
                lastTouchedItem,
                itemName,
                StringComparison.OrdinalIgnoreCase))
        {
            touchedRequested = false;
        }
    }

    private void ParseCommands()
    {
        instructions.Clear();
        labels.Clear();
        autoLabelCounter = 0;

        if (storyDataStore == null ||
            storyDataStore.Commands == null)
            return;

        List<CommandData> raw =
            new List<CommandData>(storyDataStore.Commands);

        List<ParsedLine> phase1 = PreParsePhase1(raw);

        List<CommandData> phase2 = TransformPhase2(phase1);

        CompileInstructions(phase2);
    }

    private List<ParsedLine> PreParsePhase1(List<CommandData> raw)
    {
        List<ParsedLine> result = new List<ParsedLine>();
        int currentIndent = 0;
        int nextIndent = 0;

        foreach (var cmd in raw)
        {
            if (cmd == null) continue;

            nextIndent = currentIndent;  

            switch (cmd.CommandType)
            {
                case "choice":
                    currentIndent = 0;
                    nextIndent ++;
                    break;

                case "is_set": 
                case "is_not_set":
                    nextIndent ++;
                    break;

                default:
                    nextIndent = 0;
                    break;
            }

            result.Add(new ParsedLine
            {
                Indentation = currentIndent,
                Command = new CommandData
                {
                    CommandType = cmd.CommandType,
                    Argument = NormalizeArgument(cmd.Argument)
                }
            });          

            currentIndent = nextIndent;
        }

        return result;
    }


private List<CommandData> TransformPhase2(List<ParsedLine> phase1)
{
    List<CommandData> result = new List<CommandData>();
    int i = 0;

    while (i < phase1.Count)
    {
        ParsedLine line = phase1[i];
        string type = NormalizeType(line.Command.CommandType);

        if (type != "CHOICE")
        {
            result.Add(line.Command);
            i++;
            continue;
        }

        List<(string optionText, List<CommandData> body)> options = new List<(string, List<CommandData>)>();

        while (i < phase1.Count)
        {
            ParsedLine currentLine = phase1[i];
            if (NormalizeType(currentLine.Command.CommandType) == "CHOICE")
            {
                string optionText = currentLine.Command.Argument;
                i++;

                List<CommandData> body = new List<CommandData>();
                while (i < phase1.Count)
                {
                    ParsedLine nextLine = phase1[i];
                    if (NormalizeType(nextLine.Command.CommandType) == "CHOICE")
                    {
                        break;
                    }
                    if (nextLine.Indentation == 0)
                    {
                        break;
                    }

                    body.Add(nextLine.Command);
                    i++;
                }
                options.Add((optionText, body));
            }
            else
            {
                break;
            }
        }

        foreach (var opt in options)
        {
            result.Add(new CommandData
            {
                CommandType = "SHOWOPTION",
                Argument = opt.optionText
            });
        }

        result.Add(new CommandData
        {
            CommandType = "WAITFORCHOICE"
        });

        string endChoiceLabel = $"&&__autolabel_endchoice_{autoLabelCounter++}";

        foreach (var opt in options)
        {
            string skipLabel = $"&&__autolabel_skip_{autoLabelCounter++}";

            result.Add(new CommandData
            {
                CommandType = "IF_NOT_CHOSEN",
                Argument = opt.optionText
            });
            result.Add(new CommandData
            {
                CommandType = "GOTO",
                Argument = skipLabel
            });

            foreach (var cmd in opt.body)
            {
                result.Add(cmd);
            }

            result.Add(new CommandData
            {
                CommandType = "GOTO",
                Argument = endChoiceLabel
            });

            result.Add(new CommandData
            {
                CommandType = "CHAPTER",
                Argument = skipLabel
            });
        }

        result.Add(new CommandData
        {
            CommandType = "CHAPTER",
            Argument = endChoiceLabel
        });
    }

    return result;
}

    private void CompileInstructions(List<CommandData> preparsed)
    {
        foreach (CommandData cmd in preparsed)
        {
            AddInstruction(cmd);
        }
    }

    private void AddInstruction(CommandData cmd)
    {
        if (cmd == null ||
            string.IsNullOrEmpty(cmd.CommandType))
            return;

        string type =
            NormalizeType(cmd.CommandType);

        string arg =
            (cmd.Argument ?? "").Trim();

        if (type == "CHAPTER")
        {
            labels[arg] = instructions.Count;
        }

        Instruction instruction =
            new Instruction
            {
                Type = type,
                Argument = arg
            };

        instructions.Add(instruction);
    }

private IEnumerator RunDialogueRoutine()
{
    int pc = 0;

    HashSet<string> variables =
        new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

    List<string> optionsToDisplay = new List<string>();

    while (pc < instructions.Count)
    {
        Instruction instr =
            instructions[pc];

        switch (instr.Type)
        {
            case "SAY":

                if (dialogueTextLabel != null)
                    dialogueTextLabel.text =
                        instr.Argument;

                if (continueButton != null)
                    continueButton.style.display =
                        DisplayStyle.Flex;

                advanceRequested = false;

                yield return new WaitUntil(
                    () => advanceRequested);

                if (continueButton != null)
                    continueButton.style.display =
                        DisplayStyle.None;

                pc++;
                break;

            case "SHOWOPTION":
                optionsToDisplay.Add(instr.Argument);
                pc++;
                break;

            case "WAITFORCHOICE":

                selectedOptionText = "";

                ShowOptionsUI(optionsToDisplay);

                yield return new WaitUntil(
                    () =>
                        !string.IsNullOrEmpty(
                            selectedOptionText));

                ClearOptionsUI();
                optionsToDisplay.Clear();

                pc++;
                break;

            case "IF_NOT_CHOSEN":

                if (!string.Equals(
                    instr.Argument,
                    selectedOptionText,
                    StringComparison.OrdinalIgnoreCase))
                {
                    pc++;
                }
                else
                {
                    pc += 2;
                }

                break;

            case "TOUCHED":

                touchedRequested = true;
                lastTouchedItem =
                    instr.Argument;

                HideScreen();

                yield return new WaitUntil(
                    () => !touchedRequested);

                ShowScreen();

                pc++;
                break;

            case "BLOCK":

                ToggleSceneObjectIsTrigger(
                    instr.Argument,
                    false);

                pc++;
                break;

            case "UNBLOCK":

                ToggleSceneObjectIsTrigger(
                    instr.Argument,
                    true);

                pc++;
                break;

            case "SHOW":

                ToggleSceneObjectVisibility(
                    instr.Argument,
                    true);

                pc++;
                break;

            case "HIDE":

                ToggleSceneObjectVisibility(
                    instr.Argument,
                    false);

                pc++;
                break;

            case "IS_SET":
                if (variables.Contains(
                    instr.Argument))
                {
                    pc++;
                }
                else
                {
                    pc += 2;
                }

                break;

            case "IS_NOT_SET":

                if (!variables.Contains(
                    instr.Argument))
                {
                    pc++;
                }
                else
                {
                    pc += 2;
                }

                break;

            case "SET":
            
                variables.Add(
                    instr.Argument);

                pc++;
                break;

            case "UNSET":

                variables.Remove(
                    instr.Argument);

                pc++;
                break;

            case "TELEPORT":

                KillAllPlayers(instr.Argument);

                pc++;
                break;

            case "CHAPTER":

                pc++;
                break;

            case "GOTO":

                if (labels.TryGetValue(
                    instr.Argument,
                    out int targetPc))
                {
                    pc = targetPc;
                }
                else
                {
                    Debug.LogWarning(
                        $"Dialogue jump target not found: {instr.Argument}");

                    pc++;
                }

                break;

            case "NOP":

                pc++;
                break;

            default:

                print("?unknown command");
                pc++;
                break;
        }
    }

    HideScreen();

    activeDialogueCoroutine = null;

    OnDialogueEnded?.Invoke();
}

    private void ToggleSceneObjectIsTrigger(
        string objectName,
        bool isTrigger)
    {
        GameObject sceneRoot =
            GameObject.Find("SceneManager");

        if (sceneRoot == null)
            return;

        Transform[] transforms =
            sceneRoot.GetComponentsInChildren<Transform>(
                true);

        foreach (Transform child in transforms)
        {
            if (child == sceneRoot.transform)
                continue;

            if (child.name.Equals(
                objectName,
                StringComparison.OrdinalIgnoreCase))
            {
                Collider[] colliders = child.GetComponentsInChildren<Collider>(true);
                foreach (Collider col in colliders)
                {
                    if (col is MeshCollider meshCollider && isTrigger)
                    {
                        meshCollider.convex = true;
                    }
                    col.isTrigger = isTrigger;
                }
            }
        }
    }


    private void ToggleSceneObjectVisibility(
        string objectName,
        bool show)
    {
        GameObject sceneRoot =
            GameObject.Find("SceneManager");

        if (sceneRoot == null)
            return;

        Transform[] transforms =
            sceneRoot.GetComponentsInChildren<Transform>(
                true);

        foreach (Transform child in transforms)
        {
            if (child == sceneRoot.transform)
                continue;

            if (child.name.Equals(
                objectName,
                StringComparison.OrdinalIgnoreCase))
            {
                child.gameObject.SetActive(show);
            }
        }
    }

    private void ShowOptionsUI(
        List<string> options)
    {
        if (continueButton != null)
            continueButton.style.display =
                DisplayStyle.None;

        foreach (string optText in options)
        {
            Button btn = new Button();

            btn.text = optText;

            if (optionButtonTemplate != null)
            {
                btn.style.height =
                    optionButtonTemplate.style.height;

                btn.style.backgroundColor =
                    optionButtonTemplate.style.backgroundColor;

                btn.style.borderTopLeftRadius =
                    optionButtonTemplate.style.borderTopLeftRadius;

                btn.style.borderTopRightRadius =
                    optionButtonTemplate.style.borderTopRightRadius;

                btn.style.borderBottomLeftRadius =
                    optionButtonTemplate.style.borderBottomLeftRadius;

                btn.style.borderBottomRightRadius =
                    optionButtonTemplate.style.borderBottomRightRadius;

                btn.style.color =
                    optionButtonTemplate.style.color;
            }

            btn.style.marginTop = 2;
            btn.style.marginBottom = 4;

            string textToSelect =
                optText;

            btn.clicked += () =>
            {
                selectedOptionText =
                    textToSelect;
            };

            optionsContainer?.Add(btn);
        }
    }

    private void ClearOptionsUI()
    {
        if (optionsContainer == null)
            return;

        optionsContainer.Clear();

        if (optionButtonTemplate != null)
        {
            optionsContainer.Add(
                optionButtonTemplate);

            optionButtonTemplate.style.display =
                DisplayStyle.None;
        }
    }

    public void KillAllPlayers(string target)
    {
        RespawnBehaviour[] respawnBehaviours = FindObjectsByType<RespawnBehaviour>(FindObjectsSortMode.None);
        
        foreach (RespawnBehaviour rb in respawnBehaviours)
        {
            if (rb != null)
            {
                rb.TeleportByName(target);
            }
        }
    }    

    private string NormalizeType(
        string value)
    {
        return (value ?? "")
            .Trim()
            .ToUpperInvariant();
    }

    private string NormalizeArgument(
        string value)
    {
        string result =
            (value ?? "").Trim();

        if (result.Length >= 2 &&
            result.StartsWith("\"") &&
            result.EndsWith("\""))
        {
            result =
                result.Substring(
                    1,
                    result.Length - 2);
        }

        return result;
    }
}
