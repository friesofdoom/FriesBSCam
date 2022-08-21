using System;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Collections .Generic ;

namespace MyAudioRecorder_PlayerwMixer
{
    class clsRecord:clsWinMMBase 
    {
        public struct LevelEventArgs
        {
            public short leftlevel, rightlevel;
            public MinMax leftminmax, rightminmax;
            public byte numberofchannels;
            public override string ToString()
            {
                return ( "left " + leftlevel .ToString () + " right " + rightlevel );
            }
        }
        private struct RecordStoppingStruct
        {
            public int NumberofStoppedBuffers;
            public bool Stopping;
            public bool Stopped;
        }
        byte[] RIFF =new byte[]{0x52, 0x49, 0x46, 0x46};
        byte[] WAVE = new byte[] { 0x57, 0x41, 0x56, 0x45 };
        byte[] FMT = { 0x66, 0x6d, 0x74, 0x20 };
        byte[] DATA ={ 0x64, 0x61, 0x74, 0x61 };

        private System.Timers.Timer timer1;
        private  RecordStoppingStruct stopstruct = new RecordStoppingStruct();
        private const int NUMBER_OF_HEADERS = 4;
        private  WAVEFORMATEX wavFmt;
        private  bool recording = false;
        private  object lockobject=new object();
        IntPtr hWaveIn;
        WAVEHDR[] header;
        private byte[] HeaderData;
        private GCHandle HeaderDataHandle;
        uint size;
        double sampletime = .25;
        bool paused = false;
        StringBuilder errmsg = new StringBuilder(128);
        private List<byte[]> TheList = new List<byte[]>();
        private List<MinMax> LeftMinMax = new List<MinMax>();
        private List<MinMax> RightMinMax = new List<MinMax>();

#region Delegates and events
        public delegate void WaveDelegate(IntPtr hdrvr, int uMsg, int dwUser, ref WAVEHDR wavhdr, int dwParam2);
        private WaveDelegate BufferInProc;

        public delegate void ShowLevelsEventHandler(object sender, LevelEventArgs e,bool paused);// we need an event to inform the UI of the levels for VU and Plotting
        public event ShowLevelsEventHandler LevelsEventHandler;

        public delegate void RecordingStoppedEventHandler(object sender);// we need this to inform the UI that we are truely done recording
        public event RecordingStoppedEventHandler RecordingStoppedEvent;
#endregion 

        public clsRecord(WAVEFORMATEX TheFormat)
        {
            wavFmt = TheFormat;
            timer1 = new System.Timers.Timer();
            timer1.Interval = (int)(sampletime * 1000);
            timer1.Elapsed += timer1_tick;
            timer1.Enabled = true;
            stopstruct.Stopped = false;
            stopstruct.Stopping = false;
            stopstruct.NumberofStoppedBuffers = 0;
        }

         ~clsRecord()
        {
            timer1.Enabled = false;
            timer1.Elapsed -= timer1_tick;
            timer1 = null;
            BufferInProc = null;
        }

        /// <summary>
        /// pause the recording
        /// </summary>
        public bool Paused
        {
            get
            {
                return paused;
            }
            set
            {
                paused = value;
                Debug.Print(paused.ToString());
            }
        }

        /// <summary>
        /// Our WaveDelegate function
        /// </summary>
        /// <param name="hdrvr"></param>
        /// <param name="uMsg"></param>
        /// <param name="dwUser"></param>
        /// <param name="waveheader">the place where the recorded data is stored</param>
        /// <param name="dwParam2"></param>
        private void HandleWaveIn(IntPtr hdrvr, int uMsg, int dwUser, ref WAVEHDR waveheader, int dwParam2)
        {
            uint rv1;

            lock (lockobject)// critical section
            {
                if (uMsg == MM_WIM_DATA )//&& recording)
                {
                    try
                    {
                        uint i = (uint)waveheader.dwUser.ToInt32();// for debug purposes only
                        Debug.Print("User "+i.ToString());// try to not do this because it takes a lot of time

                        byte[] _imageTemp = new byte[waveheader.dwBytesRecorded];// create an array that is big enough to hold the data
                        Marshal.Copy(waveheader.lpData, _imageTemp, 0, (int)waveheader.dwBytesRecorded);// copy that data
                        
                        /*if (!paused)// if we are not paused
                            bw.Write(_imageTemp);//write the data to a file*/
                        StoreSamples(_imageTemp);// find the min and max for this sample so we can do VU and Plotting (from a timer function ... not here)


                        if (!stopstruct.Stopping)// not stopping so add the header back to the queue
                        {
                            rv1=waveInAddBuffer(hWaveIn, ref waveheader, size);
                            if (rv1 != 0)// if not success then get the associated error message
                            {
                                mciGetErrorString(rv1, errmsg, (uint)errmsg.Capacity);
                            }
                        }
                        else// stopping
                        {
                            stopstruct.NumberofStoppedBuffers++;// keep track of the buffers that are finished
                            Debug.Print("Stopping " + stopstruct.NumberofStoppedBuffers.ToString());
                            rv1 =   waveInUnprepareHeader(hWaveIn, ref waveheader, size);// un-prepare the headers as they come back
                            if (rv1 != 0)// if not success then get the associated error message
                            {
                                mciGetErrorString(rv1, errmsg, (uint)errmsg.Capacity);
                            }

                            if (stopstruct.NumberofStoppedBuffers == NUMBER_OF_HEADERS)// when they are all done set a flag that we are done
                            {
                                stopstruct.Stopped = true;
                            }
                        }

                    }
                    catch
                    {
                    }
                }
            }
        }

        private  void VU(byte[] Temp)
        {
            short[] LeftShortArray, RightShortArray;
            MinMax lmm, rmm;
            int shortstart, shortend, bytestart, byteend;
            if (recording)
            {
                // find the min and max for this sample so we can do VU and Plotting (from a timer function ... not here)
                LeftShortArray = null;
                RightShortArray = null;
                shortstart = 0;
                shortend = 0;
                bytestart = 0;
                byteend = Temp.Length;
                //convert the byte[] to short[]
                ConvertByteArraytoInt16Array(ref LeftShortArray, ref RightShortArray, ref shortstart, ref shortend, Temp, bytestart, byteend);
                

                
                lmm.Min = short.MaxValue;
                lmm.Max = short.MinValue;
                lmm = FindMinMax(LeftShortArray, shortstart, shortend); //find the min and max for this sample left channel
                LeftMinMax.Add(lmm);
                if (wavFmt.nChannels > 1)
                {
                    rmm.Min = short.MaxValue;
                    rmm.Max = short.MinValue;

                    rmm = FindMinMax(RightShortArray, shortstart, shortend); //find the min and max for this sample right channel
                    RightMinMax.Add(rmm);
                }
            }

        }

        public float[] LeftSamples;
        public float[] RightSamples;

        private  void StoreSamples(byte[] Temp)
        {
            int shortstart, shortend, bytestart, byteend;
            if (recording)
            {
                // find the min and max for this sample so we can do VU and Plotting (from a timer function ... not here)
                LeftSamples = null;
                RightSamples = null;
                shortstart = 0;
                shortend = 0;
                bytestart = 0;
                byteend = Temp.Length;
                ConvertByteArraytoFloatArray(ref LeftSamples, ref RightSamples, ref shortstart, ref shortend, Temp, bytestart, byteend);
            }
        }

        public double SampleTime
        {
            get
            {
                return sampletime;
            }
        }
        private void RaiseLevelEvent(LevelEventArgs e)
        {
            if (LevelsEventHandler == null)
                return;
            try
            {
                LevelsEventHandler(this, e,paused);
            }
            catch
            {
            }

        }
           
        private void RaiseRecordingStoppedEvent()
        {
            if (RecordingStoppedEvent == null)
                return;
            RecordingStoppedEvent(this);

        }

        public bool StartRecording( uint InputDeviceIndex)
        {
            bool rv;
            uint rv0, rv1;
            StringBuilder errmsg = new StringBuilder(128);

            stopstruct.Stopped = false;
            stopstruct.Stopping = false;
            stopstruct.NumberofStoppedBuffers = 0;
            //hWaveIn = IntPtr.Zero;


            IntPtr dwCallback = IntPtr.Zero;// a pointer that will eventually point to our WaveDelegate callback routine(HandleWaveIn)
            BufferInProc = new WaveDelegate(HandleWaveIn);//the callback function must be cast as a WaveDelegate
            dwCallback = Marshal.GetFunctionPointerForDelegate(BufferInProc);// point our callback pointer to our WaveDelegte function

            //open the recording device ...

            //hWaveIn will be the handle to the device for all future calls
            //InputDeviceIndex is the index of the device as returned to us from a call to waveInGetDevCaps
            //wavfmt is the format the DLL will be using to record the audio... it was set in clsPlayer() to be the same standard as used in CD quality audio
            //dwCallback is the pointer to our WaveDelegate function (where the recorded data is returned to us)
            // 0 dwCallbackInstance ...  User - instance data passed to the callback mechanism. This parameter is not used with the window callback mechanism.
            // a flag to let the DLL know that we want to use a callback function
            rv0 = waveInOpen(ref hWaveIn, InputDeviceIndex, ref wavFmt, dwCallback, 0, (uint)WaveInOpenFlags.CALLBACK_FUNCTION);
            if (0 != rv0)
            {
                rv = mciGetErrorString(rv0, errmsg, (uint)errmsg.Capacity);
                return false;
            }

            size = (uint)(.1 * wavFmt.nAvgBytesPerSec);//.1 second ... the larger the number the more slugish the UI seems ... the smaller the number the less time we have to process the recorded data
            header = new WAVEHDR[NUMBER_OF_HEADERS];// create an array of WAVEHDR structures to hold the recorded data

            for (int i = 0; i < NUMBER_OF_HEADERS; i++)// preset all the headers 
            {
                HeaderData = new byte[size];// an array big enough to hold .1 seconds worth of recorded data
                HeaderDataHandle = GCHandle.Alloc(header, GCHandleType.Pinned);
                HeaderDataHandle = GCHandle.Alloc(HeaderData, GCHandleType.Pinned);// a pointer to that array

                header[i].lpData = HeaderDataHandle.AddrOfPinnedObject();// point the WAVEHDR data pointer to that data array
                header[i].dwBufferLength = size;// tell the structure how big the array is
                header[i].dwUser = new IntPtr(i);// used only for my debug ... to make sure no structures were dropped while recording

                rv1 = waveInPrepareHeader(hWaveIn, ref header[i], (uint)Marshal.SizeOf(header[i]));// tell the DLL to prepare the header
                if (0 != rv1)
                {
                    rv = mciGetErrorString(rv1, errmsg, (uint)errmsg.Capacity);
                    return false;
                }
                rv1 = waveInAddBuffer(hWaveIn, ref header[i], size);// Tell the DLL that the header is available
                if (0 != rv1)
                {
                    rv = mciGetErrorString(rv1, errmsg, (uint)errmsg.Capacity);
                    return false;
                }
            }
            rv1 = waveInStart(hWaveIn);// start recording
            if (0 != rv1)
            {
                rv = mciGetErrorString(rv1, errmsg, (uint)errmsg.Capacity);
                return false;
            }

            recording = true;
            return true;

        }

        public bool StopRecording()
        {
            bool rv = true;
            stopstruct.NumberofStoppedBuffers = 0;
            stopstruct.Stopping = true;// let the waveinhandler know to stop adding buffers back to the queque
            while (!stopstruct.Stopped)
            {
            }
            return rv;
        }

        // when the waveinhandler has no more headers to stop adding, the timer will call this function to close out the device
        private   void Stop()
        {
            uint rv;
            bool rv1;
            if (recording)
            {
                rv = waveInStop(hWaveIn);// Infor the DLL that we are not recording anymore
                if (0 != rv)
                {
                    rv1 = mciGetErrorString(rv, errmsg, (uint)errmsg.Capacity);
                    Debug.Print("waveInStop Err " + errmsg);
                }
                else
                {
                    rv = waveInClose(hWaveIn);// close the recording device
                    if (0 != rv)
                    {
                        rv1 = mciGetErrorString(rv, errmsg, (uint)errmsg.Capacity);
                        Debug.Print("waveInClose Err " + errmsg);
                    }
                }
            }
        }

        /// <summary>
        /// Simply convert the byte arrays to short arrays
        /// </summary>
        /// <param name="LeftShortArray"></param>
        /// <param name="RightShortArray"></param>
        /// <param name="shortstart"></param>
        /// <param name="shortend"></param>
        /// <param name="ByteArray"></param>
        /// <param name="bytestart"></param>
        /// <param name="byteend"></param>
        /// <returns></returns>
        private  bool ConvertByteArraytoInt16Array(ref short[] LeftShortArray, ref short[] RightShortArray, ref int shortstart, ref int shortend, byte[] ByteArray, int bytestart, int byteend)
        {
            bool rv = false;
            int bl, index = 0;
            short s;
            int step = 2 * wavFmt.nChannels;
            byte[] b = new byte[2];
            bl = byteend - bytestart;
            //sl = send - sstart;
            if (LeftShortArray == null)
                shortstart = 0;
            else
                shortstart = LeftShortArray.Length;
            shortend = (int)(shortstart + bl / step);
            LeftShortArray = new short[shortend];
            if (wavFmt.nChannels == 2)
                RightShortArray = new short[shortend];
            for (int i = 0; i < bl - 1; i += step)
            {
                b[0] = ByteArray[bytestart + i];
                b[1] = ByteArray[bytestart + i + 1];
                s = BitConverter.ToInt16(b, 0);
                LeftShortArray[index + shortstart] = s;
                if (wavFmt.nChannels == 2)
                {
                    b[0] = ByteArray[bytestart + i + 2];
                    b[1] = ByteArray[bytestart + i + 3];
                    s = BitConverter.ToInt16(b, 0);
                    RightShortArray[index + shortstart] = s;
                }
                index++;
            }
            return rv;
        }
        private  bool ConvertByteArraytoFloatArray(ref float[] LeftShortArray, ref float[] RightShortArray, ref int shortstart, ref int shortend, byte[] ByteArray, int bytestart, int byteend)
        {
            bool rv = false;
            int bl, index = 0;
            short s;
            int step = 2 * wavFmt.nChannels;
            byte[] b = new byte[2];
            bl = byteend - bytestart;
            //sl = send - sstart;
            if (LeftShortArray == null)
                shortstart = 0;
            else
                shortstart = LeftShortArray.Length;
            shortend = (int)(shortstart + bl / step);
            LeftShortArray = new float[shortend];
            if (wavFmt.nChannels == 2)
                RightShortArray = new float[shortend];
            for (int i = 0; i < bl - 1; i += step)
            {
                b[0] = ByteArray[bytestart + i];
                b[1] = ByteArray[bytestart + i + 1];
                s = BitConverter.ToInt16(b, 0);
                LeftShortArray[index + shortstart] = s / 3276.70f * 2.0f;
                if (wavFmt.nChannels == 2)
                {
                    b[0] = ByteArray[bytestart + i + 2];
                    b[1] = ByteArray[bytestart + i + 3];
                    s = BitConverter.ToInt16(b, 0);
                    RightShortArray[index + shortstart] = s / 3276.70f * 2.0f;
                }
                index++;
            }
            return rv;
        }

        /// <summary>
        /// Finds the minimum and maximum values in an array of shorts
        /// </summary>
        /// <param name="array">The array</param>
        /// <param name="start">where to start looking</param>
        /// <param name="end">where to stop looking</param>
        /// <returns></returns>
        private  MinMax FindMinMax(Int16[] array, int start, int end)
        {
            MinMax min_max;
            int index = start;
            min_max.Max = Int16.MinValue;
            min_max.Min = Int16.MaxValue;
            int n = end - start + 1;//n: the number of elements to be sorted, assuming n>0
            {
                Int16 big, small;
                for (int i = index; i < (start + n - 1); i = i + 2)
                {
                    if (i < end-1)
                    {
                        if (array[i] < array[i + 1])
                        { //one comparison
                            small = array[i];
                            big = array[i + 1];
                        }
                        else
                        {
                            small = array[i + 1];
                            big = array[i];
                        }
                        if (min_max.Min > small)
                        { //one comparison
                            min_max.Min = small;
                        }
                        if (min_max.Max < big)
                        { //one comparison
                            min_max.Max = big;
                        }
                    }
                }
            }

            return min_max;
        }

        /// <summary>
        /// calls VU and Plotting functions in the UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_tick(object sender,EventArgs e)
        {
            int i,j;
            short leftlevel,rightlevel;
            
            LevelEventArgs lea=new LevelEventArgs ();
            if (recording)
            {
                if (LeftMinMax.Count > 1)
                {
                    lea.numberofchannels = (byte)wavFmt.nChannels;//arguments for VU and Plotting culled from the min amx info that was obtained from the callback
                    i = LeftMinMax.Count - 1;
                    j = RightMinMax.Count - 1;
                    MinMax lmm, rmm;
                    lmm = LeftMinMax[i];
                    if (-1 * lmm.Min > lmm.Max)
                        leftlevel = (short)(-1 * lmm.Min);
                    else
                        leftlevel = lmm.Max;
                    lea.leftlevel = leftlevel;
                    lea.leftminmax = lmm;

                    if(wavFmt .nChannels >1)
                    {
                        rmm = RightMinMax[j];
                        if (-1 * rmm.Min > rmm.Max)
                            rightlevel = (short)(-1 * rmm.Min);
                        else
                            rightlevel = rmm.Max;
                        lea.rightlevel = rightlevel;
                        lea.rightminmax = rmm;
                    }
                    RaiseLevelEvent(lea); // call the UI to Plot and do VU
                }
                if (stopstruct.Stopped)
                {
                    timer1.Enabled = false;
                    Stop();// close the waveindevice
                    RaiseRecordingStoppedEvent();
                }

            }
        }
    }
}
