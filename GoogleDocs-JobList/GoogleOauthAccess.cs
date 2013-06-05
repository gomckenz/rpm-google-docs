using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GoogleDocs_JobList {
    ﻿using System;
    using System.Collections.Generic;
    using System.Text;
    using Google.GData.Apps;
    using Google.GData.Extensions.Apps;
    using Google.GData.Client;
    using Google.Contacts;
    using Google.GData.Apps.Groups;

    class GoogleOauthAccess
    {

        // Installed (non-web) application
        private static string redirectUri = "urn:ietf:wg:oauth:2.0:oob";

        // Requesting access to Contacts API and Groups Provisioning API
        private static string scopes = "https://spreadsheets.google.com/feeds https://docs.google.com/feeds";


        public static string getAuthorizationURL(string clientId, string clientSecret)
        {
            OAuth2Parameters parameters = getOAuth2Parameters(clientId, clientSecret);
            string authURL = OAuthUtil.CreateOAuth2AuthorizationUrl(parameters);
            return authURL;
        }

        public static void getAccessTokens(string clientId, string clientSecret, string accessCode, out string accessToken, out string refreshToken)
        {
            OAuth2Parameters parameters = getOAuth2Parameters(clientId, clientSecret,  accessCode);
            OAuthUtil.GetAccessToken(parameters);

            accessToken = parameters.AccessToken;
            refreshToken=  parameters.RefreshToken;
        }

        public static OAuth2Parameters getOAuth2Parameters(string clientId, string clientSecret, string accessCode = "", string accessToken = "")
        {
            OAuth2Parameters parameters = new OAuth2Parameters()
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                RedirectUri = redirectUri,
                Scope = scopes,
                AccessType = "offline"
            };
            if (accessCode != "")
            {
                parameters.AccessCode = accessCode;
            }
            if (accessToken != "")
            {
                parameters.AccessToken = accessToken;
            }
            return parameters;
        }

        public static GOAuth2RequestFactory getRequestFactory(string applicationName, OAuth2Parameters parameters)
        {
            return new GOAuth2RequestFactory(null, applicationName, parameters);
        }

        internal static string getRefreshedAccessToken(string clientId, string clientSecret, string refreshToken)
        {
            OAuth2Parameters parameters = getOAuth2Parameters(clientId, clientSecret);
            parameters.RefreshToken = refreshToken;

            OAuthUtil.RefreshAccessToken(parameters);
            return parameters.AccessToken;
        }
    }
}