using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Privatix.Core.Site
{
    class GoogleTokenResponse
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
        public string id_token  { get; set; }
        public string refresh_token { get; set; }
    }

    class GoogleTokenInfoResponse
    {
        public string issuer { get; set; }
        public string issued_to { get; set; }
        public string audience { get; set; }
        public string user_id { get; set; }
        public int expires_in { get; set; }
        public int issued_at { get; set; }
        public string email { get; set; }
        public bool email_verified { get; set; }
    }

    class GoogleErrorResponse
    {
        public string error { get; set; }
        public string error_description { get; set; }
    }
}
