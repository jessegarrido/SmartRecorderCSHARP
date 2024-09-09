using System.Runtime.InteropServices;
using System.Text;
using System.Net.NetworkInformation;
using PortAudioSharp;
using static SmartRecorder.Configuration;
using NAudio.Wave;
using NAudio.Lame;
using OauthPKCE;
using Microsoft.Extensions.Configuration;


namespace SmartRecorder
{

    public partial class DeviceActions
    {

        public DeviceActions()
        {
        }

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, int mode);

        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out int mode);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetStdHandle(int handle);

        const int STD_INPUT_HANDLE = -10;
        const int ENABLE_QUICK_EDIT_MODE = 0x40 | 0x80;

        public static void EnableQuickEditMode()
        {
            int mode;
            IntPtr handle = GetStdHandle(STD_INPUT_HANDLE);
            GetConsoleMode(handle, out mode);
            mode |= ENABLE_QUICK_EDIT_MODE;
            SetConsoleMode(handle, mode);
        }

        public void AudioDeviceInitAndEnumerate()
        {
            PortAudio.LoadNativeLibrary();
            PortAudio.Initialize();
            Console.WriteLine(PortAudio.VersionInfo.versionText);
            Console.WriteLine($"Number of devices: {PortAudio.DeviceCount}");
            if (Global.enumerateDevicesAtBoot == true)
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
            Console.WriteLine($"Use default device {deviceIndex} ({info.name})");
            Config.SelectedAudioDevice = deviceIndex;
            return deviceIndex;


        }

        public int AskToKeepOrEraseFiles()
        {
            if (Global.FilesInDirectory.Count() > 0)
            {
                Console.Write("Press 'record' to delete all saved recordings on SD & USB, and clear upload and play queues, or press 'play' to continue session ");
                //flash leds     
                int erased = GetValidUserSelection(new List<int> { 0, 1, 2 });
                if (erased == 1)
                {
                    Console.WriteLine($"Erase {Global.FilesInDirectory.Count()} files in Recordings Folder.");
                    DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(Global.wavPathAndName));

                    foreach (FileInfo file in di.GetFiles())
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
            var selection = GetValidUserSelection(new List<int> { 0, 1, 2, 3, 4 }); // 0=reboot,1=record,2=pla----y,3=skipforward,4skipback
            switch (selection)
            {
                case 1:
                    if (Global.MyState == 2)
                    {
                        Global.MyState = 1;
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
            Console.WriteLine("Welcome to PortaSmart");
            LoadConfig();
            Global.OS = GetOS();
            CreateNewLocalFileAndLocation();
            Global.NetworkStatus = CheckNetwork();


            Console.WriteLine($"Operating System: {Global.OS}");
            Console.WriteLine($"Session Name: {Global.SessionName}");
            Console.WriteLine($"User's Home Folder: {Global.Home}");
            //TODO setup GPIO
            //TODO mount usb drives
            //TODO blink the leds
            int erased = AskToKeepOrEraseFiles();
            CheckNetwork();
            //TODO fix network if absent / setup background check
            AudioDeviceInitAndEnumerate();
            ReturnAudioDevice(Config.SelectedAudioDevice);
            ThumbDrives();
            //CloudAuth();
            //DropBoxPlugin db = new();
            //db.AuthorizeDropBox();
            //ListCloudContents();


            Global.MyState = 1;
        }
        public bool CheckNetwork()
        {
            Global.NetworkStatus = NetworkInterface.GetIsNetworkAvailable();
            Console.WriteLine($"Network Connected Status: {Global.NetworkStatus}");
            //set network status LED
            return Global.NetworkStatus;
        }
        void EstablishNetwork()
        {

        }
        private static string GetDeviceName(nint name)
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

        void CreateNewLocalFileAndLocation()
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
                    Console.WriteLine($"The directory {path} was created successfully at {0}.", Directory.GetCreationTime(Global.LocalRecordingsFolder));
                }
            }
            int wavCount = Directory.GetFiles(Path.GetDirectoryName(newWavPath), "*", SearchOption.TopDirectoryOnly).Length;
            int mp3Count = Directory.GetFiles(Path.GetDirectoryName(newMp3Path), "*", SearchOption.TopDirectoryOnly).Length;
            int take = Math.Max(wavCount, mp3Count) + 1;
            Global.wavPathAndName = Path.Combine(newWavPath, $"{Global.SessionName}_take-{take}.wav");
            Global.mp3PathAndName = Path.Combine(newWavPath, "mp3", $"{Global.SessionName}_take-{take}.mp3");

            //  return wavPathAndName;
        }
        public void RecordAudio()
        {
            Global.MyState = 2; // TODO generic update curent state function

            StreamParameters param = SetAudioParameters();

            CreateNewLocalFileAndLocation(); // writes to Global

            DeviceInfo info = PortAudio.GetDeviceInfo(Config.SelectedAudioDevice);
            Console.WriteLine();
            Console.WriteLine($"Use default device {Config.SelectedAudioDevice} ({info.name})");
            int numChannels = param.channelCount;

            //FileStream f = new FileStream(wavPathAndName, FileMode.Create);
            using (Float32WavWriter wr = new Float32WavWriter(Global.wavPathAndName, Config.SampleRate, numChannels))
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
                    wr.WriteSamples(samples);
                    return StreamCallbackResult.Continue;
                };

                Console.WriteLine(param);
                Console.WriteLine(Config.SampleRate);
                Console.WriteLine("Now Recording");

                Global.MyState = 2;
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
                        Thread.Sleep(200);
                    } while (Global.MyState == 2);
                    stream.Stop();
                    Console.WriteLine("Recording Stopped.");
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
        async void ListCloudContents()
        {

        }

        /// <summary>
        /// Uploads a big file in chunk. The is very helpful for uploading large file in slow network condition
        /// and also enable capability to track upload progerss.
        /// </summary>
        /// <param name="client">The Dropbox client.</param>
        /// <param name="folder">The folder to upload the file.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <returns></returns>

        void SetStartPlayback()
        {
        }
        void StopPlayback()
        {

        }
        void SkipPlaybackForward()
        {

        }
        void SkipPlaybackBack()
        {

        }
        void UpdateLEDs()
        {

        }
        void CloudSelect()
        {

        }
        void CloudAuth()
        {

        }


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
        void ThumbDrives()
        {
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            if (allDrives.Count() > 0 && Global.enumerateDevicesAtBoot == true)
            {
                foreach (DriveInfo d in allDrives)
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
                }
            }
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
    }

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

