using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace GraceAI
{
   
    public partial class MainWindow : Window
    {
      
        private readonly Chatbot _chatbot = new();
        private readonly SpeechSynthesizer _synth = new();
        private readonly DispatcherTimer _clockTimer = new();
        private readonly ObservableCollection<string> _detectedKeywords = new();

     
        private bool _speechEnabled = true;
        private int _tipIndex = 0;
        private readonly Random _rand = new();

      
        private static readonly Dictionary<string, (Color border, Color bg, Color text)> MessageColors = new()
        {
            ["bot"]       = (Color.FromRgb(0, 100, 140),  Color.FromRgb(13, 26, 48),  Color.FromRgb(200, 216, 232)),
            ["user"]      = (Color.FromRgb(0, 80, 60),    Color.FromRgb(10, 30, 24),  Colors.White),
            ["success"]   = (Color.FromRgb(0, 180, 100),  Color.FromRgb(10, 30, 20),  Color.FromRgb(180, 255, 200)),
            ["warning"]   = (Color.FromRgb(200, 150, 0),  Color.FromRgb(30, 24, 8),   Color.FromRgb(255, 220, 100)),
            ["error"]     = (Color.FromRgb(200, 40, 40),  Color.FromRgb(30, 8, 8),    Color.FromRgb(255, 160, 160)),
            ["info"]      = (Color.FromRgb(60, 100, 180), Color.FromRgb(10, 20, 40),  Color.FromRgb(180, 210, 255)),
            ["topic"]     = (Color.FromRgb(0, 180, 220),  Color.FromRgb(8, 22, 42),   Colors.White),
            ["sentiment"] = (Color.FromRgb(160, 80, 220), Color.FromRgb(20, 10, 36),  Color.FromRgb(220, 180, 255)),
            ["farewell"]  = (Color.FromRgb(0, 140, 180),  Color.FromRgb(8, 20, 38),   Color.FromRgb(180, 230, 255)),
            ["system"]    = (Color.FromRgb(40, 70, 100),  Color.FromRgb(8, 14, 24),   Color.FromRgb(100, 140, 180)),
        };

        public MainWindow()
        {
            InitializeComponent();
            KeywordsList.ItemsSource = _detectedKeywords;

            ConfigureSpeech();
            ConfigureClock();
            StartSession();
        }

        private void ConfigureSpeech()
        {
            try
            {
                
                var voices = _synth.GetInstalledVoices();
                var preferred = voices.FirstOrDefault(v =>
                    v.VoiceInfo.Name.Contains("David", StringComparison.OrdinalIgnoreCase));

                if (preferred != null)
                    _synth.SelectVoice(preferred.VoiceInfo.Name);

                _synth.Rate = 2;
                _synth.Volume = 80;
            }
            catch
            {
                _speechEnabled = false;
            }
        }

        private void ConfigureClock()
        {
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += (_, _) =>
                ClockLabel.Text = DateTime.Now.ToString("HH:mm:ss");
            _clockTimer.Start();
            ClockLabel.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private async void StartSession()
        {
     
            await Task.Delay(400);

            SpeakAsync("Hi there! Welcome to the Cybersecurity Awareness Bot. I'm Grace, here to help you stay safe online. Can you please tell me your name?");

        
            AppendBotMessage(
                "🛡 Hello! I'm GRACE — your Cybersecurity Awareness Bot.\n\n" +
                "I'm here to help you stay safe in the digital world. " +
                "Let's start by getting to know each other.\n\n" +
                "👋 What's your name?",
                "bot");

            UpdateSidePanels();
            InputBox.Focus();
        }

      
        private void BtnSend_Click(object sender, RoutedEventArgs e) => HandleSend();

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift))
            {
                e.Handled = true;
                HandleSend();
            }
        }

        private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
         
            InputPlaceholder.Visibility = string.IsNullOrEmpty(InputBox.Text)
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void TopicButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string topic)
            {
             
                AppendUserMessage($"[Topic selected: {topic}]");

                var result = _chatbot.ProcessTopic(topic);
                HandleChatResult(result);
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
          
            while (ChatPanel.Children.Count > 1)
                ChatPanel.Children.RemoveAt(ChatPanel.Children.Count - 1);

            AppendSystemMessage("💬 Chat cleared. Your session memory is preserved.");
            UpdateSidePanels();
        }

        private void BtnRestart_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show(
                "Restart your session? This will clear the chat and reset your profile.",
                "Restart Session", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            _chatbot.ResetSession();
            _detectedKeywords.Clear();

            while (ChatPanel.Children.Count > 1)
                ChatPanel.Children.RemoveAt(ChatPanel.Children.Count - 1);

            UpdateSidePanels();
            StartSession();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show(
                "Are you sure you want to exit GRACE AI?",
                "Exit", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm == MessageBoxResult.Yes)
                Application.Current.Shutdown();
        }


        private void HandleSend()
        {
            var text = InputBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                AppendBotMessage("⚠ Please type something before sending!", "warning");
                ShakeInputBox();
                return;
            }

          
            AppendUserMessage(text);
            InputBox.Clear();
            InputBox.Focus();

        
            try
            {
                var result = _chatbot.Process(text);
                HandleChatResult(result);
            }
            catch (Exception ex)
            {
                AppendBotMessage($"⚠ An error occurred: {ex.Message}", "error");
            }
        }

        private void HandleChatResult(ChatResult result)
        {
            if (result.IsSpecialCommand)
            {
                if (result.SpecialCommand == "clear")
                    BtnClear_Click(null!, null!);
                return;
            }

            if (result.ShouldExit)
            {
                AppendBotMessage(result.BotMessage, result.MessageType);
                SpeakAsync("Goodbye! Stay safe and take care!");
                UpdateSidePanels();

               
                Dispatcher.InvokeAsync(async () =>
                {
                    await Task.Delay(3000);
                    var close = MessageBox.Show(
                        "GRACE AI has said goodbye. Close the application?",
                        "Session Ended", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (close == MessageBoxResult.Yes)
                        Application.Current.Shutdown();
                });
                return;
            }

            AppendBotMessage(result.BotMessage, result.MessageType);

        
            if (!string.IsNullOrEmpty(result.DetectedTopic))
            {
                var display = $"🔍 {result.DetectedTopic}";
                if (!_detectedKeywords.Contains(display))
                    _detectedKeywords.Insert(0, display);

            
                while (_detectedKeywords.Count > 8)
                    _detectedKeywords.RemoveAt(_detectedKeywords.Count - 1);

                NoKeywordsLabel.Visibility = Visibility.Collapsed;
                SpeakTopicIntro(result.DetectedTopic);
            }
            else
            {
               
                var speakText = result.BotMessage.Length > 200
                    ? result.BotMessage.Substring(0, 200) + "..."
                    : result.BotMessage;
                SpeakAsync(speakText);
            }

            UpdateSidePanels();
        }


        private void AppendUserMessage(string text)
        {
            var (borderColor, bgColor, textColor) = MessageColors["user"];

            var grid = new Grid { Margin = new Thickness(60, 4, 0, 4) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var bubble = BuildBubble(text, bgColor, borderColor, textColor, isUser: true);
            Grid.SetColumn(bubble, 1);

            var label = new TextBlock
            {
                Text = $"🧑  {(_chatbot.Memory.IsNameKnown() ? _chatbot.Memory.UserName : "You")}",
                FontSize = 10,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = new SolidColorBrush(Color.FromRgb(100, 160, 200)),
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 4, 2)
            };

            var wrapper = new StackPanel();
            wrapper.Children.Add(label);
            wrapper.Children.Add(grid);
            grid.Children.Add(bubble);

            FadeIn(wrapper);
            ChatPanel.Children.Add(wrapper);
            ScrollToBottom();
        }

        private void AppendBotMessage(string text, string type)
        {
            if (!MessageColors.TryGetValue(type, out var colors))
                colors = MessageColors["bot"];

            var (borderColor, bgColor, textColor) = colors;

            var prefix = type switch
            {
                "success"   => "✅ GRACE AI",
                "warning"   => "⚠ GRACE AI",
                "error"     => "❌ GRACE AI",
                "info"      => "ℹ GRACE AI",
                "topic"     => "🛡 GRACE AI",
                "sentiment" => "💬 GRACE AI",
                "farewell"  => "👋 GRACE AI",
                "system"    => "⚙ SYSTEM",
                _           => "🤖 GRACE AI"
            };

            var label = new TextBlock
            {
                Text = $"{prefix}  •  {DateTime.Now:HH:mm}",
                FontSize = 10,
                FontFamily = new FontFamily("Consolas"),
                Foreground = new SolidColorBrush(borderColor) { Opacity = 0.8 },
                Margin = new Thickness(4, 0, 0, 2)
            };

            var bubble = BuildBubble(text, bgColor, borderColor, textColor, isUser: false);

            var wrapper = new StackPanel { Margin = new Thickness(0, 4, 60, 4) };
            wrapper.Children.Add(label);
            wrapper.Children.Add(bubble);

            FadeIn(wrapper);
            ChatPanel.Children.Add(wrapper);
            ScrollToBottom();
        }

        private void AppendSystemMessage(string text)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 11,
                FontFamily = new FontFamily("Consolas"),
                Foreground = new SolidColorBrush(Color.FromRgb(60, 100, 140)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 8),
                TextWrapping = TextWrapping.Wrap
            };
            FadeIn(tb);
            ChatPanel.Children.Add(tb);
            ScrollToBottom();
        }

        private Border BuildBubble(string text, Color bg, Color border, Color textColor, bool isUser)
        {
            var tb = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13.5,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = new SolidColorBrush(textColor),
                LineHeight = 20,
                Padding = new Thickness(0)
            };

            var bubble = new Border
            {
                Background = new SolidColorBrush(bg),
                BorderBrush = new SolidColorBrush(border),
                BorderThickness = new Thickness(1),
                CornerRadius = isUser
                    ? new CornerRadius(12, 2, 12, 12)
                    : new CornerRadius(2, 12, 12, 12),
                Padding = new Thickness(14, 10, 14, 10),
                MaxWidth = 700,
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Child = tb,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = border,
                    BlurRadius = 6,
                    ShadowDepth = 0,
                    Opacity = 0.2
                }
            };

            return bubble;
        }


        private void UpdateSidePanels()
        {
       
            MemUserName.Text = _chatbot.Memory.IsNameKnown() ? _chatbot.Memory.UserName : "—";
            MemMood.Text = !string.IsNullOrEmpty(_chatbot.Memory.UserMood)
                ? _chatbot.Memory.UserMood : "—";
            MemTopic.Text = !string.IsNullOrEmpty(_chatbot.Memory.FavouriteTopic)
                ? _chatbot.Memory.FavouriteTopic : "—";
            MemMessages.Text = _chatbot.Memory.MessageCount.ToString();

            
            UserStatusLabel.Text = _chatbot.Memory.IsNameKnown()
                ? $"Session: {_chatbot.Memory.UserName}"
                : "Guest Session";

       
            var score = _chatbot.Memory.AwarenessScore;
            AwarenessBar.Value = score;
            AwarenessLabel.Text = score switch
            {
                0       => "0% — Just getting started",
                < 25    => $"{score}% — Beginner",
                < 50    => $"{score}% — Developing",
                < 75    => $"{score}% — Intermediate",
                < 90    => $"{score}% — Advanced",
                _       => $"{score}% — Cyber Expert! 🏆"
            };

          
            _tipIndex = (_tipIndex + 1) % Responses.SecurityTips.Count;
            SecurityTipLabel.Text = Responses.SecurityTips[_tipIndex];


            var topicCount = _chatbot.Memory.TopicsExplored;
            StatusLabel.Text = topicCount > 0
                ? $"GRACE AI active  •  {topicCount} topic(s) explored  •  {_chatbot.Memory.MessageCount} message(s) sent"
                : "GRACE AI ready. Type your name to begin your cybersecurity session.";

          
            NoKeywordsLabel.Visibility = _detectedKeywords.Count == 0
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SpeakAsync(string text)
        {
            if (!_speechEnabled) return;
            try
            {
               
                Task.Run(() =>
                {
                    try { _synth.SpeakAsync(text); }
                    catch { /* speech failure is non-fatal */ }
                });
            }
            catch { /* ignore */ }
        }

        private void SpeakTopicIntro(string topic)
        {
            var intro = topic switch
            {
                "phishing"                  => $"Here's what you need to know about phishing.",
                "password safety"           => $"Let me tell you about keeping your passwords safe.",
                "two-factor authentication" => $"Two-factor authentication is one of the best defences you have.",
                "ransomware"                => $"Ransomware is a serious threat. Here's how to protect yourself.",
                "social engineering"        => $"Social engineering targets your mind, not your machine.",
                "safe browsing"             => $"Here are some safe browsing tips for you.",
                "malware"                   => $"Let me explain malware and how to stay protected.",
                "cyber hygiene"             => $"Good cyber hygiene is your daily digital health routine.",
                "scams"                     => $"Scams are everywhere. Here's how to spot and avoid them.",
                "privacy"                   => $"Your privacy matters. Here's how to protect it.",
                _                           => $"Here's some information on {topic}."
            };
            SpeakAsync(intro);
        }

   

        private void ScrollToBottom()
        {
            Dispatcher.InvokeAsync(() =>
                ChatScrollViewer.ScrollToBottom(),
                DispatcherPriority.Background);
        }

        private static void FadeIn(UIElement element)
        {
            element.Opacity = 0;
            var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            element.BeginAnimation(OpacityProperty, anim);
        }

        private void ShakeInputBox()
        {
            var transform = new TranslateTransform();
            InputBox.RenderTransform = transform;

            var anim = new DoubleAnimationUsingKeyFrames();
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(0,   KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(-8,  KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(50))));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(8,   KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(100))));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(-8,  KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(150))));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(8,   KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200))));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(0,   KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(250))));

            transform.BeginAnimation(TranslateTransform.XProperty, anim);
        }

        protected override void OnClosed(EventArgs e)
        {
            _clockTimer.Stop();
            _synth.Dispose();
            base.OnClosed(e);
        }
    }
}
