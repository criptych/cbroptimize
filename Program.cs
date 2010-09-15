using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
#if DEBUG
using System.Diagnostics;
#endif

namespace cbrOptimize
{
    class Program
    {
        public Program()
        {

        }

        EncoderParameters encoderParameters;

        public int Run(string[] args)
        {
            string name;
            if (args.Length > 0)
                name = args[0];
            else
            {
                Console.WriteLine("cbroptimize by Borza Industries");
                Console.WriteLine("usage: cbroptimize [file]");
                Console.WriteLine("[file] = cbr,cbz comic bool archive");
                Console.WriteLine();
                Console.WriteLine("parameters (jpeg quality, desired image width are set in cbroptimize.exe.config");
                return 0;
            }

            long quality = Properties.Settings.Default.quality;
            int desiredWidth = Properties.Settings.Default.width;
            if (quality < 1 || quality > 100)
            {
                Console.WriteLine("Configuration error: quality must be between 1 and 100!");
                return 3;
            }
            if (desiredWidth < 0)
            {
                Console.WriteLine("Configuration error: width must be positive!");
                return 4;
            }
            Console.WriteLine(string.Format("cbroptimize by Borza Industries - quality={0} width={1}",quality,desiredWidth));
            encoderParameters = GetEncoderParameters(quality);

            if (!File.Exists(name))
            {
                Console.WriteLine("File " + name + " does not exist or I can't open it!");
                return 1;
            }
            if (!File.Exists("7z.exe"))
            {
                Console.WriteLine("7z.exe not found, exiting!");
                return 2;
            }
            //clean up temp. dir
            string tempDirName = "tmp";

            if (Directory.Exists(tempDirName))
                DeleteFileSystemInfo(new DirectoryInfo(tempDirName));
            if (!Directory.Exists(tempDirName))
                Directory.CreateDirectory(tempDirName);

            //extract cbr to temp. dir
            Console.WriteLine("Extracting CBR to temporary directory...");
            ProcessStartInfo psi = new ProcessStartInfo("7z", string.Format("e -y -o\"{0}\" \"{1}\"", tempDirName, name)); // e extract, -y Yes on all questions, -o output directory, archive name
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            Process.Start(psi).WaitForExit();
            Console.WriteLine("Extraction OK, now resizing images...");
            List<string> exts = new List<string>() { ".jpg", ".png", ".gif", ".bmp", ".jpeg" };
            string[] files = Directory.GetFiles(tempDirName, "*.*");
            foreach (var fileName in files)
            {
                FileInfo finfo = new FileInfo(fileName);
                if (!exts.Contains(finfo.Extension.ToLower()))
                    continue;

                Console.Write("\r                                                        ");
                Console.Write("\r" + fileName);
                using (Bitmap originalBmp = new Bitmap(fileName))
                {
                    Bitmap newBmp;
                    ImageOrientation orientation = GetOrientation(originalBmp);
                    //if image is portrait oriented, resize to specified width. on landscape, resize to twice the size (it's probably a double page)
                    int newWidth = orientation == ImageOrientation.Portrait ? desiredWidth : desiredWidth * 2;
                    if (newWidth < originalBmp.Width)
                    {
                        //resize the image
                        float ratio = (float)newWidth / (float)originalBmp.Width;
                        //ratio = 0.5f;
                        newBmp = new Bitmap((int)(ratio * originalBmp.Width), (int)(ratio * originalBmp.Height));
                        using (Graphics g = Graphics.FromImage(newBmp))
                        {
                            Matrix m = new Matrix();
                            m.Scale(1f, 1f);
                            //g.Transform = m;
                            g.DrawImage(originalBmp, 0, 0, newBmp.Width, newBmp.Height);
                        }
                    }
                    else
                        newBmp = originalBmp;
                    //save to specified quality
                    string newFileName = fileName + ".jpg";
                    Encoder qualityEncoder = Encoder.Quality;
                    EncoderParameter ratio1 = new EncoderParameter(qualityEncoder, quality);
                    EncoderParameters codecParams = new EncoderParameters(1);
                    codecParams.Param[0] = ratio1;
                    newBmp.Save(newFileName, GetCodec(), codecParams);
                    newBmp.Dispose();
                }

                FileInfo fi = new FileInfo(fileName);
                fi.Attributes = FileAttributes.Normal;
                fi.Delete();
            }
            Console.WriteLine();
            Console.WriteLine("Packing back...");
            Process.Start("7z", string.Format("a -tzip -mx0 \"{0}\" \"{1}\\*.jpg\"", name + ".cbz", tempDirName));
            return 0;
        }

        private static void DeleteFileSystemInfo(FileSystemInfo fsi)
        {
            fsi.Attributes = FileAttributes.Normal;
            var di = fsi as DirectoryInfo;

            if (di != null)
            {
                foreach (var dirInfo in di.GetFileSystemInfos())
                    DeleteFileSystemInfo(dirInfo);
            }

            fsi.Delete();
        }

        static int Main(string[] args)
        {
            Program p = new Program();
            return p.Run(args);
        }


        enum ImageOrientation
        {
            Portrait, Landscape
        }

        private static EncoderParameters GetEncoderParameters(long quality)
        {
            Encoder qualityEncoder = Encoder.Quality;
            EncoderParameter ratio = new EncoderParameter(qualityEncoder, quality);
            EncoderParameters codecParams = new EncoderParameters(1);
            codecParams.Param[0] = ratio;
            return codecParams;
        }

        private static ImageOrientation GetOrientation(Bitmap bmp)
        {
            //if the image is taller than its width, then it's portrait oriented
            return bmp.Height > bmp.Width ? ImageOrientation.Portrait : ImageOrientation.Landscape;
        }

        private static ImageCodecInfo GetCodec()
        {
            ImageCodecInfo[] iciCodecs = ImageCodecInfo.GetImageEncoders();
            for (int i = 0; i < iciCodecs.Length; i++)
            {
                // Until the one that we are interested in is found, which is image/jpeg
                if (iciCodecs[i].MimeType == "image/jpeg")
                {
                    return iciCodecs[i];
                }
            }
            return null;
        }
    }
}
