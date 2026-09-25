#nullable enable

using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;
using Yarn.Unity;

[RequireComponent(typeof(UIDocument))]
public class UIToolkitDialogueView : DialoguePresenterBase
{
    private Label? characterNameLabel;
    private Label? dialogueTextLabel;
    private VisualElement? optionsContainer;
    private VisualElement? rootElement;

    private void EnsureUIInitialized()
    {
        if (rootElement != null) return;

        rootElement = GetComponent<UIDocument>().rootVisualElement;

        characterNameLabel = rootElement.Q<Label>("character-name-label");
        dialogueTextLabel = rootElement.Q<Label>("dialogue-text-label");
        optionsContainer = rootElement.Q<VisualElement>("options-container");

        rootElement.style.display = DisplayStyle.None;
    }

    public override YarnTask OnDialogueStartedAsync()
    {
        EnsureUIInitialized();
        rootElement!.style.display = DisplayStyle.Flex;
        return YarnTask.CompletedTask;
    }

    public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        EnsureUIInitialized();
        rootElement!.style.display = DisplayStyle.Flex;

        if (characterNameLabel != null) characterNameLabel.text = line.CharacterName;
        if (dialogueTextLabel != null) dialogueTextLabel.text = line.Text.Text;

        var ycs = new YarnTaskCompletionSource();
        CancellationTokenRegistration registration = default;

        void OnClick(ClickEvent evt)
        {
            rootElement.UnregisterCallback<ClickEvent>(OnClick);
            registration.Dispose();
            ycs.TrySetResult();
        }

        rootElement.RegisterCallback<ClickEvent>(OnClick);

        registration = token.NextContentToken.Register(() =>
        {
            rootElement.UnregisterCallback<ClickEvent>(OnClick);
            ycs.TrySetResult();
        });

        return ycs.Task;
    }

    public override YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] options, LineCancellationToken token)
    {
        EnsureUIInitialized();
        rootElement!.style.display = DisplayStyle.Flex;

        if (optionsContainer == null) return YarnTask.FromResult<DialogueOption?>(null);

        optionsContainer.Clear();
        var ycs = new YarnTaskCompletionSource<DialogueOption?>();
        CancellationTokenRegistration registration = default;

        foreach (var option in options)
        {
            if (!option.IsAvailable) continue;

            var selected = option;
            var button = new Button(() =>
            {
                optionsContainer.Clear();
                registration.Dispose();
                ycs.TrySetResult(selected);
            })
            {
                text = option.Line.Text.Text
            };

            optionsContainer.Add(button);
        }

        registration = token.NextContentToken.Register(() =>
        {
            optionsContainer.Clear();
            ycs.TrySetResult(null);
        });

        return ycs.Task;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        EnsureUIInitialized();
        rootElement!.style.display = DisplayStyle.None;
        return YarnTask.CompletedTask;
    }
}