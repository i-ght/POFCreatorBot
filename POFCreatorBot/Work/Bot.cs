using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using POFCreatorBot.Declarations;
using DankLibWaifuz;
using DankLibWaifuz.HttpWaifu;
using DankLibWaifuz.CollectionsWaifu;
using System.Net;
using DankLibWaifuz.Etc;
using System.Net.Http;
using DankLibWaifuz.SettingsWaifu;
using System.Threading;
using System.Text.RegularExpressions;
using PofCreatorBot.Declarations;
using DankLibWaifuz.ScriptWaifu;

namespace POFCreatorBot.Work
{
    class Bot : Pof
    {

        public Bot(int index, ObservableCollection<DataGridItem> collection, Account account) : base(index, collection)
        {
            Account = account;
        }

        private bool _return;

        public async Task Base()
        {
            while (true)
            {
                try
                {
                    if (_return)
                        return;

                    Init();

                    if (!await GetGoodProxy().ConfigureAwait(false))
                    {
                        await SleepBeforeReconnect().ConfigureAwait(false);
                        continue;
                    }

                    if (!await TryLogin().ConfigureAwait(false))
                    {
                        Account.LoginErrors++;
                        await SleepBeforeReconnect().ConfigureAwait(false);
                        continue;
                    }

                    await MainBotLoop().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    await ErrorLogger.WriteAsync(e).ConfigureAwait(false);
                }
            }
        }

        private async Task SleepBeforeReconnect()
        {
            await UpdateThreadStatusAsync($"Sleeping before reconnect ({Account.LoginErrors})", 10000).ConfigureAwait(false);
        }

        private void Init()
        {
            if (Account.LoginErrors > Settings.Get<int>("MaxLoginErrors"))
            {
                _return = true;
                return;
            }

            var cfg = new HttpWaifuConfig
            {
                Proxy = Collections.BotProxies.GetNext().ToWebProxy(),
                UserAgent = Account.UserAgent,
                DefaultHeaders = new WebHeaderCollection
                {
                    ["HTTP_ACCEPT_LANGUAGE"] = "en",
                    ["x-Accepts"] = "compression",
                    ["x-Content-Encoding"] = "gzip"
                }
            };
            ApiClient = new HttpWaifu(cfg);
        }

        private async Task<bool> GetGoodProxy()
        {
            const string s = "Checking proxy: ";
            await AttemptingAsync(s);

            var request = new HttpReq(HttpMethod.Get, "http://httpbin.org/get")
            {
                Timeout = 8000
            };

            for (var i = 0; i < 5; i++)
            {
                var response = await ApiClient.SendRequestAsync(request).ConfigureAwait(false);
                if (response.IsOK)
                    return true;

                ApiClient.Config.Proxy = Collections.BotProxies.GetNext().ToWebProxy();
                await Task.Delay(500).ConfigureAwait(false);
            }

            return await FailedAsync(s).ConfigureAwait(false);
        }

        private async Task<bool> TryLogin()
        {
            if (await CheckToken().ConfigureAwait(false))
                return true;

            if (await Session().ConfigureAwait(false))
                return true;

            return false;
        }

        private async Task<bool> CheckToken()
        {
            const string s = "Checking token: ";
            await AttemptingAsync(s).ConfigureAwait(false);

            var req = Requests.BadgeCounts(Account.SessionToken, Account.UserAgent);
            var response = await SendApiRequestAsync(req).ConfigureAwait(false);
            if (!response.IsExpected("viewedCount"))
                return await UnexpectedResponseAsync(s).ConfigureAwait(false);

            Console.WriteLine(response.ContentBody);
            return true;
        }

        private async Task MainBotLoop()
        {
            try
            {
                Interlocked.Increment(ref Stats.Online);
                var breakLoop = false;
                
                if (!Account.SetInitialTimeout)
                {
                    Account.LastMsgReceived = DateTime.Now;
                    Account.SetInitialTimeout = true;
                }

                while (true)
                {
                    try
                    {
                        if (await TooManySendMsgErrors().ConfigureAwait(false))
                        {
                            _return = true;
                            breakLoop = true;
                            return;
                        }

                        if (await TimedOut().ConfigureAwait(false))
                        {
                            _return = true;
                            breakLoop = true;
                            return;
                        }

                        if (!await CheckToken().ConfigureAwait(false))
                        {
                            breakLoop = true;
                            return;
                        }

                        await HandleLiking().ConfigureAwait(false);
                        await HandleMessages().ConfigureAwait(false);
                    }
                    catch(Exception e)
                    {
                        await ErrorLogger.WriteAsync(e).ConfigureAwait(false);
                    }
                    finally
                    {
                        if (!breakLoop)
                            await DelaySession().ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                Interlocked.Decrement(ref Stats.Online);
            }
        }

        private async Task HandleLiking()
        {
            if (Settings.Get<bool>("DisableLiking"))
                return;

            if (Account.UidsToLike.Count == 0 && !await GetUsersToLike().ConfigureAwait(false))
                return;

            try
            {
                if (!Account.BlastSessionReady)
                    return;

                const string s = "Liking: ";
                await AttemptingAsync(s).ConfigureAwait(false);

                var attempts = 0;
                var cnt = 0;
                var max = Settings.GetRandom("LikesPerSession");

                string profileId;
                while (!string.IsNullOrWhiteSpace(profileId = Account.UidsToLike.GetNext(false)) && attempts++ < max)
                {
                    if (!await Like(profileId).ConfigureAwait(false))
                        continue;

                    cnt++;
                }

                if (cnt > 1)
                    await SuccessAsync(s, $"{cnt} interactions sent").ConfigureAwait(false);
                else if (cnt == 1)
                    await SuccessAsync(s, $"1 interaction sent").ConfigureAwait(false);
                else
                    await FailedAsync(s, "0 interactions sent").ConfigureAwait(false);
            }
            finally
            {
                Account.SessionCount();
            }
        }

        private async Task HandleMessages()
        {
            if (Settings.Get<bool>("DisableMessaging"))
                return;

            const string s = "Checking messages: ";
            await AttemptingAsync(s).ConfigureAwait(false);

            var firstPage = await LoadConvoPage().ConfigureAwait(false);
            if (firstPage.Count == 0)
            {
                await SuccessAsync(s, "0 returned").ConfigureAwait(false);
                return;
            }

            var pages = new List<List<IncomingConversation>>() { firstPage };

            if (firstPage.Count >= 20)
            {
                var currentPage = firstPage;

                while (true)
                {
                    var lastConvoId = currentPage[currentPage.Count - 1].ConvoId;
                    if (string.IsNullOrWhiteSpace(lastConvoId))
                        break;

                    currentPage = await LoadConvoPage(lastConvoId);
                    if (currentPage.Count == 0)
                        break;

                    pages.Add(currentPage);
                }
            }

            foreach (var page in pages)
            {
                foreach (var convo in page)
                {
                    if (convo.Replied == "true")
                        continue;

                    if (MsgBlacklistContains(Account.UserId, convo.FromUid, convo.MsgId))
                        continue;

                    if (Blacklists.Dict[BlacklistType.Chat].Contains(convo.FromUid))
                        continue;

                    if (convo.FromUsername == "markus")
                        continue;

                    var input = $"{Account.UserId}:{convo.FromUid}:{convo.MsgId}";
                    await AddBlacklistAsync(BlacklistType.Message, input).ConfigureAwait(false);

                    OnMessageReceived(convo.FromUid, "new message");

                    await HandleIncomingMessage(convo).ConfigureAwait(false);
                    //await Task.Run(() => HandleOutgoingMessage())

                }
            }
        }

        private async Task HandleIncomingMessage(IncomingConversation convo)
        {
            if (!Account.Convos.ContainsKey(convo.FromUid))
            {
                lock (Account.Convos)
                    Account.Convos.Add(convo.FromUid, new ScriptWaifu(Collections.Script));

                Interlocked.Increment(ref Stats.Convos);
            }

            if (Account.Convos[convo.FromUid].Pending)
                return;

            Account.Convos[convo.FromUid].Pending = true;

            var reply = Account.Convos[convo.FromUid].NextLineUnSpun();
            if (string.IsNullOrWhiteSpace(reply))
            {
                Account.Convos[convo.FromUid].Pending = false;
                return;
            }

            if (Account.Convos[convo.FromUid].IsComplete)
            {
                await AddBlacklistAsync(BlacklistType.Chat, convo.FromUid).ConfigureAwait(false);
                Interlocked.Increment(ref Stats.Completed);
            }

            var msg = new OutgoingMessage(convo.FromUsername, convo.FromUid, convo.ConvoId, reply);
            await Task.Run(() => HandleOutgoingMessage(msg).ConfigureAwait(false)).ConfigureAwait(false);
        }

        private async Task HandleOutgoingMessage(OutgoingMessage message)
        {
            try
            {
                var seconds = Settings.GetRandom("SendMessageDelay") * 1000;
                await Task.Delay(seconds).ConfigureAwait(false);

                for (var i = 0; i < 3; i++)
                {
                    if (!await SendMessage(message).ConfigureAwait(false))
                    {
                        await Task.Delay(5000).ConfigureAwait(false);
                        continue;
                    }

                    Account.FailedMsgSends = 0;
                    OnMessageSent(message);
                    return;
                }

                Interlocked.Increment(ref Account.FailedMsgSends);
                Interlocked.Increment(ref Stats.FailedMsgSends);
            }
            catch (Exception e)
            {
                await ErrorLogger.WriteAsync(e).ConfigureAwait(false);
            }
            finally
            {
                try { Account.Convos[message.ToUid].Pending = false; }
                catch { /*ignored haha c# fags triggered*/ }
            }
        }

        private void OnMessageSent(OutgoingMessage message)
        {
            Interlocked.Increment(ref Stats.Out);
            Interlocked.Increment(ref Account.Out);
            UpdateOutStat(Account.Out);

            if (message.HasLink)
                Interlocked.Increment(ref Stats.Links);
        }

        private void OnMessageReceived(string fromUid, string body)
        {
            UpdateInStat(++Account.In);
            Interlocked.Increment(ref Stats.In);
        }

        private static bool MsgBlacklistContains(string myUid, string theirUid, string msgId)
        {
            var input = $"{myUid}:{theirUid}:{msgId}";
            return Blacklists.Dict[BlacklistType.Message].Contains(input);
        }

        private static readonly Regex IncomingMessagesRegex = new Regex("\"conversationId\":(\\d+),\"utcSentDate\":\"(.*?)\",\".*?\"replied\":(.*?),.*?\"userId\":(\\d+),.*?\"userName\":\"(.*?)\"", 
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private async Task<List<IncomingConversation>> LoadConvoPage(string lastConvoId = "-1")
        {
            var req = Requests.ConversationsPage(Account.SessionToken, Account.UserAgent, lastConvoId);
            var response = await SendApiRequestAsync(req).ConfigureAwait(false);
            if (!response.IsExpected("conversations"))
                return new List<IncomingConversation>();

            var ret = new List<IncomingConversation>();
            foreach (Match match in IncomingMessagesRegex.Matches(response.ContentBody))
            {
                var convoId = match.Groups[1].Value;
                var msgId = match.Groups[2].Value;
                var replied = match.Groups[3].Value;
                var userId = match.Groups[4].Value;
                var username = match.Groups[5].Value;

                if (GeneralHelpers.AnyNullOrWhiteSpace(convoId, userId, username))
                    continue;

                if (username == "markus")
                    continue;

                Account.LastMsgReceived = DateTime.Now;

                ret.Add(new IncomingConversation(username, userId, convoId, msgId, replied));
            }

            return ret;
        }

        private async Task<bool> SendMessage(OutgoingMessage msg)
        {
            var req = Requests.SendMessage(Account.SessionToken, Account.UserAgent, Account.LoginId, msg.ReplyMsgId, msg.SourceId, msg.SourceStr, msg.Text, msg.ToUsername, msg.ToUid);
            var response = await SendApiRequestAsync(req).ConfigureAwait(false);
            if (!response.IsExpected("Success"))
                return false;

            return true;
        }

        private async Task<bool> Like(string profileId)
        {
            var req = Requests.LikeProfile(Account.SessionToken, Account.UserAgent, profileId);
            var response = await SendApiRequestAsync(req).ConfigureAwait(false);
            if (!response.IsExpected("\"success\":true"))
                return false;

            Interlocked.Increment(ref Stats.Likes);
            UpdateLikeStat(++Account.Likes);
            return true;
        }

        private static readonly Regex UidProfileIdRegex = new Regex("\"userId\":(\\d+),\"profileId\":(\\d+),", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private async Task<bool> GetUsersToLike()
        {
            const string s = "Getting uids to like: ";
            await AttemptingAsync(s).ConfigureAwait(false);

            var req = Requests.GetMeetMeUsers(Account.SessionToken, Account.UserAgent);
            var response = await SendApiRequestAsync(req).ConfigureAwait(false);
            if (!response.IsExpected("users"))
                return await UnexpectedResponseAsync(s).ConfigureAwait(false);

            var cnt = 0;
            lock (Account.UidsToLike)
                foreach (Match match in UidProfileIdRegex.Matches(response.ContentBody))
                {
                    var profileId = match.Groups[2].Value;
                    if (string.IsNullOrWhiteSpace(profileId))
                        continue;

                    Account.UidsToLike.Enqueue(profileId);
                    cnt++;
                }

            if (cnt == 0)
                return await FailedAsync(s, "0 returned").ConfigureAwait(false);
            else if (cnt == 1)
                return await SuccessAsync(s, "1 returned").ConfigureAwait(false);
            else
                return await SuccessAsync(s, $"{cnt} returned").ConfigureAwait(false);
        }

        private async Task<bool> TooManySendMsgErrors()
        {
            if (Account.FailedMsgSends < Settings.Get<int>("MaxMsgSendErrors"))
                return false;

            await UpdateThreadStatusAsync("Too many msg send errors").ConfigureAwait(false);
            return true;
        }

        private async Task<bool> TimedOut()
        {
            if (Account.LastMsgReceived.AddMinutes(Settings.Get<int>("AccountTimeOut")) > DateTime.Now)
                return false;

            await UpdateThreadStatusAsync("Timed out", 2000).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Delays between the main loop
        /// </summary>
        /// <returns></returns>
        private async Task DelaySession()
        {
            var seconds = Settings.GetRandom("SessionDelay");
            for (var i = seconds; i > 0; i--)
            {
                var msg = i <= 1 ?
                    $"Delaying: {i} second remains" :
                    $"Delaying: {i} seconds remain";
                await UpdateThreadStatusAsync(msg).ConfigureAwait(false);
            }
        }
    }
}
