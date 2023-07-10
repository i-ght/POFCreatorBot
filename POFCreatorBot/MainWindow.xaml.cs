using DankLibWaifuz.LocationsWaifu;
using DankLibWaifuz.SettingsWaifu;
using DankLibWaifuz.SettingsWaifu.SettingObjects;
using DankLibWaifuz.SettingsWaifu.SettingObjects.Collections;
using HtmlAgilityPack;
using PofCreatorBot.Declarations;
using POFCreatorBot.Declarations;
using POFCreatorBot.Work;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace POFCreatorBot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SettingsUi _settingsWaifu;
        private bool _running;
        private int _maxWorkers;

        public ObservableCollection<DataGridItem> ThreadMonitorSource { get; } = new ObservableCollection<DataGridItem>();

        public MainWindow()
        {
            var unxored = new byte[] { 248, 9, 172, 18, 33, 59, 103, 50, 178, 166, 137, 79, 92, 119, 226, 72, 2, 101, 1, 89, 127, 80, 20, 130, 210, 215, 182, 78, 224, 43, 210, 233, 220, 7, 188, 2, 100, 149, 74, 248, 225, 155, 59, 38, 62, 156, 65, 234, 245, 170, 87, 185, 81, 91, 169, 86, 142, 114, 70, 71, 167, 161, 146, 38, 109, 157, 214, 213, 140, 177, 244, 120, 217, 8, 203, 231, 50, 87, 43, 12, 38, 221, 254, 65, 96, 222, 220, 251, 83, 88, 125, 177, 75, 9, 167, 183, 131, 151, 32, 60, 88, 72, 76, 58, 0, 139, 248, 124, 100, 36, 186, 137, 179, 19, 115, 218, 32, 103, 113, 162, 230, 15, 26, 35, 235, 240, 12, 75, 10, 212, 152, 15, 228, 54, 206, 150, 101, 15, 246, 170, 125, 102, 165, 179, 2, 132, 129, 253, 187, 190, 211, 126, 236, 217, 46, 197, 55, 130, 26, 213, 93, 90, 28, 188, 226, 241, 87, 62, 127, 71, 179, 222, 80, 234, 50, 84, 173, 24, 230, 105, 221, 112, 248, 187, 106, 141, 160, 73, 225, 189, 253, 224, 240, 216, 175, 12, 71, 148, 8, 224, 69, 8, 232, 113, 203, 89, 216, 111, 181, 4, 187, 196, 247, 88, 125, 133, 226, 36, 10, 139, 174, 48, 33, 35, 106, 93, 71, 203, 199, 255, 76, 43, 122, 231, 37, 51, 169, 139, 34, 197, 230, 20, 99, 70, 100, 155, 205, 242, 48, 135, 154, 221, 31, 39, 132, 93, 52, 234, 93, 197, 124, 86, 197, 195, 152, 208, 130, 249, 22, 11, 60, 227, 137, 231, 0, 95, 54, 218, 6, 52, 146, 202, 20, 40, 99, 214, 135, 117, 109, 167, 226, 73, 239, 132, 202, 27, 203, 245, 6, 112, 232, 248, 170, 127, 213, 35, 152, 22, 73, 26, 113, 5, 59, 98, 246, 110, 116, 25, 13, 58, 19, 212, 144, 151, 153, 211, 217, 154, 75, 66, 5, 32, 93, 46, 100, 101, 171, 225, 150, 192, 123, 217, 248, 13, 88, 94, 5, 211, 198, 146, 94, 74, 65, 197, 23, 8, 48, 237, 133, 60, 223, 157, 139, 231, 12, 240, 241, 235, 14, 91, 146, 13, 244, 68, 225, 183, 164, 167, 43, 21, 57, 145, 108, 83, 80, 7, 187 };
            var xored = new byte[] { 248, 9, 172, 18, 217, 50, 203, 32, 74, 175, 37, 93, 164, 126, 78, 90, 250, 108, 173, 75, 135, 89, 184, 144, 42, 222, 26, 92, 24, 34, 126, 251, 36, 14, 16, 16, 156, 156, 230, 234, 25, 146, 151, 52, 198, 149, 237, 248, 13, 163, 251, 171, 169, 82, 5, 68, 118, 123, 234, 85, 95, 168, 62, 52, 149, 148, 122, 199, 116, 184, 88, 106, 33, 1, 103, 245, 202, 94, 135, 30, 222, 212, 82, 83, 152, 215, 112, 233, 171, 81, 209, 163, 179, 0, 11, 165, 123, 158, 140, 46, 160, 65, 224, 40, 248, 130, 84, 110, 156, 45, 22, 155, 75, 26, 223, 200, 216, 110, 221, 176, 30, 6, 182, 49, 19, 249, 160, 89, 242, 221, 52, 29, 28, 63, 98, 132, 157, 6, 90, 184, 133, 111, 9, 161, 250, 141, 45, 239, 67, 183, 127, 108, 20, 208, 130, 215, 207, 139, 182, 199, 165, 83, 176, 174, 26, 248, 251, 44, 135, 78, 31, 204, 168, 227, 158, 70, 85, 17, 74, 123, 37, 121, 84, 169, 146, 132, 12, 91, 25, 180, 81, 242, 8, 209, 3, 30, 191, 157, 164, 242, 189, 1, 68, 99, 51, 80, 116, 125, 77, 13, 23, 214, 15, 81, 209, 151, 26, 45, 166, 153, 86, 57, 141, 49, 146, 84, 235, 217, 63, 246, 224, 57, 130, 238, 137, 33, 81, 130, 142, 215, 30, 29, 207, 84, 156, 146, 97, 224, 200, 142, 54, 207, 231, 46, 40, 79, 204, 227, 241, 215, 132, 95, 105, 209, 96, 217, 46, 235, 238, 2, 144, 241, 113, 238, 172, 77, 206, 211, 170, 38, 106, 195, 184, 58, 155, 223, 43, 103, 149, 174, 78, 91, 23, 141, 102, 9, 51, 252, 170, 98, 16, 241, 6, 109, 45, 42, 52, 4, 177, 19, 221, 23, 195, 107, 90, 124, 140, 16, 161, 40, 235, 221, 60, 133, 97, 218, 117, 136, 179, 75, 169, 50, 165, 39, 200, 119, 83, 232, 58, 210, 131, 208, 84, 31, 160, 87, 169, 193, 62, 155, 242, 88, 185, 204, 187, 26, 200, 228, 41, 46, 39, 148, 39, 245, 244, 249, 93, 249, 246, 82, 62, 31, 12, 77, 77, 165, 92, 174, 135, 21, 57, 145, 108, 83, 80, 7, 187 };

            Crypto.Xor(xored, 4);

            Console.WriteLine(unxored.SequenceEqual(xored));

            InitializeComponent();
            ThreadMonitor.DataContext = this;
            Location.CoordinatesLen = 7;
            HtmlNode.ElementsFlags.Remove("option");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Settings.Load();
            Blacklists.Load();
            InitSettingsUi();

            ThreadMonitor.LoadingRow += (o, args) =>
            {
                args.Row.Header = (args.Row.GetIndex() + 1).ToString();
            };
        }

        private void InitSettingsUi()
        {
            var settings = new ObservableCollection<SettingObj>
            {
                new SettingPrimitive<int>("Max concurrent creator requests", "MaxConcurrentRequests", 30),
                new SettingPrimitive<int>("Max workers", "MaxWorkers", 1),
                new SettingPrimitive<int>("Max login errors", "MaxLoginErrors", 8),
                new SettingPrimitive<int>("Minimum delay before username gets focus", "MinDelayBeforeUsernameInputGetsFocus", 5),
                new SettingPrimitive<int>("Maximum before username gets focus", "MaxDelayBeforeUsernameInputGetsFocus", 11),
                new SettingPrimitive<int>("Minimum delay before registration", "MinDelayBeforeRegistration", 60),
                new SettingPrimitive<int>("Maximum delay before username gets focus", "MaxDelayBeforeRegistration", 70),
                new SettingPrimitive<int>("Minimum delay before submitting questionaire", "MinDelayBeforeSubmitQuestionaire", 70),
                new SettingPrimitive<int>("Maximum delay before submitting questionaire", "MaxDelayBeforeSubmitQuestionaire", 90),
                new SettingPrimitive<int>("Minimum delay before uploading image", "MinImageUploadDelay", 10),
                new SettingPrimitive<int>("Maximum delay before uploading image", "MaxImageUploadDelay", 17),
                new SettingPrimitive<int>("Minimum session delay", "MinSessionDelay", 30),
                new SettingPrimitive<int>("Maximum session delay", "MaxSessionDelay", 38),
                new SettingPrimitive<int>("Minimum likes per session", "MinLikesPerSession", 4),
                new SettingPrimitive<int>("Maximum likes per session", "MaxLikesPerSession", 9),
                new SettingPrimitive<int>("Minimum send message delay", "MinSendMessageDelay", 20),
                new SettingPrimitive<int>("Maximum send message delay", "MaxSendMessageDelay", 55),
                new SettingPrimitive<int>("Time out account after (x) min", "AccountTimeOut", 15),
                new SettingPrimitive<int>("Max msg send errors", "MaxMsgSendErrors", 10),
                new SettingPrimitive<int>("Like every (x) sessions", "LikeEvery", 3),
                new SettingPrimitive<bool>("Disable liking?", "DisableLiking", false),
                new SettingPrimitive<bool>("Disable msging?", "DisableMessaging", false),
                new SettingQueue("Creator proxies", "CreatorProxies"),
                new SettingQueue("Creator form submit proxies", "CreatorFormSubmitProxies"),
                new SettingQueue("Bot proxies", "BotProxies"),
                new SettingQueue("First names", "FirstNames"),
                new SettingQueue("Last names", "LastNames"),
                new SettingQueue("Locations"),
                new SettingPrimitive<string>("Image directories", "ImageDirs", string.Empty),
                new SettingQueue("Interests"),
                new SettingQueue("About me", "AboutMe"),
                new SettingQueue("Headlines"),
                new SettingQueue("Professions"),
                new SettingList("Script"),
                new SettingQueue("Links"),
                
            };
            var settingsPage = (TabItem)TbMain.Items[1];
            var gridContent = (Grid)settingsPage.Content;

            _settingsWaifu = new SettingsUi(this, gridContent, settings);
            _settingsWaifu.CreateUi();

            Collections.SettingsUi = _settingsWaifu;
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs args)
        {
            _settingsWaifu.SavePrimitives();
            Process.GetCurrentProcess().Kill();
        }

        private void InitThreadMonitor()
        {
            _maxWorkers = Settings.Get<int>("MaxWorkers");

            ThreadMonitorSource.Clear();

            for (var i = 0; i < _maxWorkers; i++)
            {
                var item = new DataGridItem
                {
                    Account = (i + 1).ToString(),
                    Status = string.Empty,
                    InCount = 0,
                    OutCount = 0,
                    LikeCount = 0
                };
                ThreadMonitorSource.Add(item);
            }
        }

        private void StatsUi()
        {
            var start = DateTime.Now;
            while (_running)
            {
                Thread.Sleep(950);

                var runTime = DateTime.Now.Subtract(start);

                Dispatcher.Invoke(() =>
                {
                    Title =
                        $"{Assembly.GetExecutingAssembly().GetName().Name} {Assembly.GetExecutingAssembly().GetName().Version} " +
                        $"[{string.Format("{3:D2}:{0:D2}:{1:D2}:{2:D2}", runTime.Hours, runTime.Minutes, runTime.Seconds, runTime.Days)}]";

                    LblAttempts.Content = $"Attempts: [{Stats.Attempts.ToString("N0")}]";
                    LblCreated.Content = $"Created: [{Stats.Created.ToString("N0")}]";
                    LblOnline.Content = $"Online: [{Stats.Online.ToString("N0")}]";
                    LblLikes.Content = $"Likes: [{Stats.Likes.ToString("N0")}]";
                    LblConvos.Content = $"Convos: [{Stats.Convos.ToString("N0")}]";
                    LblIn.Content = $"In: [{Stats.In.ToString("N0")}]";
                    LblOut.Content = $"Out: [{Stats.Out.ToString("N0")}]";
                    LblLinks.Content = $"Links: [{Stats.Links.ToString("N0")}]";
                    LblCompleted.Content = $"Completed: [{Stats.Completed.ToString("N0")}]";
                    LblFailedMsgSends.Content = $"Failed msg sends: [{Stats.FailedMsgSends.ToString("N0")}]";
                });
            }
        }

        private async Task Init()
        {
            _running = true;
            new Thread(StatsUi).Start();

            var tasks = new List<Task>();
            for (var i = 0; i < _maxWorkers; i++)
            {
                var cls = new Creator(i, ThreadMonitorSource);
                tasks.Add(cls.Base());
            }

            await Task.WhenAll(tasks);

            _running = false;
            CmdLaunch.IsEnabled = true;
            MessageBox.Show("Work complete");
        }

        private async void CmdLaunch_OnClick(object sender, RoutedEventArgs e)
        {
            _settingsWaifu.SavePrimitives();

            if (!Collections.IsValid())
            {
                MessageBox.Show("Missing required files");
                return;
            }

            Collections.Shuffle();

            InitThreadMonitor();

            CmdLaunch.IsEnabled = false;

            await Init().ConfigureAwait(false);
        }
    }
}
