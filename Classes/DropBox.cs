using System.Net;
using Dropbox.Api;
using Dropbox.Api.Files;
using Newtonsoft.Json;
using System.Text;
using Microsoft.Extensions.FileSystemGlobbing;
using System.Management;
using Usb.Events;
using System.Threading.Channels;
using myoddweb.directorywatcher;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.FileProviders.Physical;
using Swan.Parsers;
namespace SmartRecorder
{
    public class DropBox
    {

        public async void DbHttpPost()
        {
            HttpClient client = new HttpClient();

            // Revoke Login
            var checklogin = new Dictionary<string, string>
            {
                { "Authorization", "Bearer null" },
                { "ContentType", "application/json" },
           //     { "User-Agent", "api-explorer-client" }
             };
            var checkloginapi = "/2/check/user";

            // Check Login Status
            var revokelogin = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {Settings.Default.AccessToken}" },
             };
            var revokeloginapi = "/2/auth/token/revoke";
            //int type = 1;  // select post type

            Dictionary<string, string> args;
            string api;
            switch (2)
            {
                case 1:
                    args = checklogin;
                    api = checkloginapi;
                    break;
                case 2:
                    args = revokelogin;
                    api = revokeloginapi;
                    break;
            }
            Console.WriteLine(args);
            var dict2json = JsonConvert.SerializeObject(args);
            //Console.WriteLine(valson);
            var content = new StringContent(dict2json, Encoding.UTF8, "application/json");
            Console.WriteLine(content);
            var uri = "https://api.dropboxapi.com" + api;
            Console.WriteLine(uri);
            //Console.WriteLine(JsonSerializer.Serialize(valson);
            var response = await client.PostAsync(uri, content);

            //    var responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine(response);
        }

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
        }
        public void DBAuth()
        {
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
            Console.WriteLine("DropBox Authorization Starting");
                await AcquireAccessToken(null, IncludeGrantedScopes.None);
                var httpClient = new HttpClient(new HttpClientHandler { });
                try
                {
                    var config = new DropboxClientConfig("SmartRecorder")
                    {
                        HttpClient = httpClient
                    };

                    var client = new DropboxClient(Settings.Default.RefreshToken, Config.DbApiKey, config);

                    // This call should succeed since the correct scope has been acquired

                    var dbGetAccountTask = GetCurrentAccount(client);
                    dbGetAccountTask.Wait();
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
        private string _dbauthcode { get; set; } = string.Empty;
        async Task<string> AcquireAccessToken(string[] scopeList, IncludeGrantedScopes includeGrantedScopes)
        {

            Console.Write("Resetting auth keys ");
            //if (Console.ReadKey().Key == ConsoleKey.Y)
            //{
            //    /*Settings.Default.Reset();*/
            //}
            Console.WriteLine();
            Settings.Default.Reset();
            var accessToken = Settings.Default.AccessToken;
            var refreshToken = Settings.Default.RefreshToken;

            if (string.IsNullOrEmpty(accessToken))
            {
                try
                {
                    // Console.WriteLine("Waiting for credentials.");
                    var OAuthFlow = new PKCEOAuthFlow();
                    var authorizeUri = OAuthFlow.GetAuthorizeUri(OAuthResponseType.Code, Config.DbApiKey, state: "N", tokenAccessType: TokenAccessType.Offline, scopeList: scopeList, includeGrantedScopes: includeGrantedScopes);

                    Console.WriteLine("Visit this webpage and get credentials:");
                    //Console.WriteLine(authorizeUri);
                    //WAIT FOR USB INSERT SOMEHOW
                    // Create a file to write to.
                    Global.DropBoxCodeDotTxtContains = authorizeUri + Environment.NewLine;
                    Console.WriteLine($"DropBox Code Path: {Global.RemovableDrivePath}");
                    File.WriteAllText(Path.Combine(Global.RemovableDrivePath, "DropBoxCode.txt"), Global.DropBoxCodeDotTxtContains);
                    
                    Console.WriteLine("Waiting For DropBox Authorization Code");
                    while (Global.DropBoxCodeDotTxtContains.StartsWith("https:")) // ADD condition for not already authorized
                        {
                        DetectUSB().GetAwaiter().GetResult();
                        //await Task.Delay(1000);
                        }

                    //string accessCodenil = Console.ReadLine();
                    //var 
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
                    var accessCode = Global.DropBoxCodeDotTxtContains;
                    Console.WriteLine("Exchanging code for token");
                    // tokenResult.DefaultIkenizedUri = OAuthFlow.ProcessCodeFlowAsync(accessCode, Global.DbApiKey);

                    var tokenResult = await OAuthFlow.ProcessCodeFlowAsync(accessCode, Config.DbApiKey);//, RedirectUri.ToString(), state);
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
                    return "Recorder";// uid;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: {0}", e.Message);
                    return null;
                }
            }
            return null;
        }
        public async Task DetectUSB()
        {
            Console.WriteLine(Global.Home);
                //using var provider = new PhysicalFileProvider(Global.Home);
                
                using var provider = new PhysicalFileProvider(Global.RemovableDrivePath);
                Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "true");
                provider.UsePollingFileWatcher = true;
                provider.UseActivePolling = true;
                var contents = provider.GetDirectoryContents(string.Empty);
                //foreach (PhysicalDirectoryInfo fileInfo in contents)
                //{
                //    Console.WriteLine(fileInfo.PhysicalPath);
                //}
                IChangeToken changeToken = provider.Watch("DropBoxCode.txt");
                var tcs = new TaskCompletionSource<object>();
                changeToken.RegisterChangeCallback(state =>
                ((TaskCompletionSource<object>)state).TrySetResult(null), tcs);
            await tcs.Task.ConfigureAwait(true);
            Console.WriteLine("USB event detected");
            await Task.Delay(1000);
            Global.DropBoxCodeDotTxtContains = GetDBCodeFromUSB();
            Console.WriteLine(Global.DropBoxCodeDotTxtContains);
        }
        public void USBDetected()
        {
            Console.WriteLine("USB event detected");
            //Global.DropBoxCodeDotTxtContains = GetDBCodeFromUSB();
            Task.Delay(1000);
            Global.DropBoxCodeDotTxtContains = GetDBCodeFromUSB();
        }
        public void watcher_USBDetected(object sender, EventArrivedEventArgs e)
        {
            Console.WriteLine("USB event detected");
            //Global.DropBoxCodeDotTxtContains = GetDBCodeFromUSB();
            Task.Delay(1000);
            Global.DropBoxCodeDotTxtContains = GetDBCodeFromUSB();
        }
        public string GetDBCodeFromUSB()
        {
            
            string path = Path.Combine(Global.RemovableDrivePath.ToString(), "DropBoxCode.txt");
            UtilClass gc = new();
            while (!gc.IsFileReady(path))
            {
                Task.Delay(1000);
            }
            Console.WriteLine(path);
            using (System.IO.StreamReader sr = new System.IO.StreamReader(path))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] split = line.Split(',');
                    foreach (string word in split)
                    {
                        _dbauthcode = word;
                    }
                }
            }
            return _dbauthcode;
        }

    }
    }
