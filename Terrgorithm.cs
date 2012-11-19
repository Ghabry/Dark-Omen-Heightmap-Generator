using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace DarkOmen.HeightMapGenerator
{
    [Flags]
    public enum TargetHeightmaps
    {
        None = 0,
        FirstHeightmap = 1,
        SecondHeightmap = 2
    }

    /// <summary>
    /// Algorithms for the TERR block
    /// </summary>
    public static class Terrgorithm
    {
        /// <summary>
        /// Creates a Terr heightmap from a Bitmap
        /// </summary>
        /// <param name="oldTerr">Source terr</param>
        /// <param name="heightmap">Heightmap Bitmap</param>
        /// <param name="targetHmaps">Flags to determine which Heightmaps shall be changed</param>
        /// <returns></returns>
        public static Terr FromBitmap(this Terr oldTerr, Bitmap heightmap, TargetHeightmaps targetHmaps)
        {
            if (heightmap.Width != oldTerr.Width || heightmap.Height != oldTerr.Height)
            {
                throw new ArgumentException(
                    "Heightmap measures wrong. Expected: " + oldTerr.Width + "x" + oldTerr.Height +
                    " (Image: " + heightmap.Width + "x" + heightmap.Height + ")");
            }
            if ((targetHmaps & (TargetHeightmaps.FirstHeightmap | TargetHeightmaps.SecondHeightmap)) == TargetHeightmaps.None)
            {
                throw new ArgumentException("No valid TargetHeightmap specified");
            }

            oldTerr = oldTerr.Decompress();

            bool keepFirstHmap = (targetHmaps & TargetHeightmaps.FirstHeightmap) == TargetHeightmaps.None;
            bool keepSecondHmap = (targetHmaps & TargetHeightmaps.SecondHeightmap) == TargetHeightmaps.None;

            Terr newTerr = new Terr();
            newTerr.Width = oldTerr.Width;
            newTerr.Height = oldTerr.Height;

            if (keepFirstHmap)
            {
                oldTerr.CopyFirstHmap(newTerr);
            }

            // This scary loop iterates row by row over all 8x8 blocks
            for (int h = 0; h < heightmap.Height; h += 8)
            {
                for (int w = 0; w < heightmap.Width; w += 8)
                {
                    // Single 8x8 Block
                    Terrblock block = new Terrblock();
                    if (!keepFirstHmap)
                    {
                        newTerr.Blocks.Add(block);
                    }
                    if (!keepSecondHmap)
                    {
                        newTerr.Blocks_hmap2.Add(block);
                    }
                    int minHeight = int.MaxValue;
                    byte[] newOffsets = new byte[64];
                    for (int y = 0; y < 8; ++y)
                    {
                        if (h + y >= heightmap.Height)
                        {   // Image height not multiple of 8
                            //block.Height = y + 1;
                            break;
                        }
                        for (int x = 0; x < 8; ++x)
                        {
                            if (w + x >= heightmap.Width)
                            {   // Image width not multiple of 8
                                //block.Width = x + 1;
                                break;
                            }
                            int height = heightmap.GetPixel(w + x, h + y).R;
                            minHeight = Math.Min(minHeight, height);
                            newOffsets[x + y * 8] = (byte)height;
                        }
                    }
                    // Adjust offsets based on minimum
                    for (int i = 0; i < 64; ++i)
                    {
                        newOffsets[i] -= (byte)(minHeight);
                    }
                    // Scale to 65535 (65535 / 255 = 257)
                    minHeight *= 257;

                    newTerr.Offsets.Add(newOffsets);
                    block.Minimum = minHeight;
                    block.OffsetIndex = newTerr.Offsets.Count - 1;
                }
            }

            if (keepSecondHmap)
            {
                oldTerr.CopySecondHmap(newTerr);
            }

            return newTerr;
        }

        /// <summary>
        /// Takes the 1st heightmap of the old Terr item and writes it into the new.
        /// Assumes that old Terr is uncompressed and new Terr has no 2nd heightmap yet.
        /// </summary>
        /// <param name="oldTerr"></param>
        /// <param name="newTerr"></param>
        private static void CopyFirstHmap(this Terr oldTerr, Terr newTerr)
        {
            foreach (Terrblock block in oldTerr.Blocks)
            {
                Terrblock newBlock = new Terrblock(block);
                newTerr.Blocks.Add(newBlock);
            }
            // It's decompressed, so the first half of the offsets references the first hmap
            for (int i = 0; i < oldTerr.Blocks.Count; ++i)
            {
                byte[] newOffsets = new byte[64];
                Array.Copy(oldTerr.Offsets[i], newOffsets, 64);
                newTerr.Offsets.Add(newOffsets);
            }
        }

        /// <summary>
        /// Takes the 2nd heightmap of the old Terr item and writes it into the new.
        /// Assumes that old Terr is uncompressed and new Terr has no 2nd heightmap yet.
        /// </summary>
        /// <param name="oldTerr"></param>
        /// <param name="newTerr"></param>
        private static void CopySecondHmap(this Terr oldTerr, Terr newTerr)
        {
            foreach (Terrblock block in oldTerr.Blocks_hmap2)
            {
                Terrblock newBlock = new Terrblock(block);
                newTerr.Blocks_hmap2.Add(newBlock);
            }
            // It's decompressed, so the second half of the offsets references the second hmap
            for (int i = oldTerr.Blocks.Count; i < oldTerr.Offsets.Count; ++i)
            {
                byte[] newOffsets = new byte[64];
                Array.Copy(oldTerr.Offsets[i], newOffsets, 64);
                newTerr.Offsets.Add(newOffsets);
            }
        }

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

        /// <summary>
        /// Swaps the content of the two heightmaps in a terr block
        /// </summary>
        /// <param name="oldTerr">Source terr</param>
        /// <returns>New terr with swapped heightmaps</returns>
        public static Terr Swap(this Terr oldTerr)
        {
            Terr newTerr = new Terr();
            newTerr.Width = oldTerr.Width;
            newTerr.Height = oldTerr.Height;

            foreach (Terrblock block in oldTerr.Blocks)
            {
                Terrblock newBlock = new Terrblock(block);
                newTerr.Blocks_hmap2.Add(newBlock);
            }
            foreach (Terrblock block in oldTerr.Blocks_hmap2)
            {
                Terrblock newBlock = new Terrblock(block);
                newTerr.Blocks.Add(newBlock);
            }

            foreach (byte[] oldOffsets in oldTerr.Offsets)
            {
                byte[] newOffsets = new byte[64];
                Array.Copy(oldOffsets, newOffsets, 64);
                newTerr.Offsets.Add(newOffsets);
            }

            return newTerr;
        }
    }
}

// Trick to get Extension Methods in .net 2.0 working
namespace System.Runtime.CompilerServices
{
    public class ExtensionAttribute : Attribute { }
}
