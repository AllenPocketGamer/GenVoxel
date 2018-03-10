using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace GenVoxelTools
{
    public static class VoxTerrainMeshingTool
    {

        public static readonly byte emptyVoxelType = 0x00;
        public static readonly byte uMask = 0x0f;
        public static readonly byte vMask = 0xf0;
        public static readonly float scale = 0.0625f;
        public static readonly int maxVerticeIndexes = 65536;
        public static readonly int maxTrianglesIndexes = 98304;

        private static readonly float uvOffset = 0.002f;

        public static void GreedyMeshing(_16x256x16VoxChunk voxChunk,out Mesh mesh)
        {
            if(voxChunk == null)
            {
                mesh = new Mesh();
                return;
            }

            int[] size = { _16x256x16VoxChunk.Width, _16x256x16VoxChunk.Height, _16x256x16VoxChunk.Length };
            int[] position = { 0, 0, 0 };
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            // 遍历扫描三个维度
            for (int d = 0; d < 3; d++)
            {
                // 当前维度的二维截面的u,v坐标轴所代表的其余二个维度
                int u = (d + 1) % 3, v = (d + 2) % 3;
                // 用于辅助计算
                int[] q = { 0, 0, 0 }; q[d] = 1;
                // 用于记录当前维度截面的数据
                byte[] mask = new byte[size[u] * size[v]];
                // 用于记录Quad的方向
                int[] direction = new int[size[u] * size[v]];

                // 遍历该维度每个深度的截面数据
                for (position[d] = -1; position[d] < size[d];)
                {
                    // 求得截面数据
                    for (position[v] = 0; position[v] < size[v]; position[v]++)
                        for (position[u] = 0; position[u] < size[u]; position[u]++)
                        {
                            // 前后两层体素对比，使用异或规则(T = True,F = False)，计算获得可见面
                            // Front  ||     Back
                            //   T    ||   T  -->  F
                            //   T    ||   F  -->  T
                            //   F    ||   T  -->  T
                            //   F    ||   F  -->  F
                            byte frontVoxelType = voxChunk.GetVoxel(position[0], position[1], position[2]);
                            byte backVoxelType = voxChunk.GetVoxel(position[0] + q[0], position[1] + q[1], position[2] + q[2]);

                            bool frontExist = frontVoxelType != emptyVoxelType;
                            bool backExist = backVoxelType != emptyVoxelType;
                            // 计算当前Voxel是否可显示
                            bool canDisplay = frontExist ^ backExist;

                            // 计算当前截面位置的类型
                            int index = position[v] * size[u] + position[u];
                            if (canDisplay)
                            {
                                if (frontExist) mask[index] = frontVoxelType;
                                else mask[index] = backVoxelType;
                            }
                            else
                            {
                                mask[index] = emptyVoxelType;
                            }

                            if (!canDisplay)
                                direction[index] = 0;
                            else
                            {
                                direction[index] = frontExist ? 1 : -1;
                            }
                        }

                    // 累加深度
                    position[d]++;

                    // 简化截面
                    for (position[v] = 0; position[v] < size[v]; position[v]++)
                    {
                        for (position[u] = 0; position[u] < size[u];)
                        {
                            int stIndex = position[v] * size[u] + position[u];

                            if (mask[stIndex] == emptyVoxelType)
                            {
                                position[u]++;
                                continue;
                            }

                            // 当前起始面朝向
                            int stDirection = direction[stIndex];
                            // 当前体素类型
                            byte curVoxelType = mask[stIndex];

                            // 计算最大宽度
                            int width = 1;
                            while (true)
                            {
                                if (width + position[u] >= size[u]) break;
                                if (mask[stIndex + width] != curVoxelType || direction[stIndex + width] != stDirection) break;

                                width++;
                            }
                            // 计算最大高度
                            int height = 1;
                            bool flag = true;
                            while (flag)
                            {
                                if (height + position[v] >= size[v]) break;
                                int temp = (height + position[v]) * size[u];
                                for (int k = 0; k < width; k++)
                                {
                                    int index = position[u] + k + temp;
                                    if (mask[index] != curVoxelType || direction[index] != stDirection)
                                    {
                                        flag = false;
                                        break;
                                    }
                                }

                                if (flag)
                                    height++;
                            }

                            // 记录结果
                            int[] du = { 0, 0, 0 }, dv = { 0, 0, 0 };
                            du[u] = width; dv[v] = height;

                            stIndex = vertices.Count;

                            vertices.Add(new Vector3(position[0], position[1], position[2]));
                            vertices.Add(new Vector3(position[0] + du[0], position[1] + du[1], position[2] + du[2]));
                            vertices.Add(new Vector3(position[0] + du[0] + dv[0], position[1] + du[1] + dv[1], position[2] + du[2] + dv[2]));
                            vertices.Add(new Vector3(position[0] + dv[0], position[1] + dv[1], position[2] + dv[2]));

                            float sU = (uMask & curVoxelType) * scale;
                            float sV = ((uMask & curVoxelType) >> 4) * scale;

                            uvs.Add(new Vector2(sU,sV));
                            uvs.Add(new Vector2(sU + scale,sV));
                            uvs.Add(new Vector2(sU + scale, sV + scale));
                            uvs.Add(new Vector2(sU, sV + scale));

                            if (stDirection == 1)
                            {
                                triangles.Add(stIndex);
                                triangles.Add(stIndex + 1);
                                triangles.Add(stIndex + 2);
                                triangles.Add(stIndex + 2);
                                triangles.Add(stIndex + 3);
                                triangles.Add(stIndex);
                            }
                            else if (stDirection == -1)
                            {
                                triangles.Add(stIndex + 2);
                                triangles.Add(stIndex + 1);
                                triangles.Add(stIndex);
                                triangles.Add(stIndex);
                                triangles.Add(stIndex + 3);
                                triangles.Add(stIndex + 2);
                            }

                            // 在截面涂黑已记录结果
                            for (int j = 0; j < height; j++)
                            {
                                int temp = (j + position[v]) * size[u];
                                for (int i = 0; i < width; i++)
                                {
                                    mask[temp + position[u] + i] = emptyVoxelType;
                                }
                            }
                            // 累加
                            position[u] += width;
                        }
                    }
                }
            }

            mesh = new Mesh();
            mesh.subMeshCount = vertices.Count / maxVerticeIndexes + 1;
            mesh.SetVertices(vertices);
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                List<int> range;
                int sub = - i * maxVerticeIndexes;
                if(i+1 == mesh.subMeshCount)
                {
                    range = triangles.GetRange(i * maxTrianglesIndexes, triangles.Count % maxTrianglesIndexes);
                }
                else
                {
                    range = triangles.GetRange(i * maxTrianglesIndexes, maxTrianglesIndexes);
                }
                for(int j = 0; j < range.Count; j++)
                {
                    range[j] += sub;
                }
                mesh.SetTriangles(range, i, true, i * maxVerticeIndexes);
            }
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
        }
        public static MeshingMaterial GreedyMeshing(_16x256x16VoxChunk voxChunk)
        {
            if (voxChunk == null)
            {
                return null;
            }

            int[] size = { _16x256x16VoxChunk.Width, _16x256x16VoxChunk.Height, _16x256x16VoxChunk.Length };
            int[] position = { 0, 0, 0 };
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            // 遍历扫描三个维度
            for (int d = 0; d < 3; d++)
            {
                // 当前维度的二维截面的u,v坐标轴所代表的其余二个维度
                int u = (d + 1) % 3, v = (d + 2) % 3;
                // 用于辅助计算
                int[] q = { 0, 0, 0 }; q[d] = 1;
                // 用于记录当前维度截面的数据
                byte[] mask = new byte[size[u] * size[v]];
                // 用于记录Quad的方向
                int[] direction = new int[size[u] * size[v]];

                // 遍历该维度每个深度的截面数据
                for (position[d] = -1; position[d] < size[d];)
                {
                    // 求得截面数据
                    for (position[v] = 0; position[v] < size[v]; position[v]++)
                        for (position[u] = 0; position[u] < size[u]; position[u]++)
                        {
                            // 前后两层体素对比，使用异或规则(T = True,F = False)，计算获得可见面
                            // Front  ||     Back
                            //   T    ||   T  -->  F
                            //   T    ||   F  -->  T
                            //   F    ||   T  -->  T
                            //   F    ||   F  -->  F
                            byte frontVoxelType = voxChunk.GetVoxel(position[0], position[1], position[2]);
                            byte backVoxelType = voxChunk.GetVoxel(position[0] + q[0], position[1] + q[1], position[2] + q[2]);

                            bool frontExist = frontVoxelType != emptyVoxelType;
                            bool backExist = backVoxelType != emptyVoxelType;
                            // 计算当前Voxel是否可显示
                            bool canDisplay = frontExist ^ backExist;

                            // 计算当前截面位置的类型
                            int index = position[v] * size[u] + position[u];
                            if (canDisplay)
                            {
                                if (frontExist) mask[index] = frontVoxelType;
                                else mask[index] = backVoxelType;
                            }
                            else
                            {
                                mask[index] = emptyVoxelType;
                            }

                            if (!canDisplay)
                                direction[index] = 0;
                            else
                            {
                                direction[index] = frontExist ? 1 : -1;
                            }
                        }

                    // 累加深度
                    position[d]++;

                    // 简化截面
                    for (position[v] = 0; position[v] < size[v]; position[v]++)
                    {
                        for (position[u] = 0; position[u] < size[u];)
                        {
                            int stIndex = position[v] * size[u] + position[u];

                            if (mask[stIndex] == emptyVoxelType)
                            {
                                position[u]++;
                                continue;
                            }

                            // 当前起始面朝向
                            int stDirection = direction[stIndex];
                            // 当前体素类型
                            byte curVoxelType = mask[stIndex];

                            // 计算最大宽度
                            int width = 1;
                            while (true)
                            {
                                if (width + position[u] >= size[u]) break;
                                if (mask[stIndex + width] != curVoxelType || direction[stIndex + width] != stDirection) break;

                                width++;
                            }
                            // 计算最大高度
                            int height = 1;
                            bool flag = true;
                            while (flag)
                            {
                                if (height + position[v] >= size[v]) break;
                                int temp = (height + position[v]) * size[u];
                                for (int k = 0; k < width; k++)
                                {
                                    int index = position[u] + k + temp;
                                    if (mask[index] != curVoxelType || direction[index] != stDirection)
                                    {
                                        flag = false;
                                        break;
                                    }
                                }

                                if (flag)
                                    height++;
                            }

                            // 记录结果
                            int[] du = { 0, 0, 0 }, dv = { 0, 0, 0 };
                            du[u] = width; dv[v] = height;

                            stIndex = vertices.Count;

                            vertices.Add(new Vector3(position[0], position[1], position[2]));
                            vertices.Add(new Vector3(position[0] + du[0], position[1] + du[1], position[2] + du[2]));
                            vertices.Add(new Vector3(position[0] + du[0] + dv[0], position[1] + du[1] + dv[1], position[2] + du[2] + dv[2]));
                            vertices.Add(new Vector3(position[0] + dv[0], position[1] + dv[1], position[2] + dv[2]));

                            float sU = (uMask & curVoxelType) * scale + uvOffset;
                            float sV = ((uMask & curVoxelType) >> 4) * scale + uvOffset;

                            uvs.Add(new Vector2(sU, sV));
                            uvs.Add(new Vector2(sU + scale - 2 * uvOffset, sV));
                            uvs.Add(new Vector2(sU + scale - 2 * uvOffset, sV + scale - 2 * uvOffset));
                            uvs.Add(new Vector2(sU, sV + scale - 2 * uvOffset));

                            if (stDirection == 1)
                            {
                                triangles.Add(stIndex);
                                triangles.Add(stIndex + 1);
                                triangles.Add(stIndex + 2);
                                triangles.Add(stIndex + 2);
                                triangles.Add(stIndex + 3);
                                triangles.Add(stIndex);
                            }
                            else if (stDirection == -1)
                            {
                                triangles.Add(stIndex + 2);
                                triangles.Add(stIndex + 1);
                                triangles.Add(stIndex);
                                triangles.Add(stIndex);
                                triangles.Add(stIndex + 3);
                                triangles.Add(stIndex + 2);
                            }

                            // 在截面涂黑已记录结果
                            for (int j = 0; j < height; j++)
                            {
                                int temp = (j + position[v]) * size[u];
                                for (int i = 0; i < width; i++)
                                {
                                    mask[temp + position[u] + i] = emptyVoxelType;
                                }
                            }
                            // 累加
                            position[u] += width;
                        }
                    }
                }
            }

            return new MeshingMaterial(vertices, triangles, uvs);
        }
        public static MeshingMaterial GreedyMeshing_ColiderMesh(_16x256x16VoxChunk voxChunk)
        {
            if (voxChunk == null)
            {
                return null;
            }

            int[] size = { _16x256x16VoxChunk.Width, _16x256x16VoxChunk.Height, _16x256x16VoxChunk.Length };
            int[] position = { 0, 0, 0 };
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            // 遍历扫描三个维度
            for (int d = 0; d < 3; d++)
            {
                // 当前维度的二维截面的u,v坐标轴所代表的其余二个维度
                int u = (d + 1) % 3, v = (d + 2) % 3;
                // 用于辅助计算
                int[] q = { 0, 0, 0 }; q[d] = 1;
                // 用于记录当前维度截面的数据
                byte[] mask = new byte[size[u] * size[v]];
                // 用于记录Quad的方向
                int[] direction = new int[size[u] * size[v]];

                // 遍历该维度每个深度的截面数据
                for (position[d] = -1; position[d] < size[d];)
                {
                    // 求得截面数据
                    for (position[v] = 0; position[v] < size[v]; position[v]++)
                        for (position[u] = 0; position[u] < size[u]; position[u]++)
                        {
                            // 前后两层体素对比，使用异或规则(T = True,F = False)，计算获得可见面
                            // Front  ||     Back
                            //   T    ||   T  -->  F
                            //   T    ||   F  -->  T
                            //   F    ||   T  -->  T
                            //   F    ||   F  -->  F
                            byte frontVoxelType = voxChunk.GetVoxel(position[0], position[1], position[2]);
                            byte backVoxelType = voxChunk.GetVoxel(position[0] + q[0], position[1] + q[1], position[2] + q[2]);

                            bool frontExist = frontVoxelType != emptyVoxelType;
                            bool backExist = backVoxelType != emptyVoxelType;
                            // 计算当前Voxel是否可显示
                            bool canDisplay = frontExist ^ backExist;

                            // 计算当前截面位置的类型
                            int index = position[v] * size[u] + position[u];
                            if (canDisplay)
                            {
                                //if (frontExist) mask[index] = frontVoxelType;
                                //else mask[index] = backVoxelType;
                                mask[index] = 0x01;
                            }
                            else
                            {
                                mask[index] = emptyVoxelType;
                            }

                            if (!canDisplay)
                                direction[index] = 0;
                            else
                            {
                                direction[index] = frontExist ? 1 : -1;
                            }
                        }

                    // 累加深度
                    position[d]++;

                    // 简化截面
                    for (position[v] = 0; position[v] < size[v]; position[v]++)
                    {
                        for (position[u] = 0; position[u] < size[u];)
                        {
                            int stIndex = position[v] * size[u] + position[u];

                            if (mask[stIndex] == emptyVoxelType)
                            {
                                position[u]++;
                                continue;
                            }

                            // 当前起始面朝向
                            int stDirection = direction[stIndex];
                            // 当前体素类型
                            byte curVoxelType = mask[stIndex];

                            // 计算最大宽度
                            int width = 1;
                            while (true)
                            {
                                if (width + position[u] >= size[u]) break;
                                if (mask[stIndex + width] != curVoxelType || direction[stIndex + width] != stDirection) break;

                                width++;
                            }
                            // 计算最大高度
                            int height = 1;
                            bool flag = true;
                            while (flag)
                            {
                                if (height + position[v] >= size[v]) break;
                                int temp = (height + position[v]) * size[u];
                                for (int k = 0; k < width; k++)
                                {
                                    int index = position[u] + k + temp;
                                    if (mask[index] != curVoxelType || direction[index] != stDirection)
                                    {
                                        flag = false;
                                        break;
                                    }
                                }

                                if (flag)
                                    height++;
                            }

                            // 记录结果
                            int[] du = { 0, 0, 0 }, dv = { 0, 0, 0 };
                            du[u] = width; dv[v] = height;

                            stIndex = vertices.Count;

                            vertices.Add(new Vector3(position[0], position[1], position[2]));
                            vertices.Add(new Vector3(position[0] + du[0], position[1] + du[1], position[2] + du[2]));
                            vertices.Add(new Vector3(position[0] + du[0] + dv[0], position[1] + du[1] + dv[1], position[2] + du[2] + dv[2]));
                            vertices.Add(new Vector3(position[0] + dv[0], position[1] + dv[1], position[2] + dv[2]));

                            float sU = (uMask & curVoxelType) * scale;
                            float sV = ((uMask & curVoxelType) >> 4) * scale;

                            if (stDirection == 1)
                            {
                                triangles.Add(stIndex);
                                triangles.Add(stIndex + 1);
                                triangles.Add(stIndex + 2);
                                triangles.Add(stIndex + 2);
                                triangles.Add(stIndex + 3);
                                triangles.Add(stIndex);
                            }
                            else if (stDirection == -1)
                            {
                                triangles.Add(stIndex + 2);
                                triangles.Add(stIndex + 1);
                                triangles.Add(stIndex);
                                triangles.Add(stIndex);
                                triangles.Add(stIndex + 3);
                                triangles.Add(stIndex + 2);
                            }

                            // 在截面涂黑已记录结果
                            for (int j = 0; j < height; j++)
                            {
                                int temp = (j + position[v]) * size[u];
                                for (int i = 0; i < width; i++)
                                {
                                    mask[temp + position[u] + i] = emptyVoxelType;
                                }
                            }
                            // 累加
                            position[u] += width;
                        }
                    }
                }
            }

            return new MeshingMaterial(vertices, triangles,null);
        }

        public static Mesh CreateMesh(List<Vector3> vertices,List<int> triangles,List<Vector2> uvs)
        {
            Mesh mesh = new Mesh();

            mesh.subMeshCount = vertices.Count / maxVerticeIndexes + 1;
            mesh.SetVertices(vertices);
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                List<int> range;
                int sub = -i * maxVerticeIndexes;
                if (i + 1 == mesh.subMeshCount)
                {
                    range = triangles.GetRange(i * maxTrianglesIndexes, triangles.Count % maxTrianglesIndexes);
                }
                else
                {
                    range = triangles.GetRange(i * maxTrianglesIndexes, maxTrianglesIndexes);
                }
                for (int j = 0; j < range.Count; j++)
                {
                    range[j] += sub;
                }
                mesh.SetTriangles(range, i, true, i * maxVerticeIndexes);
            }
            if (uvs != null) mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();

            return mesh;
        }
    }

    public sealed class MeshingMaterial
    {
        public List<Vector3> Vertices
        {
            get { return vertices; }
        } 
        public List<int> Triangles
        {
            get { return triangles; }
        }
        public List<Vector2> UVS
        {
            get { return uvs; }
        }

        private List<Vector3> vertices;
        private List<int> triangles;
        private List<Vector2> uvs;

        public MeshingMaterial(List<Vector3> vertices,List<int> triangles,List<Vector2> uvs)
        {
            this.vertices = vertices;
            this.triangles = triangles;
            this.uvs = uvs;
        }

        public Mesh ToMesh()
        {
            return VoxTerrainMeshingTool.CreateMesh(vertices, triangles, uvs);
        }
    }
}
