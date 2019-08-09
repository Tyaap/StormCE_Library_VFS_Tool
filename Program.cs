using System;
using System.IO;

namespace StormCE_Library_VFS_Tool
{
    class Program
    {
        static void Main(string[] args)
        {

            int count = args.Length;
            for (int i = 0; i < count; i++)
            {
                try
                {
                    string path = args[i];
                    if (File.Exists(path) && (path.ToUpper().EndsWith(".VFS")))
                    {
                        Console.WriteLine("Extracting: \"{0}\"", Path.GetFileName(path));
                        ExtractVFS(path);
                        Console.WriteLine();
                    }
                    else if (Directory.Exists(path))
                    {
                        Console.WriteLine("Creating: \"{0}\"", Path.GetFileNameWithoutExtension(path) + ".VFS");
                        CreateVFS(path);
                        Console.WriteLine();
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine("Error!");
                    Console.WriteLine(e);
                }
            }
            Console.WriteLine("Finished! Press any key to exit.");
            Console.ReadKey();
        }

        public static void CreateVFS(string directoryPath)
        {
            string outPath = Path.GetDirectoryName(directoryPath) + "\\" + Path.GetFileName(directoryPath) + ".VFS";
            FileStream fs = File.Open(outPath, FileMode.Create);
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                bw.Write("StormCE Library VFS 1.0\0".ToCharArray());
                DirectoryInfo directory = new DirectoryInfo(directoryPath);
                FileInfo[] files = directory.GetFiles();
                int fileCount = files.Length;
                int vfsOffset = 24 + (fileCount + 1) * 104;

                for (int i = 0; i < fileCount; i++)
                {
                    FileInfo file = files[i];
                    bw.Write(file.Name.PadRight(96, char.MinValue).ToCharArray());
                    bw.Write((int)file.Length);
                    bw.Write(vfsOffset);
                    vfsOffset += (int)file.Length;
                }

                for (int i = 0; i < 26; i ++)
                {
                    bw.Write(0);
                }

                for (int i = 0; i < fileCount; i++)
                {
                    FileInfo file = files[i];
                    Console.WriteLine("{0} Size:{1} Offset:{2:X8}", file.Name, file.Length, fs.Position);
                    using (FileStream stream = file.OpenRead())
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        while ((bytesRead = stream.Read(buffer, 0, 1024)) > 0)
                        {
                            bw.Write(buffer, 0, bytesRead);
                        }
                    }
                }
            }
        }

        public static void ExtractVFS(string filePath)
        {
            FileStream fs = File.Open(filePath, FileMode.Open);
            string outPath = Path.GetDirectoryName(filePath) + "\\" + Path.GetFileNameWithoutExtension(filePath);
            if (!Directory.Exists(outPath))
            {
                Directory.CreateDirectory(outPath);
            }
            using (BinaryReader br = new BinaryReader(fs))
            {
                fs.Seek(24, SeekOrigin.Begin);
                bool filesLeft = true;
                while (filesLeft)
                {
                    char[] chars = br.ReadChars(96);
                    string tmp = new string(chars);
                    string name = new string(chars, 0, Array.IndexOf(chars, char.MinValue));
                    filesLeft = !string.IsNullOrEmpty(name);
                    if (filesLeft)
                    {
                        int length = br.ReadInt32();
                        int offset = br.ReadInt32();
                        Console.WriteLine("{0} Size:{1} Offset:{2:X8}", name, length, offset);

                        long position = fs.Position;
                        fs.Seek(offset, SeekOrigin.Begin);

                        FileStream fs2 = File.Create(outPath + "\\" + name);
                        using (BinaryWriter bw = new BinaryWriter(fs2))
                        {
                            byte[] buffer = new byte[1024];
                            int bytesLeft = length;
                            while (bytesLeft > 0)
                            {
                                int bytesRead = Math.Min(1024, bytesLeft);
                                fs.Read(buffer, 0, bytesRead);
                                bw.Write(buffer, 0, bytesRead);
                                bytesLeft -= bytesRead;
                            }
                        }
                        fs.Seek(position, SeekOrigin.Begin);
                    }
                }
            }
        }
    }
}
