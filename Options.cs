using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;
using CommandLine.Text;

namespace DarkOmen.HeightMapGenerator
{
    public class Options : CommandLineOptionsBase
    {
        [ValueList(typeof(List<string>), MaximumElements = -1)]    
        public IList<string> SourcePrjFile { get; set; }

        [Option("i", "heightmap",
            HelpText = "Replaces the heightmap in the TERR block with the specified image. " +
                       //"Use a 16-Bit TIFF for best results. " + 
                       "Per default both heightmaps are overwritten, use -1 or -2 to change this.")]
        public String Heightmap { get; set; }

        [Option("c", "compress", DefaultValue = false, HelpText = "Compresses the TERR block. Default for -i.")]
        public bool Compress { get; set; }

        [Option("d", "decompress", DefaultValue = false, HelpText = "Decompresses the TERR block.")]
        public bool Decompress { get; set; }

        [Option("o", "outfile",
            HelpText = "Instead of overwriting the PRJ-File the output is written into the " +
                       "specified file. Can be used with -i, -d and -c.")]
        public String TargetFile { get; set; }

        [Option("s", "statistic", DefaultValue = false,
            HelpText = "Print statistics about the TERR block. Default if no other options passed.")]
        public bool ShowStatistics { get; set; }

        [Option("1", "hmap1", DefaultValue = false, HelpText = "Select first heightmap.")]
        public bool SelectHeightmap1 { get; set; }

        [Option("2", "hmap2", DefaultValue = false, HelpText = "Select second heightmap.")]
        public bool SelectHeightmap2 { get; set; }

        /*[Option("e", "extract", DefaultValue = false,
            HelpText = "Extracts the heightmap of the TERR block and writes it into a Bitmap file. " +
                       "Specify output image file with -o. Must be used with -1 or -2.")]
        public bool ExtractHeightmap { get; set; }

        [Option("t", "tiffout", DefaultValue = false, HelpText = "Extracted heightmap image is written as a 16-bit TIFF instead of a Bitmap. Use with -e.")]
        public bool ExtractTiff { get; set; }*/

        [HelpOption("h", "help")]
        public string GetUsage()
        {
            var help = new HelpText
            {
                Heading = new HeadingInfo(ThisAssembly.Product, "v" + ThisAssembly.InformationalVersion),
                Copyright = new CopyrightInfo(ThisAssembly.Author, ThisAssembly.Year),
                AdditionalNewLineAfterOption = false,
                AddDashesToOption = true
            };

            help.AddPreOptionsLine("\nBased on Robs Heightmap Parser Code");
            help.AddPreOptionsLine("");
            help.AddPreOptionsLine("Usage: HeightMapGenerator [PRJ-FILE] [OPTION]...");

            help.AddPostOptionsLine("Example: HeightMapGenerator B1_01.PRJ -i heightmap.png");
            help.AddPostOptionsLine("Overwrites both heightmaps of B1_01.PRJ with the one in heightmap.png");
            help.AddPostOptionsLine("");
            help.AddPostOptionsLine("Enjoy!");

            help.AddOptions(this);

            return help;
        }
    }
}
