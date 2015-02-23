/*******************************************************************************
 * nipa2 v1
 *
 * Nitro+ - http://www.nitroplus.co.jp/
 * 
 * Revision history:
 *     v1 - Initial build has support for the system.npa file included in
 *     the Steins;Gate v1.10 patch. The game isn't out yet so this is what
 *     I am limited to.
 *
 * Supported Games:
 *     Steins;Gate PC Edition
 * 
 * Format:
 *     This format is a lot simpler than its previous version it seems.
 *     
 *     Base key and key mutation:
 *         The base key used (in Steins;Gate at least) is "BUCKTICK".
 *         This gets NOTd to form the final key. It seems the max a key
 *         can be is 8 bytes (this gets loaded into an MMX register).
 *         
 *         This is the code (just for the sake of fully documenting this)
 *         that generates the final key out of "BUCKTICK". It goes through
 *         and NOTs two bytes at a time.
 *         
 *         004A5330  /$  8B0D E4DD6400 MOV ECX,DWORD PTR DS:[64DDE4]            ;  BUCK
 *         004A5336  |. 8948 04        MOV DWORD PTR DS:[EAX+4],ECX
 *         004A5339  |.  8B15 E8DD6400 MOV EDX,DWORD PTR DS:[64DDE8]            ;  TICK
 *         004A533F  |. 8950 08        MOV DWORD PTR DS:[EAX+8],EDX
 *         004A5342  |. 0FB650 05      MOVZX EDX,BYTE PTR DS:[EAX+5]
 *         004A5346  |. 0FB6C9         MOVZX ECX,CL
 *         004A5349  |. F6D1           NOT CL
 *         004A534B  |. F6D2           NOT DL
 *         004A534D  |. 8848 04        MOV BYTE PTR DS:[EAX+4],CL
 *         004A5350  |. 0FB648 06      MOVZX ECX,BYTE PTR DS:[EAX+6]
 *         004A5354  |. 8850 05        MOV BYTE PTR DS:[EAX+5],DL
 *         004A5357  |. 0FB650 07      MOVZX EDX,BYTE PTR DS:[EAX+7]
 *         004A535B  |. F6D1           NOT CL
 *         004A535D  |. F6D2           NOT DL
 *         004A535F  |. 8848 06        MOV BYTE PTR DS:[EAX+6],CL
 *         004A5362  |. 0FB648 08      MOVZX ECX,BYTE PTR DS:[EAX+8]
 *         004A5366  |. 8850 07        MOV BYTE PTR DS:[EAX+7],DL
 *         004A5369  |. 0FB650 09      MOVZX EDX,BYTE PTR DS:[EAX+9]
 *         004A536D  |. F6D1           NOT CL
 *         004A536F  |. F6D2           NOT DL
 *         004A5371  |. 8848 08        MOV BYTE PTR DS:[EAX+8],CL
 *         004A5374  |. 0FB648 0A      MOVZX ECX,BYTE PTR DS:[EAX+A]
 *         004A5378  |. 8850 09        MOV BYTE PTR DS:[EAX+9],DL
 *         004A537B  |. 0FB650 0B      MOVZX EDX,BYTE PTR DS:[EAX+B]
 *         004A537F  |. F6D1           NOT CL
 *         004A5381  |. F6D2           NOT DL
 *         004A5383  |. 8848 0A        MOV BYTE PTR DS:[EAX+A],CL
 *         004A5386  |. 8850 0B        MOV BYTE PTR DS:[EAX+B],DL
 *         004A5389  \. C3             RETN
 *         
 *         And here's the code that does the actual XORing.
 *         004A53E7   .  60            PUSHAD
 *         004A53E8   .  33C9          XOR ECX,ECX
 *         004A53EA   .  8B55 E8       MOV EDX,DWORD PTR SS:[EBP-18]
 *         004A53ED   .  F3:           PREFIX REP:                              ;  Superfluous prefix
 *         004A53EE   .  0F6F45 EC     MOVQ MM0,QWORD PTR SS:[EBP-14]
 *         004A53F2   .  8B5D DC       MOV EBX,DWORD PTR SS:[EBP-24]
 *         004A53F5   >  F3:           PREFIX REP:                              ;  Superfluous prefix
 *         004A53F6   .  0F6F0B        MOVQ MM1,QWORD PTR DS:[EBX]
 *         004A53F9   .  66:0FEFC8     PXOR MM1,MM0
 *         004A53FD   .  F3:           PREFIX REP:                              ;  Superfluous prefix
 *         004A53FE   .  0F7F0B        MOVQ QWORD PTR DS:[EBX],MM1
 *         004A5401   .  83C3 10       ADD EBX,10
 *         004A5404   .  41            INC ECX
 *         004A5405   .  3BCA          CMP ECX,EDX
 *         004A5407   .^ 7C EC         JL SHORT STEINSGA.004A53F5
 * 
 *     Archive format:
 *         The first 4 bytes is the size of the header (encrypted at this
 *         point). Read in x amount of bytes to get the header then decrypt
 *         it using the key you generated out of "BUCKTICK". From there you
 *         get a more standard header.
 *         
 *         The first 4 bytes of the encrypted header is the amount of files
 *         in the archive. From there you have 4 bytes that indicate the
 *         filename length, followed by the actual filename (not null
 *         terminated). After that is 4 bytes for the filesize. Following that
 *         is the offset in the file where the data is stored (direct offset).
 *         There is still one more unknown field to be figured out later.
 *         
 *         Compressions are currently unknown but zlib is most likely if any.
 */

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
