using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security;
using System.Text;
using CommandLine;

namespace DarkOmen.HeightMapGenerator
{
    public static class Program
    {
        private static string prjFile = null;
        private static string imageFile = null;

        public static int Main(string[] args)
        {
            var options = new Options();

            int retCode = 1;
            if (CommandLineParser.Default.ParseArguments(args, options))
            {
                if (options.SourceFiles.Count < 1 || options.SourceFiles.Count > 3)
                {
                    Console.Error.WriteLine(options.GetUsage());
                    return retCode;
                }
                prjFile = options.SourceFiles[0];

                try
                {
                    if (options.ExtractHeightmap)
                    {
                        retCode = DoImageExtraction(options);
                    }
                    else
                    {
                        retCode = DoDefault(options);
                    }
                }
                catch (Exception e)
                {
                    if (e is ArgumentException ||
                        e is NotSupportedException ||
                        e is SecurityException ||
                        e is IOException ||
                        e is UnauthorizedAccessException)
                    {
                        Console.Error.Write("Error: ");
                        Console.Error.WriteLine(e.Message);
                        return 2;
                    }
                    else
                    {   // Unexpected Exception -> Crash
                        throw;
                    }
                }
            }
            return retCode;
        }

        /// <summary>
        /// Program flow for everything but Image Extraction
        /// </summary>
        private static int DoDefault(Options options)
        {
            if (options.SourceFiles.Count == 2)
            {
                imageFile = options.SourceFiles[1];
            }

            if (options.Decompress && options.Compress)
            {
                Console.Error.WriteLine("-c and -d are incompatible.");
                return 1;
            }

            if (imageFile != null)
            {
                if (!options.Decompress && !options.Compress)
                {
                    options.Compress = true;
                }
            }

            // If no compression set and no heightmap only display statistics
            if (!options.SwapHeightmaps && !options.Decompress && !options.Compress)
            {
                if (imageFile != null)
                {
                    options.Compress = true;
                }
                else
                {
                    options.ShowInformation = true;
                }
            }

            // Overwrite both heightmaps if none selected
            if (!options.SelectHeightmap1 && !options.SelectHeightmap2)
            {
                options.SelectHeightmap1 = true;
                options.SelectHeightmap2 = true;
            }

            // All arguments correct
            SimplePrj prj = new SimplePrj(prjFile);

            // Overwrite file if no output file specified
            if (options.TargetFile == null)
            {
                options.TargetFile = prjFile;
            }

            DebugPrintOptions(options);

            // Replace heightmaps if we have a Image File
            if (imageFile != null)
            {
                using (Bitmap bmp = new Bitmap(imageFile))
                {
                    TargetHeightmaps t = TargetHeightmaps.FirstHeightmap | TargetHeightmaps.SecondHeightmap;
                    if (options.SelectHeightmap1 && !options.SelectHeightmap2)
                    {
                        t = TargetHeightmaps.FirstHeightmap;
                    }
                    else if (!options.SelectHeightmap1 && options.SelectHeightmap2)
                    {
                        t = TargetHeightmaps.SecondHeightmap;
                    }
                    prj.Terr = prj.Terr.FromBitmap(bmp, t);
                }
            }

            if (options.SwapHeightmaps)
            {
                prj.Terr = prj.Terr.Swap();
            }

            if (options.Decompress)
            {
                prj.Terr = prj.Terr.Decompress();
            }
            else if (options.Compress)
            {
                prj.Terr = prj.Terr.Compress();
            }

            // Edit operations should be saved of course
            // Image is implicit because it sets Decompress
            if (options.SwapHeightmaps ||
                options.Decompress ||
                options.Compress)
            {
                prj.Save(options.TargetFile);
            }

            if (options.ShowInformation)
            {
                PrintStatistic(prj.Terr);
            }
            return 0;

        }

        /// <summary>
        /// Program flow for Image Extraction of Heightmap
        /// </summary>
        private static int DoImageExtraction(Options options)
        {
            // Need a target file
            if (options.TargetFile == null)
            {
                Console.Error.WriteLine("No target file (-o) provided for image extraction.");
                return 1;
            }

            // Need one heightmap
            TargetHeightmaps t;
            if (options.SelectHeightmap1 && options.SelectHeightmap2)
            {
                Console.Error.WriteLine("Only one heightmap can be extracted at once.");
                return 1;
            }
            else if (options.SelectHeightmap1 && !options.SelectHeightmap2)
            {
                t = TargetHeightmaps.FirstHeightmap;
            }
            else if (!options.SelectHeightmap1 && options.SelectHeightmap2)
            {
                t = TargetHeightmaps.SecondHeightmap;
            }
            else
            {
                Console.Error.WriteLine("No heightmap (-1 or -2) specified.");
                return 1;
            }

            // Argument list correct
            SimplePrj prj = new SimplePrj(prjFile);

            Bitmap image = prj.Terr.ToBitmap(t,
                options.RobSmoothing ? SmoothingAlgorithm.Rob : SmoothingAlgorithm.Default);
            image.Save(options.TargetFile, System.Drawing.Imaging.ImageFormat.Png);

            return 0;
        }

        private static void PrintStatistic(Terr terr)
        {
            // Map dimension
            Console.Error.WriteLine("Dimension: " + terr.Width + "x" + terr.Height);

            // Min/Max Heightmap Value (1st hmap)
            int min = int.MaxValue;
            int max = int.MinValue;
            foreach (Terrblock block in terr.BlocksHmap1)
            {
                min = Math.Min(min, block.Minimum);
                max = Math.Max(min, block.Minimum);
            }
            Console.Error.WriteLine("Min/Max (Hmap1): " + min + "/" + max);

            // Min/Max Heightmap Value (2nd hmap)
            min = int.MaxValue;
            max = int.MinValue;
            foreach (Terrblock block in terr.BlocksHmap2)
            {
                min = Math.Min(min, block.Minimum);
                max = Math.Max(min, block.Minimum);
            }
            Console.Error.WriteLine("Min/Max (Hmap2): " + min + "/" + max);

            // Block count (Macro and Micro blocks) + Compression ratio
            Console.Error.WriteLine("Blocks: " + terr.BlocksHmap1.Count * 2 + "/" + terr.Offsets.Count);
            String ratio = (100 - (float)terr.Offsets.Count / (terr.BlocksHmap1.Count * 2) * 100).ToString("0.00");
            Console.Error.WriteLine("Compression: " + ratio + "%");
        }

        private static void DebugPrintOptions(Options options)
        {
#if DEBUG
            Console.Error.WriteLine("Prj File: " + prjFile);
            Console.Error.WriteLine("Hmap File: " + imageFile);
            Console.Error.WriteLine("Outfile: " + options.TargetFile);
            Console.Error.WriteLine("Compress: " + options.Compress);
            Console.Error.WriteLine("Decompress: " + options.Decompress);
            Console.Error.WriteLine("Information: " + options.ShowInformation);
            Console.Error.WriteLine("1st Heightmap: " + options.SelectHeightmap1);
            Console.Error.WriteLine("2nd Heightmap: " + options.SelectHeightmap2);
            Console.Error.WriteLine("Swap Heightmaps: " + options.SwapHeightmaps);
#endif
        }
    }
}
