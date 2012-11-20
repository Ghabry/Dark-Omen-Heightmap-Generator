using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;
using CommandLine.Text;

namespace DarkOmen.HeightMapGenerator
{
    public class Options : CommandLineOptionsBase
    {
        [ValueList(typeof(List<string>), MaximumElements = 2)]    
        public IList<string> SourceFiles { get; set; }

        [Option("c", "compress", DefaultValue = false, HelpText = "Compresses the TERR block. " +
            "Default if a heightmap is replaced with an image.")]
        public bool Compress { get; set; }

        [Option("d", "decompress", DefaultValue = false, HelpText = "Decompresses the TERR block.")]
        public bool Decompress { get; set; }

        [Option("e", "extract", DefaultValue = false,
            HelpText = "Extracts the heightmap of the TERR block and writes it into a PNG-Image file. " +
                       "Specify output image file with -o. Must be used with -1 or -2. Can be used with -r. " +
                       /*"Combine with -t to get a TIFF-Image instead. */"Other arguments are ignored.")]
        public bool ExtractHeightmap { get; set; }

        [Option("i", "information", DefaultValue = false,
            HelpText = "Print information about the TERR block. Default if neither other options nor heightmap passed.")]
        public bool ShowInformation { get; set; }

        [Option("o", "outfile",
            HelpText = "Instead of overwriting the PRJ-File the output is written into the " +
                       "specified file. Use with -c, -d and/or -s.")]
        public String TargetFile { get; set; }

        [Option("r", "rob", DefaultValue = false,
            HelpText = "Uses Robs smoothing algorithm to get a better visual PNG heightmap output. Use with -e. " +
            "Do not reimport a heightmap created with this option.")]
        public bool RobSmoothing { get; set; }

        [Option("s", "swap", DefaultValue = false,
            HelpText = "Swaps the content of the first heightmap with the second and vice versa.")]
        public bool SwapHeightmaps { get; set; }

        /*[Option("t", "tiffout", DefaultValue = false, HelpText = "Extracted heightmap image is written as a 16-bit TIFF instead of a PNG-Image. Use with -e.")]
        public bool ExtractTiff { get; set; }*/

        [Option("1", "hmap1", DefaultValue = false, HelpText = "Modify first heightmap.")]
        public bool SelectHeightmap1 { get; set; }

        [Option("2", "hmap2", DefaultValue = false, HelpText = "Modify second heightmap.")]
        public bool SelectHeightmap2 { get; set; }

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
            help.AddPreOptionsLine("       HeightMapGenerator [PRJ-FILE] [HEIGHTMAP-IMAGE] [OPTION]...");

            help.AddPostOptionsLine("Per default both heightmaps are overwritten with the image, use -1 or -2 to change this.");
            //help.AddPostOptionsLine("Use a 16-Bit TIFF for best results.);
            help.AddPostOptionsLine("");
            help.AddPostOptionsLine("Example: HeightMapGenerator B1_01.PRJ heightmap.png");
            help.AddPostOptionsLine("Overwrites both heightmaps of B1_01.PRJ with the one in heightmap.png");
            help.AddPostOptionsLine("");
            help.AddPostOptionsLine("Enjoy!");

            help.AddOptions(this);

            return help;
        }
    }
}
