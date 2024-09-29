using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Dropbox.Api;
using Microsoft.Extensions.Configuration;

namespace SmartRecorder
{
    public static class Global
    {
        public static int MyState { get; set; } = 0;
        public static string SessionName { get; set; } = DateTime.Today == null ? "UNKNOWN" : DateTime.Today.ToString("yyyy-MM-dd");
        public static string Home { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        public static string LocalRecordingsFolder { get; set; } = Path.Combine(Home, "SmartRecorder", "Recordings");
        public static string wavPathAndName { get; set; }
        public static string mp3PathAndName { get; set; }
        public static string OS { get; set; }
        public static bool NetworkStatus { get; set; }
        public static bool OAuthStatus { get; set; } = false;
        public static int recordButtonPinAddress { get; set; } = 1;
        public static int playButtonPinAddress { get; set; } = 2;
        public static int networkLEDPinAddress { get; set; } = 3;
        public static string? RemovableDrivePath { get; set; } 
        public static int RemovableDriveCount { get; set; } = 0;
        public static string DropBoxCodeDotTxtContains { get; set; }
        // public static int Takes { get; set; }
        // public static DateOnly LastTakeDate { get; set; }
        public static List<FileInfo> FilesInDirectory { get; set; } = new DirectoryInfo(LocalRecordingsFolder).GetFiles()
                                                                      .OrderBy(f => f.LastWriteTime)
                                                                     .ToList();
     //   public static DropboxClientConfig dbConfig { get; set; }
    }
    public class Config
    {
        //load from App.config
       public static string SSID { get; set; }
       public static string SSIDpw { get; set; }
       public static string DbCode { get; set; }
       public static string DbApiKey { get; set; } 
       public static string DbApiSecret { get; set; } 
       public static int SampleRate { get; set; }
       public static int SelectedAudioDevice { get; set; }
    }
}
