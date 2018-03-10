using UnityEngine;
using UnityEditor;


namespace GenVoxelTools
{
    public class WorldTreeAsset : ScriptableObject
    {
        [MenuItem("Assets/Create/WorldTree", false, 651)]
        public static void CreateWorldTreeFile()
        {
            string name = "New World Tree";
            string path = AssetDatabase.GetAssetPath(Selection.activeObject) + @"/" + name + ".WT";

            var wt = WorldTree.CreateWT(path);
            wt.Close();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }

        public string Path
        {
            get
            {
                return AssetDatabase.GetAssetPath(this);
            }
        }

        private WorldTree wt;

        void OnDestroy()
        {
            CloseWT();
        }

        public _16x256x16VoxChunk GetChunk(int uniqueID)
        {
            if (!ExistWT()) OpenWT();

            return wt.ReadChunk(uniqueID);
        }
        public void SetChunk(_16x256x16VoxChunk chunk)
        {
            if (!ExistWT()) OpenWT();

            wt.WriteChunk(chunk);
        }

        private void OpenWT()
        {
            if (wt == null) 
                wt = new WorldTree(Path);
        }
        private void CloseWT()
        {
            if (wt != null) wt.Close();
            wt = null;
        }
        private bool ExistWT()
        {
            if (wt == null) return false;
            return true;
        }
    }
}