using RiseEffect.FFmpeg;
using RiseEffect.Shader;
using System.Configuration;
using System.Drawing;

public class Entry
{
    public static async Task ProcessVideo(string filename)
    {
        var ffmpegSettings = new Settings { BinFolder = ConfigurationManager.AppSettings["ffmpegBinPath"] };
        var inputVideo = new VideoReader(ffmpegSettings, filename);
        var outPath = Path.Combine(Path.GetDirectoryName(filename), $"{Path.GetFileNameWithoutExtension(filename)}_rise{Path.GetExtension(filename)}");
        var outputVideo = new VideoWriter(ffmpegSettings, outPath, int.Parse(ConfigurationManager.AppSettings["videoQuality"]));
        var audio = inputVideo.GetAudio();
        var inputVideoInfo = inputVideo.GetInfo();
        var processor = new ImageProcessor(new RiseFilter(inputVideoInfo.Width, inputVideoInfo.Height,
            float.Parse(ConfigurationManager.AppSettings["alpha"])),
            inputVideoInfo.Width, inputVideoInfo.Height);
        outputVideo.AddAudio(audio);
        outputVideo.Initialize(inputVideoInfo);
        var framesProcessed = 0;
        var lastBatchTime = DateTime.Now;
        foreach(var frame in inputVideo.ReadFrames())
        {
            framesProcessed++;
            var processedFrame = processor.ProcessImage(frame);
            outputVideo.AddFrame(processedFrame);
            if(framesProcessed % 30 == 0)
            {
                var remain = (int)((DateTime.Now - lastBatchTime).TotalMilliseconds / 30 * (inputVideoInfo.Frames - framesProcessed) / 1000);
                Console.Write($"\r{framesProcessed * 100 / inputVideoInfo.Frames}% processed ({remain / 60} min. {remain % 60} sec.)");
                lastBatchTime = DateTime.Now;
            }
        }
        outputVideo.Flush();
    }

    public static async Task ProcessImage(string filename)
    {
        var image = Bitmap.FromFile(filename) as Bitmap;
        var processor = new ImageProcessor(new RiseFilter(image.Width, image.Height,
            float.Parse(ConfigurationManager.AppSettings["alpha"])),
            image.Width, image.Height);
        var result = processor.ProcessImage(image);
        result.Save(Path.Combine(Path.GetDirectoryName(filename), $"{Path.GetFileNameWithoutExtension(filename)}_rise{Path.GetExtension(filename)}"));
    }

    public static void Main(string[] args)
    {
        if(args.Length == 0)
        {
            Console.WriteLine("No mp4,avi,jpg,png file provided in args");
            return;
        }
        if (!File.Exists(args[0]))
        {
            Console.WriteLine("Specified file not found");
            return;
        }
        var fileName = args[0];
        if(fileName.EndsWith(".mp4") || fileName.EndsWith(".avi"))
        {
            ProcessVideo(fileName).GetAwaiter().GetResult();
            return;
        }
        if (fileName.EndsWith(".jpg") || fileName.EndsWith(".png"))
        {
            ProcessImage(fileName).GetAwaiter().GetResult();
            return;
        }
        Console.WriteLine("Unknown file provided");
    }
}
