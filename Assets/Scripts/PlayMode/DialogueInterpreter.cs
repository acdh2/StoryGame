using System;
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

        List<CommandData> preparsed =
            PreParseCommands(raw);

        CompileInstructions(preparsed);
    }

    private List<CommandData> PreParseCommands(
        List<CommandData> raw)
    {
        List<CommandData> result =
            new List<CommandData>();

        int i = 0;

        while (i < raw.Count)
        {
            CommandData cmd = raw[i];

            if (cmd == null)
            {
                i++;
                continue;
            }

            string type =
                NormalizeType(cmd.CommandType);

            if (string.IsNullOrEmpty(type))
            {
                i++;
                continue;
            }

            if (type == "HAS" ||
                type == "HAS_NOT")
            {
                if (i + 1 < raw.Count)
                {
                    string nextType =
                        NormalizeType(
                            raw[i + 1].CommandType);

                    if (nextType == "CHOICE")
                    {
                        result.Add(cmd);
                        result.Add(raw[i + 1]);

                        i += 2;
                        continue;
                    }
                }

                List<int> ifIndices =
                    new List<int> { i };

                int peek = i + 1;

                while (peek < raw.Count)
                {
                    string nextType =
                        NormalizeType(
                            raw[peek].CommandType);

                    if (nextType == "HAS" ||
                        nextType == "HAS_NOT")
                    {
                        ifIndices.Add(peek);
                        peek++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (ifIndices.Count > 1)
                {
                    string exitLabel =
                        $"__autolabel_{autoLabelCounter++}";

                    for (int idx = 0;
                         idx < ifIndices.Count;
                         idx++)
                    {
                        CommandData currentIf =
                            raw[ifIndices[idx]];

                        string currentType =
                            NormalizeType(
                                currentIf.CommandType);

                        string invertedType =
                            currentType == "HAS"
                                ? "HAS_NOT"
                                : "HAS";

                        result.Add(
                            new CommandData
                            {
                                CommandType = invertedType,
                                Argument =
                                    NormalizeArgument(
                                        currentIf.Argument)
                            });

                        result.Add(
                            new CommandData
                            {
                                CommandType = "GOTO",
                                Argument = exitLabel
                            });
                    }

                    i = peek;

                    while (i < raw.Count)
                    {
                        string bodyType =
                            NormalizeType(
                                raw[i].CommandType);

                        if (bodyType == "CHAPTER" ||
                            bodyType == "GOTO" ||
                            bodyType == "CHOICE" ||
                            bodyType == "HAS" ||
                            bodyType == "HAS_NOT")
                        {
                            break;
                        }

                        result.Add(raw[i]);
                        i++;
                    }

                    result.Add(
                        new CommandData
                        {
                            CommandType = "CHAPTER",
                            Argument = exitLabel
                        });

                    continue;
                }
            }

            result.Add(cmd);
            i++;
        }

        return ParseOptionBlocks(result);
    }

    private List<CommandData> ParseOptionBlocks(
        List<CommandData> source)
    {
        List<CommandData> result =
            new List<CommandData>();

        int i = 0;

        while (i < source.Count)
        {
            string type =
                NormalizeType(source[i].CommandType);

            if (type != "CHOICE")
            {
                result.Add(source[i]);
                i++;
                continue;
            }

            List<CommandData> optionTexts =
                new List<CommandData>();

            List<CommandData> optionInstructions =
                new List<CommandData>();

            while (i < source.Count)
            {
                string optionType =
                    NormalizeType(
                        source[i].CommandType);

                if (optionType != "CHOICE")
                    break;

                CommandData option =
                    source[i];

                optionTexts.Add(option);

                i++;

                if (i >= source.Count)
                {
                    optionInstructions.Add(
                        new CommandData
                        {
                            CommandType = "NOP"
                        });

                    break;
                }

                string nextType =
                    NormalizeType(
                        source[i].CommandType);

                if (nextType == "CHOICE")
                {
                    optionInstructions.Add(
                        new CommandData
                        {
                            CommandType = "NOP"
                        });

                    continue;
                }

                if (nextType == "HAS" ||
                    nextType == "HAS_NOT")
                {
                    optionInstructions.Add(
                        new CommandData
                        {
                            CommandType = "NOP"
                        });

                    break;
                }

                optionInstructions.Add(
                    source[i]);

                i++;

                if (i >= source.Count)
                    break;

                string afterInstruction =
                    NormalizeType(
                        source[i].CommandType);

                if (afterInstruction != "CHOICE")
                    break;
            }

            for (int optionIndex = 0;
                 optionIndex < optionTexts.Count;
                 optionIndex++)
            {
                result.Add(
                    new CommandData
                    {
                        CommandType = "SHOWOPTION",
                        Argument =
                            optionTexts[optionIndex].Argument
                    });
            }

            result.Add(
                new CommandData
                {
                    CommandType = "WAITFORCHOICE"
                });

            for (int optionIndex = 0;
                 optionIndex < optionInstructions.Count;
                 optionIndex++)
            {
                result.Add(
                    new CommandData
                    {
                        CommandType = "IF_OPTION",
                        Argument =
                            optionTexts[optionIndex].Argument
                    });

                result.Add(
                    optionInstructions[optionIndex]);
            }
        }

        return result;
    }

    private void CompileInstructions(
        List<CommandData> preparsed)
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
            NormalizeArgument(cmd.Argument);

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

        List<string> cachedOptions =
            new List<string>();

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

                    cachedOptions.Add(
                        instr.Argument);

                    pc++;
                    break;

                case "WAITFORCHOICE":

                    selectedOptionText = "";

                    ShowOptionsUI(
                        cachedOptions);

                    yield return new WaitUntil(
                        () =>
                            !string.IsNullOrEmpty(
                                selectedOptionText));

                    ClearOptionsUI();

                    cachedOptions.Clear();

                    pc++;
                    break;

                case "IF_OPTION":

                    if (string.Equals(
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

                case "HAS":

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

                case "HAS_NOT":

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

                case "GIVE":

                    variables.Add(
                        instr.Argument);

                    pc++;
                    break;

                case "TAKE":

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
