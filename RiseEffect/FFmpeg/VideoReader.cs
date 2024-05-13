using System.Diagnostics;
using System.Drawing;
using System.Globalization;

namespace RiseEffect.FFmpeg
{
    internal class VideoReader
    {
        private readonly Settings _settings;
        private readonly string _videoFile;

        public VideoReader(Settings settings, string videoFile)
        {
            _settings = settings;
            _videoFile = videoFile;
        }

        public VideoInfo GetInfo()
        {
            var ffprobe = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = $"{_settings.BinFolder}\\ffprobe.exe",
                    Arguments = $"-v error -select_streams v:0 -count_packets -show_entries stream=nb_read_packets,width,height:stream_tags=rotate -show_entries format=duration -of default=noprint_wrappers=1 \"{_videoFile}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            ffprobe.Start();
            ffprobe.WaitForExit();
            var output = ffprobe.StandardOutput.ReadToEnd();
            var info = output.Split('\n').Where(line => line.Contains('=')).ToDictionary(
                line => line.Substring(0, line.IndexOf('=')),
                line => line.Substring(line.IndexOf("=") + 1));
            var result = new VideoInfo
            {
                Width = int.Parse(info["width"]),
                Height = int.Parse(info["height"]),
                Frames = int.Parse(info["nb_read_packets"])
            };
            result.Fps = (int)Math.Round(result.Frames / double.Parse(info["duration"], CultureInfo.InvariantCulture));
            if (info.ContainsKey("TAG:rotate"))
            {
                var angle = int.Parse(info["TAG:rotate"]);
                if(angle == 90 || angle == 180)
                {
                    (result.Height, result.Width) = (result.Width, result.Height);
                }
            }
            ffprobe.Kill();
            return result;
        }

        public IEnumerable<Bitmap> ReadFrames()
        {
            var info = GetInfo();
            var ffmpeg = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = $"{_settings.BinFolder}\\ffmpeg.exe",
                    Arguments = $"-i \"{_videoFile}\" -f image2pipe -pix_fmt bgr24 -vcodec rawvideo -",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            ffmpeg.Start();
            var inputStream = ffmpeg.StandardOutput.BaseStream;
            byte[] frameBuffer = new byte[info.Width * info.Height * 3];
            for(int i = 0; i < info.Frames; i++)
            {
                var remainBytes = frameBuffer.Length;
                var offset = 0;
                while(remainBytes > 0)
                {
                    var readBytes = inputStream.Read(frameBuffer, offset, remainBytes);
                    offset += readBytes;
                    remainBytes -= readBytes;
                }
                var result = new Bitmap(info.Width, info.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                var rect = new Rectangle(0, 0, info.Width, info.Height);
                System.Drawing.Imaging.BitmapData bmpData =
                    result.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                    result.PixelFormat);
                var ptr = bmpData.Scan0;
                System.Runtime.InteropServices.Marshal.Copy(frameBuffer, 0, ptr, frameBuffer.Length);
                result.UnlockBits(bmpData);
                yield return result;
            }
            ffmpeg.Kill();
        }

        public byte[] GetAudio()
        {
            var ffmpeg = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = $"{_settings.BinFolder}\\ffmpeg.exe",
                    Arguments = $"-i \"{_videoFile}\" -f s16le -acodec pcm_s16le -ar 44100 -ac 2 pipe:1",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            ffmpeg.Start();
            var stream = new MemoryStream();
            byte[] buffer = new byte[32768];
            while(true)
            {
                var read = ffmpeg.StandardOutput.BaseStream.Read(buffer, 0, buffer.Length);
                if(read == 0)
                {
                    break;
                }
                stream.Write(buffer, 0, read);
            }
            return stream.ToArray();
        }
    }
}
