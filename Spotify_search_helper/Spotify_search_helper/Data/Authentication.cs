using Newtonsoft.Json;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using static SpotifyAPI.Web.Scopes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;
using Windows.Storage;

namespace Spotify_search_helper.Data
{
    class Authentication
    {
        public static SpotifyClient SpotifyClient { get; set; }

        private static readonly string CredentialsPath = ApplicationData.Current.LocalFolder.Path + "\\credentials.json";
        private static string clientId = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID");
        private static string clientSecret = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET");
        private static readonly EmbedIOAuthServer _server = new EmbedIOAuthServer(new Uri("http://localhost:5000/callback"), 5000);

        /// <summary>
        /// Checks if user is authenticated
        /// </summary>
        /// <returns>
        /// Null if failed, Profile if successful
        /// </returns>
        public static async Task<PrivateUser> IsAuthenticated()
        {
            //check if file with token exists, if it does not exist, login will be shown
            if (!File.Exists(CredentialsPath))
                return null;

            var json = await File.ReadAllTextAsync(CredentialsPath);
            var token = JsonConvert.DeserializeObject<AuthorizationCodeTokenResponse>(json);

            CheckCliendSecretId();

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                return null;

            var authenticator = new AuthorizationCodeAuthenticator(clientId, clientSecret, token);
            authenticator.TokenRefreshed += (sender, tokenx) => File.WriteAllText(CredentialsPath, JsonConvert.SerializeObject(tokenx));

            //might throw an error if user revoked access to their spotify account
            var config = SpotifyClientConfig.CreateDefault()
              .WithAuthenticator(authenticator);

            SpotifyClient = new SpotifyClient(config);
            //try and get user profile
            return await SpotifyClient.UserProfile.Current();
        }

        public static async Task<SpotifyClient> GetSpotifyClientAsync()
        {
            if (SpotifyClient == null)
                await Authenticate();

            //check if client is valid
            try
            {
                await SpotifyClient.UserProfile.Current();
                return SpotifyClient;
            }
            catch (Exception)
            {
                //error, try to re-authenticate
                return await Authenticate();
            }
        }

        public static async Task<SpotifyClient> Authenticate()
        {
            CheckCliendSecretId();

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                return null;

            var request = new LoginRequest(_server.BaseUri, clientId, LoginRequest.ResponseType.Code)
            {
                Scope = new List<string> { UserReadPrivate, PlaylistReadPrivate, UserModifyPlaybackState,
                    UserLibraryModify, UserLibraryRead, PlaylistModifyPrivate,
                    PlaylistModifyPublic, UgcImageUpload }
            };

            Uri uri = request.ToUri();

            System.Uri StartUri = uri;
            System.Uri EndUri = new Uri("http://localhost:5000/callback");

            WebAuthenticationResult WebAuthenticationResult = await WebAuthenticationBroker.AuthenticateAsync(
                                                    WebAuthenticationOptions.None,
                                                    StartUri,
                                                    EndUri);
            if (WebAuthenticationResult.ResponseStatus == WebAuthenticationStatus.Success)
            {
                var index = WebAuthenticationResult.ResponseData.IndexOf("code=");
                string code = WebAuthenticationResult.ResponseData.Substring(index + 5);

                var config = SpotifyClientConfig.CreateDefault();
                var tokenResponse = await new OAuthClient(config).RequestToken(
              new AuthorizationCodeTokenRequest(
                clientId, clientSecret, code, EndUri));

                SpotifyClient = new SpotifyClient(tokenResponse.AccessToken);

                try
                {
                    if (!File.Exists(CredentialsPath))
                    {
                        await ApplicationData.Current.LocalFolder.CreateFileAsync("credentials.json");
                    }
                    await File.WriteAllTextAsync(CredentialsPath, JsonConvert.SerializeObject(tokenResponse));
                }
                catch (Exception)
                {
                    
                }
                return SpotifyClient;
            }
            else
                return null;
        }

        private static void CheckCliendSecretId()
        {
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                if (string.IsNullOrEmpty(clientId))
                {
                    Environment.SetEnvironmentVariable("SPOTIFY_CLIENT_ID", "de354ca4295141c6ad3a7a07086fbd32");
                    clientId = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID");
                }

                if (string.IsNullOrEmpty(clientSecret))
                {
                    Environment.SetEnvironmentVariable("SPOTIFY_CLIENT_SECRET", "");
                    clientSecret = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET");
                }
            }
        }
    }

}
