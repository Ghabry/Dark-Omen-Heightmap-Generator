using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace DarkOmen.HeightMapGenerator
{
    /// <summary>
    /// Algorithms for the TERR block
    /// </summary>
    public static class Terrgorithm
    {
        /// <summary>
        /// Compresses the passed Terr block.
        /// </summary>
        /// <param name="terr">Terr block to compress</param>
        /// <returns>Compressed version</returns>
        public static Terr Compress(this Terr oldTerr)
        {
            oldTerr = Decompress(oldTerr);
            
            Terr newTerr = new Terr();
            newTerr.Width = oldTerr.Width;
            newTerr.Height = oldTerr.Height;

            foreach (Terrblock oldBlock in oldTerr.Blocks)
            {
                compressBlock(oldTerr, newTerr, oldBlock, true);
            }

            foreach (Terrblock oldBlock in oldTerr.Blocks_hmap2)
            {
                compressBlock(oldTerr, newTerr, oldBlock, false);
            }
            
            return newTerr;
        }

        /// <summary>
        /// Simply search through all offsets and check if any of them is a
        /// 100% match, in this case reference the same offset block.
        /// Naive algorithm, but fast enough in practise...
        /// </summary>
        private static void compressBlock(Terr oldTerr, Terr newTerr, Terrblock block, bool firstHmap)
        {
            Terrblock newBlock = new Terrblock();
            newBlock.Minimum = block.Minimum;

            if (firstHmap)
            {
                newTerr.Blocks.Add(newBlock);
            }
            else
            {
                newTerr.Blocks_hmap2.Add(newBlock);
            }

            byte[] oldOffsets = oldTerr.Offsets[block.OffsetIndex];

            int count = 0;
            foreach (var newOffset in newTerr.Offsets) // outer
            {
                for (int i = 0; i < 64; ++i)
                {
                    if (newOffset[i] != oldOffsets[i])
                    {
                        goto next; // outer continue
                    }
                }

                // Block matches
                newBlock.OffsetIndex = count;
                return;

            next:
                ++count;
            }

            // No Block matches
            byte[] newOffsets = new byte[64];
            Array.Copy(oldOffsets, newOffsets, 64);
            newTerr.Offsets.Add(newOffsets);

            newBlock.OffsetIndex = newTerr.Offsets.Count - 1;
        }

        /// <summary>
        /// Decompresses the passed Terr block.
        /// </summary>
        /// <param name="terr">Terr block to decompress</param>
        /// <returns>Decompressed version</returns>
        public static Terr Decompress(this Terr oldTerr)
        {
            Terr newTerr = new Terr();
            newTerr.Width = oldTerr.Width;
            newTerr.Height = oldTerr.Height;

            foreach (Terrblock block in oldTerr.Blocks)
            {
                decompressBlock(oldTerr, newTerr, block, true);
            }
            foreach (Terrblock block in oldTerr.Blocks_hmap2)
            {
                decompressBlock(oldTerr, newTerr, block, false);
            }

            return newTerr;
        }

        private static void decompressBlock(Terr oldTerr, Terr newTerr, Terrblock block, bool firstHmap)
        {
            Terrblock newBlock = new Terrblock();
            newBlock.Minimum = block.Minimum;
            newBlock.OffsetIndex = newTerr.Blocks.Count + newTerr.Blocks_hmap2.Count;

            byte[] newOffsets = new byte[64];
            byte[] oldOffsets = oldTerr.Offsets[block.OffsetIndex];

            Array.Copy(oldOffsets, newOffsets, 64);

            if (firstHmap)
            {
                newTerr.Blocks.Add(newBlock);
            }
            else
            {
                newTerr.Blocks_hmap2.Add(newBlock);
            }
            
            newTerr.Offsets.Add(newOffsets);
        }
    }
}

// Trick to get Extension Methods in .net 2.0 working
namespace System.Runtime.CompilerServices
{
    public class ExtensionAttribute : Attribute { }
}
