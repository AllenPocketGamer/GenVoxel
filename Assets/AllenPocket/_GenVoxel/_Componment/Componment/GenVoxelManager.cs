using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace GenVoxelTools
{
    // [ExecuteInEditMode]
    public class GenVoxelManager : MonoBehaviour
    {
        // World Tree 资源引用
        [SerializeField]
        private WorldTreeAsset wtAsset;
        // 世界生成规则
        [SerializeField]
        private VoxTerrainGenerateRule rule;
        // 世界材质
        [SerializeField]
        private Material voxMaterial;
        // 加载范围
        [SerializeField]
        private int width = 16;
        [SerializeField]
        private int length = 16;
        // 起始点类型选择
        [SerializeField]
        private PositionType positionType = PositionType.Normal;
        // 加载起始点
        [SerializeField]
        private int x;
        [SerializeField]
        private int z;
        // 目标Transform,与起始点互斥
        [SerializeField]
        private Transform target;
        // 伸缩比
        [SerializeField]
        [Range(0.01f, 1)]
        private float scale = 1.0f;

        // 区域内的chunks
        private List<_16x256x16VoxChunk> chunksInLastArea;
        // 范围内的chunks的UniqueIDs
        private List<int> uniqueIDsInArea;

        // Meshing 线程
        private bool isMeshing = false;
        private MeshingThread meshingThread;

        // 更新间隔
        private float updateInterv = 0.03f;
        private float time = 0;

        // 选择器
        private _16x256x16VoxChunk selectChunk;
        private Vector3 selectVoxelPosition;

        public bool IsMeshing
        {
            get { return isMeshing; }
            set { isMeshing = value; }
        }

        void Awake()
        {
            ClearChild();
        }
        void Start()
        {
            StartManager();
        }
        void Update()
        {
            if (UpdateXZByTarget())
            {
                UpdateArea();
            }
        }

        void OnDestroy()
        {
            EndManager();
        }
        void OnValidate()
        {
            if (width > 65536) width = 65536;
            if (length > 65536) length = 65536;
            if (x > 65536) x = 65536;
            if (z > 65536) z = 65536;

            if (width < 0) width = 0;
            if (length < 0) length = 0;
            if (x < 0) x = 0;
            if (z < 0) z = 0;
        }

        // 区域更新
        public void UpdateArea()
        {
            if (IsMeshing == true)
            {
                return;
            }
            else
            {
                IsMeshing = true;
            }
            CalculateUniqueIDsInArea();
            UnloadChunks();
            StartCoroutine("LoadChunks");
        }

        // 获得射线交点所在Voxel的世界坐标信息
        public Vector3 SetSelectedVoxel(RaycastHit hit) 
        {
            if (hit.point.x < 0 || hit.point.y < 0 || hit.point.z < 0) return new Vector3(0, 0, 0);

            float acc = 0.00001f;
            int[] delta = new int[] { 0, 0, 0 };
            int[] start = new int[] { 0, 0, 0 };
            int[] local = new int[] { 0, 0, 0 };

            if((hit.point.x % 1) < acc)
            {
                delta[0] = 1;
            }
            else if((hit.point.y % 1) < acc)
            {
                delta[1] = 1;
            }
            else if((hit.point.z %1) < acc)
            {
                delta[2] = 1;
            }

            start[0] = (int)hit.point.x;
            start[1] = (int)hit.point.y;
            start[2] = (int)hit.point.z;

            int uniqueID = GetUniqueIDByVoxelWorldPosition(start[0], start[2]);
            var chunk = GetChunkByUniqueID(uniqueID);

            if(chunk == null)
            {
                selectChunk = null;
                selectVoxelPosition = new Vector3(0, 0, 0);
                return selectVoxelPosition;
            }

            local[0] = start[0] % _16x256x16VoxChunk.Width;
            local[1] = start[1] % _16x256x16VoxChunk.Height;
            local[2] = start[2] % _16x256x16VoxChunk.Length;

            byte voxType = chunk.GetVoxel(local[0], local[1], local[2]);

            if(voxType != _16x256x16VoxChunk.EmptyType)
            {
                selectChunk = chunk;
                selectVoxelPosition = new Vector3(start[0], start[1], start[2]);
            }
            else
            {
                selectChunk = GetChunkByUniqueID(GetUniqueIDByVoxelWorldPosition(start[0] - delta[0], start[2] - delta[2]));
                selectVoxelPosition = new Vector3(start[0] - delta[0], start[1] - delta[1], start[2] - delta[2]);
            }

            return selectVoxelPosition;
        }
        // 删除指定位置的Voxel
        public void DeleteVoxel()
        {
            if (selectChunk == null) return;

            int[] local = new int[] { 0, 0, 0 };
            local[0] = (int)(selectVoxelPosition.x) % _16x256x16VoxChunk.Width;
            local[1] = (int)(selectVoxelPosition.y) % _16x256x16VoxChunk.Height;
            local[2] = (int)(selectVoxelPosition.z) % _16x256x16VoxChunk.Length;

            selectChunk.SetVoxel(local[0], local[1], local[2], _16x256x16VoxChunk.EmptyType);

            // 更新该chunk的MeshObj
            UpdateChunk(selectChunk);

        }
        // 增加指定位置的Voxel
        public void AddVoxel(byte voxel,RaycastHit hit)
        {
            if (selectChunk == null) return;

            float acc = 0.00001f;
            Vector3 target = new Vector3(0, 0, 0);

            Vector3 temp = new Vector3((int)(hit.point.x), (int)(hit.point.y), (int)(hit.point.z));
            Vector3 delta = temp - selectVoxelPosition;

            if(Mathf.Abs(delta.magnitude - 1.0f) < acc)
            {
                target = temp;
            }
            else
            {
                if((hit.point.x - selectVoxelPosition.x) < acc)
                {
                    target = selectVoxelPosition - new Vector3(1, 0, 0);
                }
                else if((hit.point.y - selectVoxelPosition.y) < acc)
                {
                    target = selectVoxelPosition - new Vector3(0, 1, 0);
                }
                else if((hit.point.z - selectVoxelPosition.z) < acc)
                {
                    target = selectVoxelPosition - new Vector3(0, 0, 1);
                }
            }

            int[] local = new int[]
            {
                (int)(target.x) % _16x256x16VoxChunk.Width,
                (int)(target.y) % _16x256x16VoxChunk.Height,
                (int)(target.z) % _16x256x16VoxChunk.Length
            };

            selectChunk.SetVoxel(local[0],local[1],local[2], voxel);

            UpdateChunk(selectChunk);
        }


        // 启动管理器
        private void StartManager()
        {
            InitBufferContainers();

            if (positionType == PositionType.Transform && target != null)
            {
                ConvertTransform2XZ();
            }

            UpdateArea();
        }
        private void EndManager()
        {
            ClearChild();
            Save();
        }

        // 初始化缓存容器
        private void InitBufferContainers()
        {
            if(chunksInLastArea == null)
            {
                chunksInLastArea = new List<_16x256x16VoxChunk>();
            }
            if(uniqueIDsInArea == null)
            {
                uniqueIDsInArea = new List<int>();
            }
        }

        // 通过Voxel的世界坐标来计算Voxel所属的Chunk
        private int GetUniqueIDByVoxelWorldPosition(int x,int z)
        {
            x /= _16x256x16VoxChunk.Width;
            z /= _16x256x16VoxChunk.Length;

            return _16x256x16VoxChunk.DefaultDecoderFromStPosition2UniqueID(new int[] { x, z });
        }
        // 在chunks中寻找对应uniqueID的chunk
        private _16x256x16VoxChunk GetChunkByUniqueID(int uniqueID)
        {
            foreach(var chunk in chunksInLastArea)
            {
                if(chunk.UniqueID == uniqueID)
                {
                    return chunk;
                }
            }
            return null;
        }

        // 计算区域内的uniqueIDs
        private void CalculateUniqueIDsInArea()
        {
            uniqueIDsInArea.Clear();
            // 计算范围内chunks
            for(int i = 0; i < width; i++)
            {
                for(int j = 0; j < length; j++)
                {
                    uniqueIDsInArea.Add(_16x256x16VoxChunk.DefaultDecoderFromStPosition2UniqueID(new int[] { i + x, j + z }));
                }
            }
        }
        // 区域更新:卸载不在区域内的chunks
        private void UnloadChunks()
        {
            List<_16x256x16VoxChunk> unloadChunks = new List<_16x256x16VoxChunk>();

            foreach(var chunk in chunksInLastArea)
            {
                if(!uniqueIDsInArea.Contains(chunk.UniqueID))
                {
                    unloadChunks.Add(chunk);

                    if (chunk.IsRefresh)    // 若chunk被更新
                    {
                        wtAsset.SetChunk(chunk);
                    }

                    var MeshObj = transform.Find(chunk.UniqueID.ToString()).gameObject;
                    DestroyImmediate(MeshObj);
                }
            }

            foreach(var unloadChunk in unloadChunks)
            {
                chunksInLastArea.Remove(unloadChunk);
            }
        }
        // 区域更新:加载未加载区域内的chunks
        private IEnumerator LoadChunks()
        {
            List<_16x256x16VoxChunk> loadChunks = new List<_16x256x16VoxChunk>();

            foreach(var uniqueID in uniqueIDsInArea)
            {
                bool equal = false;

                foreach(var chunk in chunksInLastArea)
                {
                    if(uniqueID == chunk.UniqueID)
                    {
                        equal = true;
                    }
                }

                if (!equal) // 若uniqueID不在老区域内，则被加载
                {
                    _16x256x16VoxChunk loadChunk = wtAsset.GetChunk(uniqueID);
                    if(loadChunk == null)   // 若在wt文件中不存在，则重新创建一个
                    {
                        loadChunk = rule.GenerateVoxChunk(uniqueID);
                    }

                    loadChunks.Add(loadChunk);
                }
            }

            foreach(var loadChunk in loadChunks)
            {
                chunksInLastArea.Add(loadChunk);
            }

            if (meshingThread == null) meshingThread = new MeshingThread();

            meshingThread.MeshingChunk = loadChunks;
            meshingThread.Run();

            int cur = 0;
            while(cur < meshingThread.MeshingChunk.Count)
            {
                MeshingMaterial meshingMaterial = meshingThread.MeshingMaterials[cur];
                MeshingMaterial coliderMeshingMaterial = meshingThread.ColiderMeshMaterials[cur];

                if(meshingMaterial == null || coliderMeshingMaterial == null)
                {
                    yield return null;
                }
                else
                {
                    CreateMeshObj(meshingThread.MeshingChunk[cur], meshingMaterial,coliderMeshingMaterial);
                    cur++;
                }
            }

            IsMeshing = false;
        }

        // 保存:当退出时，保存区域内经过修改的chunk
        private void Save()
        {
            if (chunksInLastArea == null) return;

            foreach(var chunk in chunksInLastArea)
            {
                if (chunk.IsRefresh)
                {
                    wtAsset.SetChunk(chunk);
                }
            }
        }
        // 更新指定Chunk
        private void UpdateChunk(_16x256x16VoxChunk chunk)
        {
            if (!HasTheChunk(chunk)) return;

            MeshingMaterial meshMat = VoxTerrainMeshingTool.GreedyMeshing(chunk);
            MeshingMaterial coliderMeshMat = VoxTerrainMeshingTool.GreedyMeshing_ColiderMesh(chunk);
            Mesh mesh = meshMat.ToMesh();
            Mesh coliderMesh = coliderMeshMat.ToMesh();

            var oldMeshObj = GetMeshObj(chunk.UniqueID);
            DestroyImmediate(oldMeshObj);
            CreateMeshObj(chunk, mesh, coliderMesh);
        }
        // 查找在Terrain中是否存在该chunk
        private bool HasTheChunk(_16x256x16VoxChunk chunk)
        {
            foreach(var lchunk in chunksInLastArea)
            {
                if (lchunk == chunk) return true;
            }
            return false;
        }
        // 根据uniqueID获得MeshObj
        private GameObject GetMeshObj(int uniqueID)
        {
            return transform.Find(uniqueID.ToString()).gameObject;
        }
        // 将Transform转换为X,Z
        private void ConvertTransform2XZ()
        {
            Vector3 position = target.position;

            position /= scale;
            position.x = position.x / _16x256x16VoxChunk.Width;
            position.z = position.z / _16x256x16VoxChunk.Length;

            position.x -= width / 2;
            position.z -= length / 2;

            x = position.x >= 0 ? (int)(position.x) : 0;
            z = position.z >= 0 ? (int)(position.z) : 0;
        }
        // 根据Transform来更新区域
        private bool UpdateXZByTarget()
        {
            bool flag = false;
            if(positionType == PositionType.Transform && target != null)
            {
                if(time > updateInterv)
                {
                    int oldX = x;
                    int oldZ = z;
                    ConvertTransform2XZ();

                    if (x != oldX || z != oldZ) flag = true;
                    
                    time -= updateInterv;
                }

                time += Time.deltaTime;
            }
            return flag;
        }
        // Mesh chunk
        private GameObject CreateMeshObj(_16x256x16VoxChunk chunk,MeshingMaterial meshingMaterial,MeshingMaterial coliderMeshingMaterial)
        {
            Mesh mesh = meshingMaterial.ToMesh();
            Mesh coliderMesh = coliderMeshingMaterial.ToMesh();

            return CreateMeshObj(chunk, mesh,coliderMesh);
        }
        private GameObject CreateMeshObj(_16x256x16VoxChunk chunk,Mesh mesh,Mesh coliderMesh)
        {
            GameObject meshObj = new GameObject(chunk.UniqueID.ToString());

            meshObj.transform.parent = transform;
            meshObj.AddComponent<MeshRenderer>();
            meshObj.AddComponent<MeshFilter>();

            meshObj.GetComponent<MeshRenderer>().material = voxMaterial;
            meshObj.GetComponent<MeshFilter>().mesh = mesh;

            // Set Position
            meshObj.transform.position = new Vector3(_16x256x16VoxChunk.Width * chunk.Position[0] * scale, 0, _16x256x16VoxChunk.Length * chunk.Position[1] * scale);
            meshObj.transform.localScale = new Vector3(scale, scale, scale);

            var colider = meshObj.AddComponent<MeshCollider>();
            colider.sharedMesh = coliderMesh;

            meshObj.layer = LayerMask.NameToLayer("VoxTerrain");

            return meshObj;
        }

        // 清理子物体
        private void ClearChild()
        {
            List<Transform> transh = new List<Transform>();

            foreach(Transform child in transform)
            {
                transh.Add(child);
            }

            foreach(var child in transh)
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    public enum PositionType
    {
        Normal,
        Transform
    }
    internal class MeshingThread
    {
        public List<_16x256x16VoxChunk> MeshingChunk
        {
            get { return meshChunks; }
            set
            {
                meshChunks = value;
                meshingMaterials = new MeshingMaterial[meshChunks.Count];
                coliderMeshMaterials = new MeshingMaterial[meshChunks.Count];
            }
        }
        public MeshingMaterial[] MeshingMaterials
        {
            get { return meshingMaterials; }
        }
        public MeshingMaterial[] ColiderMeshMaterials
        {
            get { return coliderMeshMaterials; }
        }

        private List<_16x256x16VoxChunk> meshChunks;
        private MeshingMaterial[] meshingMaterials;
        private MeshingMaterial[] coliderMeshMaterials;

        private Thread workThread;

        public MeshingThread()
        {
        }

        public void Run()
        {
            if (workThread != null && workThread.ThreadState == ThreadState.Running) return;

            workThread = new Thread(this.Proc);
            workThread.Name = "MeshingThread";

            workThread.Start();
        }

        private void Proc()
        {
            if (meshChunks == null) return;

            for (int i = 0; i < meshChunks.Count; i++)
            {
                meshingMaterials[i] = VoxTerrainMeshingTool.GreedyMeshing(meshChunks[i]);
                coliderMeshMaterials[i] = VoxTerrainMeshingTool.GreedyMeshing_ColiderMesh(meshChunks[i]);
            }
        }
    }
}
