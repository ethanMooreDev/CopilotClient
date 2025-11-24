using CopilotClient.ViewModels;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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

        RootGrid.SizeChanged += (_, __) => Bindings.Update();

        DataContextChanged += ChatView_DataContextChanged;
    }

    private void TypingIndicator_Loaded(object sender, RoutedEventArgs e)
    {
        if (Resources["TypingDotsStoryboard"] is Storyboard storyboard)
        {
            storyboard.Begin();
        }
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

            string toInsert = Environment.NewLine;
            textBox.Text = current.Insert(caretIndex, toInsert);

            // Move caret after the newline
            textBox.SelectionStart = caretIndex + toInsert.Length;
            textBox.SelectionLength = 0;

            e.Handled = true;
            return;
        }

        // Plain Enter: send the message via the ViewModel's command
        if (DataContext is ChatViewModel vm && vm.SendCommand.CanExecute(null))
        {
            vm.SendCommand.Execute(null);
            e.Handled = true;
        }
    }
}
