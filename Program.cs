using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace HeightMapGenerator
{
    class Program
    {
        static int roundedWidth;
        static int[] heightMapOffset = null;

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("PRJ Heightmap Generator v1.0 by Ghabry");
                Console.Error.WriteLine("Based on Robs Heightmap Parser Code");
                Console.Error.WriteLine();

                Console.Error.WriteLine("Usage:");
                Console.Error.WriteLine("HeightMapGenerator.exe [PRJ-FILE] [HEIGHTMAP-IMAGE]");
                Console.Error.WriteLine();

                Console.Error.WriteLine("The PRJ File gets overwritten!");
                Console.WriteLine();
                Console.Error.WriteLine("Enjoy!");
                Environment.Exit(2);
            }

            /// PRJ Stuff before TERR (including TERR string)
            byte[] prjBegin = null;
            /// PRJ after TERR
            byte[] prjEnd = null;
            /// Bitmap Width (from PRJ)
            int prjWidth = 0;
            /// Bitmap Height (from PRJ)
            int prjHeight = 0;

            // PRJ File Reading
            try
            {
                using (FileStream prjFileStream = new FileStream(args[0], FileMode.Open, FileAccess.Read))
                {
                    BinaryReader prjReader = new BinaryReader(prjFileStream);

                    // check that this is a PRJ file
                    string idString = ASCIIEncoding.ASCII.GetString(prjReader.ReadBytes(32));
                    if (idString != "Dark Omen Battle file 1.10      ")
                        throw new IOException("Not a PRJ File");

                    // ignore the BASE block
                    {
                        prjFileStream.Seek(4, SeekOrigin.Current);
                        int size = prjReader.ReadInt32();
                        prjFileStream.Seek(size, SeekOrigin.Current);
                    }

                    // ignore the WATR block
                    {
                        prjFileStream.Seek(4, SeekOrigin.Current);
                        int size = prjReader.ReadInt32();
                        prjFileStream.Seek(size, SeekOrigin.Current);
                    }

                    // ignore the FURN block
                    {
                        prjFileStream.Seek(4, SeekOrigin.Current);
                        int size = prjReader.ReadInt32();
                        int fixup = prjReader.ReadInt32();
                        prjFileStream.Seek(size + fixup * 4 - 4, SeekOrigin.Current);
                    }

                    // ignore the INST block
                    {
                        prjFileStream.Seek(4, SeekOrigin.Current);
                        int size = prjReader.ReadInt32();
                        int fixup = 8;
                        prjFileStream.Seek(size + fixup, SeekOrigin.Current);
                    }

                    // read the TERR block
                    {
                        string id = ASCIIEncoding.ASCII.GetString(prjReader.ReadBytes(4));
                        if (id != "TERR") throw new IOException("Could not find TERR block");
                        long pos = prjFileStream.Position;

                        prjFileStream.Seek(0, SeekOrigin.Begin);
                        prjBegin = prjReader.ReadBytes((int)pos);

                        int size = prjReader.ReadInt32();
                        prjWidth = prjReader.ReadInt32();
                        prjHeight = prjReader.ReadInt32();

                        prjFileStream.Seek(size - 8, SeekOrigin.Current);

                        // End of TERR block, read rest of file...
                        pos = prjFileStream.Position;
                        prjFileStream.Seek(0, SeekOrigin.End);
                        size = (int)(prjFileStream.Position - pos);
                        prjFileStream.Seek(pos, SeekOrigin.Begin);
                        prjEnd = prjReader.ReadBytes(size);
                    }
                }
            }
            catch (IOException e)
            {
                Console.Error.WriteLine("PRJ File Access Error: " + e.ToString());
                Environment.Exit(1);
            }


            byte[] newHeightmap = null;

            // Bitmap File Reading
            try
            {
                using (Bitmap bitmap = new Bitmap(args[1]))
                {
                    if (bitmap.Width != prjWidth || bitmap.Height != prjHeight)
                    {
                        throw new IOException("Heightmap measures wrong. Expected: " + prjWidth + "x" + prjHeight + " (Image: " + bitmap.Width + "x" + bitmap.Height + ")");
                    }
                    using (MemoryStream stream = new MemoryStream())
                    {
                        BinaryWriter bw = new BinaryWriter(stream);
                        //bw.Write(0); // Size
                        bw.Write(bitmap.Width);
                        bw.Write(bitmap.Height);

                        roundedWidth = (bitmap.Width + 7) & ~7;
                        int roundedHeight = (bitmap.Height + 7) & ~7;

                        int numMacroBlocks = roundedWidth / 8 * roundedHeight / 8;

                        bw.Write(numMacroBlocks); // Num Macro Blocks
                        bw.Write(numMacroBlocks); // Uncompressed Num of Macro Blocks (this code does not support compression)
                        bw.Write(numMacroBlocks * 16); // FIXME: This is a guess :/
                        heightMapOffset = new int[numMacroBlocks];
                        for (int i = 0; i < numMacroBlocks; ++i)
                        {
                            heightMapOffset[i] = i * 64;
                            bw.Write(0); // Minimum
                            bw.Write(i * 64);
                        }
                        for (int i = 0; i < numMacroBlocks; ++i)
                        {
                            bw.Write(0); // Minimum
                            bw.Write(i * 64);
                        }

                        bw.Write((numMacroBlocks) * 64); // Macro Blocks * 64

                        byte[] macroBlocks = new byte[(numMacroBlocks) * 64];
                        for (int i = 0; i < numMacroBlocks * 64; ++i)
                        {
                            int w = i % bitmap.Width;
                            int h = i / bitmap.Width;
                            if (w >= bitmap.Width || h >= bitmap.Height) continue;
                            Color clr = bitmap.GetPixel(w, h);
                            macroBlocks[getBlockPos(w, h, bitmap.Width)] = clr.R;
                            //Console.WriteLine(w + " " + h);
                        }
                        bw.Write(macroBlocks);
                        newHeightmap = stream.GetBuffer();
                    }
                }
            }
            catch (ArgumentException e)
            {
                Console.Error.WriteLine("Heightmap File Access Error: " + e.ToString());
                Environment.Exit(1);
            }
            catch (IOException e)
            {
                Console.Error.WriteLine("Heightmap File Access Error: " + e.ToString());
                Environment.Exit(1);
            }

            // PRJ File Writing (Now the fun begins)
            try
            {
                using (FileStream prjFileStream = new FileStream(args[0], FileMode.Open, FileAccess.Write))
                {
                    BinaryWriter prjWriter = new BinaryWriter(prjFileStream);
                    prjWriter.Write(prjBegin);
                    prjWriter.Write(newHeightmap.Length);
                    prjWriter.Write(newHeightmap);
                    prjWriter.Write(prjEnd);
                }
            }
            catch (IOException e)
            {
                Console.Error.WriteLine("PRJ File Access Error (Write): " + e.ToString());
                Environment.Exit(1);
            }
        }

        static int getBlockPos(int x, int y, int width)
        {
            int offAddress = ((y >> 3) * (roundedWidth >> 3)) + (x >> 3);

            int mblock = heightMapOffset[offAddress];
            int macroBlockAddress = (y % 8) * 8 + (x % 8);

            return mblock + macroBlockAddress;
        }
    }
}
