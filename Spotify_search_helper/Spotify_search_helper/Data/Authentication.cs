using Newtonsoft.Json;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using static SpotifyAPI.Web.Scopes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

            return SpotifyClient;
        }

        private static async Task Auth()
        {
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                if(string.IsNullOrEmpty(clientId))
                {
                    Environment.SetEnvironmentVariable("SPOTIFY_CLIENT_ID", "xxx");
                    clientId = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID");
                }

                if (string.IsNullOrEmpty(clientSecret))
                {
                    Environment.SetEnvironmentVariable("SPOTIFY_CLIENT_SECRET", "xxx");
                    clientSecret = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET");
                }

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                    throw new NullReferenceException();
            }

            await Start();
        }

        private static async Task Start()
        {
            try
            {
                if (File.Exists(CredentialsPath))
                {
                    var json = await File.ReadAllTextAsync(CredentialsPath);
                    var token = JsonConvert.DeserializeObject<AuthorizationCodeTokenResponse>(json);

                    var authenticator = new AuthorizationCodeAuthenticator(clientId, clientSecret, token);
                    authenticator.TokenRefreshed += (sender, tokenx) => File.WriteAllText(CredentialsPath, JsonConvert.SerializeObject(tokenx));

                    var config = SpotifyClientConfig.CreateDefault()
                      .WithAuthenticator(authenticator);

                    /*_server.Dispose();
                    Environment.Exit(0);*/

                    SpotifyClient = new SpotifyClient(config);
                }
                else
                    await StartAuthentication();
            }
            catch (Exception)
            {

            }
        }

        private static async Task StartAuthentication()
        {
            var request = new LoginRequest(_server.BaseUri, clientId, LoginRequest.ResponseType.Code)
            {
                Scope = new List<string> { UserReadEmail, UserReadPrivate, PlaylistReadPrivate }
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
            else
            {
                ViewModels.Helpers.DisplayDialog("Authentication error!", "Error code: " + WebAuthenticationResult.ResponseErrorDetail + ", please try again.");
            }
            DataSource.AuthComplete();
        }
    }

}
