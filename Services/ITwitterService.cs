using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SocialPost.Services
{
    public interface ITwitterService
    {
        IList<string> Errors { get; set; }

        byte[] DownloadFile(string url);
        JObject GetAuthorizeLink();
        JObject GetTweetEmbeded(string userScreenName, string tweetId);
        JObject PostRequestToken();
        JObject PostRequestUserToken(string oauth_token, string oauth_verifier);
        JObject PostTwitsOnBehalfOf(string oauth_access_token, string oauth_token_secret, Dictionary<string, object> postData);
        JObject UploadMedia(string userAccessToken, string userTokenSecret, byte[] mediaRawBinary);
        JObject VerifyAccount(string userAccessToken, string userTokenSecret);
    }
}
