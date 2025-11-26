using CopilotClient.Persistence;
using CopilotClient.Services;
using CopilotClient.ViewModels;
using Microsoft.UI.Xaml;

namespace CopilotClient;

public sealed partial class MainWindow : Window
{
    private readonly ConversationManagerViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();

        var store = new JsonConversationStore();
        _vm = new ConversationManagerViewModel(new MockChatService(), store);

        WindowChatView.DataContext = _vm;

        WindowChatView.Loaded += WindowChatView_Loaded;
    }

    private async void WindowChatView_Loaded(object sender, RoutedEventArgs e)
    {
        await _vm.LoadConversationsAsync();
    }
}
