using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HeightMapGenerator
{
    public class Terr
    {
        public int Size { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        /// <summary>
        /// List of large block entries
        /// </summary>
        public IList<Terrblock> Blocks { get; private set; }

        /// <summary>
        /// List of offsets for 8x8 block (height offset for each block
        /// based on minimum height)
        /// </summary>
        public IList<byte[]> Offsets { get; private set; }

        // Same for 2nd heightmap
        public IList<Terrblock> Blocks_hmap2 { get; private set; }

        public Terr()
        {
            Blocks = new List<Terrblock>();
            Offsets = new List<byte[]>();
            Blocks_hmap2 = new List<Terrblock>();
        }

        /// <summary>
        /// Constructs a Terr-Object by reading the PRJ File.
        /// Read pointer must be before TERR string.
        /// After returning the new stream pos is behind the TERR block.
        /// </summary>
        /// <param name="reader">Prj file stream</param>
        public Terr(BinaryReader reader) : this()
        {
            string id = ASCIIEncoding.ASCII.GetString(reader.ReadBytes(4));
            if (id != "TERR") throw new IOException("Could not find TERR block");

            Size = reader.ReadInt32();
            Width = reader.ReadInt32();
            Height = reader.ReadInt32();

            reader.ReadInt32(); // Compressed block count (not needed)
            int uncompressedBlockCount = reader.ReadInt32();
            reader.ReadInt32(); // Block count * 16? (purpose unknown)

            // 1st Heightmap
            for (int i = 0; i < uncompressedBlockCount; ++i)
            {
                Terrblock block = new Terrblock();
                block.Minimum = reader.ReadInt32();

                block.OffsetIndex = reader.ReadInt32();
                if (block.OffsetIndex % 64 != 0)
                {
                    throw new IOException("Offset index not a multiple of 64");
                }
                block.OffsetIndex /= 64;
                Blocks.Add(block);
            }

            // 2nd Heightmap
            for (int i = 0; i < uncompressedBlockCount; ++i)
            {
                Terrblock block = new Terrblock();
                block.Minimum = reader.ReadInt32();

                block.OffsetIndex = reader.ReadInt32();
                if (block.OffsetIndex % 64 != 0)
                {
                    throw new IOException("Offset index not a multiple of 64");
                }
                block.OffsetIndex /= 64;
                Blocks_hmap2.Add(block);
            }

            int offsetCount = reader.ReadInt32();

            if (offsetCount % 64 != 0)
            {
                throw new IOException("Offset count not a multiple of 64");
            }

            for (int i = 0; i < offsetCount / 64; ++i)
            {
                Offsets.Add(reader.ReadBytes(64));
            }

            // Should be end of TERR now...
        }
    }

    /// <summary>
    /// A single 8x8 block.
    /// Contains the minimum height of the block and reference in offset area
    /// </summary>
    public class Terrblock
    {
        public int Minimum { get; set; }
        public int OffsetIndex { get; set; }

        public Terrblock()
        {
        }
    }
}
