using CopilotClient.Models;
using CopilotClient.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Devices.Sms;
using Windows.Media.Devices;

namespace CopilotClient.ViewModels;

public class ChatViewModel : INotifyPropertyChanged
{
    private readonly IChatService _chatService;
    private bool _isBusy;
    private string _inputText = string.Empty;

    public ObservableCollection<ChatMessage> Messages { get; } = new();

    public string InputText
    {
        get => _inputText;
        set
        {
            if(_inputText != value)
            {
                _inputText = value;
                OnPropertyChanged();
                ((RelayCommand)SendCommand).RaiseCanExecuteChanged();
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
                OnPropertyChanged();
                ((RelayCommand)SendCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand SendCommand { get; }

    public ChatViewModel(IChatService chatService)
    {
        _chatService = chatService;

        SendCommand = new RelayCommand(
            async _ => await SendAsync(),
            _ => !IsBusy && !string.IsNullOrWhiteSpace(InputText)
        );
    }

    private async Task SendAsync()
    {
        var text = InputText.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        InputText = string.Empty;

        var userMessage = new ChatMessage(ChatRole.User, text);
        Messages.Add(userMessage);

        IsBusy = true;
        try
        {
            var assistantMessage = await _chatService.SendAsync(Messages.ToList());
            Messages.Add(assistantMessage);
        }
        catch (Exception ex)
        {
            Messages.Add(new ChatMessage(ChatRole.System, $"Error: {ex.Message}"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

}
