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
        public static void Main(string[] args)
        {
            var options = new Options();

            if (CommandLineParser.Default.ParseArguments(args, options))
            {
                if (options.SourcePrjFile.Count != 1)
                {
                    Console.Error.WriteLine(options.GetUsage());
                    Environment.Exit(1);
                }

                if (options.Decompress && options.Compress)
                {
                    Console.Error.WriteLine("-c and -d are incompatible.");
                    Environment.Exit(1);
                }

                // If no option set only display statistics
                if (!options.Decompress && !options.Compress)
                {
                    options.ShowStatistics = true;
                }

                try
                {
                    string prjFile = options.SourcePrjFile[0];
                    SimplePrj prj = new SimplePrj(prjFile);

                    // Overwrite file is no output file specified
                    if (options.TargetFile == null)
                    {
                        options.TargetFile = prjFile;
                    }

                    if (options.Heightmap != null)
                    {
                        throw new InvalidOperationException("Heightmap code not implemented yet");
                    }

                    if (options.Decompress)
                    {
                        prj.Terr = prj.Terr.Decompress();
                        prj.Save(options.TargetFile);
                    }
                    else if (options.Compress)
                    {
                        prj.Terr = prj.Terr.Compress();
                        prj.Save(options.TargetFile);
                    }

                    if (options.ShowStatistics)
                    {
                        PrintStatistic(prj.Terr);
                    }
                }
                catch (Exception e)
                {
                    if (e is ArgumentException ||
                        e is NotSupportedException ||
                        e is SecurityException ||
                        e is IOException)
                    {
                        Console.Error.WriteLine("HeightMapGenerator encountered a problem:");
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
            Console.Error.WriteLine("Dimension: " + terr.Width + "x" + terr.Height);
            Console.Error.WriteLine("Blocks: " + terr.Blocks.Count * 2 + "/" + terr.Offsets.Count);
            String ratio = (100 - (float)terr.Offsets.Count / (terr.Blocks.Count * 2) * 100).ToString("0.00");
            Console.Error.WriteLine("Compression: " + ratio + "%");
        }
    }
}
