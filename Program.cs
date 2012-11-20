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

        public static void Main(string[] args)
        {
            var options = new Options();

            if (CommandLineParser.Default.ParseArguments(args, options))
            {
                if (options.SourceFiles.Count < 1 || options.SourceFiles.Count > 3)
                {
                    Console.Error.WriteLine(options.GetUsage());
                    Environment.Exit(1);
                }
                prjFile = args[0];
                if (options.SourceFiles.Count == 2)
                {
                    imageFile = args[1];
                }

                if (options.Decompress && options.Compress)
                {
                    Console.Error.WriteLine("-c and -d are incompatible.");
                    Environment.Exit(1);
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

                try
                {
                    SimplePrj prj = new SimplePrj(prjFile);

                    // Overwrite file is no output file specified
                    if (options.TargetFile == null)
                    {
                        options.TargetFile = prjFile;
                    }

                    DebugPrintOptions(options);

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
                        Environment.Exit(2);
                    }
                    else
                    {   // Crash
                        throw;
                    }
                }
                Environment.Exit(0);
            }

            Environment.Exit(1);
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
