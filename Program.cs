
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using PortAudioSharp;
using SmartRecorder;
using static SmartRecorder.Configuration;
using System.Configuration;
using Microsoft.Extensions.Configuration;


namespace SmartRecorder
{
    internal class Program
    {

        static void Main(string[] args)
        {
            var deviceActions = new DeviceActions();
            deviceActions.SessionInit();
            int selection;
            do
            {
                selection = deviceActions.MainMenu();
            }
            while (selection != 0);
        }
    }
}
