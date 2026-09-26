using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using MoonSharp.Interpreter;

public class LuaDialogueManager : MonoBehaviour
{
    [Header("Script Input Source")]
    public StoryEditor storyEditor; // Sleep hier de StoryEditor GameObject in

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
    private string selectedOptionTarget = null;
    private UnityEngine.Coroutine activeDialogueCoroutine = null;

    // MoonSharp instance (eenmalig aangemaakt)
    private Script moonSharpScript;

    // Data Structures
    public class Instruction
    {
        public string Type;
        public string Speaker;
        public string Text;
        public string Target;
        public string VarName;
        public bool BoolValue;
    }

    private List<Instruction> instructions = new List<Instruction>();
    private Dictionary<string, int> labels = new Dictionary<string, int>();

    void Awake()
    {
        // Initieer de MoonSharp omgeving eenmalig om GC allocaties bij herstarten te minimaliseren
        moonSharpScript = new Script();
        moonSharpScript.Globals["print"] = (System.Action<string>)((text) => Debug.Log($"[Lua] {text}"));
        moonSharpScript.Globals["say"] = (System.Action<string, string>)LuaSay;
        moonSharpScript.Globals["option"] = (System.Action<string, string>)LuaOption;
        moonSharpScript.Globals["label"] = (System.Action<string>)LuaLabel;
        moonSharpScript.Globals["jump"] = (System.Action<string>)LuaJump;
        moonSharpScript.Globals["jumpif"] = (System.Action<string, string>)LuaJumpIf;
        moonSharpScript.Globals["set"] = (System.Action<string, bool>)LuaSet;
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

        // Haal de code op via de StoryEditor, of gebruik de fallback
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
        selectedOptionTarget = null;
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

        continueButton.clicked += OnContinueClicked;
        
        // Verberg elementen initieel
        continueButton.style.display = DisplayStyle.None;
        optionButtonTemplate.style.display = DisplayStyle.None;
    }

    private void OnContinueClicked()
    {
        advanceRequested = true;
    }

    // ==========================================
    // FASE 1: BUILDER / PARSING VIA MOONSHARP
    // ==========================================
    private void ParseLuaScript(string scriptSource)
    {
        instructions.Clear();
        labels.Clear();

        moonSharpScript.DoString(scriptSource);
    }

    private void LuaSay(string speaker, string text)
    {
        instructions.Add(new Instruction { Type = "SAY", Speaker = speaker, Text = text });
    }

    private void LuaOption(string text, string targetLabel)
    {
        instructions.Add(new Instruction { Type = "OPTION", Text = text, Target = targetLabel });
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

    private void LuaJumpIf(string varName, string targetLabel)
    {
        instructions.Add(new Instruction { Type = "JUMP_IF", VarName = varName, Target = targetLabel });
    }

    private void LuaSet(string varName, bool value)
    {
        instructions.Add(new Instruction { Type = "SET", VarName = varName, BoolValue = value });
    }

    // ==========================================
    // FASE 2: EXECUTION LOOP (VM)
    // ==========================================
    private IEnumerator RunDialogueRoutine()
    {
        int pc = 0;
        Dictionary<string, bool> variables = new Dictionary<string, bool>();
        List<Instruction> pendingOptions = new List<Instruction>();

        while (pc < instructions.Count)
        {
            Instruction instr = instructions[pc];

            // 1. CHECK FOR PENDING OPTIONS (Flush voor elk niet-OPTION commando)
            if (pendingOptions.Count > 0 && instr.Type != "OPTION")
            {
                selectedOptionTarget = null;
                ShowOptionsUI(pendingOptions);

                // Wacht tot de gebruiker een optie aanklikt
                yield return new WaitUntil(() => selectedOptionTarget != null);

                ClearOptionsUI();
                pendingOptions.Clear();

                // Spring naar het gekozen label (overschrijft huidige instructie)
                if (labels.TryGetValue(selectedOptionTarget, out int jumpPc))
                {
                    pc = jumpPc;
                    continue;
                }
                else
                {
                    Debug.LogError($"Label '{selectedOptionTarget}' niet gevonden!");
                    break;
                }
            }

            // 2. INSTRUCTION SWITCH
            switch (instr.Type)
            {
                case "SAY":
                    characterNameLabel.text = instr.Speaker;
                    dialogueTextLabel.text = instr.Text;
                    continueButton.style.display = DisplayStyle.Flex;

                    advanceRequested = false;
                    yield return new WaitUntil(() => advanceRequested);

                    continueButton.style.display = DisplayStyle.None;
                    pc++;
                    break;

                case "OPTION":
                    pendingOptions.Add(instr);
                    pc++;
                    break;

                case "LABEL":
                    // Een label fungeert als marker in de VM; ga direct door
                    pc++;
                    break;

                case "JUMP":
                    if (labels.TryGetValue(instr.Target, out int targetPc))
                        pc = targetPc;
                    else
                        pc++;
                    break;

                case "JUMP_IF":
                    if (variables.TryGetValue(instr.VarName, out bool val) && val)
                    {
                        if (labels.TryGetValue(instr.Target, out int jumpIfPc))
                            pc = jumpIfPc;
                        else
                            pc++;
                    }
                    else
                    {
                        pc++;
                    }
                    break;

                case "SET":
                    variables[instr.VarName] = instr.BoolValue;
                    pc++;
                    break;

                default:
                    pc++;
                    break;
            }
        }

        // Einde dialoog
        rootElement.style.display = DisplayStyle.None;
        activeDialogueCoroutine = null;
    }

    // ==========================================
    // UI HELPERS
    // ==========================================
    private void ShowOptionsUI(List<Instruction> options)
    {
        continueButton.style.display = DisplayStyle.None;

        foreach (var opt in options)
        {
            Button btn = new Button();
            btn.text = opt.Text;
            btn.style.height = optionButtonTemplate.style.height;
            btn.style.backgroundColor = optionButtonTemplate.style.backgroundColor;
            btn.style.borderTopLeftRadius = optionButtonTemplate.style.borderTopLeftRadius;
            btn.style.borderTopRightRadius = optionButtonTemplate.style.borderTopRightRadius;
            btn.style.borderBottomLeftRadius = optionButtonTemplate.style.borderBottomLeftRadius;
            btn.style.borderBottomRightRadius = optionButtonTemplate.style.borderBottomRightRadius;
            btn.style.color = optionButtonTemplate.style.color;
            btn.style.marginTop = 2;
            btn.style.marginBottom = 4;

            string target = opt.Target;
            btn.clicked += () => { selectedOptionTarget = target; };

            optionsContainer.Add(btn);
        }
    }

    private void ClearOptionsUI()
    {
        optionsContainer.Clear();
        optionsContainer.Add(optionButtonTemplate);
        optionButtonTemplate.style.display = DisplayStyle.None;
    }
}