
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using PortAudioSharp;
using SmartRecorder;
using static SmartRecorder.Configuration;
using System.Configuration;


namespace SmartRecorder
{
    internal class Program
    {

        static void Main(string[] args)
        {
            var deviceActions = new DeviceActions();
            deviceActions.InitSession();
            int selection;
            do
            {
                selection = deviceActions.MainMenu();
            }
            while (selection != 0);
        }
    }
}
