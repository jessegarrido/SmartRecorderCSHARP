namespace OauthPKCE
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;

    using Dropbox.Api;
    using Dropbox.Api.Files;
    using RestSharp;
    using SmartRecorder;


    public class DropBox
    {
        // Add an ApiKey (from https://www.dropbox.com/developers/apps) here
        private const string ApiKey = "XXXXXXXXXXXXXXXX";

        // This loopback host is for demo purpose. If this port is not
        // available on your machine you need to update this URL with an unused port.
        private const string LoopbackHost = "http://127.0.0.1:52475/";
        //private const string LoopbackHost = null;

        // URL to receive OAuth 2 redirect from Dropbox server.
        // You also need to register this redirect URL on https://www.dropbox.com/developers/apps.
        private readonly Uri RedirectUri = new Uri(LoopbackHost + "authorize");

        // URL to receive access token from JS.
        private readonly Uri JSRedirectUri = new Uri(LoopbackHost + "token");


        //  [DllImport("kernel32.dll", ExactSpelling = true)]
        // private static extern IntPtr GetConsoleWindow();
        //
        //   [DllImport("user32.dll")]
        //  [return: MarshalAs(UnmanagedType.Bool)]
        //  private static extern bool SetForegroundWindow(IntPtr hWnd);


        public void DBPush()
        {
            //var instance = new Program();
            var client = new DropboxClient(Settings.Default.RefreshToken, Config.DbApiKey);
            string folder = $"/{Global.SessionName}";
            // var client = new DropboxClient(Settings.Default.RefreshToken, ApiKey, config);
            Console.WriteLine("Push To Remote");
            Console.WriteLine(client);
            Console.WriteLine(folder);
            Console.WriteLine(Global.mp3PathAndName);

            try
            {
                var createFolderTask = CreateFolder(client, folder);
                createFolderTask.Wait();
                string file = Global.mp3PathAndName;
                var uploadTask = ChunkUpload(client, folder, file);//    Task.Run((Func<Task<int>>)instance.Run);
                uploadTask.Wait();
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw e;
            }
             async Task<FolderMetadata> CreateFolder(DropboxClient client, string path)
            {
                Console.WriteLine("--- Creating Folder ---");
                Console.WriteLine(path);

                var folderArg = new CreateFolderArg(path);
                Console.WriteLine(folderArg);
                try
                {
                    var folder = await client.Files.CreateFolderV2Async(folderArg);

                    Console.WriteLine("Folder: " + path + " created!");

                    return folder.Metadata;
                }
                catch (ApiException<CreateFolderError> e)
                {
                    if (e.Message.StartsWith("path/conflict/folder"))
                    {
                        Console.WriteLine("Folder already exists... Skipping create");
                        return null;
                    }
                    else
                    {
                        throw e;
                    }
                }
            }
        }
        async Task ChunkUpload(DropboxClient client, string folder, string fileName)
            {
                Console.WriteLine("Chunk upload file...");
                // Chunk size is 128KB.
                const int chunkSize = 128 * 1024;
            byte[] myByteArray = File.ReadAllBytes(fileName);
            using (var stream = new MemoryStream(myByteArray))
                {
                    int numChunks = (int)Math.Ceiling((double)stream.Length / chunkSize);

                    byte[] buffer = new byte[chunkSize];
                    string sessionId = null;

                    for (var idx = 0; idx < numChunks; idx++)
                    {
                        Console.WriteLine("Start uploading chunk {0}", idx);
                        var byteRead = stream.Read(buffer, 0, chunkSize);

                        using (MemoryStream memStream = new MemoryStream(buffer, 0, byteRead))
                        {
                            if (idx == 0)
                            {
                                var result = await client.Files.UploadSessionStartAsync(body: memStream);
                                sessionId = result.SessionId;
                            }

                            else
                            {
                                UploadSessionCursor cursor = new UploadSessionCursor(sessionId, (ulong)(chunkSize * idx));

                                if (idx == numChunks - 1)
                                {
                                    var name = Path.GetFileName(fileName);
                                    await client.Files.UploadSessionFinishAsync(cursor, new CommitInfo(folder + "/" + name), body: memStream);
                                }

                                else
                                {
                                    await client.Files.UploadSessionAppendV2Async(cursor, body: memStream);
                                }
                            }
                        }
                    }
                }
            }
            
        public void DBAuth()
            {
            var instance = new Program();
                // var client = new DropboxClient(Settings.Default.RefreshToken, ApiKey, config);
                 Console.WriteLine("Example OAuth PKCE Application");
                try
                {
                    var task = Run();//    Task.Run((Func<Task<int>>)instance.Run);
                    task.Wait();
                return;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw e;
                }
            }

        async Task GetCurrentAccount(DropboxClient client)
        {
            try
            {
                Console.WriteLine("Current Account:");
                var full = await client.Users.GetCurrentAccountAsync();

                Console.WriteLine("Account id    : {0}", full.AccountId);
                Console.WriteLine("Country       : {0}", full.Country);
                Console.WriteLine("Email         : {0}", full.Email);
                Console.WriteLine("Is paired     : {0}", full.IsPaired ? "Yes" : "No");
                Console.WriteLine("Locale        : {0}", full.Locale);
                Console.WriteLine("Name");
                Console.WriteLine("  Display  : {0}", full.Name.DisplayName);
                Console.WriteLine("  Familiar : {0}", full.Name.FamiliarName);
                Console.WriteLine("  Given    : {0}", full.Name.GivenName);
                Console.WriteLine("  Surname  : {0}", full.Name.Surname);
                Console.WriteLine("Referral link : {0}", full.ReferralLink);

                if (full.Team != null)
                {
                    Console.WriteLine("Team");
                    Console.WriteLine("  Id   : {0}", full.Team.Id);
                    Console.WriteLine("  Name : {0}", full.Team.Name);
                }
                else
                {
                    Console.WriteLine("Team - None");
                }
            }
            catch (Exception e)
            {
                throw e;
            }

        }
        async Task<int> Run()
            {
                await AcquireAccessToken(null, IncludeGrantedScopes.None);
                var httpClient = new HttpClient(new HttpClientHandler { });
                try
                {
                    var config = new DropboxClientConfig("SmartRecorder")
                    {
                        HttpClient = httpClient
                    };

                    var client = new DropboxClient(Settings.Default.RefreshToken, ApiKey, config);

                // This call should succeed since the correct scope has been acquired
                    
                    var dbGetAccountTask = GetCurrentAccount(client);
                    dbGetAccountTask.Wait();
                    Console.WriteLine("Oauth PKCE Test Complete!");
                    Console.WriteLine("Exit with any key");
                    Console.ReadKey();
                }
                catch (HttpException e)
                {
                    Console.WriteLine("Exception reported from RPC layer");
                    Console.WriteLine("    Status code: {0}", e.StatusCode);
                    Console.WriteLine("    Message    : {0}", e.Message);
                    if (e.RequestUri != null)
                    {
                        Console.WriteLine("    Request uri: {0}", e.RequestUri);
                    }
                }

                return 0;
            }

            /// <summary>
            /// Handles the redirect from Dropbox server. Because we are using token flow, the local
            /// http server cannot directly receive the URL fragment. We need to return a HTML page with
            /// inline JS which can send URL fragment to local server as URL parameter.
            /// </summary>
            /// <param name="http">The http listener.</param>
            /// <returns>The <see cref="Task"/></returns>
             async Task HandleOAuth2Redirect(HttpListener http)
            {
                var context = await http.GetContextAsync();

                // We only care about request to RedirectUri endpoint.
                while (context.Request.Url.AbsolutePath != RedirectUri.AbsolutePath)
                {
                    context = await http.GetContextAsync();
                }

                context.Response.ContentType = "text/html";

                // Respond with a page which runs JS and sends URL fragment as query string
                // to TokenRedirectUri.
                using (var file = File.OpenRead("index.html"))
                {
                    file.CopyTo(context.Response.OutputStream);
                }

                context.Response.OutputStream.Close();
            }

            /// <summary>
            /// Handle the redirect from JS and process raw redirect URI with fragment to
            /// complete the authorization flow.
            /// </summary>
            /// <param name="http">The http listener.</param>
            /// <returns>The <see cref="OAuth2Response"/></returns>
             async Task<Uri> HandleJSRedirect(HttpListener http)
            {
                
            var context = await http.GetContextAsync();

                // We only care about request to TokenRedirectUri endpoint.
                while (context.Request.Url.AbsolutePath != JSRedirectUri.AbsolutePath)
                {
                    context = await http.GetContextAsync();
                }

                var redirectUri = new Uri(context.Request.QueryString["url_with_fragment"]);

                return redirectUri;
            }
            
            /// <summary>
            /// Acquires a dropbox access token and saves it to the default settings for the app.
            /// <para>
            /// This fetches the access token from the applications settings, if it is not found there
            /// (or if the user chooses to reset the settings) then the UI in <see cref="LoginForm"/> is
            /// displayed to authorize the user.
            /// </para>
            /// </summary>
            /// <returns>A valid uid if a token was acquired or null.</returns>
             async Task<string> AcquireAccessToken(string[] scopeList, IncludeGrantedScopes includeGrantedScopes)
            {
                Console.Write("Reset settings (Y/N) ");
                if (Console.ReadKey().Key == ConsoleKey.Y)
                {
                    Settings.Default.Reset();
                }
                Console.WriteLine();

                var accessToken = Settings.Default.AccessToken;
                var refreshToken = Settings.Default.RefreshToken;

                if (string.IsNullOrEmpty(accessToken))
                {
                    try
                    {
                    // Console.WriteLine("Waiting for credentials.");
                    var OAuthFlow = new PKCEOAuthFlow();
                    var authorizeUri = OAuthFlow.GetAuthorizeUri(OAuthResponseType.Code, ApiKey, state: "N", tokenAccessType: TokenAccessType.Offline, scopeList: scopeList, includeGrantedScopes: includeGrantedScopes);

                    Console.WriteLine("Visit this webpage and get credentials:");
                    Console.WriteLine(authorizeUri);
                    Console.WriteLine("Paste the response here:");

                    string accessCode = Console.ReadLine();
                    //Settings.Default.AccessToken = accessToken;
                    // Summary:
                    //     Processes the second half of the OAuth 2.0 code flow. Uses the codeVerifier created
                    //     in this class to execute the second half.
                    //
                    // Parameters:
                    //   code:
                    //     The code acquired in the query parameters of the redirect from the initial authorize
                    //     url.
                    //
                    //   appKey:
                    //     The application key, found in the App Console.
                    //
                    //   redirectUri:
                    //     The redirect URI that was provided in the initial authorize URI, this is only
                    //     used to validate that it matches the original request, it is not used to redirect
                    //     again.
                    //
                    //   client:
                    //     An optional http client instance used to make requests.
                    //
                    // Returns:
                    //     The authorization response, containing the access token and uid of the authorized
                    //     user.
                    Console.WriteLine("Exchanging code for token");
                    // tokenResult.DefaultIkenizedUri = OAuthFlow.ProcessCodeFlowAsync(accessCode, Global.DbApiKey);



                    var tokenResult = await OAuthFlow.ProcessCodeFlowAsync(accessCode, ApiKey);//, RedirectUri.ToString(), state);
                    Console.WriteLine("Finished Exchanging Code for Token");
                    // Bring console window to the front.
                   // SetForegroundWindow(GetConsoleWindow());
                    accessToken = tokenResult.AccessToken;
                    refreshToken = tokenResult.RefreshToken;
                    var uid = tokenResult.Uid;
                    Console.WriteLine("Uid: {0}", uid);
                    Console.WriteLine("AccessToken: {0}", accessToken);
                    if (tokenResult.RefreshToken != null)
                    {
                        Console.WriteLine("RefreshToken: {0}", refreshToken);
                        Settings.Default.RefreshToken = refreshToken;
                    }
                    if (tokenResult.ExpiresAt != null)
                    {
                        Console.WriteLine("ExpiresAt: {0}", tokenResult.ExpiresAt);
                    }
                    if (tokenResult.ScopeList != null)
                    {
                        Console.WriteLine("Scopes: {0}", String.Join(" ", tokenResult.ScopeList));
                    }
                    Settings.Default.AccessToken = accessToken;
                    Settings.Default.Uid = uid;
                    Settings.Default.Save();
/*
                    var dbClient = new RestClient("https://api.dropbox.com/oauth2/token");
                    RestRequest request = new RestRequest("Smart", Method.Post);
                    request.AddParameter("grant_type", "refresh_token");
                    request.AddParameter("client_id", Global.DbApiKey);
                    request.AddParameter("client_secret", Global.DbApiSecret);

                    var response = dbClient.Post(request);
                    var content = response.Content;
                    Console.WriteLine(content);
                    var tokenResult = Settings.Default.RefreshToken;
                    */
                    //  http.Stop();
                    return "SmartCorder";// uid;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error: {0}", e.Message);
                        return null;
                    }
                }
                return null;
            }

            /// <summary>
            /// Gets information about the currently authorized account.
            /// <para>
            /// This demonstrates calling a simple rpc style api from the Users namespace.
            /// </para>
            /// </summary>
            /// <param name="client">The Dropbox client.</param>
            /// <returns>An asynchronous task.</returns>

        }
    }