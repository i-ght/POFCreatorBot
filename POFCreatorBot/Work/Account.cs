using DankLibWaifuz.CollectionsWaifu;
using DankLibWaifuz.Etc;
using DankLibWaifuz.LocationsWaifu;
using DankLibWaifuz.ScriptWaifu;
using DankLibWaifuz.SettingsWaifu;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;

namespace POFCreatorBot.Work
{
    class Account
    {
        private static readonly string[] Seperators = { "_", "" };

        private int _sessionCnt;

        public string LoginId { get; set; }
        public string Email { get; }
        public string Password { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public string InstallId { get; }
        public string AppSessionId { get; }
        public Location Location { get; }
        public AndroidDevice AndroidDevice { get; }
        public string RegistrationUrl { get; set; }
        public string UserAgent { get; }
        public string AboutMe { get; }
        public string Headline { get; }
        public string Profession { get; }
        public string Interests { get; }

        public DateTime Birthday { get; }
        public string AuthToken { get; set; }
        public string SessionToken { get; set; }
        public Queue<string> ImagesToUpload { get; }
        public string UserId { get; set; }
        public string ProfileId { get; set; }
        public string AndroidId { get; }
        public string GcmToken { get; }
        public TimeZoneInfo TimeZone { get; }
        public string QuestinareUrl { get; set; }
        public RegFormVars RegFormVars { get; set; }
        public ProfileFormVars ProfileFormVars { get; set; }
        public HtmlDocument HtmlDoc { get; set; }

        public DateTime LastMsgReceived { get; set; }
        public int FailedMsgSends;

        public bool SetInitialTimeout { get; set; }

        public Queue<string> UidsToLike { get; } = new Queue<string>();

        public Dictionary<string, ScriptWaifu> Convos { get; } = new Dictionary<string, ScriptWaifu>();

        public int LoginErrors { get; set; }

        public int In { get; set; }
        public int Out;
        public int Likes { get; set; }

        public bool IsValid { get; }

        public bool BlastSessionReady => _sessionCnt == 0;

        public Account(string firstName, string lastName, string aboutMe, string headline, string interests, string profession, Location loc, AndroidDevice device, Queue<string> imagesToUpload)
        {
            if (GeneralHelpers.AnyNullOrWhiteSpace(firstName, lastName, aboutMe, headline, interests, profession))
                return;

            if (imagesToUpload.Count == 0)
                return;

            Location = loc;
            AndroidDevice = device;

            FirstName = GeneralHelpers.Normalize(firstName);
            LastName = GeneralHelpers.Normalize(lastName);
            Email = $"{FirstName}{LastName}{Mode.Random.Next(999)}@{GeneralHelpers.RandomEmailDomain()}".ToLower();
            LoginId = $"{FirstName}{Seperators.RandomSelection()}{LastName.Substring(0, Mode.Random.Next(2, 4))}".ToLower();

            InstallId = Guid.NewGuid().ToString();
            AppSessionId = Guid.NewGuid().ToString();

            Password = GeneralHelpers.RandomString(Mode.Random.Next(8, 13), GeneralHelpers.GenType.LowerLetNum);
            AndroidId = AndroidHelpers.GenerateAndroidId();
            GcmToken = AndroidHelpers.GenerateGcmToken();
            TimeZone= TimeHelpers.RandomUsTimeZone();
            Birthday = GeneralHelpers.GenerateDateOfBirth(18, 59);

            AboutMe = aboutMe;
            Headline = headline;
            Profession = profession;
            Interests = interests;

            ImagesToUpload = imagesToUpload;

            UserAgent = $"Dalvik {Pof.AppVersion}; (Linux; U; Android {AndroidDevice.OsVersion}; {AndroidDevice.Model}; ON; en_US) {AndroidId}; {AndroidDevice.Width}x{AndroidDevice.Height}x1.5";

            IsValid = true;
        }

        public void SessionCount()
        {
            _sessionCnt++;
            var max = Settings.Get<int>("LikeEvery");
            if (_sessionCnt >= max)
                _sessionCnt = 0;
        }
    }
}
