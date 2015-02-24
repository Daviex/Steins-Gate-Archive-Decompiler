// Copyright (c) 2015 Davide Iuffrida
// License: Academic Free License ("AFL") v. 3.0
// AFL License page: http://opensource.org/licenses/AFL-3.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Steins_Gate_Translation_Tool
{
    public class Program
    {
        private static byte[] key = Encoding.ASCII.GetBytes("BUCKTICK");
        private static int keyLen = 8;
        private static BinaryReader br;

        static void Main(string[] args)
        {
            Console.WriteLine("###################################");
            Console.WriteLine("#     NPA Archives Decompiler     #");
            Console.WriteLine("###################################");
            Console.WriteLine("#   Original Tool Made by Nagato  #");
            Console.WriteLine("###################################");
            Console.WriteLine("#     New Tool Made by Daviex     #");
            Console.WriteLine("###################################");
            Console.WriteLine("#   Italian Steins;Gate VN Team   #");
            Console.WriteLine("###################################");
            Console.WriteLine("#        Version 1.2 Alpha        #");
            Console.WriteLine("###################################");
            Console.WriteLine("#            CodeName:            #");
            Console.WriteLine("###################################");
            Console.WriteLine("#         El Psy Congrue          #");
            Console.WriteLine("###################################");
            Console.WriteLine();
            Console.WriteLine("Press any key to start...");
            Console.ReadLine();

            if (args.Length == 0)
            {
                Console.WriteLine("You should move the file .npa on me to make it works!");
                Console.WriteLine("Press a button to close the program.");
                Console.ReadLine();
                Environment.Exit(0);
            }
            
            string originalFile = String.Empty;

            originalFile = args[0].Substring(args[0].LastIndexOf('\\')+1);
            br = new BinaryReader(File.OpenRead(originalFile));

            Console.WriteLine("I'm reading your file...");

            byte[] header;
            uint headerLen;

            headerLen = br.ReadUInt32();
            header = br.ReadBytes((int)headerLen);

            Console.WriteLine("I'm decrypting the header...");

            ScrambleKey(keyLen);
            DecryptBuffer(keyLen, ref header, (int)headerLen);

            Console.WriteLine("Gnam Gnam, now i want more data!"); ;
            ParseHeader(header, originalFile);

            Console.WriteLine();
            Console.WriteLine("I ended, but, remember, the Agency still watch you!");
            Console.WriteLine("Press a button to close the program.");
            Console.ReadLine();
        }

        static public void ScrambleKey(int keylen)
        {
            for (int i = 0; i < keylen; i++)
                key[i] = (byte)~key[i];
        }

        static public void DecryptBuffer(int keylen, ref byte[] header, int headerlen)
        {
            for (int i = 0; i < headerlen; i++)
                header[i] ^= key[i % keylen];

        #if DEBUG
            BinaryWriter bw = new BinaryWriter(File.Create("derypted.npa"));
            bw.Write(BitConverter.GetBytes(headerlen));
            bw.Write(header);
            bw.Flush();
            bw.Close();
        #endif

        }

        static public void ParseHeader(byte[] header, string originalFile)
        {
            StreamWriter sw = new StreamWriter(File.Create(originalFile + ".log"));

            int bufferOffset = 4;

            int fileCount = BitConverter.ToInt32(header, 0);

            int fileNameLen, size, offset, unk;
            string fileName = String.Empty; 

            for (int i = 0; i < fileCount; i++)
            {
                fileNameLen = BitConverter.ToInt32(header, bufferOffset);
                size        = BitConverter.ToInt32(header, bufferOffset+fileNameLen+4);
                offset      = BitConverter.ToInt32(header, bufferOffset+fileNameLen+8);
                unk         = BitConverter.ToInt32(header, bufferOffset+fileNameLen+12);

                fileName = Encoding.Unicode.GetString(header, bufferOffset+4, fileNameLen);

                Console.WriteLine("[+]" + fileName + "\tOffset[" + offset.ToString("X") + "]\tSize[" + size.ToString("X") + "]");
                sw.WriteLine("[+]" + fileName + "\tOffset[" + offset.ToString("X") + "]\tSize[" + size.ToString("X") + "]");

                ExtractData(fileName, size, offset);

                bufferOffset += fileNameLen + 16;
            }
            sw.Flush();
            sw.Close();
        }

        static public void ExtractData(string path, int size, int offset)
        {
            int slashPos = path.LastIndexOf('/');            
            string folder = path.Substring(0, slashPos);

            Directory.CreateDirectory(folder);

            BinaryWriter bw = new BinaryWriter(File.Create(path));
            byte[] buffer = new byte[size];

            //long oldPos = br.BaseStream.Position; Useless?

            br.BaseStream.Position = offset;
            buffer = br.ReadBytes(size);

            DecryptBuffer(keyLen, ref buffer, (int)size);

            bw.Write(buffer);
            bw.Flush();
            bw.Close();
        }
    }
}
