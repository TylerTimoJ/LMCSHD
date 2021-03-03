using NAudio.CoreAudioApi;
using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Collections.Generic;

namespace LMCSHD
{
    public static class AudioProcesser
    {
        public delegate void Callback(float[] fftData);
        private static Callback fftDataCallback;

        public static bool isRecording = false;

        public static int LowFreqClip = 20;
        public static int HighFreqClip = 20000;
        public static int Amplitiude = 1024;

        public static FFTWindow Window = FFTWindow.BlackmannHarris;
        public enum FFTWindow { BlackmannHarris, Hamming, Hann };

        private static IWaveIn waveIn;

        private static int _fftLength = 2048; //2^n
        private static int _m = (int)Math.Log(_fftLength, 2.0);
        private static int _fftPos = 0;
        private static int _sampleRate;
        private static Complex[] _fftBuffer = new Complex[_fftLength];
        private static int queueLength = 4;
        private static Queue<float>[] fftSampleQueue = new Queue<float>[_fftLength];

        private static MMDeviceEnumerator _deviceEnumerator = new MMDeviceEnumerator();

        public static void SetupAudioProcessor(Callback fftCallback)
        {
            fftDataCallback = fftCallback;
            _m = (int)Math.Log(_fftLength, 2.0);

            for (int i = 0; i < _fftLength; i++)
            {
                fftSampleQueue[i] = new Queue<float>();
                for (int e = 0; e < queueLength; e++)
                {
                    fftSampleQueue[i].Enqueue(0);
                   // fftSampleQueue[i].Enqueue(0.0f);
                }
            }

        }

        public static void Dispose()
        {
            if (waveIn != null)
                waveIn.Dispose();
        }

        public static MMDevice GetDefaultDevice(DataFlow flow)
        {
            if (_deviceEnumerator.HasDefaultAudioEndpoint(flow, Role.Multimedia))
            {
                return _deviceEnumerator.GetDefaultAudioEndpoint(flow, Role.Multimedia);
            }
            else
            {
                return null;
            }
        }

        public static MMDeviceCollection GetActiveDevices()
        {
            return _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);
        }

        public static void BeginCapture(Callback fftCallback, int deviceIndex)
        {
            if (!isRecording)
            {
                if (deviceIndex <= GetActiveDevices().Count)
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
                    _sampleRate = waveIn.WaveFormat.SampleRate;

                    waveIn.DataAvailable += OnDataAvailable;
                    waveIn.RecordingStopped += WaveIn_RecordingStopped;

                    waveIn.StartRecording();
                    isRecording = true;


                }
            }
        }

        public static void StopRecording()
        {
            if (waveIn != null)
            {
                waveIn.StopRecording();
                isRecording = false;
                Dispose();
            }
        }

        private static void WaveIn_RecordingStopped(object sender, StoppedEventArgs e)
        {
            isRecording = false;
        }

        static void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            byte[] buffer = e.Buffer;
            int bytesRecorded = e.BytesRecorded;
            int bufferIncrement = waveIn.WaveFormat.BlockAlign;

            for (int index = 0; index < bytesRecorded; index += bufferIncrement)
            {
                float sample32 = BitConverter.ToSingle(buffer, index);
                AddSample(sample32);
            }
        }

        static void FftCalculated()
        {
            //System.Windows.MessageBox.Show("got here");
            float binFreqRange = _sampleRate / _fftLength;//fftlength 2048

            int freqRange = HighFreqClip - LowFreqClip;

            int arrayLength = (int)(freqRange / binFreqRange);
            if (arrayLength < 1)
                arrayLength = 1;

            int startIndex = (int)(LowFreqClip / binFreqRange);

            float[] topHalfFFT = new float[arrayLength];

            for (int i = startIndex; i < arrayLength + startIndex; i++)
            {
                fftSampleQueue[i - startIndex].Dequeue();
                fftSampleQueue[i - startIndex].Enqueue((float)Math.Sqrt((_fftBuffer[i].X * _fftBuffer[i].X) + (_fftBuffer[i].Y * _fftBuffer[i].Y)) * Amplitiude);
                // topHalfFFT[i - startIndex] = (float)Math.Sqrt((_fftBuffer[i].X * _fftBuffer[i].X) + (_fftBuffer[i].Y * _fftBuffer[i].Y)) * Amplitiude;
            }

            for (int i = 0; i < arrayLength; i++)
            {
                float[] allSamples = fftSampleQueue[i].ToArray();
                //System.Windows.MessageBox.Show(allSamples.Length.ToString());

                for (int e = 0; e < allSamples.Length; e++)
                {
                    topHalfFFT[i] += (float)allSamples[e];
                }
                topHalfFFT[i] /= allSamples.Length;
            }
            
            fftDataCallback(topHalfFFT);
        }

        public static void AddSample(float value)
        {
            switch (Window)
            {
                case FFTWindow.BlackmannHarris:
                    _fftBuffer[_fftPos].X = (float)(value * FastFourierTransform.BlackmannHarrisWindow(_fftPos, _fftLength));
                    break;
                case FFTWindow.Hamming:
                    _fftBuffer[_fftPos].X = (float)(value * FastFourierTransform.HammingWindow(_fftPos, _fftLength));
                    break;

                case FFTWindow.Hann:
                    _fftBuffer[_fftPos].X = (float)(value * FastFourierTransform.HannWindow(_fftPos, _fftLength));
                    break;
            }
            _fftBuffer[_fftPos].Y = 0;
            _fftPos++;
            if (_fftPos >= _fftLength)
            {
                _fftPos = 0;
                FastFourierTransform.FFT(true, _m, _fftBuffer);
                FftCalculated();
            }
        }
    }
}