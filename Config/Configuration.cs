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
