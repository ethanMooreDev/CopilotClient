using CopilotClient.Models;
using CopilotClient.Persistence;
using CopilotClient.Services;
using CopilotClient.ViewModels;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

public class ConversationManagerViewModel : ViewModelBase
{
    private readonly IChatService _chatService;
    private readonly IConversationStore? _store;

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

    public ConversationManagerViewModel(IChatService chatService, IConversationStore? store = null)
    {
        _chatService = chatService;
        _store = store;

        // Start with a single conversation
        var first = CreateNewConversation();
        WirePersistence(first);
        Conversations.Add(first);
        SelectedConversation = first;

        NewConversationCommand = new RelayCommand(_ => NewConversation());
    }

    private ChatViewModel CreateNewConversation()
    {
        var convo = new Conversation();
        var cvm = new ChatViewModel(_chatService);

        return cvm;
    }

    private ChatViewModel NewConversation()
    {
        var convo = new Conversation();
        var vm = new ChatViewModel(_chatService, convo);

        WirePersistence(vm);
        Conversations.Add(vm);

        SelectedConversation = vm;

        return vm;
    }

    private void WirePersistence(ChatViewModel vm)
    {
        if (_store is null) return;

        vm.PersistRequested = async c => await _store.SaveConversationAsync(c);
    }

    public async Task LoadConversationsAsync()
    {
        if (_store is null)
        {
            return;
        }

        var summaries = await _store.GetSummariesAsync();

        if (summaries.Count == 0)
        {
            return;
        }

        Conversations.Clear();

        foreach (var summary in summaries)
        {
            var convo = await _store.LoadConversationAsync(summary.Id);
            if (convo is null)
            {
                continue;
            }

            var vm = new ChatViewModel(_chatService, convo);
            WirePersistence(vm);
            Conversations.Add(vm);
        }

        SelectedConversation ??= Conversations.FirstOrDefault();
    }
}