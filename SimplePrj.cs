using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DarkOmen.HeightMapGenerator
{
    /// <summary>
    /// Extreme simple class that contains the PRJ Binary stuff and a
    /// reference to the Terr class.
    /// </summary>
    public class SimplePrj
    {
        private byte[] prjBegin;
        private byte[] prjEnd;
        public Terr Terr { get; set; }

        /// <summary>
        /// Reads from the PRJ file
        /// </summary>
        /// <param name="filename">PRJ file</param>
        public SimplePrj(String filename)
        {
            using (FileStream prjFileStream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                BinaryReader prjReader = new BinaryReader(prjFileStream);
                Parse(prjReader);
            }
        }

        /// <summary>
        /// Reads from a stream pointing at the begin of a PRJ file
        /// </summary>
        /// <param name="reader">PRJ file stream</param>
        public SimplePrj(BinaryReader reader)
        {
            Parse(reader);
        }

        /// <summary>
        /// Does the dirty work
        /// </summary>
        /// <param name="reader"></param>
        private void Parse(BinaryReader prjReader) 
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter prjBegin = new BinaryWriter(ms);

                // check that this is a PRJ file
                string idString = ASCIIEncoding.ASCII.GetString(prjReader.ReadBytes(32));
                string header = "Dark Omen Battle file 1.10      ";
                if (idString != header)
                    throw new IOException("Not a PRJ File");
                prjBegin.Write(Encoding.ASCII.GetBytes(header));

                // ignore the BASE block
                prjBegin.Write(prjReader.ReadInt32());
                int size = prjReader.ReadInt32();
                prjBegin.Write(size);
                prjBegin.Write(prjReader.ReadBytes(size));

                // ignore the WATR block
                prjBegin.Write(prjReader.ReadInt32());
                size = prjReader.ReadInt32();
                prjBegin.Write(size);
                prjBegin.Write(prjReader.ReadBytes(size));

                // ignore the FURN block
                prjBegin.Write(prjReader.ReadInt32());
                size = prjReader.ReadInt32();
                int fixup = prjReader.ReadInt32();
                prjBegin.Write(size);
                prjBegin.Write(fixup);
                prjBegin.Write(prjReader.ReadBytes(size + fixup * 4 - 4));

                // ignore the INST block
                prjBegin.Write(prjReader.ReadInt32());
                size = prjReader.ReadInt32();
                fixup = 8;
                prjBegin.Write(size);
                prjBegin.Write(prjReader.ReadBytes(size + fixup));

                this.prjBegin = ms.ToArray();
            }

            // Read the TERR Block
            Terr = new Terr(prjReader);

            // Read until EOF
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter prjEnd = new BinaryWriter(ms);

                try
                {
                    for (;;)
                    {
                        prjEnd.Write(prjReader.ReadByte());
                    }
                }
                catch (EndOfStreamException)
                {
                    // Not very smart
                }

                this.prjEnd = ms.ToArray();
            }
        }

        /// <summary>
        /// Writes the PRJ File into a file
        /// </summary>
        /// <param name="writer">Target file</param>
        public void Save(String file)
        {
            using (FileStream prjFileStream = new FileStream(file, FileMode.Create, FileAccess.Write))
            {
                BinaryWriter prjWriter = new BinaryWriter(prjFileStream);
                prjWriter.Write(ToArray());
            }
        }

        /// <summary>
        /// Converts SimplePrj object into binary blob PRJ format
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter bw = new BinaryWriter(stream);
                bw.Write(prjBegin);
                bw.Write(Terr.ToArray());
                bw.Write(prjEnd);
                return stream.ToArray();
            }
        }
    }
}
