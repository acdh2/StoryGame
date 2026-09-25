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
    private Button? optionTemplateButton;
    private Button? continueButton;
    private VisualElement? rootElement;

    private void EnsureUIInitialized()
    {
        if (rootElement != null) return;

        rootElement = GetComponent<UIDocument>().rootVisualElement;

        characterNameLabel = rootElement.Q<Label>("character-name-label");
        dialogueTextLabel = rootElement.Q<Label>("dialogue-text-label");
        optionsContainer = rootElement.Q<VisualElement>("options-container");
        optionTemplateButton = rootElement.Q<Button>("option-button-template");
        continueButton = rootElement.Q<Button>("continue-button");

        if (optionTemplateButton != null)
        {
            optionTemplateButton.style.display = DisplayStyle.None;
        }

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

        if (optionsContainer != null) ClearGeneratedOptions();

        if (continueButton != null)
        {
            continueButton.style.display = DisplayStyle.Flex;
        }

        var ycs = new YarnTaskCompletionSource();
        CancellationTokenRegistration registration = default;

        void OnContinueClick()
        {
            if (continueButton != null)
            {
                continueButton.clicked -= OnContinueClick;
                continueButton.style.display = DisplayStyle.None;
            }

            registration.Dispose();
            ycs.TrySetResult();
        }

        if (continueButton != null)
        {
            continueButton.clicked += OnContinueClick;
        }

        registration = token.NextContentToken.Register(() =>
        {
            if (continueButton != null)
            {
                continueButton.clicked -= OnContinueClick;
                continueButton.style.display = DisplayStyle.None;
            }

            ycs.TrySetResult();
        });

        return ycs.Task;
    }

    public override YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] options, LineCancellationToken token)
    {
        EnsureUIInitialized();
        rootElement!.style.display = DisplayStyle.Flex;

        if (continueButton != null)
        {
            continueButton.style.display = DisplayStyle.None;
        }

        if (optionsContainer == null || optionTemplateButton == null)
        {
            return YarnTask.FromResult<DialogueOption?>(null);
        }

        ClearGeneratedOptions();

        var ycs = new YarnTaskCompletionSource<DialogueOption?>();
        CancellationTokenRegistration registration = default;

        foreach (var option in options)
        {
            if (!option.IsAvailable) continue;

            var selected = option;
            
            // Dupliceer de template button uit de UXML
            var button = new Button()
            {
                text = option.Line.Text.Text
            };

            // Kopieer de stijl en classes van het sjabloon
            button.style.height = optionTemplateButton.style.height;
            button.style.backgroundColor = optionTemplateButton.style.backgroundColor;
            button.style.borderTopLeftRadius = optionTemplateButton.style.borderTopLeftRadius;
            button.style.borderTopRightRadius = optionTemplateButton.style.borderTopRightRadius;
            button.style.borderBottomLeftRadius = optionTemplateButton.style.borderBottomLeftRadius;
            button.style.borderBottomRightRadius = optionTemplateButton.style.borderBottomRightRadius;
            button.style.borderTopWidth = optionTemplateButton.style.borderTopWidth;
            button.style.borderRightWidth = optionTemplateButton.style.borderRightWidth;
            button.style.borderBottomWidth = optionTemplateButton.style.borderBottomWidth;
            button.style.borderLeftWidth = optionTemplateButton.style.borderLeftWidth;
            button.style.borderTopColor = optionTemplateButton.style.borderTopColor;
            button.style.borderRightColor = optionTemplateButton.style.borderRightColor;
            button.style.borderBottomColor = optionTemplateButton.style.borderBottomColor;
            button.style.borderLeftColor = optionTemplateButton.style.borderLeftColor;
            button.style.color = optionTemplateButton.style.color;
            button.style.unityFontStyleAndWeight = optionTemplateButton.style.unityFontStyleAndWeight;
            button.style.marginBottom = optionTemplateButton.style.marginBottom;
            button.style.display = DisplayStyle.Flex;

            button.clicked += () =>
            {
                ClearGeneratedOptions();
                registration.Dispose();
                ycs.TrySetResult(selected);
            };

            optionsContainer.Add(button);
        }

        registration = token.NextContentToken.Register(() =>
        {
            ClearGeneratedOptions();
            ycs.TrySetResult(null);
        });

        return ycs.Task;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        EnsureUIInitialized();
        ClearGeneratedOptions();

        if (continueButton != null)
        {
            continueButton.style.display = DisplayStyle.None;
        }

        rootElement!.style.display = DisplayStyle.None;
        return YarnTask.CompletedTask;
    }

    private void ClearGeneratedOptions()
    {
        if (optionsContainer == null || optionTemplateButton == null) return;

        for (int i = optionsContainer.childCount - 1; i >= 0; i--)
        {
            var child = optionsContainer[i];
            if (child != optionTemplateButton)
            {
                optionsContainer.RemoveAt(i);
            }
        }
    }
}
// #nullable enable

// using System.Threading;
// using UnityEngine;
// using UnityEngine.UIElements;
// using Yarn.Unity;

// [RequireComponent(typeof(UIDocument))]
// public class UIToolkitDialogueView : DialoguePresenterBase
// {
//     private Label? characterNameLabel;
//     private Label? dialogueTextLabel;
//     private VisualElement? optionsContainer;
//     private VisualElement? rootElement;

//     private void EnsureUIInitialized()
//     {
//         if (rootElement != null) return;

//         rootElement = GetComponent<UIDocument>().rootVisualElement;

//         characterNameLabel = rootElement.Q<Label>("character-name-label");
//         dialogueTextLabel = rootElement.Q<Label>("dialogue-text-label");
//         optionsContainer = rootElement.Q<VisualElement>("options-container");

//         rootElement.style.display = DisplayStyle.None;
//     }

//     public override YarnTask OnDialogueStartedAsync()
//     {
//         EnsureUIInitialized();
//         rootElement!.style.display = DisplayStyle.Flex;
//         return YarnTask.CompletedTask;
//     }

//     public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
//     {
//         EnsureUIInitialized();
//         rootElement!.style.display = DisplayStyle.Flex;

//         if (characterNameLabel != null) characterNameLabel.text = line.CharacterName;
//         if (dialogueTextLabel != null) dialogueTextLabel.text = line.Text.Text;

//         var ycs = new YarnTaskCompletionSource();
//         CancellationTokenRegistration registration = default;

//         void OnClick(ClickEvent evt)
//         {
//             rootElement.UnregisterCallback<ClickEvent>(OnClick);
//             registration.Dispose();
//             ycs.TrySetResult();
//         }

//         rootElement.RegisterCallback<ClickEvent>(OnClick);

//         registration = token.NextContentToken.Register(() =>
//         {
//             rootElement.UnregisterCallback<ClickEvent>(OnClick);
//             ycs.TrySetResult();
//         });

//         return ycs.Task;
//     }

//     public override YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] options, LineCancellationToken token)
//     {
//         EnsureUIInitialized();
//         rootElement!.style.display = DisplayStyle.Flex;

//         if (optionsContainer == null) return YarnTask.FromResult<DialogueOption?>(null);

//         optionsContainer.Clear();
//         var ycs = new YarnTaskCompletionSource<DialogueOption?>();
//         CancellationTokenRegistration registration = default;

//         foreach (var option in options)
//         {
//             if (!option.IsAvailable) continue;

//             var selected = option;
//             var button = new Button(() =>
//             {
//                 optionsContainer.Clear();
//                 registration.Dispose();
//                 ycs.TrySetResult(selected);
//             })
//             {
//                 text = option.Line.Text.Text
//             };

//             optionsContainer.Add(button);
//         }

//         registration = token.NextContentToken.Register(() =>
//         {
//             optionsContainer.Clear();
//             ycs.TrySetResult(null);
//         });

//         return ycs.Task;
//     }

//     public override YarnTask OnDialogueCompleteAsync()
//     {
//         EnsureUIInitialized();
//         rootElement!.style.display = DisplayStyle.None;
//         return YarnTask.CompletedTask;
//     }
// }