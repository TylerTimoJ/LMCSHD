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

        private static int _fftLength = 4096; //2^n
        private static int _m = (int)Math.Log(_fftLength, 2.0);
        private static int _fftPos = 0;
        private static int _sampleRate;
        private static Complex[] _fftBuffer = new Complex[_fftLength];

        private static MMDeviceEnumerator _deviceEnumerator = new MMDeviceEnumerator();

        public static void SetupAudioProcessor(Callback fftCallback)
        {
            fftDataCallback = fftCallback;
            _m = (int)Math.Log(_fftLength, 2.0);
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
            float binFreqRange = _sampleRate / _fftLength;//fftlength 2048

            int freqRange = HighFreqClip - LowFreqClip;

            int arrayLength = (int)(freqRange / binFreqRange);
            if (arrayLength < 1)
                arrayLength = 1;

            int startIndex = (int)(LowFreqClip / binFreqRange);

            float[] topHalfFFT = new float[arrayLength];

            for (int i = startIndex; i < arrayLength + startIndex; i++)
            {
                topHalfFFT[i - startIndex] = (float)Math.Sqrt((_fftBuffer[i].X * _fftBuffer[i].X) + (_fftBuffer[i].Y * _fftBuffer[i].Y) * Amplitiude);
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