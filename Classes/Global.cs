using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace SmartRecorder
{
    public static class Global
    {
        public static int SampleRate { get; set; } = 48000;
        public static int MyState { get; set; } = 0;
        public static int SelectedDevice { get; set; }
        public static string SessionName { get; set; } = DateTime.Today == null ? "UNKNOWN" : DateTime.Today.ToString("yyyy-MM-dd");
        public static string Home { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        public static string RecordingsFolder { get; set; } = Path.Combine(Home, "SmartRecorder", "Recordings");
        public static string wavPathAndName { get; set; }
        public static string OS { get; set; }
        public static bool NetworkStatus { get; set; }
        public static bool OAuthStatus { get; set; } = false;
        public static int recordButtonPinAddress { get; set; } = 1;
        public static int playButtonPinAddress { get; set; } = 2;
        public static int networkLEDPinAddress { get; set; } = 3;
        public static bool enumerateDevicesAtBoot { get; set; } = false;
        public static List<FileInfo> FilesInDirectory { get; set; } = new DirectoryInfo(RecordingsFolder).GetFiles()
                                                                      .OrderBy(f => f.LastWriteTime)
                                                                      .ToList();
    }
}
