using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using LHADecompressor;

namespace LhaTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string startupPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string filesPath = Path.Combine(startupPath, "Files");
            string outputPath = Path.Combine(startupPath, "Output");
            string[] fileArray = { "B080203-lh0-lv1.lzh", "B080203-lh1-lv1.lzh", /* "B080203-lh2-lv1.lzh", "B080203-lh3-lv1.lzh", */ "B080203-lh4-lv2.lzh", "B080203-lh5-lv2.lzh", "B080203-lh6-lv2.lzh", "B080203-lh7-lv2.lzh", "B080203-lz4-lv0.lzh", "B080203-lz5-lv0.lzh", "B080203-lzs-lv0.lzh" };
            byte[] dest = new byte[8];
            LhaFile lhaFile = null;

            foreach (string file in fileArray)
            {
                string fullPath = Path.Combine(filesPath, file);
                lhaFile = new LhaFile(fullPath, Encoding.UTF7);
                LhaEntry lhaEntry = lhaFile.GetEntry(0);

                Console.WriteLine("Archive File   : {0}", file);
                Console.WriteLine("Path           : {0}", lhaEntry.GetPath());
                Console.WriteLine("CompressedSize : {0}", lhaEntry.GetCompressedSize());
                Console.WriteLine("OriginalSize   : {0}", lhaEntry.GetOriginalSize());
                Console.WriteLine("LastModified   : {0}", lhaEntry.GetTimeStamp());
                Console.WriteLine("Method         : {0}", lhaEntry.GetMethod());
                Console.WriteLine("CRC            : 0x{0:X8}", lhaEntry.GetCRC());
                Console.WriteLine("------------------------------------------");

                dest = lhaFile.GetEntryBytes(lhaEntry);

                lhaFile.Close();

                File.WriteAllBytes(Path.Combine(outputPath, Path.ChangeExtension(file, "txt")), dest);
            }

            string fileName = Path.Combine(filesPath, "soseki.lzh");
            int entryCount = 0;

            lhaFile = new LhaFile(fileName, Encoding.UTF7);

            foreach (LhaEntry lhaEntry in lhaFile)
            {
                Console.WriteLine("Archive File   : {0} ({1})", Path.GetFileName(fileName), entryCount++);
                Console.WriteLine("Path           : {0}", lhaEntry.GetPath());
                Console.WriteLine("CompressedSize : {0}", lhaEntry.GetCompressedSize());
                Console.WriteLine("OriginalSize   : {0}", lhaEntry.GetOriginalSize());
                Console.WriteLine("LastModified   : {0}", lhaEntry.GetTimeStamp());
                Console.WriteLine("Method         : {0}", lhaEntry.GetMethod());
                Console.WriteLine("CRC            : 0x{0:X8}", lhaEntry.GetCRC());
                Console.WriteLine("------------------------------------------");

                if (lhaEntry.GetMethod().Equals(LhaEntry.METHOD_SIG_LHD))
                {
                    string directory = Path.Combine(outputPath, lhaEntry.GetPath());

                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    continue;
                }

                dest = lhaFile.GetEntryBytes(lhaEntry);

                File.WriteAllBytes(Path.Combine(outputPath, lhaEntry.GetPath()), dest);
            }

            lhaFile.Close();

            Console.ReadKey();
        }
    }
}
