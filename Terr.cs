using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HeightMapGenerator
{
    public class Terr
    {
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

            reader.ReadInt32(); // Size (not needed)
            Width = reader.ReadInt32();
            Height = reader.ReadInt32();

            int compressedBlockCount = reader.ReadInt32();
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

            if (compressedBlockCount * 64 != offsetCount)
            {
                throw new IOException("Compressed block count and offset count mismatch");
            }

            for (int i = 0; i < offsetCount / 64; ++i)
            {
                Offsets.Add(reader.ReadBytes(64));
            }

            // Should be end of TERR now...
        }

        /// <summary>
        /// Converts Terr object into binary blob Terr format
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter bw = new BinaryWriter(stream);
                bw.Write(Encoding.ASCII.GetBytes("TERR"));
                bw.Write(0); // Padding for Size
                bw.Write(Width);
                bw.Write(Height);

                bw.Write(Offsets.Count); // Compressed Count
                bw.Write(Blocks.Count); // Block count (Uncompressed Count)
                bw.Write(Offsets.Count * 16); // FIXME: This is a guess :/

                // 1st heightmap
                foreach (Terrblock block in Blocks)
                {
                    bw.Write(block.Minimum);
                    bw.Write(block.OffsetIndex * 64);
                }
                // 2nd heightmap
                foreach (Terrblock block in Blocks_hmap2)
                {
                    bw.Write(block.Minimum);
                    bw.Write(block.OffsetIndex * 64);
                }

                bw.Write(Offsets.Count * 64);

                foreach (byte[] offsets in Offsets)
                {
                    bw.Write(offsets);
                }

                byte[] newHeightmap = stream.ToArray();

                // Size is without TERR and size itself
                int size = newHeightmap.Length - 8;
                newHeightmap[4] = (byte)size;
                newHeightmap[5] = (byte)(size >> 8);
                newHeightmap[6] = (byte)(size >> 16);
                newHeightmap[7] = (byte)(size >> 24);

                return newHeightmap;
            }
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
