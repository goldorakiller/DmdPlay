/*
 This program is a dmd-play.py port to C# from dmd-simulator project.
https://github.com/batocera-linux/dmd-simulator
Thx to Nicolas Adenis-Lamarre aka susan34 for the great work.
Thx to batocera-linux team for the great work.  
Thx to the PPUC team for the great work.    
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NumSharp;
using CommandLine.Text;
using CommandLine;
using System.Drawing.Text;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using System.Globalization;

namespace DmdPlayer
{
    static class Program
    {
        internal class ImageOptions
        {
            [Option('f', "file", Required = false, HelpText = "image path file.")]
            public string file { get; set; }

            [Option('v', "video", Required = false, HelpText = "video path file.")]
            public string video { get; set; }

            [Option('t', "text", Required = false, HelpText = "text.")]
            public string text { get; set; }

            [Option("host", Required = false, Default = "localhost", HelpText = "dmd server host.")]
            public string host { get; set; }

            [Option('p', "port", Required = false, Default = 6789, HelpText = "dmd server port.")]
            public int port { get; set; }

            [Option('m', "move", Required = false, Default = 2, HelpText = "text movement each time.")]
            public int move { get; set; }

            [Option("once", Required = false, Default = false, HelpText = "don't loop for ever.")]
            public bool once { get; set; }

            [Option("clear", Required = false, Default = false, HelpText = "clear the screen.")]
            public bool clear { get; set; }

            [Option("overlay", Required = false, Default = false, HelpText = "restore the previous frames once finish.")]
            public bool overlay { get; set; }

            [Option("overlay-time", Required = false, Default = 1000, HelpText = "time to pause fixed images for the overlay in ms.")]
            public int overlaytime { get; set; }

            [Option("moving-text", Required = false, Default = false, HelpText = "always makes the text to move, even if text fit.")]
            public bool movingtext { get; set; }

            [Option("fixed-text", Required = false, Default = false, HelpText = "never makes the text to move, even if text fit.")]
            public bool fixedtext { get; set; }

            [Option("caps", Required = false, Default = false, HelpText = "convert text in all caps.")]
            public bool caps { get; set; }

            [Option('l', "line-spacing", Required = false, Default = 2, HelpText = "number of pixels between each line of text.")]
            public int linespacing { get; set; }

            [Option("align", Required = false, Default = StringAlignment.Center, HelpText = "text alignment (center, left or right).")]
            public StringAlignment align { get; set; }

            [Option("no-fit", Required = false, Default = false, HelpText = "keep font aspect ratio (easier to read for moving text).")]
            public bool nofit { get; set; }

            [Option('s', "speed", Required = false, Default = 60, HelpText = "sleep time during each text position (in milliseconds).")]
            public int speed { get; set; }

            [Option("gradient", Required = false, HelpText = "gradient file (rainbow effect and more).")]
            public string gradient { get; set; }

            [Option('r', "red", Required = false, Default = 255, HelpText = "red text color level (0-255).")]
            public int red { get; set; }

            [Option('g', "green", Required = false, Default = 0, HelpText = "green text color level (0-255).")]
            public int green { get; set; }

            [Option('b', "blue", Required = false, Default = 0, HelpText = "blue text color level (0-255).")]
            public int blue { get; set; }

            [Option("font", Required = false, Default = "G:\\DejaVuSans.ttf", HelpText = "path to the font file.")]
            public string font { get; set; }

            [Option("width", Required = false, Default = 128, HelpText = "width.")]
            public int width { get; set; }

            [Option("height", Required = false, Default = 32, HelpText = "height.")]
            public int height { get; set; }

            [Option("hd", Required = false, Default = false, HelpText = "hd format, equivalent of --width 256 --height 64.")]
            public bool hd { get; set; }

            [Option('c', "clock", Required = false, Default = false, HelpText = "display current time.")]
            public bool clock { get; set; }

            [Option("h12", Required = false, Default = false, HelpText = "12 hours format.")]
            public bool h12 { get; set; }

            [Option("no-seconds", Required = false, Default = false, HelpText = "don't display seconds.")]
            public bool no_seconds { get; set; }

            [Option("clock-format", Required = false, HelpText = "custom clock format.")]
            public string clock_format { get; set; }

            [Option('C', "countdown", Required = false, HelpText = "countdown to a specific date.")]
            public string countdown { get; set; }

            [Option("countdown-header", Required = false, HelpText = "countdown header.")]
            public string countdownHeader { get; set; }

            [Option("countdown-format", Required = false, Default = "{D:0}d {H:0}:{M:00}:{S:00}", HelpText = "countdown format.")]
            public string countdownFormat { get; set; }

            [Option("countdown-format-0-day", Required = false, Default = "{H:00}:{M:00}:{S:00}", HelpText = "countdown format for 0 days.")]
            public string countdownFormat0Day { get; set; }

            [Option("countdown-format-0-hour", Required = false, Default = "{M:00}:{S:00}", HelpText = "countdown format for 0 hours.")]
            public string countdownFormat0Hour { get; set; }

            [Option("countdown-format-0-minute", Required = false, Default = "{S:00}", HelpText = "countdown format for 0 minutes.")]
            public string countdownFormat0Minute { get; set; }

        }

        public enum TextAlign { Left, Center, Right }              
     
        public static string[] ConvertArgs(string[] args)
        {
            List<string> newArgs = new List<string>();
            StringBuilder combinedBuilder = new StringBuilder();
            bool isCombining = false;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (IsSpecialOption(args, i) || isCombining)
                {
                    if (!isCombining)
                    {
                        combinedBuilder.Append(arg);
                        isCombining = true;
                    }
                    else
                    {
                        combinedBuilder.Append(" ").Append(arg);
                    }

                    if (i + 1 >= args.Length || IsSpecialOption(args, i + 1))
                    {
                        newArgs.Add(combinedBuilder.ToString());
                        combinedBuilder.Clear();
                        isCombining = false;
                    }
                }
                else
                {
                    newArgs.Add(arg);
                }
            }

            return newArgs.ToArray();
        }

        private static bool IsSpecialOption(string[] args, int index)
        {
            // Check if current or next argument starts with -- or -
            if (args[index].StartsWith("--") || args[index].StartsWith("-"))
            {
                return true;
            }

            // If previous argument starts with -- or -, current might be part of it
            if (index > 0 && (args[index - 1].StartsWith("--") || args[index - 1].StartsWith("-")))
            {
                return true;
            }

            // If current argument is a date-time, it's a special case
            if (DateTime.TryParse(args[index], out _))
            {
                return true;
            }

            return false;
        }

        static int Main(string[] args)
        {
            // Permet de lire les arguments sans les quotes.
            args = ConvertArgs(args);

            var result = Parser.Default.ParseArguments<ImageOptions>(args);

            result.WithParsed(options =>
            {
                string layer = options.overlay ? "overlay" : "main";
                int width = options.hd ? 256 : options.width;
                int height = options.hd ? 64 : options.height;
                byte[] header = GetHeader(width, height, layer, width * height * 2); // # RGB565 
                int move = options.move < 1 ? 1 : options.move;
                Color color = Color.FromArgb(options.red, options.green, options.blue);

                if (args.Length == 0)
                {
                    DisplayError(result, "Missing something to play");
                    return;
                }

                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    client.Connect(options.host, options.port);
                }
                catch (System.Net.Sockets.SocketException ex)
                {
                    Console.WriteLine(ex.Message);
                    return;
                }

                switch (args[0])
                {
                    case "-f":
                    case "--file":
                        if (options.file != null)
                        {
                            SendImageFile(header, client, layer, options.file, width, height, options.once);
                        }
                        else
                        {
                            DisplayError(result, "Missing something to play");
                        }                       
                        break;

                    case "-v":
                    case "--video":
                        if (options.video != null)
                        {
                            SendVideoFile(header, client, layer, options.video, width, height, options.once);
                        }
                        else
                        {
                            DisplayError(result, "Missing something to play");
                        }                       
                        break;

                    case "-t":
                    case "--text":
                        if (options.text != null)
                        {
                            if (options.caps)
                            {
                                options.text = options.text.ToUpper();
                            }
                            SendText(header, client, layer, options.text, color, width, height, options.font, options.gradient, options.movingtext, options.fixedtext, options.speed, move, options.once, options.nofit, options.linespacing, options.align);
                        }
                        else
                        {
                            DisplayError(result, "Missing something to play");
                        }
                        break;

                    case "-c":
                    case "--clock":
                        if (options.clock)
                        {
                            SendClock(header, client, layer, color, width, height, options.font, options.gradient, options.speed, options.h12, options.no_seconds, options.clock_format, options.linespacing, options.align);
                        }
                        else
                        {
                            DisplayError(result, "Missing something to play");
                        }
                        break;

                    case "-C":
                    case "--countdown":
                        if (options.countdown != null)
                        {
                            SendCountdown(header, client, layer, options.countdown, color, width, height, options.font, options.gradient, options.speed, options.countdownHeader, options.countdownFormat, options.countdownFormat0Day, options.countdownFormat0Hour, options.countdownFormat0Minute, options.linespacing, options.align);
                        }
                        else
                        {
                            DisplayError(result, "Missing something to play");
                        }
                        break;

                    case "--clear":
                        SendText(header, client, layer, "", color, width, height, options.font, options.gradient, false, true, options.speed, move, true, false, options.linespacing, options.align);
                        break;

                    default:
                        DisplayError(result, "Missing something to play");
                        break;
                }
            })
            .WithNotParsed(errors => HandleParseErrors(result, errors));

            return 0;
        }

        static void DisplayError<T>(ParserResult<T> result, string message)
        {
            Console.WriteLine(message);
            var helpText = HelpText.AutoBuild(result, h =>
            {
                h.AdditionalNewLineAfterOption = false; //remove the extra newline between options
                //h.Heading = "Myapp 2.0.0-beta"; //change header
                //h.Copyright = "Copyright (c) 2019 Global.com"; //change copyrigt text
                return HelpText.DefaultParsingErrorsHandler(result, h);
            }, e => e);
            Console.WriteLine(helpText);
        }

        static void HandleParseErrors<T>(ParserResult<T> result, IEnumerable<Error> errors)
        {
            foreach (var error in errors)
            {
                if (error is HelpRequestedError || error is VersionRequestedError)
                {
                    var helpText = HelpText.AutoBuild(result, h =>
                    {
                        h.AdditionalNewLineAfterOption = false; //remove the extra newline between options                
                        return HelpText.DefaultParsingErrorsHandler(result, h);
                    }, e => e);
                    Console.WriteLine(helpText);
                }
                else
                {
                    Console.WriteLine($"Error: {error}");
                }
            }
        }
       
        public static NDArray Im2Rgb565(Bitmap im)
        {
            Bitmap bmp565 = im.Clone(new Rectangle(0, 0, im.Width, im.Height), PixelFormat.Format16bppRgb565);         
            var dat = np_.ToNDArray(bmp565, false, true, true);
            return dat;
        }

        private static NDArray ImageConvert(Bitmap im)
        {
            return Im2Rgb565(im);
        }

        public static byte[] GetHeader(int width, int height, string layer, int nbytes)
        {
            string endianness = BitConverter.IsLittleEndian ? "little" : "big";
            byte version = 1;
            int mode = 3; // # rgb565
            byte buffered, disconnectOthers;
            if (layer == "main")
            {
                buffered = 1;
                disconnectOthers = 1;
            }
            else
            {
                buffered = 0;
                disconnectOthers = 0;
            }

            byte[] header = new byte[25];
            Encoding.UTF8.GetBytes("DMDStream", 0, 9, header, 0);
            header[9] = 0x00;
            header[10] = version;
            BitConverter.GetBytes(mode).CopyTo(header, 11);
            BitConverter.GetBytes((uint)width).CopyTo(header, 15);
            BitConverter.GetBytes((uint)height).CopyTo(header, 17);
            header[19] = buffered;
            header[20] = disconnectOthers;
            BitConverter.GetBytes(nbytes).CopyTo(header, 21);
            return header;
        }

        public static void SendFrame(byte[] header, Socket client, string layer, NDArray im)
        {
            byte[] msg = new byte[header.Length + im.size];
            msg = header.Concat(im.ToByteArray()).ToArray();

            int msglen = msg.Length;
            int totalsent = 0;
            while (totalsent < msglen)
            {
                int sent = client.Send(msg, totalsent, msglen - totalsent, SocketFlags.None);
                if (sent == 0)
                {
                    throw new Exception("socket connection broken");
                }
                totalsent += sent;
            }



        }

        private static Bitmap ImageFit(Bitmap im, int width, int height, bool padding = true)
        {
            int img_width = im.Width;
            int img_height = im.Height;
            int woffset = 0;
            int hoffset = 0;

            if (img_height == 0 || img_width == 0)
            {
                return new Bitmap(width, height, PixelFormat.Format32bppArgb);
            }

            int new_width, new_height;
            decimal a = (decimal)img_width / (decimal)img_height;

            if ((decimal)img_width / (decimal)img_height > ((decimal)width / (decimal)height))
            {
                new_width = width;
                if (padding)
                {
                    new_height = (int)Math.Round(width * (decimal)img_height / img_width);
                }
                else
                {
                    new_height = height;
                }
                if (padding)
                {
                    hoffset = (height - new_height) / 2;
                }
            }
            else
            {
                if (padding)
                {
                    new_width = (int)Math.Round(height * (decimal)img_width / img_height);
                }
                else
                {
                    new_width = width;
                }
                new_height = height;
                if (padding)
                {
                    woffset = (width - new_width) / 2;
                }
            }

            im = new Bitmap(im, new Size(new_width, new_height));

            //# need rgba conversion for alpha_composite
            if (im.PixelFormat != PixelFormat.Format32bppArgb)
            {
                Bitmap temp = new Bitmap(im.Width, im.Height, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(temp))
                {
                    g.DrawImage(im, 0, 0);
                }
                im = temp;
            }

            //# alpha_composite required over paste
            Bitmap new_im = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(new_im))
            {
                g.CompositingMode = CompositingMode.SourceOver;
                g.DrawImage(im, woffset, hoffset);
            }
            return new_im;
        }

        public static Bitmap Txt2Image(string txt, Font font, string gradient, int width, int height, Color fillColor, int xoffset, int yoffset, int spacing, StringAlignment align)
        {
            Bitmap im;
           
            if (!string.IsNullOrEmpty(gradient))
            {
                im = Draw_Multiline_text(txt, font, font.Size, fillColor, TextAlign.Center,2,true);
                Bitmap gradback = new Bitmap(gradient);
                Bitmap resizedGradback = new Bitmap(gradback, width, height);

                return  ApplyAlphaChannel(resizedGradback, im);
            }
            else
            {
                im = Draw_Multiline_text(txt, font, font.Size, fillColor, TextAlign.Center);
                return im;
            }
        }

        public static void SendImageFile(byte[] header, Socket client, string layer, string file, int width, int height, bool once)
        {
            List<Dictionary<string, NDArray>> animCache = null;

            Bitmap im = new Bitmap(1, 1);

            if (file.EndsWith(".svg"))
            {
                Svg.SvgDocument svg = Svg.SvgDocument.Open(file);
                im = svg.Draw();           
            }
            else
            {
                im = new Bitmap(file);

            }
     
            if (im.PropertyIdList.Contains(0x5100)) // animation    
            {
                animCache = new List<Dictionary<string, NDArray>>();
                for (int n = 0; n < im.GetFrameCount(FrameDimension.Time); n++)
                {
                    animCache.Add(new Dictionary<string, NDArray>
                    {
                        { "img", ImageConvert(ImageFit(new Bitmap(im), width, height)) },
                        { "duration", BitConverter.ToInt32(im.GetPropertyItem(0x5100).Value, n * 4) * 10 }
                    });
                    im.SelectActiveFrame(FrameDimension.Time, n);
                    if (im.PropertyIdList.Contains(0x5101)) //loop
                    {
                        once = true;
                    }
                }
            }
            else
            {
                Bitmap resizedImage = ImageFit(im, width, height);
                SendFrame(header, client, layer, ImageConvert(resizedImage));
            }

            if (animCache != null)
            {
                PlayAnim(header, client, layer, animCache, once);
            }
        }

        public static void SendVideoFile(byte[] header, Socket client, string layer, string file, int width, int height, bool once)
        {
            while (true)
            {
                OpenCvSharp.VideoCapture cap = new OpenCvSharp.VideoCapture(file);

                double fps = cap.Get(OpenCvSharp.VideoCaptureProperties.Fps);
                DateTime lastRendering = DateTime.MinValue;
                Console.WriteLine("fps: {0}", fps);

                int nskip = (int)(fps / 20); // skip some frames to not overload too much ; no more than 20 fps
                int f = 0;
                while (cap.IsOpened())
                {
                    OpenCvSharp.Mat frame = new OpenCvSharp.Mat();
                    cap.Read(frame);
                    if (frame.Empty())
                    {
                        break;
                    }
                    if (nskip > 0 && f % nskip == 0)
                    {
                        var tmp = frame.CvtColor(OpenCvSharp.ColorConversionCodes.BGR2RGB);
                        Bitmap im = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(tmp);
                        im = ImageFit(im, width, height);
                        SendFrame(header, client, layer, ImageConvert(im));
                    }
                    if (lastRendering != DateTime.MinValue)
                    {
                        TimeSpan d = DateTime.Now - lastRendering;
                        if (d < TimeSpan.FromSeconds(1 / fps))
                        {
                            Thread.Sleep(TimeSpan.FromSeconds(1 / fps - d.TotalSeconds));
                        }
                    }
                    lastRendering = DateTime.Now;
                    f += 1;
                }
                cap.Release();
                if (once)
                {
                    break;
                }
            }
        }

        private static void PlayAnim(byte[] header, Socket client, string layer, List<Dictionary<string, NDArray>> anim, bool once)
        {
            // Implement playing animation logic here
            while (true)
            {
                foreach (var frame in anim)
                {
                    var ts = DateTime.Now;                    
                    SendFrame(header, client, layer, frame["img"]);

                    // Ensure more homogeneous animation by accounting for system call time
                    var spentTime = (DateTime.Now - ts).TotalSeconds;
                    var delay = (int)frame["duration"] / 1000.0;
                    if (delay > spentTime)
                    {
                        Thread.Sleep((int)((delay - spentTime) * 1000));
                    }
                }

                if (once)
                {
                    break;
                }
            }
        }
       
        public static void SendText(byte[] header, Socket client, string layer, string text, Color color, int targetWidth, int targetHeight, string fontfile, string gradient, bool movingText, bool fixedText, int speed, int move, bool once, bool noFit, int lineSpacing, StringAlignment align)
        {

            //#text = bytes(text, 'utf-8').decode("unicode_escape") # so that you can use '\n' # this one break utf-8 chars
            text = text.Replace("\\n", "\n");
            string[] lines = text.Split(new[] { "\n" }, StringSplitOptions.None);
            if (lines.Length < 1)
            {
                lines = new[] { "" };
            }

            PrivateFontCollection privateFontCollection = new PrivateFontCollection();
            privateFontCollection.AddFontFile(fontfile);
            FontFamily fontFamily = privateFontCollection.Families[0];
            Font font = new Font(fontFamily, targetHeight / lines.Length, FontStyle.Regular, GraphicsUnit.Pixel);

            Bitmap im = Draw_Multiline_text(text, font, targetHeight / lines.Length, color);
            int imgWidth = (int)im.Width;
            int imgHeight = (int)im.Height;

            bool fit = (imgWidth < targetWidth) && (imgHeight < targetHeight);

            if (gradient != null)
            {
                noFit = false;
            }

            if (fit && (!movingText || fixedText)) //# the text fit on the screen
            {
                im = Txt2Image(text, font, gradient, imgWidth, imgHeight, color, 0, 0, lineSpacing, align);
                if (!noFit)
                {
                    im = ImageFit(im, targetWidth, targetHeight); //# an optimisation could be to directly fit with an extra argument in txt2image
                }           
                SendFrame(header, client, layer, ImageConvert(im));

            }
            else if (!fit && (!movingText || fixedText)) //# it doesn't fix, resize
            {
                im = Txt2Image(text, font, gradient, imgWidth, imgHeight, color, 0, 0, lineSpacing, align);
                im = new Bitmap(im, new Size(targetWidth, imgHeight * targetWidth / imgWidth));
                if (!noFit)
                {
                    im = ImageFit(im, targetWidth, targetHeight);
                }            
                SendFrame(header, client, layer, ImageConvert(im));
            }
            else
            {
                //# move the text ; generate all the frames in a cache first
                List<Dictionary<string, NDArray>> animCache = new List<Dictionary<string, NDArray>>();
                im = Txt2Image(text, font, gradient, imgWidth, imgHeight, color, 0, 0, lineSpacing, align);
                if (!noFit)
                {
                    im = ImageFit(im, targetWidth, targetHeight, false);
                }
                int resImgHeight = im.Height;
                int resWidth = im.Width;
                for (int i = 1; i <= targetWidth + resWidth; i += move)
                {
                    Bitmap newIm = new Bitmap(targetWidth, targetHeight);
                    using (Graphics g2 = Graphics.FromImage(newIm))
                    {
                        g2.Clear(Color.Transparent);
                        g2.DrawImage(im, targetWidth - i, 0);
                    }
                    animCache.Add(new Dictionary<string, NDArray> { { "img", ImageConvert(newIm) }, { "duration", speed } });
                }
                PlayAnim(header, client, layer, animCache, once);
            }


        }
  
        public static void SendClock(byte[] header, Socket client, string layer, Color color, int width, int height, string fontfile, string gradient, int speed, bool h12, bool no_seconds, string clock_format, int line_spacing, StringAlignment align)
        {
            while (true)
            {
                string localtime;
                if (!string.IsNullOrEmpty(clock_format))
                {
                    localtime = DateTime.Now.ToString(clock_format);
                }
                else if (h12)
                {
                    if (no_seconds)
                    {
                        localtime = DateTime.Now.ToString("h:mm tt");
                    }
                    else
                    {
                        localtime = DateTime.Now.ToString("h:mm:ss tt");
                    }
                }
                else
                {
                    if (no_seconds)
                    {
                        localtime = DateTime.Now.ToString("HH:mm");
                    }
                    else
                    {
                        localtime = DateTime.Now.ToString("HH:mm:ss");
                    }
                }

                SendText(header, client, layer, localtime, color, width, height, fontfile, gradient, false, true, speed, 0, true, false, line_spacing, align);
                Thread.Sleep(speed / 1000);
            }
        }

        /// <summary>
        /// Convertit un objet TimeSpan ou un nombre en une chaîne formatée de manière personnalisée,
        /// similaire à la méthode strftime() pour les objets DateTime.
        /// </summary>
        /// <param name="tdelta">La valeur à formater (peut être un TimeSpan ou un nombre).</param>
        /// <param name="fmt">Le format de sortie (par défaut, "{D:00}d {H:00}h {M:00}m {S:00}s").</param>
        /// <param name="inputtype">Le type de l'entrée ("timedelta" par défaut, sinon des unités de temps comme "s", "m", "h", "d", "w").</param>
        /// <returns>Une chaîne formatée.</returns>
        public static string StrfDelta(TimeSpan tdelta, string fmt = "{D:00}d {H:00}h {M:00}m {S:00}s", string inputtype = "timedelta")
        {
            // Convertit différentes unités de temps en TimeSpan
            if (inputtype == "s" || inputtype == "seconds")
            {
                tdelta = TimeSpan.FromSeconds(tdelta.TotalSeconds);
            }
            else if (inputtype == "m" || inputtype == "minutes")
            {
                tdelta = TimeSpan.FromMinutes(tdelta.TotalMinutes);
            }
            else if (inputtype == "h" || inputtype == "hours")
            {
                tdelta = TimeSpan.FromHours(tdelta.TotalHours);
            }
            else if (inputtype == "d" || inputtype == "days")
            {
                tdelta = TimeSpan.FromDays(tdelta.TotalDays);
            }
            else if (inputtype == "w" || inputtype == "weeks")
            {
                tdelta = TimeSpan.FromDays(tdelta.TotalDays * 7);
            }

            // Convertit TimeSpan en secondes entières.
            int remainder = (int)tdelta.TotalSeconds;

            // Définit les champs possibles et leurs constantes en secondes.
            var possibleFields = new List<string> { "W", "D", "H", "M", "S" };
            var constants = new Dictionary<string, int>
        {
            { "W", 604800 },   // 1 semaine = 604800 secondes
            { "D", 86400 },    // 1 jour = 86400 secondes
            { "H", 3600 },     // 1 heure = 3600 secondes
            { "M", 60 },       // 1 minute = 60 secondes
            { "S", 1 }         // 1 seconde = 1 seconde
        };

            // Analyse le format spécifié pour extraire les champs désirés.
            var desiredFields = new List<string>();
            foreach (var field in possibleFields)
            {
                if (fmt.Contains(field))
                {
                    desiredFields.Add(field);
                }
            }

            // Calcule les valeurs pour chaque champ désiré.
            var values = new Dictionary<string, int>();
            foreach (var field in desiredFields)
            {
                if (constants.ContainsKey(field))
                {
                    values[field] = remainder / constants[field];
                    remainder = remainder % constants[field];
                }
            }

            // Remplace les champs dans le format par les valeurs calculées.
            // Utilise une expression régulière pour détecter les spécificateurs de format.
            string result = fmt;
            foreach (var field in values)
            {
                //string pattern = @"\{" + field.Key + @":(0+)\}";
                string pattern = @"\{" + field.Key + @":(\d+)\}";
                var match = Regex.Match(result, pattern);
                if (match.Success)
                {
                    string formatSpecifier = match.Groups[1].Value;
                    result = result.Replace(match.Value, values[field.Key].ToString("D" + formatSpecifier.Length, CultureInfo.InvariantCulture));
                }
                else
                {
                    result = result.Replace("{" + field.Key + "}", values[field.Key].ToString(CultureInfo.InvariantCulture));
                }
            }

            return result;
        }
            
        public static void SendCountdown(byte[] header, Socket client, string layer, string countdown, Color color, int width, int height, string fontfile, string gradient, int speed, string countdownHeader, string countdownFormat, string countdownFormat0Day, string countdownFormat0Hour, string countdownFormat0Minute, int lineSpacing, StringAlignment align)
        {
            //Get these values however you like.
            //DateTime daysLeft = DateTime.Parse("1/1/2012 12:00:01 AM");
            //DateTime startDate = DateTime.Now;

            //Calculate countdown timer.
            //TimeSpan t = daysLeft - startDate;
            //string countDown = string.Format("{0} Days, {1} Hours, {2} Minutes, {3} Seconds til launch.", t.Days, t.Hours, t.Minutes, t.Seconds);


            DateTime target = DateTime.ParseExact(countdown, "yyyy-MM-dd HH:mm:ss", null);

            while (true)
            {
                DateTime now = DateTime.Now;
                TimeSpan delta = target.Subtract(now);
                double totalSeconds = Math.Abs(delta.TotalSeconds);

                string txt = "";
                if ((totalSeconds > 0 && totalSeconds < 60) || (totalSeconds < 0 && totalSeconds > -60))
                {
                    txt = StrfDelta(delta, countdownFormat0Minute);
                }
                else if ((totalSeconds > 0 && totalSeconds < 3600) || (totalSeconds < 0 && totalSeconds > -3600))
                {
                    txt = StrfDelta(delta, countdownFormat0Hour);
                }
                else if ((totalSeconds > 0 && totalSeconds < 86400) || (totalSeconds < 0 && totalSeconds > -86400))
                {
                    txt = StrfDelta(delta, countdownFormat0Day);
                }
                else
                {
                    txt = StrfDelta(delta, countdownFormat);
                }

                if (countdownHeader != null)
                {
                    txt = countdownHeader + "\n" + txt;
                }

                SendText(header, client, layer, txt, color, width, height, fontfile, gradient, false, true, speed, 0, true, false, lineSpacing, align);
                Thread.Sleep(speed / 1000);
            }
        }

        private static Bitmap Draw_Multiline_text(string text, Font font, float fontSize, Color textColor, TextAlign textAlign = TextAlign.Center, int lineSpacing = 2, bool alphamask = false)
        {
            Color background = Color.Black;
            if (alphamask)
            {
                background = Color.Transparent;
            }

            // Vérifier si le texte est vide
            if (string.IsNullOrEmpty(text))
            {
                // Créer une image vide ou avec un message par défaut
                Bitmap emptyImage = new Bitmap(1, 1);
                using (Graphics drawing = Graphics.FromImage(emptyImage))
                {
                    drawing.Clear(Color.Transparent);
                }
                return emptyImage;
            }

            // Séparer le texte en lignes
            string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            // Créer une nouvelle image temporaire pour mesurer la taille du texte
            using (Bitmap tempImage = new Bitmap(1, 1))
            using (Graphics tempGraphics = Graphics.FromImage(tempImage))
            {
                tempGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                // Mesurer la taille de chaque ligne
                List<RectangleF> lineBounds = new List<RectangleF>();
                float totalHeight = 0;
                float maxWidth = 0;

                foreach (var line in lines)
                {
                    using (GraphicsPath path = new GraphicsPath())
                    {
                        StringFormat stringFormat = new StringFormat(StringFormat.GenericTypographic);
                        path.AddString(line, font.FontFamily, (int)font.Style, fontSize, new Point(0, 0), stringFormat);
                        RectangleF bounds = path.GetBounds();
                        lineBounds.Add(bounds);
                        totalHeight += bounds.Height + lineSpacing; // Ajouter l'espacement entre les lignes
                        if (bounds.Width > maxWidth)
                        {
                            maxWidth = bounds.Width;
                        }
                    }
                }

                // Soustraire l'espacement ajouté après la dernière ligne
                if (lines.Length > 0)
                {
                    totalHeight -= lineSpacing;
                }

                // Ajouter de la place pour la bordure
                float borderSize = 0; // Taille de la bordure
                float imageWidth = maxWidth + 2 * borderSize;
                float imageHeight = totalHeight + 2 * borderSize;

                // Créer l'image finale avec les dimensions calculées             
                using (Bitmap img = new Bitmap((int)Math.Ceiling(imageWidth), (int)Math.Ceiling(imageHeight), PixelFormat.Format32bppArgb))
                using (Graphics drawing = Graphics.FromImage(img))
                {
                    drawing.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    drawing.SmoothingMode = SmoothingMode.AntiAlias;
                    drawing.Clear(background);

                    // Dessiner chaque ligne de texte
                    float yOffset = borderSize;
                    foreach (var line in lines)
                    {
                        using (GraphicsPath path = new GraphicsPath())
                        {
                            StringFormat stringFormat = new StringFormat(StringFormat.GenericTypographic);
                            stringFormat.Alignment = StringAlignment.Near; // Alignement par défaut à gauche
                            switch (textAlign)
                            {
                                case TextAlign.Center:
                                    stringFormat.Alignment = StringAlignment.Center;
                                    break;
                                case TextAlign.Right:
                                    stringFormat.Alignment = StringAlignment.Far;
                                    break;
                            }

                            path.AddString(line, font.FontFamily, (int)font.Style, fontSize, new Point(0, 0), stringFormat);
                            RectangleF bounds = path.GetBounds();
                            float xOffset = borderSize;
                            if (textAlign == TextAlign.Center)
                            {
                                xOffset = (img.Width - bounds.Width) / 2 - bounds.X;
                            }
                            else if (textAlign == TextAlign.Right)
                            {
                                xOffset = img.Width - bounds.Width - borderSize - bounds.X;
                            }

                            drawing.TranslateTransform(xOffset, yOffset - bounds.Y);
                            using (Brush textBrush = new SolidBrush(textColor))
                            {
                                drawing.FillPath(textBrush, path);
                            }
                            yOffset += bounds.Height + lineSpacing;
                            drawing.ResetTransform();
                        }
                    }

                    drawing.Save();

                    return (Bitmap)img.Clone(); // Retourner une copie de l'image pour éviter les problèmes de gestion de ressources
                }
            }
        }
        
        private static Bitmap ApplyAlphaChannel(Bitmap sourceImage, Bitmap alphaImage)
        {
            Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);

            for (int y = 0; y < sourceImage.Height; y++)
            {
                for (int x = 0; x < sourceImage.Width; x++)
                {
                    Color sourceColor = sourceImage.GetPixel(x, y);
                    Color alphaColor = alphaImage.GetPixel(x, y);

                    Color resultColor = Color.FromArgb(alphaColor.A, sourceColor.R, sourceColor.G, sourceColor.B);

                    resultImage.SetPixel(x, y, resultColor);
                }
            }         
            return resultImage;
        }




    }
}
