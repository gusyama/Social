using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace SocialPost.Services
{
    public class TwitterService : ITwitterService
    {
        private string _oauthConsumerKey;
        private string _oauthConsumerSecret;
        private string _accessToken;
        private string _accessTokenSecret;
        private string _baseUrl;
        private string _version;
        private string _appBaseUrl;
        private readonly ILog _logger;

        public IList<string> Errors { get; set; }


        public TwitterService()
        {
			// inject the Twitter settings
            _oauthConsumerKey = ConfigurationManager.AppSettings["TwitterConsumerKey"];
            _oauthConsumerSecret = ConfigurationManager.AppSettings["TwitterConsumerSecret"];
            _accessToken = ConfigurationManager.AppSettings["TwitterAccessToken"];
            _accessTokenSecret = ConfigurationManager.AppSettings["TwitterAccessTokenSecret"];

            _baseUrl = ConfigurationManager.AppSettings["TwitterBaseUrl"];
            _version = ConfigurationManager.AppSettings["TwitterVersion"];
            _appBaseUrl = ConfigurationManager.AppSettings["AppBaseURL"];
            _logger = LogManager.GetLogger(typeof(TwitterService));
            Errors = new List<string>();
        }

        /// <summary>
        ///  https://developer.twitter.com/en/docs/authentication/oauth-1-0a/obtaining-user-access-tokens
        ///  Returns the authorize link with the app token
        /// </summary>
        public JObject GetAuthorizeLink()
        {
            string authorizeLink = $"{_baseUrl}/oauth/authorize?oauth_token=";

            JToken response = PostRequestToken();

            if (response["oauth_token"] != null)
            {
                JObject autorize = response as JObject;
                autorize.Add("authorize_link", $"{authorizeLink}{response["oauth_token"].Value<string>()}");
                return autorize;
            }
            return null;
        }

        /// <summary>
        /// Request for a token to be used in conjuction to the authorize link (ask user to give the app permission on behalf of signed in user
        /// </summary>
        public JObject PostRequestToken()
        {
            string requestTokenUrl = $"{_baseUrl}/oauth/request_token";
            string callBackUrl = $"{_appBaseUrl}Twitter/PostAuthentication";

            RestRequest request = null;
            IRestResponse response = null;
            try
            {
                var client = new RestClient(requestTokenUrl)
                {
                    Authenticator = OAuth1Authenticator.ForProtectedResource(_oauthConsumerKey, _oauthConsumerSecret, _accessToken, _accessTokenSecret)
                };
                request = new RestRequest(Method.POST);
                request.AddParameter("oauth_callback", callBackUrl);
                response = client.Execute(request);

                return TranslateResponse(response, requestTokenUrl);
            }
            catch (Exception ex)
            {
                HandleError($"Failed on the POST request {requestTokenUrl}'", ex, request, response);
                throw;
            }
        }

        /// <summary>
        /// Get the token to be used on any request on behalf of user. This is a token associating the user giving permission to the app
        /// </summary>
        public JObject PostRequestUserToken(string oauth_token, string oauth_verifier)
        {
            string requestUserTokenUrl = $"{_baseUrl}/oauth/access_token" +
                $"?oauth_token={oauth_token}&oauth_verifier={oauth_verifier}";

            RestRequest request = null;
            IRestResponse response = null;
            try
            {
                var client = new RestClient(requestUserTokenUrl);
                request = new RestRequest(Method.POST);
                response = client.Execute(request);

                return TranslateResponse(response, requestUserTokenUrl);
            }
            catch (Exception ex)
            {
                HandleError($"Failed on the POST request '{requestUserTokenUrl}'", ex, request, response);
                throw;
            }
        }

        /// <summary>
        /// Verify the user account based on the generated token
        /// </summary>
        public JObject VerifyAccount(string userAccessToken, string userTokenSecret)
        {
            string verifyAccountURL = $"{_baseUrl}/{_version}/account/verify_credentials.json";

            RestRequest request = null;
            IRestResponse response = null;
            try
            {
                var client = new RestClient(verifyAccountURL)
                {
                    Authenticator = OAuth1Authenticator.ForProtectedResource(_oauthConsumerKey, _oauthConsumerSecret, userAccessToken, userTokenSecret, RestSharp.Authenticators.OAuth.OAuthSignatureMethod.HmacSha1)
                };
                request = new RestRequest(Method.GET);
                response = client.Execute(request);

                return TranslateResponse(response, verifyAccountURL);
            }
            catch (Exception ex)
            {
                HandleError($"Failed on the POST request '{verifyAccountURL}'", ex, request, response);
                throw;
            }
        }

        /// <summary>
        /// Upload media as raw binary
        /// </summary>
        public JObject UploadMedia(string userAccessToken, string userTokenSecret, byte[] mediaRawBinary)
        {
            string uploadMediaURL = $"https://upload.twitter.com/{_version}/media/upload.json";

            RestRequest request = null;
            IRestResponse response = null;
            try
            {
                var client = new RestClient(uploadMediaURL)
                {
                    Authenticator = OAuth1Authenticator.ForProtectedResource(_oauthConsumerKey, _oauthConsumerSecret, userAccessToken, userTokenSecret, RestSharp.Authenticators.OAuth.OAuthSignatureMethod.HmacSha1)
                };
                request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "multipart/form-data");
                request.AddFileBytes("media", mediaRawBinary, "image/jpg");
                response = client.Execute(request);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception($"Error on getting HTTP response to Twitter on '{uploadMediaURL}'. Response: {response}");
                }
                return JObject.Parse(response.Content);
            }
            catch (Exception ex)
            {
                HandleError($"Failed on the POST request '{uploadMediaURL}'", ex, request, response);
                throw;
            }
        }

        /// <summary>
        /// Download media file as byte, in case we have a URL of the media
        /// </summary>
        public byte[] DownloadFile(string url)
        {
            var request = new RestRequest(Method.GET);
            var client = new RestClient(url);
            byte[] response = client.DownloadData(request);
            return response;
        }

        /// <summary>
        /// Method to post on behalf of a user
        /// </summary>
        public JObject PostTwitsOnBehalfOf(string userAccessToken, string userTokenSecret, Dictionary<string, object> postData)
        {
            JObject response = PostTwits(userAccessToken, userTokenSecret, postData);
            return response;
        }

        /// <summary>
        /// Post a Tweet
        /// </summary>
        private JObject PostTwits(string accessToken, string tokenSecret, Dictionary<string, object> postData)
        {
            string updateStatusURL = $"{_baseUrl}/{_version}/statuses/update.json";

            RestRequest request = null;
            IRestResponse response = null;
            try
            {
                var client = new RestClient(updateStatusURL)
                {
                    Authenticator = OAuth1Authenticator.ForProtectedResource(_oauthConsumerKey, _oauthConsumerSecret, accessToken, tokenSecret, RestSharp.Authenticators.OAuth.OAuthSignatureMethod.HmacSha1)
                };
                request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddParameter("trim_user", "true");
                request.AddParameter("include_entities", "true");
                request.AddParameter("status", postData["status"]);

                // image
                if (postData.ContainsKey("media") && postData["media"] != null)
                {
                    request.AddParameter("media_ids", postData["media"]);
                }
                // gif, video
                if (postData.ContainsKey("media_data") && postData["media_data"] != null)
                {
                    request.AddParameter("media_ids", postData["media_data"]);
                }

                response = client.Execute(request);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception($"Response Status Code: {response.StatusCode}");
                }
                return JObject.Parse(response.Content);
            }
            catch (Exception ex)
            {
                HandleError($"Failed on the POST request '{updateStatusURL}'", ex, request, response);
                throw;
            }
        }

        public JObject GetTweetEmbeded(string userScreenName, string tweetId)
        {
            var tweetUrl = $"https://twitter.com/{userScreenName}/status/{tweetId}";
            var baseRequestUrl = "https://publish.twitter.com/";
            var paramRequest = $"oembed?url={HttpUtility.UrlEncode(tweetUrl)}";

            var client = new RestClient(baseRequestUrl);
            var request = new RestRequest(paramRequest, Method.GET);
            var response = client.Execute<JObject>(request).Content;

            return JObject.Parse(response);
        }

        private JObject TranslateResponse(IRestResponse response, string url)
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"Error on getting HTTP response to Twitter on '{url}'. Response: {response}");
            }
            if (response.Content == null)
            {
                throw new Exception($"Response has no content on Twitter Request User Token '{url}'");
            }
            
            if (response.Content.Contains("errors"))
            {
                throw new Exception($"Error on the request to '{url}'. TwitterContentData: '{response.Content}'");
            }
            string[] contents = response.Content.Split('&');
            var keyValueContent = contents.ToDictionary(item => item.Split('=')[0], item => item.Split('=')[1]);
            JObject jsonResponse = JObject.FromObject(keyValueContent);
            return jsonResponse;
        }

        private void HandleError(string message, Exception ex = null, object twitterRequestData = null, object twitterResponseData = null)
        {
            if (ex != null)
            {
                message += "\n\nException, message:" + ex.Message + "\ntrace:" + ex.StackTrace;
            }

            if (twitterRequestData != null)
            {
                message += "\n\nTwitterRequestData:\n" + JsonConvert.SerializeObject(twitterRequestData);
            }

            if (twitterResponseData != null)
            {
                message += "\n\nTwitterResponseData:\n" + JsonConvert.SerializeObject(twitterResponseData);
            }

            _logger.Error("TwitterService:" + message);
            Errors.Add("TwitterService:" + message);
        }

    }
}
