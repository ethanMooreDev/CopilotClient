using CopilotClient.Models;
using CopilotClient.Services;
using CopilotClient.ViewModels;
using System.Collections.ObjectModel;
using System.Windows.Input;

public class ConversationManagerViewModel : ViewModelBase
{
    private readonly IChatService _chatService;

    public ObservableCollection<ChatViewModel> Conversations { get; } =
        new ObservableCollection<ChatViewModel>();

    private ChatViewModel? _selectedConversation;
    public ChatViewModel? SelectedConversation
    {
        get => _selectedConversation;
        set
        {
            if (_selectedConversation != value)
            {
                _selectedConversation = value;
                OnPropertyChanged(nameof(SelectedConversation));
            }
        }
    }

    public ICommand NewConversationCommand { get; }

    public ConversationManagerViewModel(IChatService chatService)
    {
        _chatService = chatService;

        // Start with a single conversation
        var first = CreateNewConversation();
        Conversations.Add(first);
        SelectedConversation = first;

        NewConversationCommand = new RelayCommand(_ => NewConversation());
    }

    private ChatViewModel CreateNewConversation()
    {
        var convo = new Conversation();
        return new ChatViewModel(_chatService);
    }

    private void NewConversation()
    {
        var vm = CreateNewConversation();
        Conversations.Add(vm);
        SelectedConversation = vm;
    }
}