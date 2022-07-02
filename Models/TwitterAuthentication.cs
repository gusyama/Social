using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SocialPost.Models
{
    public class Twitter
    {
        public string AccessToken { get; set; }
        public string AccessTokenSecret { get; set; }
        public string UserId { get; set; }
        public string ScreenUsername { get; set; }
        public string Message { get; set; }

    }
}