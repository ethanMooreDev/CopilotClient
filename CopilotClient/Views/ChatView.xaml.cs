using CopilotClient.Models;
using CopilotClient.Services;
using CopilotClient.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Specialized;
using System.Data;
using System.Linq;

namespace CopilotClient.Views;

public sealed partial class ChatView : UserControl
{

    private ChatViewModel? _chatViewModel;
    public ChatViewModel? ViewModel => _chatViewModel;

    public ChatView()
    {
        InitializeComponent();

        RootGrid.SizeChanged += (_, __) => Bindings.Update();

        DataContextChanged += ChatView_DataContextChanged;
    }

    private void ChatView_DataContextChanged(Microsoft.UI.Xaml.FrameworkElement sender, Microsoft.UI.Xaml.DataContextChangedEventArgs args)
    {
        if(args.NewValue is ChatViewModel vm)
        {
            if(_chatViewModel != null)
            {
                _chatViewModel.Messages.CollectionChanged -= Messages_CollectionChanged;
            }

            vm.Messages.CollectionChanged += Messages_CollectionChanged;
            _chatViewModel = vm;
        }
    }

    private void Messages_CollectionChanged(
        object? sender, 
        System.Collections.Specialized.NotifyCollectionChangedEventArgs e
    )
    {
        if(e.Action != NotifyCollectionChangedAction.Add)
        {
            return;
        }
        if(e.NewItems == null)
        {
            return;
        }

        DispatcherQueue.TryEnqueue(() =>
        {
            MessagesItemsControl.UpdateLayout();

            var lastMessage = _chatViewModel!.Messages.Last();

            var lastMessageContainer = MessagesItemsControl.ContainerFromItem(lastMessage);

            if( lastMessageContainer != null && lastMessageContainer is FrameworkElement )
            {
                BringIntoViewOptions options = new();
                options.VerticalAlignmentRatio = 1;

                ((FrameworkElement)lastMessageContainer).StartBringIntoView(options);
            }
        });
    }

    public Visibility BusyToVisibility(bool isBusy)
    {
        return isBusy ? Visibility.Visible : Visibility.Collapsed;
    }
}
