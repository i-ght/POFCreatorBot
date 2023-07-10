using System.Collections.ObjectModel;
using System.Threading.Tasks;
using POFCreatorBot.Declarations;
using DankLibWaifuz;
using System;
using DankLibWaifuz.HttpWaifu;
using DankLibWaifuz.CollectionsWaifu;
using DankLibWaifuz.LocationsWaifu;
using DankLibWaifuz.Etc;
using System.Net;
using System.Text.RegularExpressions;
using System.Net.Http;
using HtmlAgilityPack;
using System.Linq;
using DankLibWaifuz.SettingsWaifu;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace POFCreatorBot.Work
{
    class Creator : Pof
    {
        private static readonly SemaphoreSlim Semaphore;

        static Creator()
        {
            var max = Settings.Get<int>("MaxConcurrentRequests");
            Semaphore = new SemaphoreSlim(max, max);
        }

        public Creator(int index, ObservableCollection<DataGridItem> collection) : base(index, collection)
        {
        }

        private HttpWaifu _webBrowserClient;

        public async Task Base()
        {
            while (true)
            {
                try
                {
                    ResetUiStats();

                    await Semaphore.WaitAsync().ConfigureAwait(false);

                    try
                    {
                        if (!await Init().ConfigureAwait(false))
                            continue;

                        try
                        {
                            if (!await GetExperimentsAllUsers().ConfigureAwait(false))
                                continue;

                            if (!await RecordAppInstall().ConfigureAwait(false))
                                continue;

                            if (!await GetRegUrl().ConfigureAwait(false))
                                continue;

                            if (!await LoadRegUrl().ConfigureAwait(false))
                                continue;

                            if (!await HandlePt().ConfigureAwait(false))
                                continue;

                            await DelayBeforeCheckingUsername().ConfigureAwait(false);

                            if (!await CheckUsername().ConfigureAwait(false))
                                continue;

                            CalculateGreen();

                            await Delay("DelayBeforeRegistration", "registration").ConfigureAwait(false);

                            if (!await Register().ConfigureAwait(false))
                                continue;

                            await SdfSdg().ConfigureAwait(false);

                            await Delay("DelayBeforeSubmitQuestionaire", "submitting questionaire").ConfigureAwait(false);

                            if (!await Questionaire().ConfigureAwait(false))
                                continue;
                        }
                        finally
                        {
                            if (Account.RegFormVars != null)
                            {
                                Account.RegFormVars.Clear();
                                Account.RegFormVars = null;
                            }

                            if (Account.ProfileFormVars != null)
                            {
                                Account.ProfileFormVars.Clear();
                                Account.ProfileFormVars = null;
                            }

                            if (Account.HtmlDoc != null)
                            {
                                Account.HtmlDoc.DocumentNode.RemoveAll();
                                Account.HtmlDoc = null;
                            }
                        }

                        if (!await Session().ConfigureAwait(false))
                            continue;

                        var prompts = PromptsSources();
                        var setLoc = SetLocation();

                        await Task.WhenAll(prompts, setLoc).ConfigureAwait(false);

                        if (!await UploadImageStatus().ConfigureAwait(false))
                            continue;

                        await ExperimentsLoggedIn().ConfigureAwait(false);

                        if (!await UploadImages().ConfigureAwait(false))
                            continue;

                        var convoCount = ConvoCount();
                        var badgeCount = BadgeCount();
                        var closeBy = CloseBy();

                        await Task.WhenAll(convoCount, badgeCount, closeBy).ConfigureAwait(false);

                        await AccountCreated().ConfigureAwait(false);
                    }
                    finally
                    {
                        Semaphore.Release();
                    }

                    var bot = new Bot(_index, _collection, Account);
                    await bot.Base().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    await ErrorLogger.WriteAsync(e).ConfigureAwait(false);
                }
            }
        }

        private new void ResetUiStats()
        {
            UpdateAccountColumn(string.Empty);
            UpdateInStat(0);
            UpdateOutStat(0);
            UpdateLikeStat(0);
        }

        private async Task<bool> HandlePt()
        {
            var pt = CalculatePt();
            var ptb = CalculatePtb();
            var ptResults = await Task.WhenAll(pt, ptb).ConfigureAwait(false);
            if (ptResults.Any(r => false))
                return false;

            return true;
        }

        private async Task<bool> Init()
        {
            var firstName = Collections.FirstNames.GetNext();
            var lastName = Collections.LastNames.GetNext();
            var aboutMe = Collections.AboutMe.GetNext();
            var headline = Collections.Headlines.GetNext();
            var interests = Collections.Interests.GetCommaSeperatedItems();
            var profession = Collections.Professions.GetNext();
            var imagesToUpload = GeneralHelpers.GetJpgsFromDir(Collections.ImageDirs.GetNext());

            var device = new AndroidDevice(AndroidDeviceData.Queue.GetNext());
            if (!device.IsValid)
            {
                await UpdateThreadStatusAsync("Invalid device", 2000).ConfigureAwait(false);
                return false;
            }

            Location location;
            if (Collections.Locations.Count == 0)
                location = PopulatedLocationsData.Queue.GetNext();
            else
                location = new Location(Collections.Locations.GetNext());

            if (!location.IsValid)
            {
                await UpdateThreadStatusAsync("Invalid location", 2000).ConfigureAwait(false);
                return false;
            }

            Account = new Account(firstName, lastName, aboutMe, headline, interests, profession, location, device, imagesToUpload);
            if (!Account.IsValid)
            {
                await UpdateThreadStatusAsync("Invalid account").ConfigureAwait(false);
                return false;
            }

            var cfg = new HttpWaifuConfig
            {
                Proxy = Collections.CreatorProxies.GetNext().ToWebProxy(),
                UserAgent = Account.UserAgent,
                DefaultHeaders = new WebHeaderCollection
                {
                    ["HTTP_ACCEPT_LANGUAGE"] = "en",
                    ["x-Accepts"] = "compression",
                    ["x-Content-Encoding"] = "gzip"
                } 
            };
            ApiClient = new HttpWaifu(cfg);

            var webBrowserCfg = new HttpWaifuConfig
            {
                Proxy = cfg.Proxy,
                UserAgent = Account.UserAgent,
                DefaultHeaders = new WebHeaderCollection
                {
                    ["Accept-Language"] = "en-US"
                }
            };
            _webBrowserClient = new HttpWaifu(webBrowserCfg);

            Interlocked.Increment(ref Stats.Attempts);
            UpdateAccountColumn(Account.LoginId);
            return true;
        }

        private async Task AccountCreated()
        {
            Interlocked.Increment(ref Stats.Created);
            await UpdateThreadStatusAsync("Account created", 1000).ConfigureAwait(false);
        }

        private async Task<bool> GetExperimentsAllUsers()
        {
            const string s = "Getting experiments: ";
            await AttemptingAsync(s).ConfigureAwait(false);

            var req = Requests.ExperimentsForAllUsers(Account.InstallId, Account.AndroidId, Account.UserAgent);
            var response = await SendApiRequestAsync(req).ConfigureAwait(false);
            if (!response.IsExpected("experiments"))
                return await UnexpectedResponseAsync(s).ConfigureAwait(false);

            return true;
        }

        private async Task<bool> RecordAppInstall()
        {
            const string s = "Recording app install: ";
            var delay = Random.Next(900, 1100);
            await AttemptingAsync(s, delay).ConfigureAwait(false);

            var req = Requests.RecordAppInstall(Account.UserAgent);
            var response = await SendApiRequestAsync(req).ConfigureAwait(false);
            if (!response.IsExpected("\"success\":true"))
                return await UnexpectedResponseAsync(s).ConfigureAwait(false);

            return true;
        }

        private static readonly Regex UrlRegex = new Regex("\"url\":\"(.*?)\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private async Task<bool> GetRegUrl()
        {
            const string s = "Getting reg url: ";
            var delay = Random.Next(3000, 7000);
            await AttemptingAsync(s, delay).ConfigureAwait(false);

            var req = Requests.RegistrationUrl(Account.AppSessionId, Account.InstallId, Account.AndroidDevice, Account.UserAgent);
            var response = await SendApiRequestAsync(req).ConfigureAwait(false);
            if (!response.IsExpected("url"))
                return await UnexpectedResponseAsync(s).ConfigureAwait(false);

            string url;
            if (!UrlRegex.TryGetGroup(response.ContentBody, out url))
                return await FailedAsync("Failed to parse url").ConfigureAwait(false);

            Account.RegistrationUrl = url;
            return true;
        }

        private async Task<bool> LoadRegUrl()
        {
            const string s = "Loading reg url: ";
            await AttemptingAsync(s).ConfigureAwait(false);

            var request = new HttpReq(HttpMethod.Get, Account.RegistrationUrl)
            {
                Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8",
                AcceptEncoding = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                AdditionalHeaders = new WebHeaderCollection
                {
                    ["x-user-agent"] = Account.UserAgent,
                    ["http_content_length_xl"] = Account.AndroidId,
                    ["X-Requested-With"] = "com.pof.android"
                }
            };
            var response = await _webBrowserClient.SendRequestAsync(request).ConfigureAwait(false);
            if (!response.IsExpected("start of mobile registration"))
                return await UnexpectedResponseAsync(s).ConfigureAwait(false);

            var minified = GeneralHelpers.Minify(response.ContentBody);
            var doc = new HtmlDocument();
            await Task.Run(() => doc.LoadHtml(minified)).ConfigureAwait(false);
            Account.RegFormVars = new RegFormVars(doc);
            await Account.RegFormVars.TryGetVarsAsync().ConfigureAwait(false);
            if (!Account.RegFormVars.IsValid)
                return await FailedAsync(s, "Failed to parse regform vars").ConfigureAwait(false);

            Account.RegFormVars.LoadedAt = Account.TimeZone.NowInTimeZone();

            var delay = Random.Next(3000, 3300);
            await Task.Delay(delay).ConfigureAwait(false);

            return true;
        }

        //private async Task<HttpResp> _webBrowserClient.SendRequestAsync(HttpReq request)
        //{
        //    return await _webBrowserClient.SendRequestAsync(request).ConfigureAwait(false);

        //    //await Semaphore.WaitAsync().ConfigureAwait(false);

        //    //try
        //    //{
        //    //    return await _webBrowserClient.SendRequestAsync(request).ConfigureAwait(false);
        //    //}
        //    //finally
        //    //{
        //    //    Semaphore.Release();
        //    //}
        //}


        //private async Task<DecryptedApiResponse> SendApiRequestAsync(byte[] req)
        //{
        //    return await SendApiRequestAsync(req).ConfigureAwait(false);
        //    //await Semaphore.WaitAsync().ConfigureAwait(false);

        //    //try
        //    //{
        //    //    return await SendApiRequestAsync(req).ConfigureAwait(false);
        //    //}
        //    //finally
        //    //{
        //    //    Semaphore.Release();
        //    //}
        //}

        private async Task<bool> CalculatePt()
        {
            const string s = "Calculating pt: ";
            await AttemptingAsync(s).ConfigureAwait(false);

            var request = new HttpReq(HttpMethod.Get, "http://www.pof.com/yawhat.jpg")
            {
                Accept = "image/webp,*/*;q=0.8",
                AcceptEncoding = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                Referer = Account.RegistrationUrl
            };
            var response = await _webBrowserClient.SendRequestAsync(request).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.NotFound)
                return await UnexpectedResponseAsync(s).ConfigureAwait(false);

            var end = Account.TimeZone.NowInTimeZone();
            Account.RegFormVars.Pt = (end - Account.RegFormVars.LoadedAt).Milliseconds.ToString();

            return true;
        }

        private async Task<bool> CalculatePtb()
        {
            const string s = "Calculating ptb: ";
            await AttemptingAsync(s).ConfigureAwait(false);

            var request = new HttpReq(HttpMethod.Get, "http://upload.plentyoffish.com/yawhatc.jpg")
            {
                Accept = "image/webp,*/*;q=0.8",
                AcceptEncoding = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                Referer = Account.RegistrationUrl
            };
            var response = await _webBrowserClient.SendRequestAsync(request).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.NotFound)
                return await UnexpectedResponseAsync(s).ConfigureAwait(false);

            var end = Account.TimeZone.NowInTimeZone();
            Account.RegFormVars.Ptb = (end - Account.RegFormVars.LoadedAt).Milliseconds.ToString();

            return true;
        }

        private async Task<bool> CheckUsername()
        {
            const string s = "Checking username: ";
            await AttemptingAsync(s).ConfigureAwait(false);

            for (var i = 0; i < 3; i++)
            {
                var request = new HttpReq(HttpMethod.Get, $"http://www.pof.com/ajax_response.aspx?validateusername={Account.LoginId}")
                {
                    Accept = "application/json, text/javascript, */*; q=0.01",
                    AcceptEncoding = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    Referer = Account.RegistrationUrl,
                    AdditionalHeaders = new WebHeaderCollection
                    {
                        ["X-Requested-With"] = "XMLHttpRequest"
                    }
                };

                var response = await _webBrowserClient.SendRequestAsync(request).ConfigureAwait(false);
                if (response.IsExpected("\"error\":0"))
                    return true;

                await Task.Delay(Random.Next(2000, 5000));
                Account.LoginId += Random.Next(9);
            }

            return await FailedAsync(s).ConfigureAwait(false);
        }

        private async Task DelayBeforeCheckingUsername()
        {
            var seconds = await Delay("DelayBeforeUsernameInputGetsFocus", "checking username").ConfigureAwait(false);
            Account.RegFormVars.SecondsContemplatingUsername = seconds;
            Account.RegFormVars.UsernameInputGotFocusAt = Account.TimeZone.NowInTimeZone();
        }

        private void CalculateGreen()
        {
            var now = Account.TimeZone.NowInTimeZone();
            var greenVal = now - Account.RegFormVars.UsernameInputGotFocusAt;
            Account.RegFormVars.Green = ((int)greenVal.TotalMilliseconds).ToString();
        }

        private async Task<bool> Register()
        {
            const string s = "Attempting registration: ";
            await AttemptingAsync(s).ConfigureAwait(false);

            var day = Account.Birthday.Day < 10 ? $"0{Account.Birthday.Day}" : Account.Birthday.Day.ToString();
            var toffset = ~(int)Account.TimeZone.BaseUtcOffset.TotalMinutes + 1;

            var innerHeight = Account.AndroidDevice.Height - 82;

            var postParams = new Dictionary<string, string>
            {
                ["pt"] = Account.RegFormVars.Pt,
                ["ptb"] = Account.RegFormVars.Ptb,
                [Account.RegFormVars.UsernameInput] = Account.LoginId,
                [Account.RegFormVars.PasswordInput] = Account.Password,
                [Account.RegFormVars.PasswordConfirmInput] = Account.Password,
                [Account.RegFormVars.EmailInput] = Account.Email,
                [Account.RegFormVars.EmailConfirmInput] = Account.Email,
                ["gender"] = "1",
                ["birthmonth"] = Account.Birthday.Month.ToString(),
                ["birthday"] = day,
                ["birthyear"] = Account.Birthday.Year.ToString(),
                ["ethnicity"] = "4",
                ["country"] = "1",
                [Account.RegFormVars.HiddenInput] = "",
                ["key"] = Account.RegFormVars.Key,
                ["pink"] = Account.RegFormVars.Pink,
                ["neons"] = "",
                ["green"] = Account.RegFormVars.Green,
                ["rand"] = Account.RegFormVars.Rand,
                [Account.RegFormVars.TosInput] = "ON",
                ["HTTP_CONTENT_LENGTH_XL"] = Account.AndroidId,
                ["time"] = Account.RegFormVars.LoadedAt.ToString("yyyy/M/dd hh:mm"),
                ["toffset"] = toffset.ToString(),
                ["twidth"] = Account.AndroidDevice.Width.ToString(),
                ["theight"] = Account.AndroidDevice.Height.ToString(),
                ["tcolorDepth"] = "32",
                ["tavailWidth"] = Account.AndroidDevice.Width.ToString(),
                ["tavailHeight"] = Account.AndroidDevice.Height.ToString(),
                ["screenX"] = "0",
                ["history"] = "1",
                ["screenY"] = "0",
                ["pageref"] = Account.RegistrationUrl,
                ["outerWidth"] = Account.AndroidDevice.Width.ToString(),
                ["outerHeight"] = innerHeight.ToString(),
                ["innerWidth"] = Account.AndroidDevice.Width.ToString(),
                ["innerHeight"] = innerHeight.ToString(),
                ["flen"] = Account.RegFormVars.FormLen,
                ["action"] = Account.RegFormVars.Action,
                ["Submit"] = "Go to Second Step"
            };

            var oldProxy = _webBrowserClient.Config.Proxy;
            _webBrowserClient.Config.Proxy = Collections.CreatorFormSubmitProxies.GetNext().ToWebProxy();

            HttpResp response;
            try
            {
                var request = new HttpReq(HttpMethod.Post, Account.RegFormVars.PostFormUrl)
                {
                    Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8",
                    AcceptEncoding = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    Referer = Account.RegistrationUrl,
                    Origin = "https://www.pof.com",
                    ContentType = "application/x-www-form-urlencoded",
                    ContentBody = postParams.ToUrlEncodedData(),
                    AdditionalHeaders = new WebHeaderCollection
                    {
                        ["X-Requested-With"] = "com.pof.android"
                    }
                };

                response = await _webBrowserClient.SendRequestAsync(request).ConfigureAwait(false);
                if (!response.IsExpected("Complete this questionnaire!"))
                    return await UnexpectedResponseAsync(s).ConfigureAwait(false);
            }
            finally
            {
                _webBrowserClient.Config.Proxy = oldProxy;
            }

            Account.QuestinareUrl = response.Uri.AbsoluteUri;

            var minified = GeneralHelpers.Minify(response.ContentBody);
            Account.HtmlDoc = new HtmlDocument();
            await Task.Run(() => Account.HtmlDoc.LoadHtml(minified)).ConfigureAwait(false);
            Account.ProfileFormVars = new ProfileFormVars(Account.HtmlDoc);
            await Account.ProfileFormVars.TryGetVarsAsync().ConfigureAwait(false);
            if (!Account.ProfileFormVars.IsValid)
                return await FailedAsync(s, "Failed to parse profile form vars").ConfigureAwait(false);

            CalculatePinks();
            CalculateGreens();

            return true;
        }

        private async Task SdfSdg()
        {
            var imgs = Account.HtmlDoc.DocumentNode.SelectNodes("//img");

            var lst = new List<HttpReq>();
            foreach (var img in imgs)
            {
                var src = img.GetAttributeValue("src", string.Empty);
                if (string.IsNullOrWhiteSpace(src))
                    continue;

                if (src.Contains("sdg.aspx"))
                {
                    var request = new HttpReq(HttpMethod.Get, src)
                    {
                        Accept = "image/webp,*/*;q=0.8",
                        AcceptEncoding = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                        AdditionalHeaders = new WebHeaderCollection
                        {
                            ["X-Requested-With"] = "com.pof.android"
                        }
                    };
                    lst.Add(request);
                }

                if (src.Contains("sdf_s"))
                {
                    var request = new HttpReq(HttpMethod.Get, src)
                    {
                        Accept = "image/webp,*/*;q=0.8",
                        Referer = Account.QuestinareUrl,
                        AcceptEncoding = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                        AdditionalHeaders = new WebHeaderCollection
                        {
                            ["X-Requested-With"] = "com.pof.android"
                        }
                    };
                    lst.Add(request);
                }
            }

            for (var i = 0; i < lst.Count; i++)
            {
                var req = lst[i];
                if (i != 0 || !req.Uri.AbsoluteUri.Contains("sdf_s"))
                    continue;
                lst.Reverse();
                break;
            }

            var tasks = new List<Task>();
            foreach (var req in lst)
                tasks.Add(_webBrowserClient.SendRequestAsync(req));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task<bool> Questionaire()
        {
            const string s = "Submitting questionaire: ";
            await AttemptingAsync(s).ConfigureAwait(false);

            var day = Account.Birthday.Day < 10 ? $"0{Account.Birthday.Day}" : Account.Birthday.Day.ToString();

            var postParams = new Dictionary<string, string>
            {
                ["city"] = Account.Location.City.ToLower(),
                ["postalcode"] = Account.Location.Zipcode,
                ["Iama"] = "1",
                ["Seekinga"] = "0",
                ["height"] = Account.ProfileFormVars.Height,
                ["searchtype"] = Account.ProfileFormVars.SearchType,
                ["haircolor"] = Account.ProfileFormVars.HairColor,
                ["body"] = Account.ProfileFormVars.Body,
                ["car"] = "1",
                ["fishtype"] = Account.ProfileFormVars.FishType,
                ["language"] = "0",
                ["politics_id"] = Account.ProfileFormVars.PoliticsId,
                ["college_id"] = Account.ProfileFormVars.CollegeId,
                ["state_id"] = Account.ProfileFormVars.StateId,
                ["wantchildren"] = Account.ProfileFormVars.WantChildren,
                ["maritalstatus"] = "1",
                ["haschildren"] = "2",
                [Account.ProfileFormVars.Validater] = "1",
                ["drugs"] = "1",
                ["drink"] = "2",
                ["religion"] = Account.ProfileFormVars.Religion,
                ["pets"] = Account.ProfileFormVars.Pets,
                ["eyes_id"] = Account.ProfileFormVars.EyesId,
                ["profession"] = Account.Profession,
                ["intent"] = Account.ProfileFormVars.Intent,
                ["relationshipage_id"] = Account.ProfileFormVars.RelationshipAgeId,
                ["firstname"] = Account.FirstName,
                ["firstname_id"] = "1",
                ["income"] = Account.ProfileFormVars.Income,
                ["maritalparents"] = Account.ProfileFormVars.MaritalParents,
                ["siblings"] = Account.ProfileFormVars.Siblings,
                ["birthorder"] = Account.ProfileFormVars.BirthOrder,
                ["datekids"] = Account.ProfileFormVars.DateKids,
                ["datesmokers"] = Account.ProfileFormVars.DateSmokers,
                ["weight"] = Account.ProfileFormVars.Weight,
                ["headline"] = Account.Headline,
                [Account.ProfileFormVars.FDescription] = Account.AboutMe,
                ["interests"] = Account.Interests,
                [Account.ProfileFormVars.FDate] = Account.ProfileFormVars.FirstDateValue,
                ["birthmonth"] = Account.Birthday.Month.ToString(),
                ["birthday"] = day,
                ["birthyear"] = Account.Birthday.Year.ToString(),
                ["sguid"] = Account.ProfileFormVars.Sguid,
                ["fdescription"] = Account.ProfileFormVars.FDescription,
                ["fdate"] = Account.ProfileFormVars.FDate,
                ["validater"] = Account.ProfileFormVars.Validater,
                ["SID"] = Account.ProfileFormVars.Sid,
                ["sessionTracker"] = Account.ProfileFormVars.SessionTracker,
                ["autologinid"] = Account.ProfileFormVars.AutoLoginId,
                ["ssessionid"] = Account.ProfileFormVars.SessionId,
                ["user_id"] = Account.ProfileFormVars.UserId,
                ["CreateProfile"] = Account.ProfileFormVars.CreateProfile,
                ["pink"] = Account.ProfileFormVars.Pink,
                ["greens"] = Account.ProfileFormVars.Greens,
                ["neons"] = "",
            };


            var oldProxy = _webBrowserClient.Config.Proxy;
            _webBrowserClient.Config.Proxy = Collections.CreatorFormSubmitProxies.GetNext().ToWebProxy();

            HttpResp response;
            try
            {
                var request = new HttpReq(HttpMethod.Post, Account.ProfileFormVars.PostUrl)
                {
                    Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8",
                    AcceptEncoding = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    Origin = "https://www.pof.com",
                    Referer = Account.QuestinareUrl,
                    ContentType = "application/x-www-form-urlencoded",
                    ContentBody = postParams.ToUrlEncodedData(),
                    FollowRedirect = false,
                    AdditionalHeaders = new WebHeaderCollection
                    {
                        ["X-Requested-With"] = "com.pof.android"
                    }
                };
                response = await _webBrowserClient.SendRequestAsync(request).ConfigureAwait(false);
                if (response.StatusCode != HttpStatusCode.Found || !response.Headers["Location"].Contains("createprofileimagesnew"))
                    return await UnexpectedResponseAsync(s).ConfigureAwait(false);

            }
            finally
            {
                _webBrowserClient.Config.Proxy = oldProxy;
            }

            await Task.Delay(Random.Next(1800, 2900)).ConfigureAwait(false);
            return true;
        }

        private async Task PromptsSources()
        {
            const string s = "Getting prompts/sources: ";
            await AttemptingAsync(s).ConfigureAwait(false);

            var req = Requests.PromptsSources(Account.SessionToken, Account.UserAgent);
            await SendApiRequestAsync(req).ConfigureAwait(false);
        }

        private async Task<bool> SetGcmToken()
        {
            const string s = "Setting gcm token: ";
            await AttemptingAsync(s).ConfigureAwait(false);

            var req = Requests.PushNotification(Account.SessionToken, Account.AndroidId, Account.GcmToken, Account.UserAgent);
            var response = await SendApiRequestAsync(req).ConfigureAwait(false);
            if (!response.IsExpected("\"success\":true"))
                return await UnexpectedResponseAsync(s).ConfigureAwait(false);

            return true;
        }

        private async Task<bool> SetLocation()
        {
            const string s = "Setting location: ";
            await AttemptingAsync(s).ConfigureAwait(false);

            var req = Requests.Location(Account.SessionToken, Account.UserAgent, Account.Location);
            var response = await SendApiRequestAsync(req).ConfigureAwait(false);
            if (!response.IsExpected("\"success\":true"))
                return await UnexpectedResponseAsync(s).ConfigureAwait(false);

            return true;
        }

        private async Task<bool> UploadImageStatus()
        {
            const string s = "Getting upload image status: ";
            await AttemptingAsync(s).ConfigureAwait(false);

            var req = Requests.ImageUploadStatus(Account.SessionToken, Account.UserAgent);
            var response = await SendApiRequestAsync(req).ConfigureAwait(false);
            if (!response.IsExpected("\"bannedUpload\":false"))
                return await UnexpectedResponseAsync(s).ConfigureAwait(false);

            return true;
        }

        private async Task ExperimentsLoggedIn()
        {
            const string s = "Getting experiments: ";
            await AttemptingAsync(s).ConfigureAwait(false);

            var req = Requests.ExperimentsForAllUsersLoggedIn(Account.Location, Account.UserId, Account.InstallId, Account.AndroidId, Account.SessionToken, Account.UserAgent);
            await SendApiRequestAsync(req).ConfigureAwait(false);
        }

        private async Task<bool> UploadImages()
        {
            var max = Random.Next(5, 9);
            var cnt = 0;

            string pathToimageFile;
            while (!string.IsNullOrWhiteSpace(pathToimageFile = Account.ImagesToUpload.GetNext(false)) && cnt < max)
            {
                await Delay("ImageUploadDelay", "uploading image").ConfigureAwait(false);

                var location = await ImageUploadLocation().ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(location))
                    continue;

                if (!await UploadImage(location, GeneralHelpers.RandomizeImage(pathToimageFile)).ConfigureAwait(false))
                    continue;

                cnt++;
            }

            return cnt > 0;
        }

        private async Task<string> ImageUploadLocation()
        {
            const string s = "Getting image upload location: ";
            await AttemptingAsync(s).ConfigureAwait(false);

            var req = Requests.ImageUploadLocation(Account.AppSessionId, Account.SessionToken, Account.UserAgent);
            var response = await SendApiRequestAsync(req).ConfigureAwait(false);
            if (!response.IsExpected("url"))
            {
                await UnexpectedResponseAsync(s).ConfigureAwait(false);
                return string.Empty;
            }

            string url;
            if (!UrlRegex.TryGetGroup(response.ContentBody, out url))
            {
                await FailedAsync(s, "Failed to parse url").ConfigureAwait(false);
                return string.Empty;
            }

            if (url.StartsWith("http://"))
                url = url.Replace("http://", "https://");

            return url;
        }

        private async Task<bool> UploadImage(string url, byte[] image)
        {
            const string s = "Uploading image: ";
            await AttemptingAsync(s).ConfigureAwait(false);

            var boundary = Guid.NewGuid().ToString();

            var sb = new StringBuilder();
            sb.Append($"--{boundary}\r\n");
            sb.Append($"Content-Disposition: form-data; name=\"upload_file\"; filename=\"image_upload-{GeneralHelpers.RandomString(9, GeneralHelpers.GenType.Num)}.tmp\"\r\n");
            sb.Append("Content-Type: application/octet-stream\r\n");
            sb.Append($"Content-Length: {image.Length}\r\n");
            sb.Append("Content-Transfer-Encoding: binary\r\n\r\n");

            sb.Append("%IMAGE\r\n");
            sb.Append($"--{boundary}--\r\n");

            var contentData = HttpWaifuHelpers.GetBytes(sb.ToString(), image);

            var request = new HttpReq(HttpMethod.Post, url)
            {
                AcceptEncoding = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                ContentType = $"multipart/form-data; boundary={boundary}",
                ContentData = contentData,
                OverrideUserAgent = string.Empty,
                AdditionalHeaders = new WebHeaderCollection
                {
                    ["x-stutter-id"] = Guid.NewGuid().ToString(),
                    ["x-user-agent"] = Account.UserAgent
                }
            };
            var response = await ApiClient.SendRequestAsync(request).ConfigureAwait(false);
            if (!response.IsExpected("\"error\":null"))
                return await UnexpectedResponseAsync(s).ConfigureAwait(false);

            return true;
        }

        private async Task ConvoCount()
        {
            const string s = "Getting convo count: ";
            await AttemptingAsync(s).ConfigureAwait(false);

            var req = Requests.ConversationCount(Account.SessionToken, Account.UserAgent);
            var response = await SendApiRequestAsync(req).ConfigureAwait(false);
            Console.WriteLine(response.ContentBody);
        }

        private async Task BadgeCount()
        {
            const string s = "Getting badge count: ";
            await AttemptingAsync(s).ConfigureAwait(false);

            var req = Requests.BadgeCounts(Account.SessionToken, Account.UserAgent);
            var response = await SendApiRequestAsync(req).ConfigureAwait(false);
            Console.WriteLine(response.ContentBody);
        }

        private async Task CloseBy()
        {
            const string s = "Getting close by: ";
            await AttemptingAsync(s).ConfigureAwait(false);

            var req = Requests.CloseBy(Account.SessionToken, Account.UserAgent);
            await SendApiRequestAsync(req).ConfigureAwait(false);
        }

        private void CalculatePinks()
        {
            var len = Account.Headline.Length;
            var rand = Random.Next(1, 101);
            int val;
            if (rand >= 50)
                val = len + Random.Next(1, 15);
            else
                val = len;

            Account.ProfileFormVars.Pink = val.ToString();
        }

        private void CalculateGreens()
        {
            var len = Account.AboutMe.Length;
            Account.ProfileFormVars.Greens = (len + Random.Next(13, 44)).ToString();
        }

        private async Task<int> Delay(string setting, string reason)
        {
            var seconds = Settings.GetRandom(setting);
            for (var i = seconds; i > 0; i--)
            {
                var s = i > 1 ? $"Delaying before {reason}: {i} seconds remain" : $"Delaying before {reason}: {i} second remains";
                await UpdateThreadStatusAsync(s).ConfigureAwait(false);
            }

            return seconds;
        }
    }
}
