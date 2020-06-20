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
        private static SpotifyClient _spotifyClient;
        public static SpotifyClient SpotifyClient
        {
            get => _spotifyClient;
            set => _spotifyClient = value;
        }

        private static readonly string CredentialsPath = ApplicationData.Current.LocalFolder.Path + "\\credentials.json";
        private static string clientId = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID");
        private static string clientSecret = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET");
        private static readonly EmbedIOAuthServer _server = new EmbedIOAuthServer(new Uri("http://localhost:5000/callback"), 5000);

        public static async Task<SpotifyClient> GetClient()
        {
            if (SpotifyClient == null)
                await Auth();

            //check if spotify client is still valid or has access been revoked
            try
            {
                await SpotifyClient.UserProfile.Current();
                return SpotifyClient;
                //success, return client
            }
            catch (Exception)
            {
                //error, try to re-authenticate
                await Auth(true);
                return SpotifyClient;
            }

        }


        //private static async void ReAuthenticate()
        //{
        //    //delete existing file
        //    await StartAuthentication();
        //}

        private static async Task Auth(bool reauthenticate = false)
        {
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                if(string.IsNullOrEmpty(clientId))
                {
                    Environment.SetEnvironmentVariable("SPOTIFY_CLIENT_ID", "");
                    clientId = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID");
                }

                if (string.IsNullOrEmpty(clientSecret))
                {
                    Environment.SetEnvironmentVariable("SPOTIFY_CLIENT_SECRET", "");
                    clientSecret = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET");
                }

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                    throw new NullReferenceException();
            }

            await Start(reauthenticate);
        }

        private static async Task Start(bool reauthenticate = false)
        {
            try
            {
                if (!reauthenticate && File.Exists(CredentialsPath))
                {
                    var json = await File.ReadAllTextAsync(CredentialsPath);
                    var token = JsonConvert.DeserializeObject<AuthorizationCodeTokenResponse>(json);

                    var authenticator = new AuthorizationCodeAuthenticator(clientId, clientSecret, token);
                    authenticator.TokenRefreshed += (sender, tokenx) => File.WriteAllText(CredentialsPath, JsonConvert.SerializeObject(tokenx));

                    //might throw an error if user revoked access to their spotify account
                    var config = SpotifyClientConfig.CreateDefault()
                      .WithAuthenticator(authenticator);

                    SpotifyClient = new SpotifyClient(config);
                }
                else
                    await StartAuthentication();
            }
            catch (Exception e)
            {
                ViewModels.Helpers.DisplayDialog("Authentication error", e.Message);
            }
        }

        private static async Task StartAuthentication()
        {
            var request = new LoginRequest(_server.BaseUri, clientId, LoginRequest.ResponseType.Code)
            {
                Scope = new List<string> { UserReadPrivate, PlaylistReadPrivate, UserModifyPlaybackState,
                    UserLibraryModify, UserLibraryRead, PlaylistModifyPrivate, 
                    PlaylistModifyPublic }
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
                    //
                }
            }
            else if (WebAuthenticationResult.ResponseStatus == WebAuthenticationStatus.ErrorHttp)
            {
                ViewModels.Helpers.DisplayDialog("Authentication error!", "Error code: " + WebAuthenticationResult.ResponseErrorDetail + ", please try again.");
            }
            else if(WebAuthenticationResult.ResponseStatus == WebAuthenticationStatus.UserCancel)
            {
                SpotifyClient = null;
                DataSource.AuthFailed();

                //show that user can't use app if they are not logged in
            }
            else
            {
                ViewModels.Helpers.DisplayDialog("Authentication error!", "Error code: " + WebAuthenticationResult.ResponseErrorDetail + ", please try again.");
            }
            DataSource.AuthComplete();
        }
    }

}
