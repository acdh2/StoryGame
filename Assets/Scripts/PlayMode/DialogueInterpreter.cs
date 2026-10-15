using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

using Lua;
using Lua.Standard;

public class DialogueInterpreter : MonoBehaviour
{
    public event Action OnDialogueStarted;
    public event Action OnDialogueEnded;
    public event Action OnScreenShown;
    public event Action OnScreenHidden;
    public event Action<bool, string> OnCameraChangeRequested;

    [Header("Script Input Source")]
    public StoryDataStore storyDataStore;

    private VisualElement rootElement;
    private Label characterNameLabel;
    private Label dialogueTextLabel;
    private VisualElement optionsContainer;
    private Button optionButtonTemplate;
    private Button continueButton;

    private bool advanceRequested = false;
    private int selectedIndex = -1;
    private bool touchedRequested = false;
    private string lastTouchedItem = "";
    private UnityEngine.Coroutine activeDialogueCoroutine = null;

    // ------------------------------------------------------------------
    //  Instructies
    // ------------------------------------------------------------------

    public class Instruction
    {
        public string Type;
        public string Argument;

        // Sprongdoel (instructie-index) voor voorwaarden, JUMP, GOTO en WAITFORCHOICE.
        // Voorwaarden springen hier naartoe als ze ONWAAR zijn.
        public int Target = -1;

        // SHOWOPTION: index van het menu-item.
        public int Index;

        // WAITFORCHOICE: per menu-item de instructie-index van de body.
        public int[] Targets;
    }

    private List<Instruction> instructions = new List<Instruction>();

    // ------------------------------------------------------------------
    //  AST
    //
    //  Regels van de definitie:
    //  - Voorwaarden (HAS, DONT HAVE, VAR_IS_SET, VAR_IS_NOT_SET) nemen precies
    //    het volgende statement als kind. Dat kind mag zelf een voorwaarde zijn.
    //  - Een keten van voorwaarden eindigt in een actie, of in een option.
    //    Eindigt hij in een option, dan is het een voorwaardelijk menu-item.
    //  - Opeenvolgende options vormen één menu (siblings).
    //  - De body van een option is een keten die in een actie eindigt.
    //    Volgt er direct een option (of een chapter), dan is de body leeg.
    //  - CHAPTER is een label en hoort op het hoogste niveau.
    // ------------------------------------------------------------------

    private abstract class Node { }

    private class ActionNode : Node
    {
        public CommandData Cmd;
    }

    private class CondNode : Node
    {
        public CommandData Cond;
        public Node Child;
    }

    private class MenuItem
    {
        public string Text;
        public List<CommandData> Guards = new List<CommandData>();
        public Node Body; // null = lege optie
    }

    private class MenuNode : Node
    {
        public List<MenuItem> Items = new List<MenuItem>();
    }

    // ------------------------------------------------------------------
    //  Parser
    // ------------------------------------------------------------------

    private List<CommandData> commands = new List<CommandData>();
    private int pos;

    private static bool IsGuard(CommandData c)
    {
        return c.CommandType == "HAS"
            || c.CommandType == "DONT HAVE"
            || c.CommandType == "VAR_IS_SET"
            || c.CommandType == "VAR_IS_NOT_SET";
    }

    private static bool IsOption(CommandData c) { return c.CommandType == "CHOICE"; }
    private static bool IsChapter(CommandData c) { return c.CommandType == "CHAPTER"; }

    // Geeft de index van de eerste regel vanaf i die geen voorwaarde is.
    private int SkipGuards(int i)
    {
        while (i < commands.Count && IsGuard(commands[i])) i++;
        return i;
    }

    private Node ParseStatement()
    {
        int leaf = SkipGuards(pos);

        if (leaf >= commands.Count)
        {
            Debug.LogWarning("Dialogue: voorwaarde zonder commando aan het einde van het script.");
            pos = commands.Count;
            return null;
        }

        if (IsOption(commands[leaf]))
            return ParseMenu();

        if (IsChapter(commands[leaf]) && leaf != pos)
            Debug.LogWarning($"Dialogue: chapter \"{commands[leaf].Argument}\" staat achter een voorwaarde. Een chapter hoort op het hoogste niveau.");

        return ParseChain();
    }

    // Voorwaarden* gevolgd door precies één actie.
    private Node ParseChain()
    {
        CommandData c = commands[pos++];

        if (IsGuard(c))
        {
            // De aanroeper heeft gecontroleerd dat er na de voorwaarden nog een regel komt.
            return new CondNode { Cond = c, Child = ParseChain() };
        }

        return new ActionNode { Cmd = c };
    }

    private MenuNode ParseMenu()
    {
        MenuNode menu = new MenuNode();

        while (pos < commands.Count)
        {
            int leaf = SkipGuards(pos);
            if (leaf >= commands.Count || !IsOption(commands[leaf]))
                break; // het menu is afgelopen

            MenuItem item = new MenuItem();

            // voorwaarden vóór de option = guards van dit menu-item
            while (pos < leaf)
                item.Guards.Add(commands[pos++]);

            item.Text = commands[pos++].Argument;

            // body: een keten die in een actie eindigt
            int bodyLeaf = SkipGuards(pos);
            if (bodyLeaf < commands.Count &&
                !IsOption(commands[bodyLeaf]) &&
                !IsChapter(commands[bodyLeaf]))
            {
                item.Body = ParseChain();
            }
            // anders: lege optie (de optie wordt wel getoond)

            menu.Items.Add(item);
        }

        return menu;
    }

    // ------------------------------------------------------------------
    //  Emitter
    // ------------------------------------------------------------------

    // Een label is een id; de echte positie wordt pas in ResolveLabels ingevuld.
    private List<int> labelPositions = new List<int>();

    private Dictionary<string, int> chapterLabels =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    private int NewLabel()
    {
        labelPositions.Add(-1);
        return labelPositions.Count - 1;
    }

    private void Mark(int label)
    {
        labelPositions[label] = instructions.Count;
    }

    private int ChapterLabel(string name)
    {
        if (!chapterLabels.TryGetValue(name, out int label))
        {
            label = NewLabel();
            chapterLabels[name] = label;
        }
        return label;
    }

    private Instruction Emit(string type, string arg = "", int target = -1)
    {
        Instruction ins = new Instruction
        {
            Type = type,
            Argument = arg ?? "",
            Target = target
        };
        instructions.Add(ins);
        return ins;
    }

    private void EmitNode(Node node)
    {
        switch (node)
        {
            case ActionNode a:
                if (a.Cmd.CommandType == "CHAPTER")
                    Mark(ChapterLabel(a.Cmd.Argument));
                else
                    Emit(a.Cmd.CommandType, a.Cmd.Argument);
                break;

            case CondNode c:
            {
                // Geïnverteerd: springt naar 'end' als de voorwaarde onwaar is.
                int end = NewLabel();
                Emit(c.Cond.CommandType, c.Cond.Argument, end);
                EmitNode(c.Child);
                Mark(end);
                break;
            }

            case MenuNode m:
                EmitMenu(m);
                break;
        }
    }

    private void EmitMenu(MenuNode m)
    {
        int end = NewLabel();
        int[] bodyLabels = new int[m.Items.Count];

        // 1. Items tonen. Een guard slaat alleen het tonen van dat item over.
        for (int i = 0; i < m.Items.Count; i++)
        {
            bodyLabels[i] = NewLabel();
            int skip = NewLabel();

            foreach (CommandData g in m.Items[i].Guards)
                Emit(g.CommandType, g.Argument, skip);

            Emit("SHOWOPTION", m.Items[i].Text).Index = i;
            Mark(skip);
        }

        // 2. Wachten op keuze. Zijn er geen zichtbare opties, dan springt hij naar het einde.
        Instruction wait = Emit("WAITFORCHOICE", "", end);
        wait.Targets = bodyLabels;

        // 3. Bodies. Elke body eindigt met een sprong naar het einde van het menu.
        for (int i = 0; i < m.Items.Count; i++)
        {
            Mark(bodyLabels[i]);

            if (m.Items[i].Body != null)
                EmitNode(m.Items[i].Body);

            Emit("JUMP", "", end);
        }

        Mark(end);
    }

    private void ResolveLabels()
    {
        foreach (Instruction ins in instructions)
        {
            if (ins.Type == "GOTO")
            {
                ins.Target = chapterLabels.TryGetValue(ins.Argument, out int l)
                    ? labelPositions[l]
                    : -1;
            }
            else if (ins.Target >= 0)
            {
                ins.Target = labelPositions[ins.Target];
            }

            if (ins.Targets != null)
            {
                for (int i = 0; i < ins.Targets.Length; i++)
                    ins.Targets[i] = labelPositions[ins.Targets[i]];
            }
        }
    }

    private void ParseCommands()
    {
        instructions.Clear();
        labelPositions.Clear();
        chapterLabels.Clear();
        commands.Clear();

        if (storyDataStore == null ||
            storyDataStore.Commands == null)
            return;

        // Normaliseren: type in hoofdletters, aanhalingstekens van het argument af.
        foreach (CommandData raw in storyDataStore.Commands)
        {
            if (raw == null) continue;

            string type = NormalizeType(raw.CommandType);
            if (type == "") continue;

            commands.Add(new CommandData
            {
                CommandType = type,
                Argument = NormalizeArgument(raw.Argument)
            });
        }

        pos = 0;
        while (pos < commands.Count)
        {
            Node node = ParseStatement();
            if (node != null)
                EmitNode(node);
        }

        ResolveLabels();
    }

    // ------------------------------------------------------------------
    //  Lua (nog niet in gebruik)
    // ------------------------------------------------------------------

    private LuaState state;

    private void InvokeMatchingMethod(Type type, object instance, LuaFunctionExecutionContext context, string methodName)
    {
        var bindingFlags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                        (instance == null ? System.Reflection.BindingFlags.Static : System.Reflection.BindingFlags.Instance);

        var methods = type.GetMethods(bindingFlags);
        foreach (var method in methods)
        {
            if (method.Name == methodName)
            {
                var parameters = method.GetParameters();
                var args = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    args[i] = context.GetArgument<object>(i + 3);
                }
                method.Invoke(instance, args);
                break;
            }
        }
    }

    private void StartLua()
    {
        state = LuaState.Create();

        state.Environment["unity"] = new LuaFunction((context, ct) => {
            string targetName = context.GetArgument<string>(0);
            string componentName = context.GetArgument<string>(1);
            string methodName = context.GetArgument<string>(2);

            // Zoek alle game-objecten (inclusief inactieve)
            var allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            bool foundObject = false;

            foreach (var go in allObjects)
            {
                if (go.name == targetName)
                {
                    var comp = go.GetComponent(componentName);
                    if (comp != null)
                    {
                        InvokeMatchingMethod(comp.GetType(), comp, context, methodName);
                        foundObject = true;
                    }
                }
            }

            // Fallback naar static/singleton klasse via Type
            if (!foundObject)
            {
                Type type = Type.GetType(targetName) ?? Type.GetType($"{targetName}, Assembly-CSharp");
                if (type != null)
                {
                    InvokeMatchingMethod(type, null, context, methodName);
                }
            }

            return new(context.Return());
        });

        state.OpenStandardLibraries();
    }

    private void StopLua()
    {
        if (state != null)
        {
            state.Dispose();
            state = null;
        }
    }

    // ------------------------------------------------------------------
    //  Scherm en levenscyclus
    // ------------------------------------------------------------------

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

        //StartLua();

        BindUIElements(rootElement);

        ResetDialogueState();

        ShowScreen();

        ParseCommands();

        activeDialogueCoroutine =
            StartCoroutine(RunDialogueRoutine());

        OnDialogueStarted?.Invoke();
    }

    // Ruimt een lopende dialoog op, zonder events af te vuren.
    private void ResetDialogueState()
    {
        if (activeDialogueCoroutine != null)
        {
            StopCoroutine(activeDialogueCoroutine);
            activeDialogueCoroutine = null;
        }

        advanceRequested = false;
        selectedIndex = -1;
        touchedRequested = false;

        ClearOptionsUI();
    }

    public void StopDialogue()
    {
        //StopLua();

        ResetDialogueState();
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
        if (itemName == "") return;
        if (touchedRequested &&
            string.Equals(
                lastTouchedItem,
                itemName,
                StringComparison.OrdinalIgnoreCase))
        {
            touchedRequested = false;
        }
    }

    private string ProcessString(string input)
    {
        return System.Text.RegularExpressions.Regex.Unescape(input);
    }

    // Commando's die zonder argument niets kunnen doen: die worden overgeslagen.
    private static bool RequiresArgument(string type)
    {
        switch (type)
        {
            case "HAS":
            case "DONT HAVE":
            case "VAR_IS_SET":
            case "VAR_IS_NOT_SET":
            case "TOUCHED":
            case "BLOCK":
            case "UNBLOCK":
            case "PICKUP":
            case "DROP":
            case "SHOW":
            case "HIDE":
            case "TELEPORT":
            case "SET_VAR":
            case "UNSET_VAR":
                return true;
            default:
                return false;
        }
    }

    // ------------------------------------------------------------------
    //  Runtime
    // ------------------------------------------------------------------

    private IEnumerator RunDialogueRoutine()
    {
        int pc = 0;

        HashSet<string> variables =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        // (index in het menu, tekst) van de opties die nu zichtbaar zijn
        List<(int index, string text)> visibleOptions =
            new List<(int, string)>();

        while (pc >= 0 && pc < instructions.Count)
        {
            Instruction instr =
                instructions[pc];

            // Een leeg argument bij een commando dat er een nodig heeft: overslaan.
            if (instr.Argument == "" && RequiresArgument(instr.Type))
            {
                pc++;
                continue;
            }

            switch (instr.Type)
            {
                case "SAY":
                case "TEXT":
                {
                    string text = ProcessString(instr.Argument);
                    MatchCollection matches = Regex.Matches(text, @"\[(.*?)\]([^\[]+)");

                    List<(string speaker, string dialogue)> dialogueList = new List<(string, string)>();

                    if (matches.Count > 0)
                    {
                        foreach (Match match in matches)
                        {
                            string speaker = match.Groups[1].Value.Trim();
                            string dialogue = match.Groups[2].Value.Trim();
                            dialogueList.Add((speaker, dialogue));
                        }
                    }
                    else
                    {
                        dialogueList.Add(("", text.Trim()));
                    }

                    foreach (var item in dialogueList)
                    {
                        string speaker = item.speaker;
                        string dialogue = item.dialogue;

                        if (characterNameLabel != null)
                        {
                            characterNameLabel.text = speaker;
                            characterNameLabel.style.display = string.IsNullOrEmpty(speaker) ? DisplayStyle.None : DisplayStyle.Flex;
                        }

                        if (dialogueTextLabel != null)
                            dialogueTextLabel.text = dialogue;

                        if (instr.Type == "SAY") OnCameraChangeRequested?.Invoke(true, speaker);

                        if (continueButton != null) continueButton.style.display = DisplayStyle.Flex;
                        advanceRequested = false;

                        float timer = 0f;
                        float duration = 2f;

                        while (timer < duration && !advanceRequested)
                        {
                            timer += Time.deltaTime;
                            yield return null;
                        }

                        if (continueButton != null) continueButton.style.display = DisplayStyle.None;
                        if (instr.Type == "SAY") OnCameraChangeRequested?.Invoke(false, speaker);
                    }

                    pc++;
                    break;
                }

                // ---- menu ----

                case "SHOWOPTION":
                    visibleOptions.Add((instr.Index, instr.Argument));
                    pc++;
                    break;

                case "WAITFORCHOICE":
                    if (visibleOptions.Count == 0)
                    {
                        // Alle guards onwaar: het menu wordt overgeslagen.
                        pc = instr.Target;
                        break;
                    }

                    selectedIndex = -1;
                    ShowOptionsUI(visibleOptions);

                    yield return new WaitUntil(
                        () => selectedIndex >= 0);

                    ClearOptionsUI();
                    visibleOptions.Clear();

                    pc = instr.Targets[selectedIndex];
                    break;

                case "JUMP":
                    pc = instr.Target;
                    break;

                // ---- voorwaarden: springen naar Target als ze onwaar zijn ----

                case "HAS":
                    pc = HasItem(instr.Argument) ? pc + 1 : instr.Target;
                    break;

                case "DONT HAVE":
                    pc = !HasItem(instr.Argument) ? pc + 1 : instr.Target;
                    break;

                case "VAR_IS_SET":
                    pc = variables.Contains(instr.Argument) ? pc + 1 : instr.Target;
                    break;

                case "VAR_IS_NOT_SET":
                    pc = !variables.Contains(instr.Argument) ? pc + 1 : instr.Target;
                    break;

                // ---- acties ----

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

                case "PICKUP":
                    PickupItem(instr.Argument);

                    pc++;
                    break;

                case "DROP":
                    DropItem(instr.Argument);

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

                case "CREATE_VAR":
                    pc++;
                    break;

                case "SET_VAR":
                    variables.Add(
                        instr.Argument);

                    pc++;
                    break;

                case "UNSET_VAR":
                    variables.Remove(
                        instr.Argument);

                    pc++;
                    break;

                case "TELEPORT":
                    TeleportTo(instr.Argument);

                    pc++;
                    break;

                case "GOTO":
                    if (instr.Target >= 0)
                    {
                        pc = instr.Target;
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
                    Debug.LogWarning("?unknown command: " + instr.Type);
                    pc++;
                    break;
            }
        }

        HideScreen();

        activeDialogueCoroutine = null;

        OnDialogueEnded?.Invoke();
    }

    // ------------------------------------------------------------------
    //  Scene-helpers
    // ------------------------------------------------------------------

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
        List<(int index, string text)> options)
    {
        if (continueButton != null)
            continueButton.style.display =
                DisplayStyle.None;

        foreach (var opt in options)
        {
            Button btn = new Button();

            btn.text = opt.text;

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

            // De keuze is een index in het menu, geen tekst. Zo werken dubbele opties.
            int indexToSelect = opt.index;

            btn.clicked += () =>
            {
                selectedIndex = indexToSelect;
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

    private void PickupItem(string item)
    {
        PickupItemBehaviour[] pickupItemBehaviours = FindObjectsByType<PickupItemBehaviour>(FindObjectsSortMode.None);

        foreach (PickupItemBehaviour pickupItemBehaviour in pickupItemBehaviours)
        {
            pickupItemBehaviour.Pickup(item);
        }
    }

    private void DropItem(string item)
    {
        PickupItemBehaviour[] pickupItemBehaviours = FindObjectsByType<PickupItemBehaviour>(FindObjectsSortMode.None);

        foreach (PickupItemBehaviour pickupItemBehaviour in pickupItemBehaviours)
        {
            pickupItemBehaviour.Drop(item);
        }
    }

    private bool HasItem(string item)
    {
        PickupItemBehaviour[] pickupItemBehaviours = FindObjectsByType<PickupItemBehaviour>(FindObjectsSortMode.None);

        foreach (PickupItemBehaviour pickupItemBehaviour in pickupItemBehaviours)
        {
            if (pickupItemBehaviour.Has(item)) return true;
        }
        return false;
    }

    private void TeleportTo(string target)
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

// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Text.RegularExpressions;
// using UnityEngine;
// using UnityEngine.UIElements;

// using Lua;
// using Lua.Standard;
// using UnityEngine.InputSystem;
// using Unity.VisualScripting;
// using StarterAssets;

// public class DialogueInterpreter : MonoBehaviour
// {
//     public event Action OnDialogueStarted;
//     public event Action OnDialogueEnded;
//     public event Action OnScreenShown;
//     public event Action OnScreenHidden;
//     public event Action<bool, string> OnCameraChangeRequested;

//     [Header("Script Input Source")]
//     public StoryDataStore storyDataStore;

//     private VisualElement rootElement;
//     private Label characterNameLabel;
//     private Label dialogueTextLabel;
//     private VisualElement optionsContainer;
//     private Button optionButtonTemplate;
//     private Button continueButton;

//     private bool advanceRequested = false;
//     private string selectedOptionText = "";
//     private bool touchedRequested = false;
//     private string lastTouchedItem = "";
//     private UnityEngine.Coroutine activeDialogueCoroutine = null;

//     public class Instruction
//     {
//         public string Type;
//         public string Argument;
//     }

//     private class ParsedLine
//     {
//         public int Indentation;
//         public CommandData Command;
//     }

//     private List<Instruction> instructions = new List<Instruction>();

//     private Dictionary<string, int> labels =
//         new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

//     private int autoLabelCounter = 0;

//     private LuaState state;


//     private void InvokeMatchingMethod(Type type, object instance, LuaFunctionExecutionContext context, string methodName)
//     {
//         var bindingFlags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
//                         (instance == null ? System.Reflection.BindingFlags.Static : System.Reflection.BindingFlags.Instance);

//         var methods = type.GetMethods(bindingFlags);
//         foreach (var method in methods)
//         {
//             if (method.Name == methodName)
//             {
//                 var parameters = method.GetParameters();
//                 var args = new object[parameters.Length];
//                 for (int i = 0; i < parameters.Length; i++)
//                 {
//                     args[i] = context.GetArgument<object>(i + 3);
//                 }
//                 method.Invoke(instance, args);
//                 break;
//             }
//         }
//     }
//     // void Awake()
//     // {
//     //     var state = LuaState.Create();
//     //     state.Environment["call_unity"] = new LuaFunction((context, ct) =>
//     //     {
//     //         string objectName = context.GetArgument<string>(0);
//     //         string componentName = context.GetArgument<string>(1);
//     //         string methodName = context.GetArgument<string>(2);

//     //         var go = GameObject.Find(objectName);
//     //         if (go != null)
//     //         {
//     //             var comp = go.GetComponent(componentName);
//     //             if (comp != null)
//     //             {
//     //                 var method = comp.GetType().GetMethod(methodName);
//     //                 if (method != null)
//     //                 {
//     //                     var parameters = method.GetParameters();
//     //                     var args = new object[parameters.Length];
//     //                     for (int i = 0; i < parameters.Length; i++)
//     //                     {
//     //                         args[i] = context.GetArgument<object>(i + 3);
//     //                     }
//     //                     method.Invoke(comp, args);
//     //                 }
//     //             }
//     //         }
//     //         return new(context.Return());
//     //     });
//     //     state.OpenStandardLibraries();
//     //     state.DoStringAsync("call_unity('TestObject', 'TestComponent', 'TestMethod')");
//     // }

//     private void ShowScreen()
//     {
//         if (rootElement != null &&
//             rootElement.style.display != DisplayStyle.Flex)
//         {
//             rootElement.style.display = DisplayStyle.Flex;
//             OnScreenShown?.Invoke();
//         }
//     }

//     private void HideScreen()
//     {
//         if (rootElement != null &&
//             rootElement.style.display != DisplayStyle.None)
//         {
//             rootElement.style.display = DisplayStyle.None;
//             OnScreenHidden?.Invoke();
//         }
//     }

//     public void StartDialogue(VisualElement root)
//     {
//         rootElement = root;

//         if (root == null)
//             return;

//         //StartLua();

//         BindUIElements(rootElement);

//         StopDialogue();

//         ShowScreen();

//         ParseCommands();

//         activeDialogueCoroutine =
//             StartCoroutine(RunDialogueRoutine());

//         OnDialogueStarted?.Invoke();
//     }

//     private void StartLua()
//     {
//         state = LuaState.Create();
        
//         state.Environment["unity"] = new LuaFunction((context, ct) => {
//             string targetName = context.GetArgument<string>(0);
//             string componentName = context.GetArgument<string>(1);
//             string methodName = context.GetArgument<string>(2);

//             // Zoek alle game-objecten (inclusief inactieve)
//             var allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
//             bool foundObject = false;

//             foreach (var go in allObjects)
//             {
//                 if (go.name == targetName)
//                 {
//                     var comp = go.GetComponent(componentName);
//                     if (comp != null)
//                     {
//                         InvokeMatchingMethod(comp.GetType(), comp, context, methodName);
//                         foundObject = true;
//                     }
//                 }
//             }

//             // Fallback naar static/singleton klasse via Type
//             if (!foundObject)
//             {
//                 Type type = Type.GetType(targetName) ?? Type.GetType($"{targetName}, Assembly-CSharp");
//                 if (type != null)
//                 {
//                     InvokeMatchingMethod(type, null, context, methodName);
//                 }
//             }

//             return new(context.Return());
//         });

//         state.OpenStandardLibraries();
//     }

//     private void StopLua()
//     {
//         if (state != null) {
//             state.Dispose();
//             state = null;
//         }        
//     }

//     public void StopDialogue()
//     {
//         //StopLua();

//         if (activeDialogueCoroutine != null)
//         {
//             StopCoroutine(activeDialogueCoroutine);
//             activeDialogueCoroutine = null;
//         }

//         advanceRequested = false;
//         selectedOptionText = "";
//         touchedRequested = false;

//         ClearOptionsUI();
//         HideScreen();

//         OnDialogueEnded?.Invoke();
//     }

//     private void BindUIElements(VisualElement root)
//     {
//         VisualElement dialogueRoot =
//             root.Q<VisualElement>("dialogue-root");

//         if (dialogueRoot == null)
//             dialogueRoot = root;

//         characterNameLabel =
//             dialogueRoot.Q<Label>("character-name-label");

//         dialogueTextLabel =
//             dialogueRoot.Q<Label>("dialogue-text-label");

//         optionsContainer =
//             dialogueRoot.Q<VisualElement>("options-container");

//         optionButtonTemplate =
//             dialogueRoot.Q<Button>("option-button-template");

//         continueButton =
//             dialogueRoot.Q<Button>("continue-button");

//         if (continueButton != null)
//         {
//             continueButton.clicked -= OnContinueClicked;
//             continueButton.clicked += OnContinueClicked;
//             continueButton.style.display =
//                 DisplayStyle.None;
//         }

//         if (optionButtonTemplate != null)
//         {
//             optionButtonTemplate.style.display =
//                 DisplayStyle.None;
//         }
//     }

//     private void OnContinueClicked()
//     {
//         advanceRequested = true;
//     }

//     public void OnObjectTouched(string itemName)
//     {
//         if (itemName == "") return;
//         if (touchedRequested &&
//             string.Equals(
//                 lastTouchedItem,
//                 itemName,
//                 StringComparison.OrdinalIgnoreCase))
//         {
//             touchedRequested = false;
//         }
//     }

//     private void ParseCommands()
//     {
//         instructions.Clear();
//         labels.Clear();
//         autoLabelCounter = 0;

//         if (storyDataStore == null ||
//             storyDataStore.Commands == null)
//             return;

//         List<CommandData> raw =
//             new List<CommandData>(storyDataStore.Commands);

//         List<ParsedLine> phase1 = PreParsePhase1(raw);

//         List<CommandData> phase2 = TransformPhase2(phase1);

//         CompileInstructions(phase2);
//     }

//     private List<ParsedLine> PreParsePhase1(List<CommandData> raw)
//     {
//         List<ParsedLine> result = new List<ParsedLine>();
//         int currentIndent = 0;
//         int nextIndent = 0;

//         foreach (var cmd in raw)
//         {
//             if (cmd == null) continue;

//             nextIndent = currentIndent;

//             switch (cmd.CommandType)
//             {
//                 case "choice":
//                     currentIndent = 0;
//                     nextIndent++;
//                     break;

//                 case "var_is_set":
//                 case "var_is_not_set":
//                     nextIndent++;
//                     break;

//                 default:
//                     nextIndent = 0;
//                     break;
//             }

//             result.Add(new ParsedLine
//             {
//                 Indentation = currentIndent,
//                 Command = new CommandData
//                 {
//                     CommandType = cmd.CommandType,
//                     Argument = NormalizeArgument(cmd.Argument)
//                 }
//             });

//             currentIndent = nextIndent;
//         }

//         return result;
//     }


//     private List<CommandData> TransformPhase2(List<ParsedLine> phase1)
//     {
//         List<CommandData> result = new List<CommandData>();
//         int i = 0;

//         while (i < phase1.Count)
//         {
//             ParsedLine line = phase1[i];
//             string type = NormalizeType(line.Command.CommandType);

//             if (type != "CHOICE")
//             {
//                 result.Add(line.Command);
//                 i++;
//                 continue;
//             }

//             List<(string optionText, List<CommandData> body)> options = new List<(string, List<CommandData>)>();

//             while (i < phase1.Count)
//             {
//                 ParsedLine currentLine = phase1[i];
//                 if (NormalizeType(currentLine.Command.CommandType) == "CHOICE")
//                 {
//                     string optionText = currentLine.Command.Argument;
//                     i++;

//                     List<CommandData> body = new List<CommandData>();
//                     while (i < phase1.Count)
//                     {
//                         ParsedLine nextLine = phase1[i];
//                         if (NormalizeType(nextLine.Command.CommandType) == "CHOICE")
//                         {
//                             break;
//                         }
//                         if (nextLine.Indentation == 0)
//                         {
//                             break;
//                         }

//                         body.Add(nextLine.Command);
//                         i++;
//                     }
//                     options.Add((optionText, body));
//                 }
//                 else
//                 {
//                     break;
//                 }
//             }

//             foreach (var opt in options)
//             {
//                 result.Add(new CommandData
//                 {
//                     CommandType = "SHOWOPTION",
//                     Argument = opt.optionText
//                 });
//             }

//             result.Add(new CommandData
//             {
//                 CommandType = "WAITFORCHOICE"
//             });

//             string endChoiceLabel = $"&&__autolabel_endchoice_{autoLabelCounter++}";

//             foreach (var opt in options)
//             {
//                 string skipLabel = $"&&__autolabel_skip_{autoLabelCounter++}";

//                 result.Add(new CommandData
//                 {
//                     CommandType = "IF_NOT_CHOSEN",
//                     Argument = opt.optionText
//                 });
//                 result.Add(new CommandData
//                 {
//                     CommandType = "GOTO",
//                     Argument = skipLabel
//                 });

//                 foreach (var cmd in opt.body)
//                 {
//                     result.Add(cmd);
//                 }

//                 result.Add(new CommandData
//                 {
//                     CommandType = "GOTO",
//                     Argument = endChoiceLabel
//                 });

//                 result.Add(new CommandData
//                 {
//                     CommandType = "CHAPTER",
//                     Argument = skipLabel
//                 });
//             }

//             result.Add(new CommandData
//             {
//                 CommandType = "CHAPTER",
//                 Argument = endChoiceLabel
//             });
//         }

//         return result;
//     }

//     private void CompileInstructions(List<CommandData> preparsed)
//     {
//         foreach (CommandData cmd in preparsed)
//         {
//             AddInstruction(cmd);
//         }
//     }

//     private void AddInstruction(CommandData cmd)
//     {
//         if (cmd == null ||
//             string.IsNullOrEmpty(cmd.CommandType))
//             return;

//         string type =
//             NormalizeType(cmd.CommandType);

//         string arg =
//             (cmd.Argument ?? "").Trim();

//         if (type == "CHAPTER")
//         {
//             labels[arg] = instructions.Count;
//         }

//         Instruction instruction =
//             new Instruction
//             {
//                 Type = type,
//                 Argument = arg
//             };

//         instructions.Add(instruction);
//     }

//     private string ProcessString(string input)
//     {
//         return System.Text.RegularExpressions.Regex.Unescape(input);
//     }

//     private IEnumerator RunDialogueRoutine()
//     {
//         int pc = 0;

//         HashSet<string> variables =
//             new HashSet<string>(
//                 StringComparer.OrdinalIgnoreCase);

//         List<string> optionsToDisplay = new List<string>();

//         while (pc < instructions.Count)
//         {
//             Instruction instr =
//                 instructions[pc];

//             switch (instr.Type)
//             {
//                 case "SAY":
//                 case "TEXT":
//                 {
//                     string text = ProcessString(instr.Argument);
//                     MatchCollection matches = Regex.Matches(text, @"\[(.*?)\]([^\[]+)");

//                     List<(string speaker, string dialogue)> dialogueList = new List<(string, string)>();

//                     if (matches.Count > 0)
//                     {
//                         foreach (Match match in matches)
//                         {
//                             string speaker = match.Groups[1].Value.Trim();
//                             string dialogue = match.Groups[2].Value.Trim();
//                             dialogueList.Add((speaker, dialogue));
//                         }
//                     }
//                     else
//                     {
//                         dialogueList.Add(("", text.Trim()));
//                     }

//                     foreach (var item in dialogueList)
//                     {
//                         string speaker = item.speaker;
//                         string dialogue = item.dialogue;

//                         if (characterNameLabel != null)
//                         {
//                             characterNameLabel.text = speaker;
//                             characterNameLabel.style.display = string.IsNullOrEmpty(speaker) ? DisplayStyle.None : DisplayStyle.Flex;
//                         }

//                         if (dialogueTextLabel != null)
//                             dialogueTextLabel.text = dialogue;

//                         if (instr.Type == "SAY") OnCameraChangeRequested?.Invoke(true, speaker);

//                         if (continueButton != null) continueButton.style.display = DisplayStyle.Flex;
//                         advanceRequested = false;

//                         float timer = 0f;
//                         float duration = 2f;

//                         while (timer < duration && !advanceRequested)
//                         {
//                             if (Keyboard.current.spaceKey.isPressed ||
//                                 Keyboard.current.enterKey.isPressed) break;
//                             timer += Time.deltaTime;
//                             yield return null;
//                         }

//                         if (continueButton != null) continueButton.style.display = DisplayStyle.None;
//                         if (instr.Type == "SAY") OnCameraChangeRequested?.Invoke(false, speaker);
//                     }

//                     pc++;
//                     break;
//                 }
                
//                 case "SHOWOPTION":
//                     optionsToDisplay.Add(instr.Argument);
//                     pc++;
//                     break;

//                 case "WAITFORCHOICE":

//                     selectedOptionText = "";

//                     ShowOptionsUI(optionsToDisplay);

//                     yield return new WaitUntil(
//                         () =>
//                             !string.IsNullOrEmpty(
//                                 selectedOptionText));

//                     ClearOptionsUI();
//                     optionsToDisplay.Clear();

//                     pc++;
//                     break;

//                 case "IF_NOT_CHOSEN":

//                     if (!string.Equals(
//                         instr.Argument,
//                         selectedOptionText,
//                         StringComparison.OrdinalIgnoreCase))
//                     {
//                         pc++;
//                     }
//                     else
//                     {
//                         pc += 2;
//                     }

//                     break;

//                 case "TOUCHED":

//                     if (instr.Argument == "") break;

//                     touchedRequested = true;
//                     lastTouchedItem =
//                         instr.Argument;
                    
//                     HideScreen();

//                     yield return new WaitUntil(
//                         () => !touchedRequested);

//                     ShowScreen();

//                     pc++;
//                     break;

//                 case "BLOCK":

//                     if (instr.Argument == "") break;

//                     ToggleSceneObjectIsTrigger(
//                         instr.Argument,
//                         false);

//                     pc++;
//                     break;

//                 case "UNBLOCK":

//                     if (instr.Argument == "") break;

//                     ToggleSceneObjectIsTrigger(
//                         instr.Argument,
//                         true);

//                     pc++;
//                     break;

//                 case "PICKUP":
//                     if (instr.Argument == "") break;

//                     PickupItem(instr.Argument);

//                     pc++;
//                     break;

//                 case "DROP":
//                     if (instr.Argument == "") break;

//                     DropItem(instr.Argument);

//                     pc++;
//                     break;

//                 case "DONT HAVE":
//                     if (instr.Argument == "") break;

//                     if (!HasItem(instr.Argument))
//                     {
//                         pc++;
//                     }
//                     else
//                     {
//                         pc += 2;
//                     }

//                     break;

//                 case "HAS":
//                     if (instr.Argument == "") break;

//                     if (HasItem(instr.Argument))
//                     {
//                         pc++;
//                     }
//                     else
//                     {
//                         pc += 2;
//                     }

//                     break;

//                 case "SHOW":
//                     if (instr.Argument == "") break;

//                     ToggleSceneObjectVisibility(
//                         instr.Argument,
//                         true);

//                     pc++;
//                     break;

//                 case "HIDE":
//                     if (instr.Argument == "") break;

//                     ToggleSceneObjectVisibility(
//                         instr.Argument,
//                         false);

//                     pc++;
//                     break;

//                 case "CREATE_VAR":
//                     pc++;

//                     break;

//                 case "VAR_IS_SET":
//                     if (variables.Contains(
//                         instr.Argument))
//                     {
//                         pc++;
//                     }
//                     else
//                     {
//                         pc += 2;
//                     }

//                     break;

//                 case "VAR_IS_NOT_SET":

//                     if (!variables.Contains(
//                         instr.Argument))
//                     {
//                         pc++;
//                     }
//                     else
//                     {
//                         pc += 2;
//                     }

//                     break;

//                 case "SET_VAR":

//                     variables.Add(
//                         instr.Argument);

//                     pc++;
//                     break;

//                 case "UNSET_VAR":

//                     variables.Remove(
//                         instr.Argument);

//                     pc++;
//                     break;

//                 case "TELEPORT":
//                     if (instr.Argument == "") break;

//                     TeleportTo(instr.Argument);

//                     pc++;
//                     break;

//                 case "CHAPTER":

//                     pc++;
//                     break;

//                 case "GOTO":

//                     if (labels.TryGetValue(
//                         instr.Argument,
//                         out int targetPc))
//                     {
//                         pc = targetPc;
//                     }
//                     else
//                     {
//                         Debug.LogWarning(
//                             $"Dialogue jump target not found: {instr.Argument}");

//                         pc++;
//                     }

//                     break;

//                 case "NOP":

//                     pc++;
//                     break;

//                 default:

//                     print("?unknown command");
//                     pc++;
//                     break;
//             }
//         }

//         HideScreen();

//         activeDialogueCoroutine = null;

//         OnDialogueEnded?.Invoke();
//     }

//     private void ToggleSceneObjectIsTrigger(
//         string objectName,
//         bool isTrigger)
//     {
//         GameObject sceneRoot =
//             GameObject.Find("SceneManager");

//         if (sceneRoot == null)
//             return;

//         Transform[] transforms =
//             sceneRoot.GetComponentsInChildren<Transform>(
//                 true);

//         foreach (Transform child in transforms)
//         {
//             if (child == sceneRoot.transform)
//                 continue;

//             if (child.name.Equals(
//                 objectName,
//                 StringComparison.OrdinalIgnoreCase))
//             {
//                 Collider[] colliders = child.GetComponentsInChildren<Collider>(true);
//                 foreach (Collider col in colliders)
//                 {
//                     if (col is MeshCollider meshCollider && isTrigger)
//                     {
//                         meshCollider.convex = true;
//                     }
//                     col.isTrigger = isTrigger;
//                 }
//             }
//         }
//     }


//     private void ToggleSceneObjectVisibility(
//         string objectName,
//         bool show)
//     {
//         GameObject sceneRoot =
//             GameObject.Find("SceneManager");

//         if (sceneRoot == null)
//             return;

//         Transform[] transforms =
//             sceneRoot.GetComponentsInChildren<Transform>(
//                 true);

//         foreach (Transform child in transforms)
//         {
//             if (child == sceneRoot.transform)
//                 continue;

//             if (child.name.Equals(
//                 objectName,
//                 StringComparison.OrdinalIgnoreCase))
//             {
//                 child.gameObject.SetActive(show);
//             }
//         }
//     }

//     private void ShowOptionsUI(
//         List<string> options)
//     {
//         if (continueButton != null)
//             continueButton.style.display =
//                 DisplayStyle.None;

//         foreach (string optText in options)
//         {
//             Button btn = new Button();

//             btn.text = optText;

//             if (optionButtonTemplate != null)
//             {
//                 btn.style.height =
//                     optionButtonTemplate.style.height;

//                 btn.style.backgroundColor =
//                     optionButtonTemplate.style.backgroundColor;

//                 btn.style.borderTopLeftRadius =
//                     optionButtonTemplate.style.borderTopLeftRadius;

//                 btn.style.borderTopRightRadius =
//                     optionButtonTemplate.style.borderTopRightRadius;

//                 btn.style.borderBottomLeftRadius =
//                     optionButtonTemplate.style.borderBottomLeftRadius;

//                 btn.style.borderBottomRightRadius =
//                     optionButtonTemplate.style.borderBottomRightRadius;

//                 btn.style.color =
//                     optionButtonTemplate.style.color;
//             }

//             btn.style.marginTop = 2;
//             btn.style.marginBottom = 4;

//             string textToSelect =
//                 optText;

//             btn.clicked += () =>
//             {
//                 selectedOptionText =
//                     textToSelect;
//             };

//             optionsContainer?.Add(btn);
//         }
//     }

//     private void ClearOptionsUI()
//     {
//         if (optionsContainer == null)
//             return;

//         optionsContainer.Clear();

//         if (optionButtonTemplate != null)
//         {
//             optionsContainer.Add(
//                 optionButtonTemplate);

//             optionButtonTemplate.style.display =
//                 DisplayStyle.None;
//         }
//     }

//     private void PickupItem(string item)
//     {
//         PickupItemBehaviour[] pickupItemBehaviours = FindObjectsByType<PickupItemBehaviour>(FindObjectsSortMode.None);

//         foreach (PickupItemBehaviour pickupItemBehaviour in pickupItemBehaviours)
//         {
//             pickupItemBehaviour.Pickup(item);
//         }
//     }

//     private void DropItem(string item)
//     {
//         PickupItemBehaviour[] pickupItemBehaviours = FindObjectsByType<PickupItemBehaviour>(FindObjectsSortMode.None);

//         foreach (PickupItemBehaviour pickupItemBehaviour in pickupItemBehaviours)
//         {
//             pickupItemBehaviour.Drop(item);
//         }
//     }

//     private bool HasItem(string item)
//     {
//         PickupItemBehaviour[] pickupItemBehaviours = FindObjectsByType<PickupItemBehaviour>(FindObjectsSortMode.None);

//         foreach (PickupItemBehaviour pickupItemBehaviour in pickupItemBehaviours)
//         {
//             if (pickupItemBehaviour.Has(item)) return true;
//         }
//         return false;
//     }

//     private void TeleportTo(string target)
//     {
//         RespawnBehaviour[] respawnBehaviours = FindObjectsByType<RespawnBehaviour>(FindObjectsSortMode.None);

//         foreach (RespawnBehaviour rb in respawnBehaviours)
//         {
//             if (rb != null)
//             {
//                 rb.TeleportByName(target);
//             }
//         }
//     }

//     private string NormalizeType(
//         string value)
//     {
//         return (value ?? "")
//             .Trim()
//             .ToUpperInvariant();
//     }

//     private string NormalizeArgument(
//         string value)
//     {
//         string result =
//             (value ?? "").Trim();

//         if (result.Length >= 2 &&
//             result.StartsWith("\"") &&
//             result.EndsWith("\""))
//         {
//             result =
//                 result.Substring(
//                     1,
//                     result.Length - 2);
//         }

//         return result;
//     }
// }
