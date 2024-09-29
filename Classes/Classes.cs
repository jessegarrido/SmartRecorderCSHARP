using System.Runtime.InteropServices;
using System.Text;
using System.Net.NetworkInformation;
using PortAudioSharp;
using static SmartRecorder.Configuration;
using NAudio.Wave;
using NAudio.Lame;
using SimpleWifi;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

namespace SmartRecorder
{

    public partial class DeviceActions
    {

        //public DeviceActions()  { }

        //[DllImport("kernel32.dll")]
        //static extern bool SetConsoleMode(IntPtr hConsoleHandle, int mode);

        //[DllImport("kernel32.dll")]
        //static extern bool GetConsoleMode(IntPtr hConsoleHandle, out int mode);

        //[DllImport("kernel32.dll")]
        //static extern IntPtr GetStdHandle(int handle);

        //const int STD_INPUT_HANDLE = -10;
        //const int ENABLE_QUICK_EDIT_MODE = 0x40 | 0x80;

        //public static void EnableQuickEditMode()
        //{
        //    int mode;
        //    IntPtr handle = GetStdHandle(STD_INPUT_HANDLE);
        //    GetConsoleMode(handle, out mode);
        //    mode |= ENABLE_QUICK_EDIT_MODE;
        //    SetConsoleMode(handle, mode);
        //}

        public void AudioDeviceInitAndEnumerate(bool enumerate)
        {
            PortAudio.LoadNativeLibrary();
            PortAudio.Initialize();
            Console.WriteLine(PortAudio.VersionInfo.versionText);
            Console.WriteLine($"Number of audio devices: {PortAudio.DeviceCount}");
            if (enumerate == true)
            {
                for (int i = 0; i != PortAudio.DeviceCount; ++i)
                {
                    Console.WriteLine($" Device {i}");
                    DeviceInfo deviceInfo = PortAudio.GetDeviceInfo(i);
                    Console.WriteLine($"   Name: {deviceInfo.name}");
                    Console.WriteLine($"   Max input channels: {deviceInfo.maxInputChannels}");
                    Console.WriteLine($"   Default sample rate: {deviceInfo.defaultSampleRate}");
                }
            }
        }
        public int ReturnAudioDevice(int? configSelectedIndex)
        {
            int deviceIndex;
            deviceIndex = configSelectedIndex == null ? PortAudio.DefaultInputDevice : Config.SelectedAudioDevice;

            if (deviceIndex == PortAudio.NoDevice)
            {
                Console.WriteLine("No default input device found");
                Environment.Exit(1);
            }
            DeviceInfo info = PortAudio.GetDeviceInfo(deviceIndex);
            Console.WriteLine();
            Console.WriteLine($"Initializing audio device {deviceIndex}: ({info.name})");
            Config.SelectedAudioDevice = deviceIndex;
            return deviceIndex;


        }
        public void ClearDailyTakesCount()
        {
            DateTime today = DateTime.Today;
            if (today != Settings.Default.LastTakeDate)
            {
                Settings.Default.Takes = 0;
                Settings.Default.Save();
            }
        }
        public int AskKeepOrEraseFiles()
        {
            string[] allfiles = Directory.GetFiles(Global.LocalRecordingsFolder, "*.*", SearchOption.AllDirectories);

            if (allfiles.Length > 0)
            {
                Console.Write("Press 'record' to delete all saved recordings on SD & USB, and clear upload and play queues, or press 'play' to keep files ");
                //flash leds     
                int erased = GetValidUserSelection(new List<int> { 0, 1, 2 });
                if (erased == 1)
                {
                    Console.WriteLine($"Erase {allfiles.Length} files in Recordings Folder.");
                    DirectoryInfo di = new DirectoryInfo(Global.LocalRecordingsFolder);

                    foreach (FileInfo file in di.GetFiles("*.*", SearchOption.AllDirectories))
                    {
                        file.Delete();
                    }
                    foreach (DirectoryInfo dir in di.GetDirectories())
                    {
                        dir.Delete(true);
                    }//erase all the files!
                }
                else if (erased == 0)
                {
                    //reboot the pi
                }
                return erased;
            }
            else return 0;
        }

        public int MainMenu()
        {
            //var deviceState = Global.MyState;
            Console.WriteLine("Press 1 to Record/Pause 2 to Play/Pause 3 to Skip Back 4 to Skip Forward 0 to Reboot");
            var selection = GetValidUserSelection(new List<int> { 0, 1, 2, 3, 4 }); // 0=reboot,1=record,2=play,3=skipforward,4skipback
            switch (selection)
            {
                case 1:
                    if (Global.MyState == 2)
                    {
                        Global.MyState = 1;
                        NormalizeTake();
                        WavToMP3(Global.wavPathAndName, Global.mp3PathAndName);
                        DropBox db = new DropBox();
                        //db.DBAuth();
                        db.DBPush();

                        //ListRootFolder();
                        //PushMp3ToCloud();
                    }
                    else
                    {
                        Thread recordThread = new Thread(
                            o =>
                            {
                                RecordAudio();
                            });

                        recordThread.Start();
                    }
                    //GetValidatedSelection(new List<int> { 0 });
                    ////Global.MyState = 1;
                    //Thread threadRecording = new Thread(new ThreadStart(RecordAudio(sessionName,homeDirectory)));
                    break;
                case 2:
                    break;
            }
            return -1;
        }
        public void SessionInit()
        {
            ClearDailyTakesCount();
            LoadConfig();
            Console.WriteLine("Welcome to PortaSmart");
            Global.OS = GetOS();
            SetupLocalFileAndLocation();
            Console.WriteLine($"Operating System: {Global.OS}");
            Console.WriteLine($"Session Name: {Global.SessionName}");
            Console.WriteLine($"User's Home Folder: {Global.Home}");

            //TODO mount usb drives
            //TODO blink the leds
            int erased = AskKeepOrEraseFiles();
            CheckNetwork();
            //TODO fix network if absent / setup background check
            AudioDeviceInitAndEnumerate(false);
            // ReturnAudioDevice(Config.SelectedAudioDevice);

            FindRemovableDrives(true);

            // EstablishWifi();
            DropBox db = new DropBox();
            //db.DbHttpPost();

            db.DBAuth();

            //CloudAuth();
            //DropBoxPlugin db = new();
            //db.AuthorizeDropBox();
            //ListCloudContents();
            //var usb = new USBWatch();
            //usb.USBWatcher();
            //DetectUSB();
            //GetDBCodeFromUSB();
            Global.MyState = 1;
        }
        public bool CheckNetwork()
        {
            Global.NetworkStatus = NetworkInterface.GetIsNetworkAvailable();
            Console.WriteLine($"Network Connected Status: {Global.NetworkStatus}");
            //set network status LED
            return Global.NetworkStatus;
        }
        void EstablishWifi()
        {
            Wifi wifi = new Wifi();
            IEnumerable<AccessPoint> accessPoints = wifi.GetAccessPoints();
            string ssid = Settings.Default.SSID;
            AccessPoint selectedAP = null;
            bool isApFound = false;
            foreach (AccessPoint ap in accessPoints)
            {
                Console.Write($"{ap.Name}");
                if (ap.Name.Equals(ssid, StringComparison.InvariantCultureIgnoreCase))
                {
                    selectedAP = ap;
                    isApFound = true;
                    break;
                }
            }
            if (Global.NetworkStatus == false && !selectedAP.IsConnected)
            {
                Console.WriteLine("\r\n{0}\r\n", selectedAP.ToString());
                Console.WriteLine("Trying to connect..\r\n");
                AuthRequest authRequest = new AuthRequest(selectedAP);
                selectedAP.Connect(authRequest);
            }
            else if (selectedAP.IsConnected)
            {
                Console.WriteLine($"Connected To {Settings.Default.SSID}");
            }
            else if (Global.NetworkStatus == true && !selectedAP.IsConnected)
            {
                Console.WriteLine($"Connected to network by ethernet");
            }
            else
            {
                Console.WriteLine($"{Settings.Default.SSID} is unavailable.");
            }
        }
        public static string GetDeviceName(nint name)
        {
            string s = "";
            if (name != nint.Zero)
            {
                int length = 0;
                unsafe
                {
                    byte* b = (byte*)name;
                    if (b != null)
                    {
                        while (*b != 0)
                        {
                            ++b;
                            length += 1;
                        }
                    }
                }

                if (length > 0)
                {
                    byte[] stringBuffer = new byte[length];
                    Marshal.Copy(name, stringBuffer, 0, length);
                    s = Encoding.UTF8.GetString(stringBuffer);
                }
            }
            return s;
        }

        void SetupLocalFileAndLocation()
        {
            string newWavPath = Path.Combine(Global.LocalRecordingsFolder, Global.SessionName);
            string newMp3Path = Path.Combine(newWavPath, "mp3");
            List<string> songfilepaths = new List<string> { newWavPath, newMp3Path };
            foreach (string path in songfilepaths)
            {
                if (Directory.Exists(path))
                {
                    Console.WriteLine($"Path {path} exists");
                }
                else
                {
                    DirectoryInfo di = Directory.CreateDirectory(path);
                    Console.WriteLine($"The directory {path} created at {Directory.GetCreationTime(newWavPath)}.");
                }
            }
            int wavCount = Directory.GetFiles(Path.GetDirectoryName(newWavPath), "*", SearchOption.TopDirectoryOnly).Length;
            int mp3Count = Directory.GetFiles(Path.GetDirectoryName(newMp3Path), "*", SearchOption.TopDirectoryOnly).Length;
            //int take = Math.Max(wavCount, mp3Count) + 1;
            //Settings.Default.Takes++;
            int take = Settings.Default.Takes + 1;
            Global.wavPathAndName = Path.Combine(newWavPath, $"{Global.SessionName}_take-{take}.wav");
            Global.mp3PathAndName = Path.Combine(newWavPath, "mp3", $"{Global.SessionName}_take-{take}.mp3");

            //  return wavPathAndName;
        }
        public void RecordAudio()
        {
            Global.MyState = 2; // TODO generic update curent state function

            StreamParameters param = SetAudioParameters();

            SetupLocalFileAndLocation(); // writes to Global variables

            DeviceInfo info = PortAudio.GetDeviceInfo(Config.SelectedAudioDevice);
            Console.WriteLine();
            Console.WriteLine($"Use default device {Config.SelectedAudioDevice} ({info.name})");
            int numChannels = param.channelCount;

            //FileStream f = new FileStream(wavPathAndName, FileMode.Create);
            // using (Float32WavWriter wr = new Float32WavWriter(Global.wavPathAndName, Config.SampleRate, numChannels))
            WaveFormat wavformat = new WaveFormat(Config.SampleRate, 2);
            using (WaveFileWriter wr = new WaveFileWriter(Global.wavPathAndName, wavformat))
            {
                PortAudioSharp.Stream.Callback callback = (nint input, nint output,
                    uint frameCount,
                    ref StreamCallbackTimeInfo timeInfo,
                    StreamCallbackFlags statusFlags,
                    nint userData
                    ) =>
                {
                    frameCount = frameCount * (uint)numChannels;
                    float[] samples = new float[frameCount];
                    Marshal.Copy(input, samples, 0, (int)frameCount);
                    wr.WriteSamples(samples,0, (int)frameCount);
                    return StreamCallbackResult.Continue;
                };

                Console.WriteLine(param);
                Console.WriteLine(Config.SampleRate);
                Console.WriteLine("Now Recording");

                PortAudioSharp.Stream stream = new PortAudioSharp.Stream(inParams: param, outParams: null, sampleRate: Config.SampleRate,
                    framesPerBuffer: 0,
                    streamFlags: StreamFlags.ClipOff,
                    callback: callback,
                    userData: nint.Zero
                    );
                {
                    stream.Start();
                    do
                    {
                        Thread.Sleep(500);
                    } while (Global.MyState == 2);
                    stream.Stop();
                    Console.WriteLine("Recording Stopped.");
                    Settings.Default.Takes++;
                    Settings.Default.LastTakeDate = DateTime.Today;
                    Settings.Default.Save();
                    //Global.FilesInDirectory = new DirectoryInfo(Global.LocalRecordingsFolder).GetFiles()
                    //                                                  .OrderBy(f => f.LastWriteTime)
                    //                                                 .ToList();
                };
            }
        }
        // Convert WAV to MP3 using libmp3lame library
        public static void WavToMP3(string waveFileName, string mp3FileName, int bitRate = 192)
        {
            Console.WriteLine($"Converting {Global.wavPathAndName} to mp3 file");
            LoadLameDLL();
            Thread.Sleep(1000);
            using (var reader = new AudioFileReader(waveFileName))
            using (var writer = new LameMP3FileWriter(mp3FileName, reader.WaveFormat, bitRate))
                reader.CopyTo(writer);
            Console.WriteLine($"{Global.mp3PathAndName} was created.");
        }
        public static void LoadLameDLL()
        {
            LameDLL.LoadNativeDLL(Path.Combine(AppDomain.CurrentDomain.BaseDirectory));
        }


        /// <summary>
        /// Uploads a big file in chunk. The is very helpful for uploading large file in slow network condition
        /// and also enable capability to track upload progerss.
        /// </summary>
        /// <param name="client">The Dropbox client.</param>
        /// <param name="folder">The folder to upload the file.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <returns></returns>


        string GetOS()
        {
            string os;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                os = "Windows";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                os = "Linux";
            }
            else { os = "unknown"; }
            return os;
        }
        public int FindRemovableDrives(bool displayDetails)
        {
            //   DriveInfo[] allDrives = DriveInfo.GetDrives();
            Console.WriteLine("Inspect Removable Drives");
            var drives = DriveInfo.GetDrives()
             //   .Where(drive => drive.IsReady && drive.DriveType == DriveType.Removable);
              .Where(drive => drive.IsReady && ( drive.DriveType == DriveType.Removable || ( drive.DriveType == DriveType.Fixed & Global.OS != "Windows" )) );
            
            Console.WriteLine($"Number of Drives found: {drives.Count()}");
            if (drives.Count() > 0 && displayDetails == true)
            {
                foreach (DriveInfo d in drives)
                {
                    Console.WriteLine("Drive {0}", d.Name);
                    Console.WriteLine("  Drive type: {0}", d.DriveType);
                    if (d.IsReady == true)
                    {
                        Console.WriteLine("  Volume label: {0}", d.VolumeLabel);
                        Console.WriteLine("  File system: {0}", d.DriveFormat);
                        Console.WriteLine(
                            "  Available space to current user:{0, 15} bytes",
                            d.AvailableFreeSpace);

                        Console.WriteLine(
                            "  Total available space:          {0, 15} bytes",
                            d.TotalFreeSpace);

                        Console.WriteLine(
                            "  Total size of drive:            {0, 15} bytes ",
                            d.TotalSize);
                    }
                 Global.RemovableDrivePath = d.RootDirectory.ToString();
                 Console.WriteLine(Global.RemovableDrivePath);
                }

            }
            return drives.Count();
        }
        public int GetValidUserSelection(List<int> validOptions)
        {
            string input;
            int? validSelection = null;
            do
            {
                input = Console.ReadLine();
                //if (input.ToLower() == "exit") { return 0; }
                int.TryParse(input, out int userVal);
                validSelection = userVal;
            } while (!validOptions.Contains(validSelection ?? -1));
            return validSelection ?? -1;
        }

        public void LoadConfig()
        {
            Config.DbApiKey = Settings.Default.DbApiKey;
            Config.DbApiSecret = Settings.Default.DbApiSecret;
            Config.SSID = Settings.Default.SSID;
            Config.SSIDpw = Settings.Default.SSIDpw;
            Config.DbCode = Settings.Default.DbCode;
            Config.SelectedAudioDevice = Settings.Default.SelectedAudioDevice;
            Config.SampleRate = Settings.Default.SampleRate;
        }
        public void NormalizeTake()
         // from https://markheath.net/post/normalize-audio-naudio
        {
            Console.WriteLine("Normalizing");
            var inPath = Global.wavPathAndName;
            var outPath = $"{Global.wavPathAndName}_normalized.wav";
            float max = 0;

            UtilClass gc = new();
            while (!gc.IsFileReady(inPath))
            {
                Task.Delay(1000);
            }
            using (var reader = new AudioFileReader(inPath))
            {
                // find the max peak
                float[] buffer = new float[reader.WaveFormat.SampleRate];
                int read;
                do
                {
                    read = reader.Read(buffer, 0, buffer.Length);
                    for (int n = 0; n < read; n++)
                    {
                        var abs = Math.Abs(buffer[n]);
                        if (abs > max) max = abs;
                    }
                } while (read > 0);
                Console.WriteLine($"Max sample value: {max}");

                if (max == 0 || max > 1.0f)
                    throw new InvalidOperationException("File cannot be normalized");

                // rewind and amplify
                reader.Position = 0;
                reader.Volume = 1.0f / max;

                // write out to a new WAV file
              //  WaveFileWriter.CreateWaveFile16(outPath, reader);
                WaveFileWriter.CreateWaveFile16(outPath, reader);

            }
            File.Move($"{Global.wavPathAndName}_normalized.wav", Global.wavPathAndName, true);
        }

        //public async void GetDBCodeFromUSB()
        //{
        //    Console.WriteLine("Waiting For thumb drive");
        //    while (Global.RemovableDrivePath == null)
        //    {
        //        await Task.Delay(25);
        //    }
        //    var dbCodePath = Path.Combine(Global.RemovableDrivePath.ToString(), "DropBoxCode.txt");
        //    Console.WriteLine(dbCodePath);
        //    using (System.IO.StreamReader sr = new System.IO.StreamReader(dbCodePath))
        //    {
        //        string line;
        //        while ((line = sr.ReadLine()) != null)
        //        {
        //            string[] split = line.Split(',');
        //            foreach (string word in split)
        //            {
        //                Console.WriteLine(word);
        //            }
        //        }
        //    }
        //}
        //public bool DriveDetected
        //{
        //    get { return false; }
        //    set
        //    {
        //        //var drivesCount = FindRemovableDrives(false);
        //        var drives = DriveInfo.GetDrives()
        //        .Where(drive => drive.IsReady && drive.DriveType == DriveType.Removable);
        //        if (drives.Count() > Global.RemovableDriveCount)
        //        {
        //            GetDBCodeFromUSB();
        //        }
        //        Global.RemovableDriveCount = drives.Count();
        //    }
        //}

        public class DeviceItems()
        {
            void PanelLEDs()
            {
            }
            void PanelButtons()
            {

            }
            void MP3()
            {

            }
            void WAV()
            {

            }
            void Playlist()
            {

            }
            void ExternalDrive()
            {

            }
        }
    }
}

