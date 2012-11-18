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
        public int UncompressedBlockCount { get; set; }
        public int CompressedBlockCount { get; set; }

        /// <summary>
        /// List of large block entries
        /// </summary>
        public IList<Terrblock> blocks { get; private set; }

        /// <summary>
        /// List of offsets for 8x8 block (height offset for each block
        /// based on minimum height)
        /// </summary>
        public IList<byte[]> offsets { get; private set; }

        // Same for 2nd heightmap
        public IList<Terrblock> blocks_hmap2 { get; private set; }

        private Terr()
        {
            blocks = new List<Terrblock>();
            offsets = new List<byte[]>();
            blocks_hmap2 = new List<Terrblock>();
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

            CompressedBlockCount = reader.ReadInt32();
            UncompressedBlockCount = reader.ReadInt32();
            reader.ReadInt32(); // Block count * 16? (purpose unknown)

            // 1st Heightmap
            for (int i = 0; i < UncompressedBlockCount; ++i)
            {
                Terrblock block = new Terrblock();
                block.Minimum = reader.ReadInt32();

                block.OffsetIndex = reader.ReadInt32();
                if (block.OffsetIndex % 64 != 0)
                {
                    throw new IOException("Offset index not a multiple of 64");
                }
                block.OffsetIndex /= 64;
                blocks.Add(block);
            }

            // 2nd Heightmap
            for (int i = 0; i < UncompressedBlockCount; ++i)
            {
                Terrblock block = new Terrblock();
                block.Minimum = reader.ReadInt32();

                block.OffsetIndex = reader.ReadInt32();
                if (block.OffsetIndex % 64 != 0)
                {
                    throw new IOException("Offset index not a multiple of 64");
                }
                block.OffsetIndex /= 64;
                blocks_hmap2.Add(block);
            }

            int offsetCount = reader.ReadInt32();

            if (CompressedBlockCount * 64 != offsetCount)
            {
                throw new IOException("Compressed block count and offset count mismatch");
            }

            for (int i = 0; i < CompressedBlockCount; ++i)
            {
                offsets.Add(reader.ReadBytes(64));
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
