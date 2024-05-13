using System.Diagnostics;
using System.Drawing;
using System.IO.Pipes;

namespace RiseEffect.FFmpeg
{
    internal class VideoWriter
    {
        private readonly Settings _settings;
        private readonly string _videoFile;
        private readonly int _quality;
        private NamedPipeServerStream _audioPipe;
        private byte[] _audioData;
        private Process _ffmpeg;

        public VideoWriter(Settings setings, string videoFile, int quality = 5)
        {
            _settings = setings;
            _videoFile = videoFile;
            _quality = quality;
        }

        public void AddFrame(Bitmap frame)
        {
            var rect = new Rectangle(0, 0, frame.Width, frame.Height);
            System.Drawing.Imaging.BitmapData bmpData =
                frame.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly,
                frame.PixelFormat);
            var ptr = bmpData.Scan0;
            byte[] frameBuffer = new byte[frame.Width * frame.Height * 3];
            System.Runtime.InteropServices.Marshal.Copy(ptr, frameBuffer, 0, frameBuffer.Length);
            frame.UnlockBits(bmpData);
            _ffmpeg.StandardInput.BaseStream.Write(frameBuffer, 0, frameBuffer.Length);
        }

        public void AddAudio(byte[] audio)
        {
            _audioData = audio;
        }

        public void Initialize(VideoInfo info)
        {
            var withAudio = _audioData != null;
            var pipeName = $"ffmpegRiseEffectPipe{new Random().Next(10000, 99999)}";
            if(withAudio)
            {
                _audioPipe = new NamedPipeServerStream(pipeName);
                new Thread(AppendAudio).Start();
            }
            _ffmpeg = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = $"{_settings.BinFolder}\\ffmpeg.exe",
                    Arguments = $"-y -f rawvideo -vcodec rawvideo -s {info.Width}x{info.Height} -pix_fmt bgr24 -r {info.Fps} -i - {(withAudio ? $"-f s16le -acodec pcm_s16le -ar 44100 -ac 2 -i \\\\.\\pipe\\{pipeName}" : "-an")} -vcodec mpeg4 -qscale:v {_quality} -shortest {_videoFile}",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    CreateNoWindow = true
                }
            };
            _ffmpeg.Start();
        }

        public void Flush()
        {
            _ffmpeg.StandardInput.Close();
            _ffmpeg.StandardInput.Dispose();
            _ffmpeg.WaitForExit();
        }

        private void AppendAudio()
        {
            _audioPipe.WaitForConnection();
            int offset = 0;
            const int blockSize = 1024;
            try
            {
                while (true)
                {
                    _audioPipe.Write(_audioData, offset, blockSize);
                    offset += blockSize;
                }
            }
            catch { }
            finally
            {
                _audioPipe.Close();
            }
        }
    }
}
