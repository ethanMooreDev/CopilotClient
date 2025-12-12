using CopilotClient.ViewModels;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Specialized;
using System.Linq;
using Windows.UI.Core;

namespace CopilotClient.Views;

public sealed partial class ChatView : UserControl
{

    private ChatViewModel? _chatViewModel;
    public ChatViewModel? ViewModel => _chatViewModel;

    public ChatView()
    {
        InitializeComponent();

        DataContextChanged += ChatView_DataContextChanged;
        Unloaded += ChatView_Unloaded;
    }

    private void ChatView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        // detach previous
        if (_chatViewModel != null)
            _chatViewModel.Messages.CollectionChanged -= Messages_CollectionChanged;

        _chatViewModel = null;

        if (args.NewValue is ConversationManagerViewModel vm && vm.SelectedConversation is ChatViewModel chatViewModel)
        {
            chatViewModel.Messages.CollectionChanged += Messages_CollectionChanged;
            _chatViewModel = chatViewModel;
        }
    }

    private void ChatView_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_chatViewModel != null)
            _chatViewModel.Messages.CollectionChanged -= Messages_CollectionChanged;

        DataContextChanged -= ChatView_DataContextChanged;
        Unloaded -= ChatView_Unloaded;
    }

    private void Messages_CollectionChanged(
        object? sender,
        System.Collections.Specialized.NotifyCollectionChangedEventArgs e
    )
    {
        if (e.Action != NotifyCollectionChangedAction.Add)
        {
            return;
        }
        if (e.NewItems == null)
        {
            return;
        }

        DispatcherQueue.TryEnqueue(() =>
        {

            // If the view is no longer loaded, bail out
            if (!IsLoaded)
            {
                return;
            }

            // If the ItemsControl isn't ready / attached, bail out
            if (MessagesItemsControl == null ||
                MessagesItemsControl.XamlRoot == null ||
                MessagesItemsControl.Items.Count == 0)
            {
                return;
            }

            MessagesItemsControl.UpdateLayout();

            var lastMessage = _chatViewModel!.Messages.LastOrDefault();
            if (lastMessage is null)
            {
                return;
            }

            var lastMessageContainer = MessagesItemsControl.ContainerFromItem(lastMessage);

            if (lastMessageContainer != null && lastMessageContainer is FrameworkElement)
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

    private void TextBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if(e.Key != Windows.System.VirtualKey.Enter)
        {
            return;
        }

        if (sender is not TextBox textBox)
        {
            return;
        }

        var shiftState = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift);

        bool shiftDown = shiftState.HasFlag(CoreVirtualKeyStates.Down);

        if (shiftDown)
        {
            // Manually insert a newline at the caret position
            int caretIndex = textBox.SelectionStart;
            string current = textBox.Text ?? string.Empty;

            string toInsert = "\n";
            textBox.Text = current.Insert(caretIndex, toInsert);

            // Move caret after the newline
            textBox.SelectionStart = caretIndex + toInsert.Length;
            textBox.SelectionLength = 0;

            e.Handled = true;
            return;
        }

        // Plain Enter: send the message via the ViewModel's command
        if (_chatViewModel != null && _chatViewModel.SendEnabled && _chatViewModel.SendCommand.CanExecute(null))
        {
            _chatViewModel.SendCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void ChatArea_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        // Detach previous subscription if present
        if (_chatViewModel != null)
        {
            _chatViewModel.Messages.CollectionChanged -= Messages_CollectionChanged;
            _chatViewModel = null;
        }

        // Attach to the new ChatViewModel (if provided)
        if (args.NewValue is ChatViewModel vm)
        {
            vm.Messages.CollectionChanged += Messages_CollectionChanged;
            _chatViewModel = vm;
        }
    }

    private void Grid_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Grid root && root.FindName("ListItemOptionButton") is Button btn)
        {
            btn.Visibility = Visibility.Visible;
        }

        
    }

    private void Grid_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Grid root && root.FindName("ListItemOptionButton") is Button btn)
        {
            btn.Visibility = Visibility.Collapsed;
        }
    }

    private void ListItemOptionButton_Click(object sender, RoutedEventArgs e)
    {
        if(sender is Button element && element.DataContext is ChatViewModel elementVm)
        {
            FlyoutBase.ShowAttachedFlyout(element);
        }
    }

    private void MenuFlyoutItemDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is ChatViewModel elementVm)
        {
            ((ConversationManagerViewModel) DataContext).DeleteConversationCommand.Execute(elementVm);
        }
    }

    private void MenuFlyoutItemRename_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is ChatViewModel elementVm)
        {
            elementVm.BeginEditTitleCommand.Execute(null);
        }
    }

    private void TitleTextBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter && sender is FrameworkElement element)
        {
            if (element.DataContext is ChatViewModel vm)
            {
                vm.CommitEditTitle();
            }
            e.Handled = true;
        }
    }

    private void TitleTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is ChatViewModel vm)
        {
            vm.CommitEditTitle();
        }
    }
}
