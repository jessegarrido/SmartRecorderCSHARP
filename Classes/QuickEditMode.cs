﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SmartRecorder
{
    public static class SetQuickEditConsoleMode
    {

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
    }

}