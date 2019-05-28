using System;
using NAudio.Wave;
using NAudio.Dsp;
using NAudio.CoreAudioApi;
using System.IO;
using System.Windows;
using System.Threading;
using NAudio.Wave.SampleProviders;
using NAudio.Wave.Compression;

namespace LMCSHD
{
    class AudioProcesser : IDisposable
    {
        public delegate void Callback(float[] fftData);
        public bool isRecording { get; set; } = false;

        public int LowFreqClip { get; set; } = 0;
        public int HighFreqClip { get; set; } = 22050;

        public int Amplitiude { get; set; } = 1024;
        // Other inputs are also usable. Just look through the NAudio library.
        private IWaveIn waveIn;
        private static int fftLength = 2048; // NAudio fft wants powers of two!

        // There might be a sample aggregator in NAudio somewhere but I made a variation for my needs
        private SampleAggregator sampleAggregator = new SampleAggregator(fftLength);

        Callback fftDataCallback;

        MMDeviceEnumerator enumerator;

        public AudioProcesser(Callback fftCallback)
        {
            fftDataCallback = fftCallback;
            sampleAggregator.FftCalculated += new EventHandler<FftEventArgs>(FftCalculated);
            sampleAggregator.PerformFFT = true;

            enumerator = new MMDeviceEnumerator();

            // Here you decide what you want to use as the waveIn.
            // There are many options in NAudio and you can use other streams/files.
            // Note that the code varies for each different source.

            // waveIn = new WasapiLoopbackCapture();


        }

        public void Dispose()
        {
            if (waveIn != null)
                waveIn.Dispose();
        }

        public MMDevice GetDefaultDevice(DataFlow flow)
        {
            if (enumerator.HasDefaultAudioEndpoint(flow, Role.Multimedia))
            {
                return enumerator.GetDefaultAudioEndpoint(flow, Role.Multimedia);
            }
            else
            {
                return null;
            }
        }

        public MMDeviceCollection GetActiveDevices()
        {
            return enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);
        }


        public void BeginCapture(Callback fftCallback, int deviceIndex)
        {
            if (!isRecording)
            {

                MMDevice device = GetActiveDevices()[deviceIndex];

                if (device.DataFlow == DataFlow.Render)
                {
                    waveIn = new WasapiLoopbackCapture(device);
                }
                else
                {
                    waveIn = new WasapiCapture(device);
                }

                if (waveIn.WaveFormat.SampleRate != 44100)
                {
                    MessageBox.Show("Device: " + device.DeviceFriendlyName + "\n" + "has its sample rate set to: " + waveIn.WaveFormat.SampleRate.ToString() + " Hz.\n" + "Please set it to 44100 Hz.");
                    StopRecording();
                }
                else
                {
                    waveIn.DataAvailable += OnDataAvailable;
                    waveIn.RecordingStopped += WaveIn_RecordingStopped;

                    waveIn.StartRecording();
                    isRecording = true;
                }
            }
        }

        public void StopRecording()
        {
            if (waveIn != null)
            {
                waveIn.StopRecording();
                isRecording = false;
                //waveIn.Dispose();
            }
        }

        private void WaveIn_RecordingStopped(object sender, StoppedEventArgs e)
        {
            // isRecording = false;
        }

        void OnDataAvailable(object sender, WaveInEventArgs e)
        {

            byte[] buffer = e.Buffer;
            int bytesRecorded = e.BytesRecorded;
            int bufferIncrement = waveIn.WaveFormat.BlockAlign;

            for (int index = 0; index < bytesRecorded; index += bufferIncrement)
            {
                float sample32 = BitConverter.ToSingle(buffer, index);
                sampleAggregator.Add(sample32);

            }

        }

        void FftCalculated(object sender, FftEventArgs e)
        {
            float binFreqRange = 44100f / (float)fftLength;

            int freqRange = HighFreqClip - LowFreqClip;

            int arrayLength = (int)(freqRange / binFreqRange);
            if (arrayLength < 1)
                arrayLength = 1;

            int startIndex = (int)(LowFreqClip / binFreqRange);

            float[] topHalfFFT = new float[arrayLength];

            for (int i = startIndex; i < arrayLength + startIndex; i++)
            {

                topHalfFFT[i - startIndex] = (float)Math.Sqrt((e.Result[i].X * e.Result[i].X) * Amplitiude + (e.Result[i].Y * e.Result[i].Y) * Amplitiude);

            }

            fftDataCallback(topHalfFFT);

        }
    }




    class SampleAggregator
    {
        // FFT
        public event EventHandler<FftEventArgs> FftCalculated;
        public bool PerformFFT { get; set; }

        // This Complex is NAudio's own! 
        private Complex[] fftBuffer;
        private FftEventArgs fftArgs;
        private int fftPos;
        private int fftLength;
        private int m;


        public SampleAggregator(int fftLength)
        {

            this.m = (int)Math.Log(fftLength, 2.0);
            this.fftLength = fftLength;
            this.fftBuffer = new Complex[fftLength];
            this.fftArgs = new FftEventArgs(fftBuffer);
        }


        public void Add(float value)
        {

            if (PerformFFT && FftCalculated != null)
            {

                // Remember the window function! There are many others as well.
                fftBuffer[fftPos].X = (float)(value * FastFourierTransform.HammingWindow(fftPos, fftLength));
                fftBuffer[fftPos].Y = 0; // This is always zero with audio.
                fftPos++;
                if (fftPos >= fftLength)
                {
                    fftPos = 0;
                    FastFourierTransform.FFT(true, m, fftBuffer);
                    FftCalculated(this, fftArgs);
                }
            }
        }
    }

    public class FftEventArgs : EventArgs
    {
        public FftEventArgs(Complex[] result)
        {
            this.Result = result;
        }
        public Complex[] Result { get; private set; }
    }
}

