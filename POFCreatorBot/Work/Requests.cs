using DankLibWaifuz.HttpWaifu;
using DankLibWaifuz.LocationsWaifu;
using System.Collections.Generic;
using System.Text;

namespace POFCreatorBot.Work
{
    static class Requests
    {
        public static byte[] ExperimentsForAllUsers(string installId, string androidId, string userAgent)
        {
            var url = $"/Experiments/GetAllForUser?inputs=%7B%22deviceLocale%22%3A%22en_US%22%2C%22installId%22%3A%22{installId}%22%2C%22deviceId%22%3A%22{androidId}%22%7D&sessionToken=";
            var request = Get(url, userAgent);
            return Crypto.PofEncrypt(request);
        }

        public static byte[] ExperimentsForAllUsersLoggedIn(Location location, string userId, string installId, string androidId, string sessionToken, string userAgent)
        {
            var url = $"/Experiments/GetAllForUser?inputs=%7B%22latitude%22%3A{location.Lat}%2C%22deviceLocale%22%3A%22en_US%22%2C%22userId%22%3A{userId}%2C%22installId%22%3A%22{installId}%22%2C%22longitude%22%3A{location.Lon}%2C%22deviceId%22%3A%22{androidId}%22%7D&sessionToken={sessionToken}";
            var request = Get(url, userAgent);
            return Crypto.PofEncrypt(request);
        }

        public static byte[] ImageUploadLocation(string appSessionId, string sessionToken, string userAgent)
        {
            var url = $"/Images/ImageUploadLocation?thumbnailx1={Mode.Random.Next(1000, 1300)}&thumbnaily1=0&thumbnailx2={Mode.Random.Next(8900, 9300)}&thumbnaily2=10000&rotation=0&imageSource=existing&pageSource=RegistrationImageUploadSingle&appSessionId={appSessionId}&eventId=2&sessionToken={sessionToken}";
            var request = Get(url, userAgent);
            return Crypto.PofEncrypt(request);
        }

        public static byte[] ConversationCount(string sessionToken, string userAgent)
        {
            var url = $"/Conversations/ConversationCount?sessionToken={sessionToken}";
            var request = Get(url, userAgent);
            return Crypto.PofEncrypt(request);
        }

        public static byte[] BadgeCounts(string sessionToken, string userAgent)
        {
            var url = $"/Users/GetBadgeCounts?sessionToken={sessionToken}";
            var request = Get(url, userAgent);
            return Crypto.PofEncrypt(request);
        }

        public static byte[] CloseBy(string sessionToken, string userAgent)
        {
            var url = $"/Users/CloseBy?sessionToken={sessionToken}";
            var request = Get(url, userAgent);
            return Crypto.PofEncrypt(request);
        }

        public static byte[] RecordAppInstall(string userAgent)
        {
            const string url = "/Analytic/RecordAppInstall?sessionToken=";
            var request = Get(url, userAgent);
            return Crypto.PofEncrypt(request);
        }

        public static byte[] RegistrationUrl(string appSessionId, string installId, AndroidDevice device, string userAgent)
        {
            var queryParams = new Dictionary<string, string>
            {
                ["fromSignInReminder"] = "false",
                ["appSessionId"] = appSessionId,
                ["eventId"] = "4",
                ["installId"] = installId,
                ["deviceLocale"] = "en_US",
                ["hasEfiles"] = "false",
                ["hasQPipes"] = "false",
                ["hasQProps"] = "false",
                ["appCert"] = "VALID",
                ["roInfo"] = $"{device.ProductBoard};{device.Model};{device.Manufacturer};{device.ProductName}",
                ["roFp"] = device.FingerPrint,
                ["regCount"] = "1",
                ["sessionToken"] = ""
            };

            var url = $"/url/registration?{queryParams.ToUrlEncodedData()}";
            var request = Get(url, userAgent);
            return Crypto.PofEncrypt(request);
        }

        public static byte[] Session(string username, string password, AndroidDevice androidDevice, string androidId, string gcmToken, string userAgent)
        {
            var url = $"/Session?userName={username}&password={password}&app=POF&appVersion={Pof.AppVersion}&platformVersion={androidDevice.OsVersion}&deviceId={androidId}&validateUserAgent=false&platform=Android&token={gcmToken}&sessionToken=";
            var request = Get(url, userAgent);
            return Crypto.PofEncrypt(request);
        }

        public static byte[] PromptsSources(string sessionToken, string userAgent)
        {
            var url = $"/Prompts/Sources?sessionToken={sessionToken}";
            var request = Get(url, userAgent);
            return Crypto.PofEncrypt(request);
        }

        public static byte[] PushNotification(string sessionToken, string androidId, string gcmToken, string userAgent)
        {
            var url = $"/Account/PushNotification?sessionToken={sessionToken}";
            var contentBody = $"{{\"deviceId\":\"{androidId}\",\"tokenId\":\"{gcmToken}\"}}";
            var request = Post(url, userAgent, contentBody);
            return Crypto.PofEncrypt(request);
        }

        public static byte[] Location(string sessionToken, string userAgent, Location location)
        {
            var url = $"/Users/Location?sessionToken={sessionToken}";
            var contentBody = $"{{\"latitude\":{location.Lat},\"longitude\":{location.Lon}}}";
            var request = Post(url, userAgent, contentBody);
            return Crypto.PofEncrypt(request);
        }

        public static byte[] ImageUploadStatus(string sessionToken, string userAgent)
        {
            var url = $"/Images/UploadImageStatus?sessionToken={sessionToken}";
            var request = Get(url, userAgent);
            return Crypto.PofEncrypt(request);
        }

        public static string Post(string url, string userAgent, string contentBody)
        {
            var sb = new StringBuilder();
            sb.Append($"POST {url} HTTP/1.1\r\n");
            sb.Append("HTTP_ACCEPT_LANGUAGE : en\n");
            sb.Append("Accept-Encoding : gzip, deflate\n");
            sb.Append("x-Accepts : compression\n");
            sb.Append("X-Content-Encoding : gzip\n");
            sb.Append($"User-Agent : {userAgent}\n");
            sb.Append("Content-Type : application/json\n");
            sb.Append($"Content-Length : {Encoding.UTF8.GetBytes(contentBody).Length}\r\n\r\n");

            sb.Append(contentBody);
            return sb.ToString();
        }

        public static byte[] GetMeetMeUsers(string sessionToken, string userAgent)
        {
            var url = $"/Users/MeetMe?sessionToken={sessionToken}";
            var request = Get(url, userAgent);
            return Crypto.PofEncrypt(request);
        }

        public static byte[] LikeProfile(string sessionToken, string userAgent, string profileId)
        {
            var url = $"/Users/MeetMe?sessionToken={sessionToken}";
            var contentBody = $"{{\"profileId\":{profileId},\"vote\":1}}";
            var request = Post(url, userAgent, contentBody);
            return Crypto.PofEncrypt(request);
        }

        public static byte[] ConversationsPage(string sessionToken, string userAgent, string convoId = "-1")
        {
            var url = $"/Conversations?pageSize=20&conversationId={convoId}&sessionToken={sessionToken}";
            var request = Get(url, userAgent);
            return Crypto.PofEncrypt(request);
        }

        public static byte[] SendMessage(string sessionToken, string userAgent, string myUsername, string replyMsgId, string sourceId, string sourceString, string text, string toUsername, string toUserId)
        {
            var url = $"/Conversations/SendMessage?sessionToken={sessionToken}";
            text = text.Replace("\"", "'");
            var contentBody = $"{{\"myUserName\":\"{myUsername}\",\"replyMessageId\":{replyMsgId},\"sourceId\":{sourceId},\"sourceString\":\"{sourceString}\",\"text\":\"{text}\",\"userName\":\"{toUsername}\",\"userId\":{toUserId}}}";

            var request = Post(url, userAgent, contentBody);
            return Crypto.PofEncrypt(request);
        }

        private static string Get(string url, string userAgent)
        {
            var sb = new StringBuilder();
            sb.Append($"GET {url} HTTP/1.1\r\n");
            sb.Append("HTTP_ACCEPT_LANGUAGE : en\n");
            sb.Append("Accept-Encoding : gzip, deflate\n");
            sb.Append("x-Accepts : compression\n");
            sb.Append("X-Content-Encoding : gzip\n");
            sb.Append($"User-Agent : {userAgent}\n");
            return sb.ToString();
        }
    }
}
