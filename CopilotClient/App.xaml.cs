using CopilotClient.Options;
using CopilotClient.Persistence;
using CopilotClient.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CopilotClient
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;

        public static IServiceProvider Services { get; private set; } = default!;

        public static IConfiguration Configuration { get; private set; } = default!;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();

            BuildConfiguration();

            ConfigureServices();
        }

        private static void BuildConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<App>(optional: true);

            Configuration = builder.Build();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
        }

        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            // Make configuration injectable if you ever need it
            var azureSection = Configuration.GetSection("AzureOpenAI");

            var azureOptions = new AzureOpenAiOptions
            {
                Endpoint = azureSection["Endpoint"] ?? "",
                DeploymentName = azureSection["DeploymentName"] ?? "",
                ApiKey = azureSection["ApiKey"] ?? "",

                Temperature = float.TryParse(azureSection["Temperature"], out var temp) ? temp : 0.3f,
                MaxOutputTokens = int.TryParse(azureSection["MaxOutputTokens"], out var tok) ? tok : 512,

                ExplainTemperature = float.TryParse(azureSection["ExplainTemperature"], out var explainTemp) ? explainTemp : 0.4f,
                ExplainMaxOutputTokens = int.TryParse(azureSection["ExplainMaxOutputTokens"], out var explainTok) ? explainTok : 1024,
            };

            services.AddSingleton(azureOptions);

            // Core services
            services.AddSingleton<IConversationStore, JsonConversationStore>();
            services.AddSingleton<IChatService, AzureOpenAiChatService>();

            // ViewModels
            services.AddSingleton<ConversationManagerViewModel>();

            Services = services.BuildServiceProvider();
        }

    }
}
