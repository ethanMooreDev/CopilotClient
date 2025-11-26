using CopilotClient.Models;
using CopilotClient.Services;
using System;
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

    private readonly Conversation _conversation;

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
            }
        }
    }

    public ICommand SendCommand { get; }

    public ICommand OpenSettingsCommand { get; }

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

        if(Messages.Count == 1)
        {
            if (!string.IsNullOrWhiteSpace(Title))
            {
                Title = userMessage.Content.Length > 40
                    ? $"{userMessage.Content[..40]}..."
                    : userMessage.Content;
            }
                
        }

        var assistantMessage = new ChatMessage(
            clientId: Guid.NewGuid(),
            role: ChatRole.Assistant,
            content: string.Empty,
            status: MessageStatus.Typing,
            createdAt: DateTime.Now
        );

        AddMessage(assistantMessage);

        IsBusy = true;
        try
        {
            var replyMessage = await _chatService.SendAsync(Messages.ToList());
            userMessage.Status = MessageStatus.Sent;
            RemoveMessage(assistantMessage);
            AddMessage(replyMessage);
        }
        catch (Exception ex)
        {
            assistantMessage.Status = MessageStatus.Failed;
            assistantMessage.ErrorMessage = ex.Message;
            assistantMessage.Content = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OpenSettings()
    {
        
    }

    private void AddMessage(ChatMessage m)
    {
        Messages.Add(m);
        _conversation.Messages.Add(m);
        _conversation.LastUpdatedAt = DateTime.UtcNow;
    }

    private void RemoveMessage(ChatMessage m)
    {
        Messages.Remove(m);
        _conversation.Messages.Remove(m);
        _conversation.LastUpdatedAt = DateTime.UtcNow;
    }
}
