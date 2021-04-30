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
    static void Main(string[] args)
    {
      Console.WriteLine(
          @"
                      
                      ###################################
                      #     SG  Archives Decompiler     #
                      ###################################
                      #         Made by Daviex          #
                      ###################################
                      #           Version 2.0           #
                      ###################################
                      #            Codename:            #
                      ###################################
                      #         El Psy Congroo          #
                      ###################################
                      
                           Press any key to start...    
                                                         ");
      Console.ReadLine();

      if (args.Length == 0)
      {
        Console.WriteLine("You should move the file .npa or .mpk on me to make it works!");
        Console.WriteLine("Press a button to close the program.");
        Console.ReadLine();
        Environment.Exit(0);
      }

      string originalFile = args[0].Substring(args[0].LastIndexOf('\\') + 1);

      using (BinaryReader br = new BinaryReader(File.OpenRead(args[0])))
      {
        Console.WriteLine($"Trying to read from file {originalFile}");

        //NPA File
        if (originalFile.ToLower().EndsWith(".npa"))
        {
          Console.WriteLine("Your file is a NPA format / JAST USA EDITION");

          byte[] header;
          uint headerLen;

          headerLen = br.ReadUInt32();
          header = br.ReadBytes((int)headerLen);

          Console.WriteLine("I'm decrypting the header...");

          NPA.ScrambleKey(NPA.keyLen);
          NPA.DecryptBuffer(NPA.keyLen, ref header, (int)headerLen);

          Console.WriteLine("Gnam Gnam, now i want more data!"); ;
          NPA.ParseHeader(br, header, originalFile);
        }
        //MPK File
        else if (originalFile.ToLower().EndsWith(".mpk"))
        {
          Console.WriteLine("Your file is a MPK format / STEAM EDITION");

          //MPK\0
          string magic = Encoding.ASCII.GetString(br.ReadBytes(4));
          br.ReadBytes(0x2);
          ushort headerVersion = br.ReadUInt16();
          MPK.fileCount = br.ReadInt32();

          //Removing the already readed magic and header length
          byte[] header = br.ReadBytes(0x38 + (MPK.fileCount * 0x100));

          Console.WriteLine("Gnam Gnam, now i want more data!"); ;
          MPK.ParseHeader(br, header, originalFile);
        }
        else
        {
          Console.WriteLine("Unknown file format u passed me");
        }
      }

      Console.WriteLine();
      Console.WriteLine("I ended, but, remember, the Agency still watches you!");
      Console.WriteLine("Press a button to close the program.");
      Console.ReadLine();
    }

    #region NPA Format

    public static class NPA
    {
      public static byte[] key = Encoding.ASCII.GetBytes("BUCKTICK");
      public static int keyLen = 8;

      public static void ScrambleKey(int keylen)
      {
        for (int i = 0; i < keylen; i++)
          key[i] = (byte)~key[i];
      }

      public static void DecryptBuffer(int keylen, ref byte[] header, int headerlen)
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

      public static void ParseHeader(BinaryReader stream, byte[] header, string originalFile)
      {
        StreamWriter sw = new StreamWriter(File.Create(originalFile + ".log"));

        int bufferOffset = 4;

        int fileCount = BitConverter.ToInt32(header, 0);

        int fileNameLen, size, offset, unk;
        string fileName = String.Empty;

        for (int i = 0; i < fileCount; i++)
        {
          fileNameLen = BitConverter.ToInt32(header, bufferOffset);
          size = BitConverter.ToInt32(header, bufferOffset + fileNameLen + 4);
          offset = BitConverter.ToInt32(header, bufferOffset + fileNameLen + 8);
          unk = BitConverter.ToInt32(header, bufferOffset + fileNameLen + 12);

          fileName = Encoding.Unicode.GetString(header, bufferOffset + 4, fileNameLen);

          Console.WriteLine("[+]{0}\tOffset[{1}]\tSize[{2}]", fileName, offset.ToString("X"), size.ToString("X"));
          sw.WriteLine("[+]{0}\tOffset[{1}]\tSize[{2}]", fileName, offset.ToString("X"), size.ToString("X"));

          ExtractData(stream, fileName, size, offset);

          bufferOffset += fileNameLen + 16;
        }
        sw.Flush();
        sw.Close();
      }

      public static void ExtractData(BinaryReader stream, string path, int size, int offset)
      {
        int slashPos = path.LastIndexOf('/');
        string folder = path.Substring(0, slashPos);

        Directory.CreateDirectory(folder);

        BinaryWriter bw = new BinaryWriter(File.Create(path));
        byte[] buffer = new byte[size];

        stream.BaseStream.Position = offset;
        buffer = stream.ReadBytes(size);

        DecryptBuffer(keyLen, ref buffer, (int)size);

        bw.Write(buffer);
        bw.Flush();
        bw.Close();
      }
    }

    #endregion

    #region MPK Format

    public static class MPK
    {
      public static int fileCount;

      public static void ParseHeader(BinaryReader stream, byte[] header, string originalFile)
      {
        StreamWriter sw = new StreamWriter(File.Create(originalFile + ".log"));

        int bufferOffset = 0x38;

        bool firstFile = true;

        int fileNum;
        long offset, length1, length2;
        string filename;

        for (int i = 0; i < fileCount; i++)
        {
          fileNum = BitConverter.ToInt32(header, bufferOffset);
          bufferOffset += 0x4;
          if (fileNum == 0 && !firstFile)
            break;
          else
            firstFile = true;

          offset = BitConverter.ToInt64(header, bufferOffset);
          bufferOffset += 0x8;
          length1 = BitConverter.ToInt64(header, bufferOffset);
          bufferOffset += 0x8;
          length2 = BitConverter.ToInt64(header, bufferOffset);
          bufferOffset += 0x8;

          filename = Encoding.UTF8.GetString(header, bufferOffset, 0xE4).Replace("\0", "");
          bufferOffset += 0xE4;

          Console.WriteLine("[+]{0}\tOffset[{1}]\tSize[{2}]", filename, offset.ToString("X"), length1.ToString("X"));
          sw.WriteLine("[+]{0}\tOffset[{1}]\tSize[{2}]", filename, offset.ToString("X"), length1.ToString("X"));

          ExtractData(stream, originalFile, filename, length1, offset);
        }
        sw.Flush();
        sw.Close();
      }

      public static void ExtractData(BinaryReader stream, string directory, string path, long size, long offset)
      {
        int slashPos = path.LastIndexOf('\\');
        string folder = $"dir_{directory}\\{(slashPos != -1 ? path.Substring(0, slashPos) : "")}";

        Directory.CreateDirectory(folder);

        BinaryWriter bw = new BinaryWriter(File.Create($"dir_{ directory }\\{path}"));
        byte[] buffer = new byte[size];

        stream.BaseStream.Position = offset;
        buffer = stream.ReadBytes((int)size);

        bw.Write(buffer);
        bw.Flush();
        bw.Close();
      }
    }

    #endregion
  }
}
