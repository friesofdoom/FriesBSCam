using System;
//using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
/*
            
•waveInGetNumDevs(): Determines the number of audio drivers available for input.
•waveInOpen(): Creates an instance of the specified audio device for input.
•waveInPrepareHeader(): Prepares a WAVEHDR and data block for input.
•waveInUnprepareHeader(): Releases a previously prepared WAVEHDR and data block.
•waveInClose(): Closes the specified instance of the audio device.
•waveInReset(): Stops recording and empties the queue.
•waveInStart(): Starts recording to the queued buffer.
•waveInStop(): Stops recording.
•waveInAddBuffer(): Adds a prepared buffer to the record queue.
•waveInGetDevCaps(): Gets the capabilities of the specified device
•WAVEINCAPS: Describes the capabilities of an input device.

*/
namespace MyAudioRecorder_PlayerwMixer
{
    class clsWinMMBase
    {
        #region "Dll Imports"

        //[DllImport("Kernel32")]
        //public extern static Boolean CloseHandle(IntPtr handle);

        [DllImport("Kernel32")]
        public static extern uint GetLastError();

        [DllImport("Kernel32")]
        public extern static int CloseHandle(IntPtr handle);

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern uint waveOutGetNumDevs();

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint waveOutGetDevCaps(IntPtr hwo, ref WAVEOUTCAPS pwoc, uint cbwoc);

        //[DllImport("winmm.dll", SetLastError = true)]
        //public static extern uint waveInGetNumDevs();
        [DllImport("winmm.dll")]
        public static extern Int32 mciSendString(string command, StringBuilder buffer, int bufferSize, IntPtr hwndCallback);


        [DllImport("winmm.dll")]
        public static extern Int32 mciSendString(string command, String buffer, int bufferSize, int hwndCallback);
        /* fix me later
                [DllImport("winmm.dll", EntryPoint = "mciSendCommandW", ExactSpelling = true)]
                public public static extern int mciSendCmdOpen(IntPtr device, int msg, int flags, ref MCI_OPEN_PARAMS parms);
       
                [DllImport("winmm.dll", EntryPoint = "mciSendCommandW", ExactSpelling = true)]
                public public static extern int mciSendCmdPlay(IntPtr device, int msg, int flags, ref MCI_PLAY_PARAMS parms);
                */
        //fix me later


        [DllImport("winmm.dll")]
        public static extern uint waveOutSetPlaybackRate(IntPtr hwo, uint pdwRate); 

        [DllImport("winmm.dll")]
        public static extern uint waveOutGetPlaybackRate(IntPtr hwo, ref uint pdwRate);

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint mciSendCommand(int deviceId, int command, int flags, ref MCI_OPEN_PARMS param);

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint mciSendCommand(IntPtr deviceId, int command, int flags, ref MCI_RECORD_PARMS param);

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint mciSendCommand(int deviceId, int command, int flags, ref MCI_RECORD_PARMS param);

        [DllImport("winmm.dll")]
        public static extern uint waveOutGetPosition(IntPtr hWaveOut, ref MmTime lpInfo, uint uSize);

        [DllImport("winmm.dll")]
        public static extern uint waveOutPause(IntPtr hwo);

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint waveOutOpen(ref IntPtr hWaveOut, uint uDeviceID, ref WAVEFORMATEX lpFormat, IntPtr dwCallback, uint dwInstance, uint dwFlags);

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint waveOutOpen(ref IntPtr hWaveOut, IntPtr uDeviceID, ref WAVEFORMATEX lpFormat, IntPtr dwCallback, IntPtr dwInstance, uint dwFlags);

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint waveOutWrite(IntPtr hwo, ref WAVEHDR pwh, uint cbwh);
        //public static extern int waveOutWrite(IntPtr hWaveOut, ref WaveHdr lpWaveOutHdr, int uSize);

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint waveOutReset(IntPtr hwo);

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint waveOutRestart(IntPtr hwo);

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
        //public static extern uint waveOutPrepareHeader(IntPtr hWaveOut, IntPtr pwh, int uSize);
        public static extern uint waveOutPrepareHeader(IntPtr hWaveOut, ref WAVEHDR lpWaveOutHdr, uint uSize);

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint waveOutUnprepareHeader(IntPtr hwo, ref WAVEHDR pwh, uint cbwh);

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint waveOutGetDevCaps(uint hwo, ref WAVEOUTCAPS pwoc, uint cbwoc);

        [DllImport("winmm.dll")]
        public static extern uint waveInGetDevCaps(IntPtr deviceId, out WAVEINCAPS caps, int capsSize);

        [DllImport("winmm.dll", ExactSpelling = true)]
        public static extern uint waveInGetNumDevs();

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern uint waveInReset(IntPtr hwi);

        [DllImport("winmm.dll")]
        public static extern uint waveInOpen(ref IntPtr hWaveIn, uint deviceId, ref WAVEFORMATEX wfx, IntPtr dwCallBack, uint dwInstance, uint dwFlags);

        [DllImport("winmm.dll", EntryPoint = "waveOutClose", SetLastError = true)]
        public static extern uint waveOutClose(IntPtr hwo);

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern uint waveInStart(IntPtr hwi);

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern uint waveInStop(IntPtr hwi);

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern uint waveInUnprepareHeader(IntPtr hwi, ref WAVEHDR pwh, uint cbwh);

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern uint waveInPrepareHeader(IntPtr hwi, ref WAVEHDR pwh, uint cbwh);

        [DllImport("winmm.dll", EntryPoint = "waveInAddBuffer", SetLastError = true)]
        public static extern uint waveInAddBuffer(IntPtr hwi, ref WAVEHDR pwh, uint cbwh);

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern uint waveInClose(IntPtr hwi);

        [DllImport("winmm.dll")]
        public static extern bool mciGetErrorString(uint fdwError, StringBuilder lpszErrorText, uint cchErrorText);
        #endregion

        #region "Structures and Enums"

        //struct NextMove
        //{
        //    public byte WhichBuffer;
        //    public int WhichItem;
        //    public bool Done;
        //}

        public struct MinMax
        {
            public short Min, Max;
            public override string ToString()
            {
                return ("Min " + Min.ToString () + " : Max " + Max.ToString ());
            }
        }

        [Flags]
        public enum WaveOutOpenFlags : uint
        {
            /* flags for dwFlags parameter in waveOutOpen() and waveInOpen() */
            WAVE_FORMAT_QUERY = 0x0001,
            WAVE_ALLOWSYNC = 0x0002,
            WAVE_MAPPED = 0x0004,
            WAVE_FORMAT_DIRECT = 0x0008,
            CALLBACK_WINDOW = 0x000100000,    /* dwCallback is a HWND */
            CALLBACK_TASK = 0x000200000,   /* dwCallback is a HTASK */
            CALLBACK_FUNCTION = 0x00030000//0     /* dwCallback is a FARPROC */
        };

        [Flags]
        public enum WaveInOpenFlags : uint
        {
            CALLBACK_NULL = 0,
            CALLBACK_FUNCTION = 0x30000,
            CALLBACK_EVENT = 0x50000,
            CALLBACK_WINDOW = 0x10000,
            CALLBACK_THREAD = 0x20000,
            WAVE_FORMAT_QUERY = 1,
            WAVE_MAPPED = 4,
            WAVE_FORMAT_DIRECT = 8
        }

        [Flags]
        public enum WaveHdrFlags : uint
        {
            WHDR_DONE = 1,
            WHDR_PREPARED = 2,
            WHDR_BEGINLOOP = 4,
            WHDR_ENDLOOP = 8,
            WHDR_INQUEUE = 16
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WAVEHDR
        {
            public IntPtr lpData; // pointer to locked data buffer
            public uint dwBufferLength; // length of data buffer
            public uint dwBytesRecorded; // used for input only
            public IntPtr dwUser; // for client's use
            public WaveHdrFlags dwFlags; // assorted flags (see defines)
            public uint dwLoops; // loop control counter
            public IntPtr lpNext; // PWaveHdr, reserved for driver
            public IntPtr reserved; // reserved for driver
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WAVEFORMATEX
        {
            public ushort wFormatTag;
            public ushort nChannels;
            public uint nSamplesPerSec;
            public uint nAvgBytesPerSec;
            public ushort nBlockAlign;
            public ushort wBitsPerSample;
            public ushort cbSize;
        }

        public struct MCI_RECORD_PARMS
        {
            public uint dwCallback;
            public uint dwFrom;
            public uint dwTo;
        }

        public struct MCI_OPEN_PARMS
        {
            public int dwCallback;
            public int wDeviceID;
            public string lpstrDeviceType;
            public string lpstrElementName;
            public string lpstrAlias;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WAVEOUTCAPS
        {
            public ushort wMid;
            public ushort wPid;
            public uint vDriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 164)]
            public string szPname;
            public uint dwFormats;
            public ushort wChannels;
            public ushort wReserved1;
            public uint dwSupport;
        }

        public struct WAVEINCAPS
        {
            public short ManufacturerId, ProductId;
            public uint DriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string Name;
            public uint Formats;
            public short Channels;
            ushort Reserved;
            public Guid ManufacturerGuid, ProductGuid, NameGuid;
        }

        public struct MCI_STATUS_PARMS
        {
            public int dwCallback;
            public int dwReturn;
            public int dwItem;
            public short dwTrack;
        }

        public struct MCI_INFO_PARMS
        {
            public int dwCallback;
            public string lpstrReturn;
            public int dwRetSize;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct MmTime
        {
            [FieldOffset(0)] public UInt32 wType;
            [FieldOffset(4)] public UInt32 ms;
            [FieldOffset(4)] public UInt32 sample;
            [FieldOffset(4)] public UInt32 cb;
            [FieldOffset(4)] public UInt32 ticks;
            [FieldOffset(4)] public Byte smpteHour;
            [FieldOffset(5)] public Byte smpteMin;
            [FieldOffset(6)] public Byte smpteSec;
            [FieldOffset(7)] public Byte smpteFrame;
            [FieldOffset(8)] public Byte smpteFps;
            [FieldOffset(9)] public Byte smpteDummy;
            [FieldOffset(10)] public Byte smptePad0;
            [FieldOffset(11)] public Byte smptePad1;
            [FieldOffset(4)] public UInt32 midiSongPtrPos;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct MMTIME
        {
            public int wType;
            public int u;
            public int x;
        }
        #endregion

        #region "Constants"

        public const int MAX_ITEM_COUNT = 100;

        public const int MCI_OPEN = 0x803;
        public const int MCI_OPEN_ALIAS = 0x400;
        public const int MCI_OPEN_ELEMENT = 0x200;
        public const int MCI_OPEN_ELEMENT_ID = 0x800;
        public const int MCI_OPEN_SHAREABLE = 0x100;
        public const int MCI_OPEN_TYPE = 0x2000;
        public const int MCI_OPEN_TYPE_ID = 0x1000;


        public const int MCI_RECORD = 0x80F;
        public const int MCI_NOTIFY = 1;
        public const int MCI_WAIT = 2;
        public const int MCI_FROM = 4;
        public const int MCI_TO = 8;

        public const int MMSYSERR_NOERROR = 0; // no error

        public const int MM_WOM_OPEN = 0x3BB;
        public const int MM_WOM_CLOSE = 0x3BC;
        public const int MM_WOM_DONE = 0x3BD;

        public const int MM_WIM_OPEN = 0x3BE;
        public const int MM_WIM_CLOSE = 0x3BF;
        public const int MM_WIM_DATA = 0x3C0;
        public const int MM_WIM_DONE = 0x3bd;

        public const int CALLBACK_FUNCTION = 0x00030000;    // dwCallback is a FARPROC 

        public const int TIME_MS = 0x0001;  // time in milliseconds 
        public const int TIME_SAMPLES = 0x0002;  // number of wave samples 
        public const int TIME_BYTES = 0x0004;  // current byte offset 

        public const int WAVE_INVALIDFORMAT = 0;
        public const int WAVE_FORMAT_1M08 = 1;
        public const int WAVE_FORMAT_1S08 = 2;
        public const int WAVE_FORMAT_1M16 = 4;
        public const int WAVE_FORMAT_1S16 = 8;
        public const int WAVE_FORMAT_2M08 = 16;
        public const int WAVE_FORMAT_2S08 = 32;
        public const int WAVE_FORMAT_2M16 = 64;
        public const int WAVE_FORMAT_2S16 = 128;
        public const int WAVE_FORMAT_4M08 = 256;
        public const int WAVE_FORMAT_4S08 = 512;
        public const int WAVE_FORMAT_4M16 = 1024;
        public const int WAVE_FORMAT_4S16 = 2048;
        public const int WAVE_FORMAT_PCM = 1;
        #endregion
    }
}
