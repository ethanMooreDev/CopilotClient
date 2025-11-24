using CopilotClient.Services;
using CopilotClient.ViewModels;
using Microsoft.UI.Xaml;

namespace CopilotClient;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        WindowChatView.DataContext = new ChatViewModel(new MockChatService());
    }
}
