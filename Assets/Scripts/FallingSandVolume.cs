using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.InputSystem.Utilities;
using Unity.Collections;
using UnityEngine.Rendering;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

public class FallingSandVolume : MonoBehaviour
{
    public enum Voxel : byte
    {
        None,
        Sand,
        Wall
    }

    struct VoxelUpdateJob : IJobParallelFor
    {
        [ReadOnly]
        public int3 volumeSize;

        [ReadOnly]
        public NativeArray<Voxel> readVolume;
        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<Voxel> writeVolume;

        [NativeDisableParallelForRestriction]
        public NativeArray<int> neighborsBuffer;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> randDirectionBuffer;        
        [NativeDisableParallelForRestriction]
        public NativeArray<Unity.Mathematics.Random> randomsBuffer;

        [NativeSetThreadIndex]
        private int threadIndex;

        private Voxel GetVoxel(int index)
        {
            return index == -1 ? Voxel.Wall : readVolume[index];
        }

        private void IndexToPos(ref int x, ref int y, ref int z, int index) {
            x = index % volumeSize.x;
            z = index / volumeSize.x % volumeSize.z;
            y = index / (volumeSize.x * volumeSize.z);
        }

        private int PosToIndex(int x, int y, int z)
        {
            if (x < 0 || x >= volumeSize.x || y < 0 || y >= volumeSize.y || z < 0 || z >= volumeSize.z)
            {
                return -1;
            }

            return x + z * volumeSize.x + y * volumeSize.x * volumeSize.z;
        }

        private void GetSandNeighbors(NativeSlice<int> dest, int index)
        {
            int x = 0;
            int y = 0;
            int z = 0;
            IndexToPos(ref x, ref y, ref z, index);

            dest[DOWN] = PosToIndex(x, y-1, z);
            dest[DOWN_PX] = PosToIndex(x+1, y-1, z);
            dest[DOWN_NX] = PosToIndex(x-1, y-1, z);
            dest[DOWN_PZ] = PosToIndex(x, y-1, z+1);
            dest[DOWN_NZ] = PosToIndex(x, y-1, z-1);
        }

        private void ShuffleDirections(NativeSlice<int> directions)
        {
            Unity.Mathematics.Random random = randomsBuffer[threadIndex];
            int n = directions.Length;
            for (int i = 0; i < n; i++)
            {
                int randomIndex = Math.Abs(random.NextInt()) % (n - i) + i;

                int temp = directions[i];
                directions[i] = directions[randomIndex];
                directions[randomIndex] = temp;
            }
        }

        public void Execute(int i)
        {   
            NativeSlice<int> neighbors = neighborsBuffer.Slice(threadIndex * 6, 6);
            Voxel currentVoxel = readVolume[i];

            switch (currentVoxel)
            {
                case Voxel.Sand:
                    GetSandNeighbors(neighbors, i);
                    int posYNeighbor = neighbors[DOWN];
                    if (GetVoxel(posYNeighbor) == Voxel.None)
                    {
                        writeVolume[posYNeighbor] = Voxel.Sand;
                    }
                    else
                    {
                        bool movedDiagonally = false;
                        NativeSlice<int> randDirections = randDirectionBuffer.Slice(threadIndex * 4, 4);

                        foreach (int j in randDirections)
                        {
                            if (GetVoxel(neighbors[j]) == Voxel.None)
                            {
                                writeVolume[neighbors[j]] = Voxel.Sand;
                                movedDiagonally = true;
                                ShuffleDirections(randDirections);
                                break;
                            }
                        }
                        if (!movedDiagonally)
                        {
                            writeVolume[i] = Voxel.Sand;
                        }
                    }
                    break;
            }
        }
    }
    [SerializeField] private GameObject meshObject;

    [SerializeField] private Vector3Int volumeSize;
    [SerializeField] private float voxelSize = 0.01f;

    [SerializeField] private float stepsPerSecond = 60;

    [SerializeField] private int threadCount = 16;

    private MeshFilter meshFilter;

    private int XZ_AREA = 0;
    private int XYZ_VOLUME = 0;

    private float accumulatedTime = 0;

    const int DOWN = 0;
    const int DOWN_PX = 1;
    const int DOWN_NX = 2;
    const int DOWN_PZ = 3;
    const int DOWN_NZ = 4;

    const int ADJ_PX = 0;
    const int ADJ_NX = 1;
    const int ADJ_PZ = 2;
    const int ADJ_NZ = 3;
    const int ADJ_PY = 4;
    const int ADJ_NY = 5;

    private static readonly Vector3[][] FaceCoordinates = new Vector3[][]
    {
        // ADJ_PX
        new Vector3[] { new(1, 0, 0), new(1, 0, 1), new(1, 1, 0), new(1, 1, 1) },

        // ADJ_NX
        new Vector3[] {  new(0, 1, 0), new(0, 1, 1),new(0, 0, 0), new(0, 0, 1) },

        // ADJ_PZ 
        new Vector3[] { new(1, 0, 1), new(0, 0, 1), new(1, 1, 1), new(0, 1, 1) },

        // ADJ_NZ
        new Vector3[] { new(0, 0, 0), new(1, 0, 0), new(0, 1, 0), new(1, 1, 0) },

        // ADJ_PY
        new Vector3[] { new(0, 1, 0), new(1, 1, 0), new(0, 1, 1), new(1, 1, 1) },

        // ADJ_NY
        //new Vector3[] {new(1, 0, 1), new(0, 0, 0), new(1, 0, 0), new(0, 0, 1) }
        new Vector3[] { new(1, 0, 0), new(0, 0, 1), new(1, 0, 1), new(0, 0, 0) }
    };

    private static readonly Vector3[] FaceNormals = new Vector3[]
    {
        new(1, 0, 0), new(-1, 0, 0),
        new(0, 0, 1), new(0, 0, -1),
        new(0, 1, 0), new(0, -1, 0)
    };

    private NativeArray<Voxel> readVolume;
    private NativeArray<Voxel> writeVolume;

    public void SetVoxel(int x, int y, int z, Voxel voxel)
    {
        int index = PosToIndex(x, y, z);
        if (index != -1)
        {
            readVolume[index] = voxel;
        }
    }

    public Vector3Int GetPosInVolume(Vector3 vec)
    {
        Vector3 worldVolumeSize = new Vector3(volumeSize.x, volumeSize.y, volumeSize.z) * voxelSize;
        Vector3 cornerPos = transform.position - worldVolumeSize / 2;

        Vector3 offsetVec = vec - cornerPos;

        if (offsetVec.x >= 0 && offsetVec.x < worldVolumeSize.x && 
            offsetVec.y >= 0 && offsetVec.y < worldVolumeSize.y && 
            offsetVec.z >= 0 && offsetVec.z < worldVolumeSize.z)
        {
            return new Vector3Int(
                (int)(offsetVec.x / voxelSize),
                (int)(offsetVec.y / voxelSize),
                (int)(offsetVec.z / voxelSize)
            );
        }

        return new Vector3Int(-1, -1, -1);
    }

    void Start()
    {
        meshFilter = meshObject.GetComponent<MeshFilter>();
        meshObject.transform.localScale = new Vector3(voxelSize, voxelSize, voxelSize);
        meshObject.transform.localPosition = new Vector3(volumeSize.x, volumeSize.y, volumeSize.z) * voxelSize / -2;

        XZ_AREA = volumeSize.x * volumeSize.z;
        XYZ_VOLUME = XZ_AREA * volumeSize.y;
        readVolume = new NativeArray<Voxel>(XYZ_VOLUME, Allocator.Persistent);
        writeVolume = new NativeArray<Voxel>(XYZ_VOLUME, Allocator.Persistent);
        readVolume.FillArray(Voxel.None);
        writeVolume.FillArray(Voxel.None);
    }

    void OnDestroy()
    {
        readVolume.Dispose();
        writeVolume.Dispose();
    }

    private void Update()
    {
        accumulatedTime += Time.deltaTime;
        float secondsPerStep = 1 / stepsPerSecond;
        if (accumulatedTime > secondsPerStep) {
            int stepCount = (int) (accumulatedTime / secondsPerStep);
            for (int i = 0; i < stepCount; i++)
            {
                UpdateVolume();
                GenerateMesh();
            }
            accumulatedTime = 0;
        }
    }

    private void UpdateVolume()
    {
        // Clear the write buffer
        writeVolume.FillArray(Voxel.None);

        NativeArray<int> neighborsBuffer = new NativeArray<int>(6 * threadCount, Allocator.TempJob);
        NativeArray<int> randDirectionBuffer = new NativeArray<int>(4 * threadCount, Allocator.TempJob);
        NativeArray<Unity.Mathematics.Random> randomsBuffer = new NativeArray<Unity.Mathematics.Random>(threadCount, Allocator.TempJob);

        // Fill rand direction buffer
        for (int i = 0; i < 4 * threadCount; i++)
        {
            randDirectionBuffer[i] = i % 4 + 1;
        }
        for (int i = 0; i < threadCount; i++)
        {
            randomsBuffer[i] = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(0, int.MaxValue));
        }

        VoxelUpdateJob job = new VoxelUpdateJob()
        {
            volumeSize = new int3(volumeSize.x, volumeSize.y, volumeSize.z),
            readVolume = readVolume,
            writeVolume = writeVolume,
            neighborsBuffer = neighborsBuffer,
            randDirectionBuffer = randDirectionBuffer,
            randomsBuffer = randomsBuffer
        };

        job.Schedule(XYZ_VOLUME, threadCount).Complete();

        // Swap the buffers
        NativeArray<Voxel> temp = readVolume;
        readVolume = writeVolume;
        writeVolume = temp;

        // Cleanup
        neighborsBuffer.Dispose();
        randDirectionBuffer.Dispose();
        randomsBuffer.Dispose();
    }

    private void GenerateMesh()
    {
        int[] neighbors = new int[6];
        List<Vector3> meshVertices = new List<Vector3>();
        List<int> meshTriangles = new List<int>();
        List<Vector3> meshNormals = new List<Vector3>();

        for (int i = 0; i < XYZ_VOLUME; i++)
        {
            Voxel currentVoxel = readVolume[i];
            if (currentVoxel == Voxel.Sand)
            {
                GetAdjacentNeighbors(neighbors, i);
                for (int j = ADJ_PX; j < ADJ_NY; j++)
                {
                    Voxel neighbor = GetVoxel(neighbors[j]);
                    if (neighbor == Voxel.None || neighbor == Voxel.Wall)
                    {
                        AddMeshFace(meshVertices, meshTriangles, meshNormals, i, j);
                    }
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(meshVertices);
        mesh.SetTriangles(meshTriangles, 0);
        mesh.SetNormals(meshNormals);
        meshFilter.mesh = mesh;
    }

    private void AddMeshFace(List<Vector3> vertices, List<int> tris, List<Vector3> normals, int index, int direction)
    {
        int x = 0;
        int y = 0;
        int z = 0;
        IndexToPos(ref x, ref y, ref z, index);
        Vector3 pos = new Vector3(x, y, z);

        int vindex = vertices.Count;
        Vector3[] addedVertices = FaceCoordinates[direction];
        for (int i = 0; i < 4; i++)
        {
            vertices.Add(pos + addedVertices[i]);
        }

        Vector3 normal = FaceNormals[direction];
        tris.AddRange(new int[] { vindex, vindex + 2, vindex + 1, vindex + 1, vindex + 2, vindex + 3 });
        normals.AddRange(new Vector3[] { normal, normal, normal, normal });
    }

    private Voxel GetVoxel(int index)
    {
        return index == -1 ? Voxel.Wall : readVolume[index];
    }

    private void IndexToPos(ref int x, ref int y, ref int z, int index) {
        x = index % volumeSize.x;
        z = index / volumeSize.x % volumeSize.z;
        y = index / XZ_AREA;
    }

    private int PosToIndex(int x, int y, int z)
    {
        if (x < 0 || x >= volumeSize.x || y < 0 || y >= volumeSize.y || z < 0 || z >= volumeSize.z)
        {
            return -1;
        }

        return x + z * volumeSize.x + y * XZ_AREA;
    }

    private void GetAdjacentNeighbors(int[] dest, int index)
    {
        int x = 0;
        int y = 0;
        int z = 0;
        IndexToPos(ref x, ref y, ref z, index);

        dest[ADJ_PX] = PosToIndex(x+1, y, z);
        dest[ADJ_NX] = PosToIndex(x-1, y, z);
        dest[ADJ_PZ] = PosToIndex(x, y, z+1);
        dest[ADJ_NZ] = PosToIndex(x, y, z-1);
        dest[ADJ_PY] = PosToIndex(x, y+1, z);
        dest[ADJ_NY] = PosToIndex(x, y-1, z);
    }

    private void OnDrawGizmos()
    {
        Vector3 fullVolumeSize = new Vector3(volumeSize.x, volumeSize.y, volumeSize.z) * voxelSize;
        Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 0.75f);
        Gizmos.DrawWireCube(transform.position, fullVolumeSize);
    }
}
