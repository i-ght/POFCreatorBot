using System.Net;

namespace POFCreatorBot.Work
{
    class DecryptedApiResponse
    {
        public HttpStatusCode StatusCode { get; }
        public string ContentBody { get; }

        public DecryptedApiResponse(HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
            ContentBody = string.Empty;
        }

        public DecryptedApiResponse(HttpStatusCode statusCode, byte[] response)
        {
            StatusCode = statusCode;
            if (IsOk)
                ContentBody = Crypto.PofDecrypt(response);
        }

        public bool IsOk
        {
            get
            {
                return StatusCode == HttpStatusCode.OK;
            }
        }

        public bool IsExpected(string keyword)
        {
            if (string.IsNullOrWhiteSpace(ContentBody))
                return false;

            if (!ContentBody.ToLower().Contains(keyword.ToLower()))
                return false;

            return true;
        }
    }
}
