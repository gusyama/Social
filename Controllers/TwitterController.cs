using Newtonsoft.Json.Linq;
using SocialPost.Models;
using SocialPost.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SocialPost.Controllers
{
    public class TwitterController : Controller
    {
        private readonly ITwitterService _twitterService;


        public TwitterController(ITwitterService twitterService)
        {
            _twitterService = twitterService;
        }

        // GET: Twitter
		// The initial page
        public ActionResult Index()
        {
            try
            {
                // create authorize link
                JObject authorize = _twitterService.GetAuthorizeLink();
                var RequestToken = authorize["oauth_token"].Value<string>();

                // redirecting from PostAuthentication
                if (TempData["Warning"] != null)
                {
                    ViewBag.Warning = TempData["Warning"];
                }
                ViewBag.AuthorizeLink = authorize["authorize_link"].Value<string>();
                return View();
            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
                return View();
            }
        }

        // GET: Twitter/PostAuthentication
        public ActionResult PostAuthentication(string oauth_token, string oauth_verifier)
        {
            // Twitter callback after user login and confirm the authorize SocialPost to have read/write access of post 
            try
            {
                JToken response = _twitterService.PostRequestUserToken(oauth_token, oauth_verifier);
                Twitter twitterAuthentication = new Twitter()
                {
                    AccessToken = response["oauth_token"].Value<string>(),
                    AccessTokenSecret = response["oauth_token_secret"].Value<string>(),
                    UserId = response["user_id"].Value<string>(),
                    ScreenUsername = response["screen_name"].Value<string>()
                };

                return View(twitterAuthentication);
            }
            catch (Exception e)
            {
                TempData["Warning"] = $"Something went wrong on Twitter authentication process, please try it again! \n{e.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: Twitter/PostAuthentication
        [HttpPost]
        public ActionResult PostMessage(Twitter twitter)
        {
            // Gathers the user token and send the request to create the post
            try
            {
                var content = new Dictionary<string, object>();

                byte[] mediaRawBinary = System.IO.File.ReadAllBytes($"{AppDomain.CurrentDomain.BaseDirectory}/Content/thankyou.jpg");

                var media = _twitterService.UploadMedia(twitter.AccessToken, twitter.AccessTokenSecret, mediaRawBinary);
                content["media"] = media["media_id"];
                content["status"] = twitter.Message;
                
                var tweet = _twitterService.PostTwitsOnBehalfOf(twitter.AccessToken, twitter.AccessTokenSecret, content);

                var embeded = _twitterService.GetTweetEmbeded(twitter.ScreenUsername, tweet["id"].ToString());
                TempData["url"] = embeded?["url"];
                TempData["html"] = embeded?["html"];

                return RedirectToAction("Confirmation");
            }
            catch (Exception e)
            {
                TempData["Warning"] = $"Something went wrong on Twitter authentication process, please try it again! \n{e.Message}";
                return RedirectToAction("Index");
            }
        }

        // GET: Twitter/AuthenticationConfirmation
        public ActionResult Confirmation()
        {
            if(TempData?["url"] != null)
            {
                ViewBag.url = TempData["url"];
                ViewBag.html = TempData["html"];
            }
            else
            {
                var response = _twitterService.GetTweetEmbeded("August44672602", "1447842204689448963");
                ViewBag.url = response?["url"];
                ViewBag.html = response?["html"];
            }
            return View();
        }

    }
}