using System.Collections.Generic;
using System.IO;

namespace GenVoxelTools {

    public class WorldTree {

        public static readonly string Version = "16X256X16";

        private static readonly string suffix = "WT";
        private static readonly int UniqueIDLength = 4;      // Byte
        private static readonly int DataLength = 1 << 16;    // Byte
        private static readonly int ChunkLength = UniqueIDLength + DataLength;

        // Chunk Count
        public int Count
        {
            get { return uniqueID2Index.Count; }
        }
        public string Path
        {
            get { return path; }
        }

        private string path;
        private FileStream file;
        private BinaryReader br;
        private BinaryWriter bw;
        private Dictionary<int, int> uniqueID2Index;

        // Create WT File
        public static WorldTree CreateWT(string path)
        {
            // Check Suffix
            if (!CheckSuffix(path))
            {
                throw new System.Exception("The File Suffix Name Is Not .WT.");
            }
            // Create WT
            FileStream file = File.Create(path);
            // Write Version Info
            file.Write(System.Text.Encoding.ASCII.GetBytes(Version), 0, Version.Length);
            // Close File
            file.Close();

            //using(var stream = File.Create(path))
            //{
            //    stream.Write(System.Text.Encoding.ASCII.GetBytes(Version), 0, Version.Length);
            //}

            return new WorldTree(path);
        }

        // Check whether the WT File exists
        public static bool WTExist(string path)
        {
            if (!CheckSuffix(path))
            {
                throw new System.Exception("The File Suffix Name Is Not .WT.");
            }
            return File.Exists(path);
        }

        // Check Suffix Name
        private static bool CheckSuffix(string path)
        {
            string[] res = path.Split(new char[] { '.' }, System.StringSplitOptions.RemoveEmptyEntries);

            if (res[1].ToUpper() != suffix) return false;

            return true;
        }


        public WorldTree(string path)
        {
            uniqueID2Index = new Dictionary<int, int>();

            OpenWT(path);

            this.path = path;
        }
        ~WorldTree()
        {
            Close();
        }

        // Read chunk from WT
        public _16x256x16VoxChunk ReadChunk(int uniqueID)
        {
            int index;

            if (!uniqueID2Index.TryGetValue(uniqueID, out index)) return null;

            br.BaseStream.Seek(Version.Length + index * ChunkLength + UniqueIDLength, SeekOrigin.Begin);

            byte[] voxData = br.ReadBytes(DataLength);

            return new _16x256x16VoxChunk(uniqueID, voxData);
        }

        // Write chunk to WT
        public void WriteChunk(_16x256x16VoxChunk chunk)
        {
            if (SearchChunk(chunk.UniqueID))
            {
                int index;
                uniqueID2Index.TryGetValue(chunk.UniqueID, out index);
                
                bw.BaseStream.Seek(Version.Length + index * ChunkLength + UniqueIDLength, SeekOrigin.Begin);

                bw.Write(chunk.VoxData);
                
            }
            else
            {
                int index = uniqueID2Index.Count;
                uniqueID2Index.Add(chunk.UniqueID, index);

                bw.BaseStream.Seek(0, SeekOrigin.End);

                bw.Write(chunk.UniqueID);
                bw.Write(chunk.VoxData);
            }
        }

        // Check whether the uniqueID exists
        public bool SearchChunk(int uniqueID)
        {
            int index;

            return uniqueID2Index.TryGetValue(uniqueID, out index);
        }

        // Open WT File
        private void OpenWT(string path)
        {
            // Check Suffix
            if (!CheckSuffix(path))
            {
                throw new System.Exception("The File Suffix Name Is Not .WT.");
            }

            // Open File
            file = File.Open(path, FileMode.Open, FileAccess.ReadWrite);

            // Check Version
            byte[] version_bytes = new byte[Version.Length];
            file.Read(version_bytes, 0, Version.Length);
            if (System.Text.Encoding.ASCII.GetString(version_bytes).ToUpper() != Version)
            {
                throw new System.Exception("This File Version Is Not 16X256X16.");
            }

            // Create Binary Reader/Writer
            br = new BinaryReader(file);
            bw = new BinaryWriter(file);

            // RetrieveUniqueIDs
            RetrieveUniqueIDs();
        }

        // Close WT File
        public void Close()
        {
            br.Close();
            bw.Close();

            file.Close();

            uniqueID2Index.Clear();
        }

        // Retrieve uniqueIDs
        public void RetrieveUniqueIDs()
        {
            int index = 0;
            
            br.BaseStream.Seek(Version.Length, SeekOrigin.Begin);

            while(br.BaseStream.Position != br.BaseStream.Length)
            {
                uniqueID2Index.Add(br.ReadInt32(), index++);

                br.BaseStream.Seek(DataLength, SeekOrigin.Current);
            }
        }
    }
}