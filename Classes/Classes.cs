using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.NetworkInformation;
using PortAudioSharp;
using System.IO;
using static SmartRecorder.Program;
using System.Runtime.CompilerServices;
using static System.Net.Mime.MediaTypeNames;
using static SmartRecorder.Configuration;
using Dropbox.Api;
using System.Configuration;

namespace SmartRecorder
{

    public partial class DeviceActions
    {

        public DeviceActions()
        {
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
            deviceIndex = configSelectedIndex == null ? PortAudio.DefaultInputDevice : Global.SelectedDevice;

            if (deviceIndex == PortAudio.NoDevice)
            {
                Console.WriteLine("No default input device found");
                Environment.Exit(1);
            }
            DeviceInfo info = PortAudio.GetDeviceInfo(deviceIndex);
            Console.WriteLine();
            Console.WriteLine($"Use default device {deviceIndex} ({info.name})");
            Global.SelectedDevice = deviceIndex;
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
        public void InitSession()
        {
            Global.OS = GetOS();
            Global.wavPathAndName = CreateNewFileAndLocation();
            Global.NetworkStatus = CheckNetwork();

            Console.WriteLine("Launching Pi Smart Recorder");
            Console.WriteLine(Global.OS);
            Console.WriteLine(Global.SessionName);
            Console.WriteLine(Global.Home);
            //TODO setup GPIO
            //TODO mount usb drives
            //TODO blink the leds
            int erased = AskToKeepOrEraseFiles();
            CheckNetwork();
            //TODO fix network if absent / setup background check
            AudioDeviceInitAndEnumerate();
            ReturnAudioDevice(Global.SelectedDevice);
            ThumbDrives();
            //CloudAuth();

            ListCloudContents();


            Global.MyState = 1;
        }
        public bool CheckNetwork()
        {
            Global.NetworkStatus = NetworkInterface.GetIsNetworkAvailable();
            Console.WriteLine($"Network Status: {Global.NetworkStatus}");
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

        string CreateNewFileAndLocation()
        {
            string newPath = Path.Combine(Global.RecordingsFolder, Global.SessionName);
            if (Directory.Exists(newPath))
            {
                Console.WriteLine("Path exists");
            }
            else
            {
                DirectoryInfo di = Directory.CreateDirectory(newPath);
                Console.WriteLine("The directory was created successfully at {0}.", Directory.GetCreationTime(Global.RecordingsFolder));
            }
            int take = Directory.GetFiles(Path.GetDirectoryName(newPath), "*", SearchOption.AllDirectories).Length + 1;
            string wavPathAndName = Path.Combine(newPath, $"{Global.SessionName}_take-{take}.wav");
            Console.WriteLine(wavPathAndName);
            Global.wavPathAndName = wavPathAndName;
            return wavPathAndName;
        }
        public void RecordAudio()
        {
            Global.MyState = 2; // TODO generic update curent state function

            StreamParameters param = SetAudioParameters();

            CreateNewFileAndLocation(); // writes to Global

            DeviceInfo info = PortAudio.GetDeviceInfo(Global.SelectedDevice);
            Console.WriteLine();
            Console.WriteLine($"Use default device {Global.SelectedDevice} ({info.name})");
            int numChannels = param.channelCount;

            //FileStream f = new FileStream(wavPathAndName, FileMode.Create);
            Float32WavWriter wr = new Float32WavWriter(Global.wavPathAndName, Global.SampleRate, numChannels);
            //MemoryStream ms = new MemoryStream();

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

            PortAudioSharp.Stream stream = new PortAudioSharp.Stream(inParams: param, outParams: null, sampleRate: Global.SampleRate,
                framesPerBuffer: 0,
                streamFlags: StreamFlags.ClipOff,
                callback: callback,
                userData: nint.Zero
                );

            Console.WriteLine(param);
            Console.WriteLine(Global.SampleRate);
            Console.WriteLine("Now Recording");

            // int? stoprecording = null;
            Global.MyState = 2;
            stream.Start();
            do
            {
                Thread.Sleep(200);
                // stoprecording = GetValidatedSelection(new List<int> { 0,1 });
            } while (Global.MyState == 2);
            stream.Stop();
            Console.Write("Recording Stopped.");
            // Thread.Sleep(200);
        }
        void StopRecording()
        {

        }
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
            static async Task Run()
            {

            }
        }
        void ListCloudContents()
        {
            async Task ListRootFolder(DropboxClient dbx)
            {
                var list = await dbx.Files.ListFolderAsync(string.Empty);

                // show folders then files
                foreach (var item in list.Entries.Where(i => i.IsFolder))
                {
                    Console.WriteLine("D  {0}/", item.Name);
                }

                foreach (var item in list.Entries.Where(i => i.IsFile))
                {
                    Console.WriteLine("F{0,8} {1}", item.AsFile.Size, item.Name);
                }
            }
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