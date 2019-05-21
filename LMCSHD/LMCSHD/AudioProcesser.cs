using System;
using NAudio.Wave;
using NAudio.Dsp; // The Complex and FFT are here!
using NAudio.CoreAudioApi;

using System.IO;
using System.Windows;
using System.Threading;
using NAudio.Wave.SampleProviders;
using NAudio.Wave.Compression;

namespace LMCSHD
{
    class AudioProcesser
    {
        public delegate void Callback(float[] fftData);
        // Other inputs are also usable. Just look through the NAudio library.
        private IWaveIn waveIn;
        private static int fftLength = 1024; // NAudio fft wants powers of two!

        // There might be a sample aggregator in NAudio somewhere but I made a variation for my needs
        private SampleAggregator sampleAggregator = new SampleAggregator(fftLength);

        Callback fftDataCallback;

        public AudioProcesser(Callback fftCallback)
        {
            fftDataCallback = fftCallback;
            sampleAggregator.FftCalculated += new EventHandler<FftEventArgs>(FftCalculated);
            sampleAggregator.PerformFFT = true;

            // Here you decide what you want to use as the waveIn.
            // There are many options in NAudio and you can use other streams/files.
            // Note that the code varies for each different source.

           // waveIn = new WasapiLoopbackCapture();


        }

        public MMDevice GetDefaultDevice(DataFlow flow)
        {
            var enumerator = new MMDeviceEnumerator();
            return enumerator.GetDefaultAudioEndpoint(flow, Role.Multimedia);
        }


        public void BeginCapture(Callback fftCallback, MMDevice device)
        {
            if(device.DataFlow == DataFlow.Render)
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
            }

        }

        public void StopRecording()
        {
            if (waveIn != null)
            {
                waveIn.StopRecording();
                waveIn.Dispose();
            }
        }

        private void WaveIn_RecordingStopped(object sender, StoppedEventArgs e)
        {
           // MessageBox.Show("rip, it has stopped");
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
            // float[] topHalfFFT = new float[(e.Result.Length / 2) / 8];
            float[] topHalfFFT = new float[64];

            for (int i = 4; i < 68; i++)
            {
                //     topHalfFFT[i] = 2048 * Math.Abs( (float)Math.Sqrt((e.Result[i].X * e.Result[i].X) + (e.Result[i].Y * e.Result[i].Y)));
                topHalfFFT[i - 4] = (float)Math.Sqrt((e.Result[i].X * e.Result[i].X) * 256 + (e.Result[i].Y * e.Result[i].Y) * 256);
                //topHalfFFT[i] = (float)Math.Sqrt((e.Result[i].X * e.Result[i].X) + (e.Result[i].Y * e.Result[i].Y));
              //   MessageBox.Show("index" + i + "value" + topHalfFFT[i].ToString("0.00000000"));
                // MessageBox.Show(e.Result[i].Y.ToString());
            }

                fftDataCallback(topHalfFFT);

            //Thread.Sleep(200);
            // frame.InjestFFT(topHalfFFT);
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
                fftBuffer[fftPos].X = (float)(value * FastFourierTransform.BlackmannHarrisWindow(fftPos, fftLength));
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

