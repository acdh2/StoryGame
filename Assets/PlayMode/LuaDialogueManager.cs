using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using MoonSharp.Interpreter;

public class LuaDialogueManager : MonoBehaviour
{
    [Header("Script Input Source")]
    public StoryEditor storyEditor;

    [Header("UI Document Reference")]
    public UIDocument uiDocument;

    // UI Elements
    private VisualElement rootElement;
    private Label characterNameLabel;
    private Label dialogueTextLabel;
    private VisualElement optionsContainer;
    private Button optionButtonTemplate;
    private Button continueButton;

    // Execution State
    private bool advanceRequested = false;
    private int selectedOptionNextInstructionIndex = -1;
    private bool clickedRequested = false;
    private string lastClickedItem = "";
    private UnityEngine.Coroutine activeDialogueCoroutine = null;

    // MoonSharp instance
    private Script moonSharpScript;

    // Data Structures
    public class Instruction
    {
        public string Type;
        public string Text;
        public string Target;
        public string VarName;
        public string ItemName;
    }

    private List<Instruction> instructions = new List<Instruction>();
    private Dictionary<string, int> labels = new Dictionary<string, int>();

    void Awake()
    {
        moonSharpScript = new Script();
        moonSharpScript.Globals["print"] = (System.Action<string>)((text) => Debug.Log($"[Lua] {text}"));
        moonSharpScript.Globals["say"] = (System.Action<string>)LuaSay;
        moonSharpScript.Globals["clicked"] = (System.Action<string>)LuaClicked;
        moonSharpScript.Globals["show"] = (System.Action<string>)LuaShow;
        moonSharpScript.Globals["hide"] = (System.Action<string>)LuaHide;
        moonSharpScript.Globals["option"] = (System.Action<string>)LuaOption;
        moonSharpScript.Globals["label"] = (System.Action<string>)LuaLabel;
        moonSharpScript.Globals["jump"] = (System.Action<string>)LuaJump;
        moonSharpScript.Globals["set"] = (System.Action<string>)LuaSet;
        moonSharpScript.Globals["unset"] = (System.Action<string>)LuaUnset;
        moonSharpScript.Globals["check_if"] = (System.Action<string>)LuaIf;
        moonSharpScript.Globals["check_if_not"] = (System.Action<string>)LuaIfNot;
    }

    void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        BindUIElements();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) {
            StartDialogue();
        }
    }

    public void StartDialogue()
    {
        StopDialogue();

        string codeToExecute = "";

        if (storyEditor != null && !string.IsNullOrEmpty(storyEditor.CurrentLuaCode))
        {
            codeToExecute = storyEditor.CurrentLuaCode;
        }

        if (rootElement != null)
        {
            rootElement.style.display = DisplayStyle.Flex;
        }

        ParseLuaScript(codeToExecute);
        activeDialogueCoroutine = StartCoroutine(RunDialogueRoutine());
    }

    public void StopDialogue()
    {
        if (activeDialogueCoroutine != null)
        {
            StopCoroutine(activeDialogueCoroutine);
            activeDialogueCoroutine = null;
        }

        advanceRequested = false;
        selectedOptionNextInstructionIndex = -1;
        clickedRequested = false;
        ClearOptionsUI();

        if (rootElement != null)
        {
            rootElement.style.display = DisplayStyle.None;
        }
    }

    private void BindUIElements()
    {
        rootElement = uiDocument.rootVisualElement.Q<VisualElement>("dialogue-root");
        characterNameLabel = rootElement.Q<Label>("character-name-label");
        dialogueTextLabel = rootElement.Q<Label>("dialogue-text-label");
        optionsContainer = rootElement.Q<VisualElement>("options-container");
        optionButtonTemplate = rootElement.Q<Button>("option-button-template");
        continueButton = rootElement.Q<Button>("continue-button");

        if (continueButton != null)
        {
            continueButton.clicked += OnContinueClicked;
            continueButton.style.display = DisplayStyle.None;
        }
        
        if (optionButtonTemplate != null)
        {
            optionButtonTemplate.style.display = DisplayStyle.None;
        }
    }

    private void OnContinueClicked()
    {
        advanceRequested = true;
    }

    public void OnWorldItemClicked(string itemName)
    {
        if (clickedRequested && lastClickedItem == itemName)
        {
            clickedRequested = false;
        }
    }

    // ==========================================
    // FASE 1: PARSING VIA MOONSHARP
    // ==========================================
    private void ParseLuaScript(string scriptSource)
    {
        instructions.Clear();
        labels.Clear();

        if (!string.IsNullOrEmpty(scriptSource))
        {
            moonSharpScript.DoString(scriptSource);
        }
    }

    private void LuaSay(string text)
    {
        instructions.Add(new Instruction { Type = "SAY", Text = text });
    }

    private void LuaClicked(string itemName)
    {
        instructions.Add(new Instruction { Type = "CLICKED", ItemName = itemName });
    }

    private void LuaShow(string itemName)
    {
        instructions.Add(new Instruction { Type = "SHOW", ItemName = itemName });
    }

    private void LuaHide(string itemName)
    {
        instructions.Add(new Instruction { Type = "HIDE", ItemName = itemName });
    }

    private void LuaOption(string text)
    {
        instructions.Add(new Instruction { Type = "OPTION", Text = text });
    }

    private void LuaLabel(string name)
    {
        labels[name] = instructions.Count;
        instructions.Add(new Instruction { Type = "LABEL", Target = name });
    }

    private void LuaJump(string targetLabel)
    {
        instructions.Add(new Instruction { Type = "JUMP", Target = targetLabel });
    }

    private void LuaSet(string varName)
    {
        instructions.Add(new Instruction { Type = "SET", VarName = varName });
    }

    private void LuaUnset(string varName)
    {
        instructions.Add(new Instruction { Type = "UNSET", VarName = varName });
    }

    private void LuaIf(string varName)
    {
        instructions.Add(new Instruction { Type = "IF", VarName = varName });
    }

    private void LuaIfNot(string varName)
    {
        instructions.Add(new Instruction { Type = "IF_NOT", VarName = varName });
    }

    // ==========================================
    // STRUCTUUR BEREKENING (GETNEXTBLOCK)
    // ==========================================

    /// <summary>
    /// Berekent de index direct NÁ het logische blok dat op startPc begint.
    /// - Als startPc een OPTION is: de OPTION + de onderliggende actie (tenzij de actie een nieuwe OPTION is).
    /// - Als startPc een IF / IF_NOT is: de IF + de onderliggende instructie/blok.
    /// - Anders: startPc + 1 (enkele instructie).
    /// </summary>
    private int GetNextBlock(int startPc)
    {
        if (startPc >= instructions.Count) return instructions.Count;

        Instruction instr = instructions[startPc];

        switch (instr.Type)
        {
            case "OPTION":
            {
                int nextPc = startPc + 1;
                // Als het direct gevolgd wordt door een OPTION, is de actie leeg.
                if (nextPc < instructions.Count && instructions[nextPc].Type == "OPTION")
                {
                    return nextPc;
                }
                // Anders is het blok: OPTION + het blok dat erna komt
                return GetNextBlock(nextPc);
            }

            case "IF":
            case "IF_NOT":
            {
                int nextPc = startPc + 1;
                // Blok is de IF + het blok dat erna komt
                return GetNextBlock(nextPc);
            }

            default:
                // Normale instructie met parameter is 1 stap groot
                return startPc + 1;
        }
    }

    /// <summary>
    /// Berekent het einde van een hele keten van opeenvolgende OPTION-blokken.
    /// </summary>
    private int GetEndOfOptionsChain(int startPc)
    {
        int currentPc = startPc;
        while (currentPc < instructions.Count && instructions[currentPc].Type == "OPTION")
        {
            currentPc = GetNextBlock(currentPc);
        }
        return currentPc;
    }

    // ==========================================
    // FASE 2: EXECUTION LOOP (VM)
    // ==========================================
    private IEnumerator RunDialogueRoutine()
    {
        int pc = 0;
        HashSet<string> variables = new HashSet<string>();

        while (pc < instructions.Count)
        {
            Instruction instr = instructions[pc];

            // Als we een OPTION tegenkomen, verzamelen we de hele keten
            if (instr.Type == "OPTION")
            {
                int optionChainStartPc = pc;
                int endOfOptionBlockPc = GetEndOfOptionsChain(optionChainStartPc);

                List<(Instruction optInstr, int targetPc)> options = new List<(Instruction, int)>();

                // Scan alle opties in de keten
                int scanPc = optionChainStartPc;
                while (scanPc < endOfOptionBlockPc && scanPc < instructions.Count && instructions[scanPc].Type == "OPTION")
                {
                    options.Add((instructions[scanPc], scanPc + 1));
                    scanPc = GetNextBlock(scanPc);
                }

                selectedOptionNextInstructionIndex = -1;
                ShowOptionsUI(options);

                yield return new WaitUntil(() => selectedOptionNextInstructionIndex != -1);

                ClearOptionsUI();

                int chosenActionStartPc = selectedOptionNextInstructionIndex;
                int chosenActionEndPc = GetNextBlock(chosenActionStartPc);

                // Als de gekozen actie niet direct een nieuwe OPTION is, voer deze uit
                if (chosenActionStartPc < instructions.Count && instructions[chosenActionStartPc].Type != "OPTION")
                {
                    pc = chosenActionStartPc;

                    while (pc < chosenActionEndPc && pc < instructions.Count)
                    {
                        Instruction subInstr = instructions[pc];

                        if (subInstr.Type == "IF")
                        {
                            if (variables.Contains(subInstr.VarName)) pc++;
                            else pc = GetNextBlock(pc);
                        }
                        else if (subInstr.Type == "IF_NOT")
                        {
                            if (!variables.Contains(subInstr.VarName)) pc++;
                            else pc = GetNextBlock(pc);
                        }
                        else if (subInstr.Type == "SAY")
                        {
                            if (dialogueTextLabel != null) dialogueTextLabel.text = subInstr.Text;
                            if (continueButton != null) continueButton.style.display = DisplayStyle.Flex;

                            advanceRequested = false;
                            yield return new WaitUntil(() => advanceRequested);

                            if (continueButton != null) continueButton.style.display = DisplayStyle.None;
                            pc++;
                        }
                        else if (subInstr.Type == "CLICKED")
                        {
                            clickedRequested = true;
                            lastClickedItem = subInstr.ItemName;
                            yield return new WaitUntil(() => !clickedRequested);
                            pc++;
                        }
                        else if (subInstr.Type == "JUMP")
                        {
                            if (labels.TryGetValue(subInstr.Target, out int targetPc))
                            {
                                pc = targetPc;
                            }
                            break;
                        }
                        else
                        {
                            ExecuteSingleInstruction(subInstr, variables);
                            pc++;
                        }
                    }
                }

                // Spring altijd naar het berekende einde van het complete optieblok
                if (pc < instructions.Count && instructions[pc].Type != "JUMP")
                {
                    pc = endOfOptionBlockPc;
                }

                continue;
            }

            // INSTRUCTION SWITCH VOOR NORMALE COMMANDO'S
            switch (instr.Type)
            {
                case "SAY":
                    if (dialogueTextLabel != null) dialogueTextLabel.text = instr.Text;
                    if (continueButton != null) continueButton.style.display = DisplayStyle.Flex;

                    advanceRequested = false;
                    yield return new WaitUntil(() => advanceRequested);

                    if (continueButton != null) continueButton.style.display = DisplayStyle.None;
                    pc++;
                    break;

                case "CLICKED":
                    clickedRequested = true;
                    lastClickedItem = instr.ItemName;
                    yield return new WaitUntil(() => !clickedRequested);
                    pc++;
                    break;

                case "SHOW":
                    ToggleUIElementVisibility(instr.ItemName, DisplayStyle.Flex);
                    pc++;
                    break;

                case "HIDE":
                    ToggleUIElementVisibility(instr.ItemName, DisplayStyle.None);
                    pc++;
                    break;

                case "IF":
                    if (variables.Contains(instr.VarName))
                    {
                        pc++;
                    }
                    else
                    {
                        pc = GetNextBlock(pc);
                    }
                    break;

                case "IF_NOT":
                    if (!variables.Contains(instr.VarName))
                    {
                        pc++;
                    }
                    else
                    {
                        pc = GetNextBlock(pc);
                    }
                    break;

                case "SET":
                    variables.Add(instr.VarName);
                    pc++;
                    break;

                case "UNSET":
                    variables.Remove(instr.VarName);
                    pc++;
                    break;

                case "LABEL":
                    pc++;
                    break;

                case "JUMP":
                    if (labels.TryGetValue(instr.Target, out int targetPc))
                        pc = targetPc;
                    else
                        pc++;
                    break;

                default:
                    pc++;
                    break;
            }
        }

        rootElement.style.display = DisplayStyle.None;
        activeDialogueCoroutine = null;
    }

    private void ExecuteSingleInstruction(Instruction instr, HashSet<string> variables)
    {
        switch (instr.Type)
        {
            case "SET":
                variables.Add(instr.VarName);
                break;
            case "UNSET":
                variables.Remove(instr.VarName);
                break;
            case "SHOW":
                ToggleUIElementVisibility(instr.ItemName, DisplayStyle.Flex);
                break;
            case "HIDE":
                ToggleUIElementVisibility(instr.ItemName, DisplayStyle.None);
                break;
        }
    }

    // ==========================================
    // UI HELPERS
    // ==========================================
    private void ToggleUIElementVisibility(string elementName, DisplayStyle displayStyle)
    {
        if (uiDocument == null || uiDocument.rootVisualElement == null) return;
        VisualElement elem = uiDocument.rootVisualElement.Q<VisualElement>(elementName);
        if (elem != null)
        {
            elem.style.display = displayStyle;
        }
    }

    private void ShowOptionsUI(List<(Instruction optInstr, int targetPc)> options)
    {
        if (continueButton != null) continueButton.style.display = DisplayStyle.None;

        foreach (var opt in options)
        {
            Button btn = new Button();
            btn.text = opt.optInstr.Text;
            
            if (optionButtonTemplate != null)
            {
                btn.style.height = optionButtonTemplate.style.height;
                btn.style.backgroundColor = optionButtonTemplate.style.backgroundColor;
                btn.style.borderTopLeftRadius = optionButtonTemplate.style.borderTopLeftRadius;
                btn.style.borderTopRightRadius = optionButtonTemplate.style.borderTopRightRadius;
                btn.style.borderBottomLeftRadius = optionButtonTemplate.style.borderBottomLeftRadius;
                btn.style.borderBottomRightRadius = optionButtonTemplate.style.borderBottomRightRadius;
                btn.style.color = optionButtonTemplate.style.color;
            }
            
            btn.style.marginTop = 2;
            btn.style.marginBottom = 4;

            int targetInstructionIndex = opt.targetPc;
            btn.clicked += () => { selectedOptionNextInstructionIndex = targetInstructionIndex; };

            optionsContainer.Add(btn);
        }
    }

    private void ClearOptionsUI()
    {
        if (optionsContainer == null) return;
        optionsContainer.Clear();
        if (optionButtonTemplate != null)
        {
            optionsContainer.Add(optionButtonTemplate);
            optionButtonTemplate.style.display = DisplayStyle.None;
        }
    }
}