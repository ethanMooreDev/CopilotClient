using CopilotClient.Models;
using CopilotClient.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CopilotClient.ViewModels;

public class ChatViewModel : ViewModelBase
{
    private readonly IChatService _chatService;
    private bool _isBusy;
    private string _inputText = string.Empty;
    private string _title;
    private bool _sendEnabled = false;

    private bool _isEditingTitle;

    private bool _useStreaming = true;

    private const int SummarizeThresholdMessages = 40;
    private const int KeepRecentMessages = 10;

    private readonly Conversation _conversation;

    public Guid ConversationId { get =>  _conversation.Id; }

    public ObservableCollection<ChatMessage> Messages { get; }

    public string InputText
    {
        get => _inputText;
        set
        {
            if(_inputText != value)
            {
                _inputText = value;
                OnPropertyChanged(nameof(InputText));
                ((RelayCommand)SendCommand).RaiseCanExecuteChanged();

                SendEnabled = !string.IsNullOrWhiteSpace(_inputText);
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy != value)
            {
                _isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
                ((RelayCommand)SendCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool SendEnabled
    {
        get => _sendEnabled;
        private set
        {
            if (_sendEnabled != value)
            {
                _sendEnabled = value;
                OnPropertyChanged(nameof(SendEnabled));
                ((RelayCommand)SendCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                _conversation.Title = value;
                OnPropertyChanged(nameof(Title));

                _conversation.LastUpdatedAt = DateTime.Now;
                PersistRequested?.Invoke(_conversation);
            }
        }
    }

    public ConversationMode Mode
    {
        get => _conversation.Mode;
        set
        {
            if(_conversation.Mode != value)
            {
                _conversation.Mode = value;
                OnPropertyChanged(nameof(Mode));

                _conversation.LastUpdatedAt = DateTime.Now;
                PersistRequested?.Invoke(_conversation);
            }
        }
    }

    public bool IsEditingTitle
    {
        get => _isEditingTitle;
        set
        {
            if (_isEditingTitle != value)
            {
                _isEditingTitle = value;
                OnPropertyChanged(nameof(IsEditingTitle));
            }
        }
    }

    public ICommand SendCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand BeginEditTitleCommand { get; }
    public ICommand CommitEditTitleCommand { get; }

    public Action<Conversation>? PersistRequested { get; set; }

    public ChatViewModel(IChatService chatService) : this(chatService, new Conversation())
    {
    }

    public ChatViewModel(IChatService chatService, Conversation conversation)
    {
        _chatService = chatService;

        _conversation = conversation;

        Messages = new ObservableCollection<ChatMessage>(_conversation.Messages);

        Title = _conversation.Title;

        SendCommand = new RelayCommand(
            async _ => await SendAsync(),
            _ => !IsBusy && !string.IsNullOrWhiteSpace(InputText)
        );

        OpenSettingsCommand = new RelayCommand(_ => OpenSettings());

        BeginEditTitleCommand = new RelayCommand(_ => BeginEditTitle());
        CommitEditTitleCommand = new RelayCommand(_ => CommitEditTitle());
    }

    private async Task SendAsync()
    {
        var text = InputText.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        InputText = string.Empty;

        var userMessage = ChatMessage.CreateNewUserMessage(text);
        AddMessage(userMessage);

        // Auto-title only for first message and default title
        if (Messages.Count == 1 && (string.IsNullOrWhiteSpace(Title) || Title == "New conversation"))
        {
            Title = userMessage.Content.Length > 40
                ? $"{userMessage.Content[..40]}..."
                : userMessage.Content;
        }

        // Persist: new user message (and maybe new title)
        PersistRequested?.Invoke(_conversation);

        IsBusy = true;
        try
        {
            if (UseStreaming)
            {
                await SendWithStreamingAsync(userMessage);
            }
            else
            {
                await SendNonStreamingAsync(userMessage);
            }

            await SummarizeIfNeededAsync();
        }
        catch (OperationCanceledException)
        {
            
        }
        finally
        {
            IsBusy = false;
        }

        PersistRequested?.Invoke(_conversation);
    }

    private async Task SendNonStreamingAsync(ChatMessage userMessage)
    {
        var request = BuildServiceConversation();

        var typingMessage = new ChatMessage(
            clientId: Guid.NewGuid(),
            role: ChatRole.Assistant,
            content: string.Empty,
            status: MessageStatus.Typing,
            createdAt: DateTime.UtcNow
        );

        AddMessage(typingMessage);

        try
        {
            var replyMessage = await _chatService.SendAsync(request);
            userMessage.Status = MessageStatus.Sent;

            RemoveMessage(typingMessage);
            AddMessage(replyMessage);
        }
        catch (OperationCanceledException)
        {
            RemoveMessage(typingMessage);
        }
        catch (Exception ex)
        {
            typingMessage.Status = MessageStatus.Failed;
            typingMessage.ErrorMessage = ex.Message;
            typingMessage.Content = ex.Message;
        }
    }


    private async Task SendWithStreamingAsync(ChatMessage userMessage)
    {
        var request = BuildServiceConversation();

        var typingMessage = new ChatMessage(
            clientId: Guid.NewGuid(),
            role: ChatRole.Assistant,
            content: string.Empty,
            status: MessageStatus.Typing,
            createdAt: DateTime.UtcNow
        );

        AddMessage(typingMessage);

        ChatMessage? assistantMessage = null;

        try
        {
            var stream = _chatService.StreamAsync(request);

            bool sawFirstChunk = false;

            await foreach (var chunk in stream)
            {
                if (!sawFirstChunk)
                {
                    sawFirstChunk = true;

                    RemoveMessage(typingMessage);

                    assistantMessage = new ChatMessage(
                        clientId: Guid.NewGuid(),
                        role: ChatRole.Assistant,
                        content: chunk,
                        status: MessageStatus.Streaming,
                        createdAt: DateTime.UtcNow
                    );

                    AddMessage(assistantMessage);
                }
                else
                {
                    assistantMessage!.Content += chunk;
                }
            }

            if (assistantMessage is not null)
            {
                assistantMessage.Status = MessageStatus.Sent;
                userMessage.Status = MessageStatus.Sent;
            }
            else
            {
                typingMessage.Status = MessageStatus.Failed;
                typingMessage.ErrorMessage = "The model did not return any content.";
                typingMessage.Content = typingMessage.ErrorMessage;
            }
        }
        catch (OperationCanceledException)
        {
            RemoveMessage(typingMessage);
        }
        catch (Exception ex)
        {
            var target = assistantMessage ?? typingMessage;
            target.Status = MessageStatus.Failed;
            target.ErrorMessage = ex.Message;
            target.Content = ex.Message;
        }
    }

    private async Task SummarizeIfNeededAsync()
    {

        bool tooManyMessages = _conversation.Messages.Count > 40;
        bool tooManyTokens = EstimateConversationTokens(_conversation) > 6000;

        if (!tooManyTokens && !tooManyMessages)
        {
            return;
        }

        // Take all but the last KeepRecentMessages
        var total = _conversation.Messages.Count;
        var cutoff = Math.Max(0, total - KeepRecentMessages);

        var toSummarize = _conversation.Messages
            .Take(cutoff)
            .Where(m => m.Status == MessageStatus.Sent) // ignore transient/failed
            .ToList();

        if (toSummarize.Count == 0)
            return;

        try
        {
            var summaryText = await _chatService.SummarizeAsync(toSummarize);

            if (!string.IsNullOrWhiteSpace(summaryText))
            {
                // Update conversation summary
                if (string.IsNullOrWhiteSpace(_conversation.Summary))
                {
                    _conversation.Summary = summaryText;
                }
                else
                {
                    // Append / refine existing summary
                    _conversation.Summary += Environment.NewLine + Environment.NewLine + summaryText;
                }

                _conversation.SummaryUpdatedAt = DateTime.UtcNow;

                // Remove summarized messages from both the backing list and the UI
                foreach (var msg in toSummarize)
                {
                    Messages.Remove(msg);
                    _conversation.Messages.Remove(msg);
                }

                // Persist updated conversation (summary + trimmed history)
                PersistRequested?.Invoke(_conversation);
            }
        }
        catch
        {
            
        }
    }

    private ServiceConversation BuildServiceConversation()
    {
        return PromptBuilder.Build(_conversation);
    }

    private void OpenSettings()
    {
        
    }

    public void AddMessage(ChatMessage m)
    {
        Messages.Add(m);
        _conversation.Messages.Add(m);
        _conversation.LastUpdatedAt = DateTime.UtcNow;
    }

    public void RemoveMessage(ChatMessage m)
    {
        Messages.Remove(m);
        _conversation.Messages.Remove(m);
        _conversation.LastUpdatedAt = DateTime.UtcNow;
    }

    public void BeginEditTitle()
    {
        IsEditingTitle = true;
    }

    public void CommitEditTitle()
    {
        // Title is already updated via binding when the TextBox loses focus or Enter is pressed
        IsEditingTitle = false;
    }

    public bool UseStreaming
    {
        get => _useStreaming;
        set
        {
            if (_useStreaming != value)
            {
                _useStreaming = value;
                OnPropertyChanged();
            }
        }
    }

    private static int EstimateTokens(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0;

        return content.Length / 4 + 1;
    }

    private int EstimateConversationTokens(Conversation conversation)
    {
        int total = 0;

        foreach (var msg in conversation.Messages)
        {
            total += EstimateTokens(msg.Content);
        }

        return total;
    }

    public Array AvailableModes => Enum.GetValues(typeof(ConversationMode));
}
