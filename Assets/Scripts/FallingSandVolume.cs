using System;
using System.Collections.Generic;
using UnityEngine;

public class FallingSandVolume : MonoBehaviour
{
    [SerializeField] private GameObject meshObject;

    [SerializeField] private Vector3Int volumeSize;
    [SerializeField] private float voxelSize = 0.01f;

    [SerializeField] private float stepsPerSecond = 60;

    private MeshFilter meshFilter;

    private int XZ_AREA = 0;
    private int XYZ_VOLUME = 0;

    private float accumulatedTime = 0;

    public enum Voxel : byte
    {
        None,
        Sand,
        Wall
    }

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

    private Voxel[] readVolume;
    private Voxel[] writeVolume;

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
        readVolume = new Voxel[XYZ_VOLUME];
        writeVolume = new Voxel[XYZ_VOLUME];
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
        int[] neighbors = new int[6];
        int[] randDirectionBuffer = new int[4] { DOWN_PX, DOWN_NX, DOWN_PZ, DOWN_NZ };
        ShuffleArray(randDirectionBuffer);

        // Clear the write buffer
        Array.Fill(writeVolume, Voxel.None);

        for (int i = 0; i < XYZ_VOLUME; i++)
        {
            Voxel currentVoxel = readVolume[i];

            switch (currentVoxel)
            {
                case Voxel.Sand:
                    GetSandNeighbors(neighbors, i);
                    int posYNeighbor = neighbors[DOWN];
                    if (GetVoxel(readVolume, posYNeighbor) == Voxel.None)
                    {
                        writeVolume[posYNeighbor] = Voxel.Sand;
                    }
                    else
                    {
                        bool movedDiagonally = false;
                        foreach (int j in randDirectionBuffer)
                        {
                            if (GetVoxel(readVolume, neighbors[j]) == Voxel.None)
                            {
                                writeVolume[neighbors[j]] = Voxel.Sand;
                                movedDiagonally = true;
                                ShuffleArray(randDirectionBuffer);
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

        // Swap the buffers
        Voxel[] temp = readVolume;
        readVolume = writeVolume;
        writeVolume = temp;
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
                    Voxel neighbor = GetVoxel(readVolume, neighbors[j]);
                    if (neighbor == Voxel.None || neighbor == Voxel.Wall)
                    {
                        AddMeshFace(meshVertices, meshTriangles, meshNormals, i, j);
                    }
                }
            }
        }

        Mesh mesh = new Mesh();
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

    private Voxel GetVoxel(Voxel[] buffer, int index)
    {
        return index == -1 ? Voxel.Wall : buffer[index];
    }

    private void IndexToPos(ref int x, ref int y, ref int z, int index) {
        x = index % volumeSize.x;
        z = index / volumeSize.x % volumeSize.z;
        y = index / (XZ_AREA);
    }

    private int PosToIndex(int x, int y, int z)
    {
        // Clamp to [-1, XYZ_VOLUME] then add 1 to create the following arrangement:
        // volume[0] = wall
        // volume[1..XYZ_VOLUME] = actual voxel grid
        // volume[XYZ_VOLUME+1] = wall
        // This makes it so that any out of bounds indices automatically return a wall.
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

    private void GetSandNeighbors(int[] dest, int index)
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

    private void OnDrawGizmos()
    {
        Vector3 fullVolumeSize = new Vector3(volumeSize.x, volumeSize.y, volumeSize.z) * voxelSize;
        Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 0.75f);
        Gizmos.DrawWireCube(transform.position, fullVolumeSize);
    }

    private void ShuffleArray<T>(T[] array)
    {
        int n = array.Length;
        for (int i = 0; i < n; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, n);

            T temp = array[i];
            array[i] = array[randomIndex];
            array[randomIndex] = temp;
        }
    }
}
