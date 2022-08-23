using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace FriesBSCameraPlugin
{
    static class AudioCapture
    {
        [DllImport("AudioCapture.dll")]
        public static extern int Init([MarshalAs(UnmanagedType.BStr)] string device);

        [DllImport("AudioCapture.dll")]
        public static extern int GetNumChannels();

        [DllImport("AudioCapture.dll")]
        public static extern int Shutdown();

        [DllImport("AudioCapture.dll")]
        public static extern bool HasError();

        [DllImport("AudioCapture.dll")]
        public static extern int ReadData([Out] byte[] data, int size);

        [DllImport("AudioCapture.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.BStr)]
        public static extern string GetDeviceName(int index);

        [DllImport("AudioCapture.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.BStr)]
        public static extern string GetSelectedDeviceName();

        [DllImport("AudioCapture.dll")]
        public static extern int GetNumDevices();


    }
}
