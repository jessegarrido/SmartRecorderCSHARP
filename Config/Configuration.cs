using PortAudioSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartRecorder;
using System.Configuration;
using System.Runtime.InteropServices;

namespace SmartRecorder
{
    public static class Configuration
    {
        //static string MY_CLIENT_ID = "k8qle0tzgfj8p8i";
        //static string RedirectUri = $"https://www.dropbox.com/oauth2/authorize?client_id={MY_CLIENT_ID}&token_access_type=offline&response_type=code";
        //String.Concat($"https://www.dropbox.com/oauth2/authorize?,client_id={MY_CLIENT_ID}&redirect_uri={MYREDIRECT_URI}&response_type=code");
    
        public static StreamParameters SetAudioParameters()
        {
            StreamParameters param = new StreamParameters();
            DeviceInfo info = PortAudio.GetDeviceInfo(Config.SelectedAudioDevice);
            param.device = Config.SelectedAudioDevice;
            param.channelCount = 2;
            param.sampleFormat = SampleFormat.Float32;
            //param.suggestedLatency = info.defaultLowInputLatency;
            param.suggestedLatency = .9;
            param.hostApiSpecificStreamInfo = IntPtr.Zero;
            return param;
        }
    }
}
