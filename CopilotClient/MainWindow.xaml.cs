using CopilotClient.Options;
using CopilotClient.Persistence;
using CopilotClient.Services;
using CopilotClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;

namespace CopilotClient;

public sealed partial class MainWindow : Window
{
    private readonly ConversationManagerViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();

        _vm = App.Services.GetRequiredService<ConversationManagerViewModel>();

        WindowChatView.DataContext = _vm;

        WindowChatView.Loaded += WindowChatView_Loaded;
    }

    private async void WindowChatView_Loaded(object sender, RoutedEventArgs e)
    {
        await _vm.LoadConversationsAsync();
    }

    private static AzureOpenAiOptions GetAzureOpenAiOptionsFromConfig()
    {
        var section = App.Configuration.GetSection("AzureOpenAI");

        var options = new AzureOpenAiOptions
        {
            Endpoint = section["Endpoint"] ?? string.Empty,
            DeploymentName = section["DeploymentName"] ?? string.Empty,
            ApiKey = section["ApiKey"] ?? string.Empty
        };

        if (string.IsNullOrWhiteSpace(options.Endpoint) ||
            string.IsNullOrWhiteSpace(options.DeploymentName))
        {
            throw new InvalidOperationException("AzureOpenAI:Endpoint and DeploymentName must be configured in appsettings.json.");
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException(
                "AzureOpenAI:ApiKey must be configured via user secrets (dotnet user-secrets set \"AzureOpenAI:ApiKey\" \"<key>\").");
        }

        return options;
    }
}
