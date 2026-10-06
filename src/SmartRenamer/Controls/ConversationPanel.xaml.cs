using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media;
using SmartRenamer.Guide.Models;
using SmartRenamer.ViewModels.Guide;

namespace SmartRenamer.Controls
{
    public partial class ConversationPanel : UserControl
    {
        private readonly Queue<GuideMessage> _messageQueue = new();
        private readonly HashSet<GuideMessage> _queuedOrDisplayed = new();
        private bool _displayPumpRunning;
        private bool _isLoaded;
        private bool _autoScrollPaused;
        private ScrollViewer? _conversationScrollViewer;

        /// <summary>
        /// Messages actually presented to the user.
        ///
        /// GuideConversation.Messages remains the authoritative history. This
        /// presentation collection deliberately reveals new Scout messages at
        /// a human-readable pace so a burst of background activity does not
        /// appear as a wall of rapidly changing text.
        /// </summary>
        public ObservableCollection<GuideMessage> DisplayedMessages { get; } = new();

        /// <summary>
        /// Delay between Scout messages. This is presentation only; it does
        /// not slow investigations, repairs, research, or organization.
        /// </summary>
        private static readonly TimeSpan MessageDisplayDelay =
            TimeSpan.FromMilliseconds(1300);

        // The conversation is intentionally calm and readable. The content
        // itself is added once per message; only the movement to the newest
        // message is animated.
        private static readonly Duration ConversationScrollDuration =
            new(TimeSpan.FromMilliseconds(700));

        public ConversationPanel()
        {
            InitializeComponent();
            Loaded += ConversationPanel_Loaded;
            Unloaded += ConversationPanel_Unloaded;
        }

        private void ConversationPanel_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            ResumeScrollButton.Visibility = Visibility.Collapsed;
            _autoScrollPaused = false;
            _conversationScrollViewer =
                FindVisualChild<ScrollViewer>(ConversationList);

            ConversationList.PreviewMouseWheel += ConversationList_PreviewMouseWheel;
            ConversationList.PreviewMouseDown += ConversationList_PreviewMouseDown;
            ConversationList.PreviewMouseUp += ConversationList_PreviewMouseUp;

            FocusInputBox();

            if (DataContext is GuideViewModel guide)
            {
                guide.Conversation.Messages.CollectionChanged -= Messages_CollectionChanged;
                guide.Conversation.Messages.CollectionChanged += Messages_CollectionChanged;

               
                // Populate any messages that already existed before the panel
                // was loaded, preserving their original order.
                foreach (GuideMessage message in guide.Conversation.Messages)
                {
                    if (_queuedOrDisplayed.Add(message))
                        _messageQueue.Enqueue(message);
                }

                _ = PumpMessagesAsync();
            }
        }

        private void ConversationPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            ConversationList.PreviewMouseWheel -= ConversationList_PreviewMouseWheel;
            ConversationList.PreviewMouseDown -= ConversationList_PreviewMouseDown;
            ConversationList.PreviewMouseUp -= ConversationList_PreviewMouseUp;

            if (_conversationScrollViewer != null)
                _conversationScrollViewer.BeginAnimation(VerticalOffsetProperty, null);

            _conversationScrollViewer = null;
            _isLoaded = false;
            _autoScrollPaused = false;

            if (DataContext is GuideViewModel guide)
            {
                guide.Conversation.Messages.CollectionChanged -= Messages_CollectionChanged;
            }
        }

        /// <summary>
        /// Restores keyboard focus after Scout changes the conversation into a
        /// free-form input state. The request is deferred until the current
        /// control-layout change has completed so the first clipboard paste is
        /// not consumed while focus is moving.
        /// </summary>
        private void Guide_InputFocusRequested(
            object? sender,
            EventArgs e)
        {
            if (!_isLoaded)
                return;

            Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Input,
                new Action(FocusInputBox));
        }

        /// <summary>
        /// Gives the conversation TextBox keyboard focus after WPF has finished
        /// applying the current conversation/control-state change.
        /// Keeping this in the control (rather than in the ViewModel) avoids
        /// coupling the domain layer to a WPF control instance.
        /// </summary>
        private void FocusInputBox()
        {
            if (!_isLoaded ||
                !InputBox.IsVisible ||
                !InputBox.IsEnabled)
            {
                return;
            }

            InputBox.Focus();
            Keyboard.Focus(InputBox);
            InputBox.CaretIndex = InputBox.Text?.Length ?? 0;
        }

        private void PauseAutoScroll()
        {
            _autoScrollPaused = true;
            _conversationScrollViewer?.BeginAnimation(
                VerticalOffsetProperty,
                null);

            ResumeScrollButton.Visibility = Visibility.Visible;
        }

        private void ConversationList_PreviewMouseWheel(
            object sender,
            MouseWheelEventArgs e)
        {
            // Any wheel movement is an explicit request to read the
            // conversation where the user has chosen to be. Scout must never
            // pull the viewport back while the user is doing that.
            PauseAutoScroll();
        }

        private void ConversationList_PreviewMouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            // A mouse interaction inside the transcript is enough to establish
            // that the user is taking control. This is deliberately broader
            // than just scrollbar clicks: selecting text or grabbing the
            // scrollbar must both defeat automatic movement.
            PauseAutoScroll();
        }

        private void ConversationList_PreviewMouseUp(
            object sender,
            MouseButtonEventArgs e)
        {
            UpdateAutoScrollStateFromOffset();
        }

        private void ResumeScrollButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _autoScrollPaused = false;
            ResumeScrollButton.Visibility = Visibility.Collapsed;
            ScrollConversationToBottom();
        }

        private void UpdateAutoScrollStateFromOffset()
        {
            if (_conversationScrollViewer == null)
                return;

            const double tolerance = 8.0;

            if (_conversationScrollViewer.VerticalOffset >=
                _conversationScrollViewer.ScrollableHeight - tolerance)
            {
                _autoScrollPaused = false;
                ResumeScrollButton.Visibility = Visibility.Collapsed;
            }
        }

        private static T? FindVisualParent<T>(DependencyObject? child)
            where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T match)
                    return match;

                child = VisualTreeHelper.GetParent(child);
            }

            return null;
        }

        private void Messages_CollectionChanged(
            object? sender,
            NotifyCollectionChangedEventArgs e)
        {
            if (!_isLoaded)
                return;

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                _messageQueue.Clear();
                _queuedOrDisplayed.Clear();
                DisplayedMessages.Clear();
                return;
            }

            if (e.NewItems == null)
                return;

            foreach (object? item in e.NewItems)
            {
                if (item is GuideMessage message &&
                    _queuedOrDisplayed.Add(message))
                {
                    _messageQueue.Enqueue(message);
                }
            }

            _ = PumpMessagesAsync();
        }

        private async Task PumpMessagesAsync()
        {
            if (_displayPumpRunning)
                return;

            _displayPumpRunning = true;

            try
            {
                while (_isLoaded && _messageQueue.Count > 0)
                {
                    GuideMessage message = _messageQueue.Dequeue();

                    await Dispatcher.InvokeAsync(() =>
                    {
                        DisplayedMessages.Add(message);
                        ScrollConversationToBottom();
                    });

                    // User messages and decision/action messages should not be
                    // made to feel artificially delayed. Narrative Scout
                    // messages get the human-readable pause.
                    if (message.Speaker == GuideSpeaker.Guide &&
                        message.Payload == null &&
                        message.Card == null)
                    {
                        await Task.Delay(MessageDisplayDelay);
                    }
                }
            }
            finally
            {
                _displayPumpRunning = false;

                // A message may have arrived between the final dequeue and
                // clearing the pump flag.
                if (_isLoaded && _messageQueue.Count > 0)
                    _ = PumpMessagesAsync();
            }
        }

        private void ScrollConversationToBottom()
        {
            if (ConversationList.Items.Count == 0 ||
                _autoScrollPaused)
            {
                return;
            }

            // Follow the conversation only while the user has not deliberately
            // moved away from the bottom. Once the user scrolls upward, new
            // messages must not take control of the viewport back.
            Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Loaded,
                new Action(() =>
                {
                    if (_autoScrollPaused)
                        return;

                    ScrollViewer? scrollViewer =
                        _conversationScrollViewer ??
                        FindVisualChild<ScrollViewer>(ConversationList);

                    if (scrollViewer == null)
                        return;

                    _conversationScrollViewer = scrollViewer;

                    double target = scrollViewer.ScrollableHeight;
                    if (target <= 0)
                        return;

                    AnimateVerticalOffset(scrollViewer, target);
                }));
        }

        private void AnimateVerticalOffset(
            ScrollViewer scrollViewer,
            double target)
        {
            scrollViewer.BeginAnimation(VerticalOffsetProperty, null);

            DoubleAnimation animation = new()
            {
                From = scrollViewer.VerticalOffset,
                To = target,
                Duration = ConversationScrollDuration,
                EasingFunction = new QuadraticEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            animation.Completed += (_, _) =>
            {
                if (!_autoScrollPaused)
                    UpdateAutoScrollStateFromOffset();
            };

            scrollViewer.BeginAnimation(VerticalOffsetProperty, animation);
        }

        private static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.RegisterAttached(
                "AnimatedVerticalOffset",
                typeof(double),
                typeof(ConversationPanel),
                new PropertyMetadata(0.0, OnAnimatedVerticalOffsetChanged));

        private static void OnAnimatedVerticalOffsetChanged(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is ScrollViewer scrollViewer)
                scrollViewer.ScrollToVerticalOffset((double)e.NewValue);
        }

        private static T? FindVisualChild<T>(DependencyObject parent)
            where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child =
                    VisualTreeHelper.GetChild(parent, i);

                if (child is T match)
                    return match;

                T? nested = FindVisualChild<T>(child);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            e.Handled = true;

            if (DataContext is GuideViewModel guide &&
                guide.SendCommand.CanExecute(null))
            {
                guide.SendCommand.Execute(null);
            }

            Keyboard.Focus(InputBox);
        }

    }
}
