using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;
using Yarn;
using Yarn.Compiler;
using Yarn.Markup;
using Yarn.Unity;

public class RuntimeYarnLoader : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument = default!;
    [SerializeField] private DialogueRunner dialogueRunner = default!;
    [SerializeField] private string startNode = "Start";

    private TextField? _codeTextField;
    private RuntimeLineProvider? _runtimeLineProvider;

    private void OnEnable()
    {
        var root = uiDocument.rootVisualElement;
        _codeTextField = root.Q<TextField>("code");
    }

    private void Awake()
    {
        // Haal een bestaande LineProviderBehaviour op of voeg onze runtime variant toe
        _runtimeLineProvider = dialogueRunner.GetComponent<RuntimeLineProvider>();
        if (_runtimeLineProvider == null)
        {
            _runtimeLineProvider = dialogueRunner.gameObject.AddComponent<RuntimeLineProvider>();
        }
    }

    public string GetYarnCodeFromUI()
    {
        if (_codeTextField == null)
        {
            Debug.LogError("TextField met naam 'code' niet gevonden in UI Document.");
            return string.Empty;
        }

        return _codeTextField.value;
    }

    public void LoadYarnCodeIntoDialogue(string yarnCode)
    {
        if (string.IsNullOrWhiteSpace(yarnCode))
        {
            Debug.LogWarning("Geen Yarn-code ingevoerd.");
            return;
        }

        var job = CompilationJob.CreateFromString(
            fileName: "RuntimeScript.yarn",
            source: yarnCode
        );

        CompilationResult result = Compiler.Compile(job);

        if (result.Diagnostics.Any(d => d.Severity == Diagnostic.DiagnosticSeverity.Error))
        {
            foreach (var diag in result.Diagnostics)
            {
                if (diag.Severity == Diagnostic.DiagnosticSeverity.Error)
                {
                    Debug.LogError($"Yarn Compile Fout [{diag.Range.Start.Line}:{diag.Range.Start.Character}]: {diag.Message}");
                }
            }
            return;
        }

        if (dialogueRunner.IsDialogueRunning)
        {
            dialogueRunner.Stop();
        }

        dialogueRunner.Dialogue.SetProgram(result.Program);

        if (_runtimeLineProvider != null)
        {
            _runtimeLineProvider.SetStrings(result.StringTable.ToDictionary(k => k.Key, v => v.Value.text));
        }

        if (dialogueRunner.Dialogue.NodeExists(startNode))
        {
            dialogueRunner.StartDialogue(startNode);
        }
        else
        {
            Debug.LogError($"Node '{startNode}' niet gevonden in de gecompileerde Yarn-code.");
        }
    }

    public void LoadAndStartFromUI()
    {
        string code = GetYarnCodeFromUI();
        LoadYarnCodeIntoDialogue(code);
    }
}

public class RuntimeLineProvider : LineProviderBehaviour
{
    private Dictionary<string, string> _strings = new();
    private string _localeCode = "en";

    public void SetStrings(Dictionary<string, string> strings)
    {
        _strings = strings;
    }

    public override string LocaleCode
    {
        get => _localeCode;
        set => _localeCode = value;
    }

    public override YarnTask<LocalizedLine> GetLocalizedLineAsync(Line line, CancellationToken cancellationToken)
    {
        string text = _strings.TryGetValue(line.ID, out var value) ? value : line.ID;
        var result = new LocalizedLine
        {
            TextID = line.ID,
            RawText = text,
            Substitutions = line.Substitutions
        };
        return YarnTask<LocalizedLine>.FromResult(result);
    }

    public override void RegisterMarkerProcessor(string attributeName, IAttributeMarkerProcessor processor) { }

    public override void DeregisterMarkerProcessor(string attributeName) { }
}