using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using POFCreatorBot.Declarations;
using DankLibWaifuz.HttpWaifu;
using System.Net.Http;
using System.Net;
using System.Text.RegularExpressions;
using DankLibWaifuz.Etc;

namespace POFCreatorBot.Work
{
    class Pof : Mode
    {
        public const string AppVersion = "3.37.2.141704";
        public const string ApiUrl = "https://2.api.pof.com/";

        public Pof(int index, ObservableCollection<DataGridItem> collection) : base(index, collection)
        {
        }

        protected Account Account { get; set; }
        protected HttpWaifu ApiClient { get; set; }

        protected void UpdateInStat(int val)
        {
            _collection[_index].InCount = val;
        }

        protected void UpdateOutStat(int val)
        {
            _collection[_index].OutCount = val;
        }

        protected void UpdateLikeStat(int val)
        {
            _collection[_index].LikeCount = val;
        }

        protected async Task<DecryptedApiResponse> SendApiRequestAsync(byte[] request)
        {
            var httpReq = new HttpReq(HttpMethod.Post, ApiUrl)
            {
                AcceptEncoding = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                ContentData = request
            };
            var response = await ApiClient.SendRequestAsync(httpReq).ConfigureAwait(false);
            if (!response.IsOK)
                return new DecryptedApiResponse(0);

            return new DecryptedApiResponse(response.StatusCode, response.ContentData);
        }

        private static readonly Regex AuthSessionTokensRegex =
           new Regex("\"authenticationToken\":\"(.*?)\",\"sessionToken\":\"(.*?)\".*?\"userId\":(\\d+),\"profileId\":(\\d+)"
               , RegexOptions.Compiled | RegexOptions.IgnoreCase);
        protected async Task<bool> Session()
        {
            const string s = "Getting session: ";
            await AttemptingAsync(s).ConfigureAwait(false);

            var req = Requests.Session(Account.LoginId, Account.Password, Account.AndroidDevice, Account.AndroidId, Account.GcmToken, Account.UserAgent);
            var response = await SendApiRequestAsync(req).ConfigureAwait(false);
            if (!response.IsExpected("authenticationToken"))
                return await UnexpectedResponseAsync(s).ConfigureAwait(false);

            var authToken = string.Empty;
            var sessionToken = string.Empty;
            var uid = string.Empty;
            var profileId = string.Empty;
            var match = AuthSessionTokensRegex.Match(response.ContentBody);
            if (match.Success)
            {
                authToken = match.Groups[1].Value;
                sessionToken = match.Groups[2].Value;
                uid = match.Groups[3].Value;
                profileId = match.Groups[4].Value;
            }
            if (GeneralHelpers.AnyNullOrWhiteSpace(authToken, sessionToken, uid, profileId))
                return await FailedAsync(s, "failed to parse auth or session token").ConfigureAwait(false);

            Account.AuthToken = authToken;
            Account.SessionToken = sessionToken;
            Account.UserId = uid;
            Account.ProfileId = profileId;

            return true;

        }
    }
}
