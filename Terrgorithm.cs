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

    public enum SmoothingAlgorithm
    {
        Default = 0,
        Rob = 1
    }

    /// <summary>
    /// Algorithms for the TERR block
    /// </summary>
    public static class Terrgorithm
    {
        // Scaling factors for Robs smoothing algorithm
        public static readonly double HEIGHTMAP_SCALE = 1024.0;
        public static readonly int MACROBLOCK_SCALE = 128;

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
                        newTerr.BlocksHmap1.Add(block);
                    }
                    if (!keepSecondHmap)
                    {
                        newTerr.BlocksHmap2.Add(block);
                    }
                    int minHeight = int.MaxValue;
                    byte[] newOffsets = new byte[64];
                    for (int y = 0; y < 8; ++y)
                    {
                        if (h + y >= heightmap.Height)
                        {   // Image height not multiple of 8
                            break;
                        }
                        for (int x = 0; x < 8; ++x)
                        {
                            if (w + x >= heightmap.Width)
                            {   // Image width not multiple of 8
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

        public static Bitmap ToBitmap(this Terr oldTerr, TargetHeightmaps targetHmaps, SmoothingAlgorithm salg)
        {
            bool firstHeightmap = (targetHmaps & TargetHeightmaps.FirstHeightmap) == TargetHeightmaps.FirstHeightmap;
            bool secondHeightmap = (targetHmaps & TargetHeightmaps.SecondHeightmap) == TargetHeightmaps.SecondHeightmap;

            if (firstHeightmap && secondHeightmap)
            {
                throw new ArgumentException("Both TargetHeightmaps specified");
            }
            else if (!firstHeightmap && !secondHeightmap)
            {
                throw new ArgumentException("No valid TargetHeightmaps specified");
            }

            IList<Terrblock> blocks;
            if (firstHeightmap)
            {
                blocks = oldTerr.BlocksHmap1;
            }
            else
            {
                blocks = oldTerr.BlocksHmap2;
            }

            Bitmap heightmap = new Bitmap(oldTerr.Width, oldTerr.Height);

            // This scary loop iterates row by row over all 8x8 blocks
            int row = 0;
            int col = 0;
            for (int i = 0; i < blocks.Count; ++i)
            {
                Terrblock block = blocks[i];
                byte[] offsets = oldTerr.Offsets[block.OffsetIndex];

                ++col;
                if (col * 8 >= heightmap.Width)
                {
                    col = 0;
                    row++;
                }

                // Single 8x8 Block
                for (int y = 0; y < 8; ++y)
                {
                    int targetY = row * 8 + y;

                    if (targetY >= heightmap.Height)
                    {
                        break;
                    }
                    for (int x = 0; x < 8; ++x)
                    {
                        int targetX = col * 8 + x;

                        if (targetX >= heightmap.Width)
                        {
                            break;
                        }

                        Color color;
                        if (salg == SmoothingAlgorithm.Default)
                        {
                            // Downscale from 65535 Minimum to 255 color space
                            int c = offsets[x + y * 8] + block.Minimum / 257;
                            color = Color.FromArgb(c, c, c);
                        }
                        else if (salg == SmoothingAlgorithm.Rob)
                        {
                            // Robs heightmap smoothing code
                            int c = 20 + ((offsets[x + y * 8] * 128 + block.Minimum) / 1024) * 2;
                            color = Color.FromArgb(c, c, c);
                        }
                        else
                        {
                            throw new ArgumentException("Unsupported Smoothing Algorithm");
                        }

                        heightmap.SetPixel(targetX, targetY, color);
                    }
                }
            }

            return heightmap;
        }

        /// <summary>
        /// Takes the 1st heightmap of the old Terr item and writes it into the new.
        /// Assumes that old Terr is uncompressed and new Terr has no 2nd heightmap yet.
        /// </summary>
        /// <param name="oldTerr"></param>
        /// <param name="newTerr"></param>
        private static void CopyFirstHmap(this Terr oldTerr, Terr newTerr)
        {
            foreach (Terrblock block in oldTerr.BlocksHmap1)
            {
                Terrblock newBlock = new Terrblock(block);
                newTerr.BlocksHmap1.Add(newBlock);
            }
            // It's decompressed, so the first half of the offsets references the first hmap
            for (int i = 0; i < oldTerr.BlocksHmap1.Count; ++i)
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
            foreach (Terrblock block in oldTerr.BlocksHmap2)
            {
                Terrblock newBlock = new Terrblock(block);
                newTerr.BlocksHmap2.Add(newBlock);
            }
            // It's decompressed, so the second half of the offsets references the second hmap
            for (int i = oldTerr.BlocksHmap1.Count; i < oldTerr.Offsets.Count; ++i)
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

            foreach (Terrblock oldBlock in oldTerr.BlocksHmap1)
            {
                compressBlock(oldTerr, newTerr, oldBlock, true);
            }

            foreach (Terrblock oldBlock in oldTerr.BlocksHmap2)
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
                newTerr.BlocksHmap1.Add(newBlock);
            }
            else
            {
                newTerr.BlocksHmap2.Add(newBlock);
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

            foreach (Terrblock block in oldTerr.BlocksHmap1)
            {
                decompressBlock(oldTerr, newTerr, block, true);
            }
            foreach (Terrblock block in oldTerr.BlocksHmap2)
            {
                decompressBlock(oldTerr, newTerr, block, false);
            }

            return newTerr;
        }

        private static void decompressBlock(Terr oldTerr, Terr newTerr, Terrblock block, bool firstHmap)
        {
            Terrblock newBlock = new Terrblock();
            newBlock.Minimum = block.Minimum;
            newBlock.OffsetIndex = newTerr.BlocksHmap1.Count + newTerr.BlocksHmap2.Count;

            byte[] newOffsets = new byte[64];
            byte[] oldOffsets = oldTerr.Offsets[block.OffsetIndex];

            Array.Copy(oldOffsets, newOffsets, 64);

            if (firstHmap)
            {
                newTerr.BlocksHmap1.Add(newBlock);
            }
            else
            {
                newTerr.BlocksHmap2.Add(newBlock);
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

            foreach (Terrblock block in oldTerr.BlocksHmap1)
            {
                Terrblock newBlock = new Terrblock(block);
                newTerr.BlocksHmap2.Add(newBlock);
            }
            foreach (Terrblock block in oldTerr.BlocksHmap2)
            {
                Terrblock newBlock = new Terrblock(block);
                newTerr.BlocksHmap1.Add(newBlock);
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
