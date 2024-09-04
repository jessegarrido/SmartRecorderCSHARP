using PortAudioSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartRecorder;
using System.Configuration;

namespace SmartRecorder
{
    public static class Configuration
    {
        public static StreamParameters SetAudioParameters()
        {
            StreamParameters param = new StreamParameters();
            DeviceInfo info = PortAudio.GetDeviceInfo(Global.SelectedDevice);
            param.device = Global.SelectedDevice;
            param.channelCount = 2;
            param.sampleFormat = SampleFormat.Float32;
            //param.suggestedLatency = info.defaultLowInputLatency;
            param.suggestedLatency = .9;
            param.hostApiSpecificStreamInfo = IntPtr.Zero;
            return param;
        }
    }
}
