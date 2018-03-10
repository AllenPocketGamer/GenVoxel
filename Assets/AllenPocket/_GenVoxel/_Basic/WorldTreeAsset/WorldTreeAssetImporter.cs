using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace GenVoxelTools
{
    [ScriptedImporter(1, "WT")]
    public class WorldTreeAssetImporter : ScriptedImporter
    {

        public override void OnImportAsset(AssetImportContext ctx)
        {
            WorldTreeAsset wtAsset = ScriptableObject.CreateInstance("WorldTreeAsset") as WorldTreeAsset;
            ctx.AddObjectToAsset("MainAsset", wtAsset);
            ctx.SetMainObject(wtAsset);
        }
    }
}