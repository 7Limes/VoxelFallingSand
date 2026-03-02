using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class FallingSandVolume : MonoBehaviour
{
    [SerializeField] private GameObject meshObject;

    [SerializeField] private Vector3Int volumeSize;
    [SerializeField] private float voxelSize = 0.01f;

    private MeshFilter meshFilter;

    private int XZ_AREA = 0;
    private int XYZ_VOLUME = 0;

    enum Voxel : byte
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
        new Vector3[] { new(0, 0, 1), new(0, 0, 0), new(0, 1, 1), new(0, 1, 0) },

        // ADJ_PZ 
        new Vector3[] { new(1, 0, 1), new(0, 0, 1), new(1, 1, 1), new(0, 1, 1) },

        // ADJ_NZ
        new Vector3[] { new(0, 0, 0), new(1, 0, 0), new(0, 1, 0), new(1, 1, 0) },

        // ADJ_PY
        new Vector3[] { new(0, 1, 0), new(1, 1, 0), new(0, 1, 1), new(1, 1, 1) },

        // ADJ_NY
        new Vector3[] { new(0, 0, 1), new(1, 0, 1), new(0, 0, 0), new(1, 0, 0) }
    };

    private Voxel[] readVolume;
    private Voxel[] writeVolume;

    void Start()
    {
        meshFilter = meshObject.GetComponent<MeshFilter>();
        meshObject.transform.localScale = new Vector3(voxelSize, voxelSize, voxelSize);
        meshObject.transform.localPosition = new Vector3(volumeSize.x, volumeSize.y, volumeSize.z) * voxelSize / -2;

        XZ_AREA = volumeSize.x * volumeSize.z;
        XYZ_VOLUME = XZ_AREA * volumeSize.y;
        readVolume = new Voxel[XYZ_VOLUME+2];
        writeVolume = new Voxel[XYZ_VOLUME+2];
    }

    private void FixedUpdate()
    {
        UpdateVolume();
        GenerateMesh();
    }

    private void UpdateVolume()
    {
        int[] neighbors = new int[6];

        // Clear the write buffer
        Array.Fill(writeVolume, Voxel.None);
        writeVolume[3000] = Voxel.Sand;
        writeVolume[0] = Voxel.Wall;
        writeVolume[XYZ_VOLUME + 1] = Voxel.Wall;

        for (int i = 0; i < XYZ_VOLUME; i++)
        {
            Voxel currentVoxel = readVolume[i];

            switch (currentVoxel)
            {
                case Voxel.Sand:
                    GetSandNeighbors(neighbors, i);
                    int posYNeighbor = neighbors[DOWN];
                    if (readVolume[posYNeighbor] == Voxel.None)
                    {
                        writeVolume[posYNeighbor] = Voxel.Sand;
                    }
                    else
                    {
                        for (int j = DOWN_PX; j <= DOWN_NZ; j++)
                        {
                            if (readVolume[neighbors[j]] == Voxel.None)
                            {
                                writeVolume[neighbors[j]] = Voxel.Sand;
                                break;
                            }
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

        for (int i = 0; i < XYZ_VOLUME; i++)
        {
            Voxel currentVoxel = readVolume[i];
            if (currentVoxel == Voxel.Sand)
            {
                GetAdjacentNeighbors(neighbors, i);
                for (int j = ADJ_PX; j < ADJ_NY; j++)
                {
                    Voxel neighbor = readVolume[neighbors[j]];
                    if (neighbor == Voxel.None || neighbor == Voxel.Wall)
                    {
                        AddMeshFace(meshVertices, meshTriangles, i, j);
                    }
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(meshVertices);
        mesh.SetTriangles(meshTriangles, 0);
        meshFilter.mesh = mesh;
    }

    private void AddMeshFace(List<Vector3> vertices, List<int> tris, int index, int direction)
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

        tris.AddRange(new int[] { vindex, vindex+2, vindex+1, vindex+1, vindex+2, vindex+3 });
    }

    private void IndexToPos(ref int x, ref int y, ref int z, int index) {
        index -= 1;
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

        return Mathf.Clamp(x + z * volumeSize.x + y * XZ_AREA, -1, XYZ_VOLUME) + 1;
    }

    private void GetAdjacentNeighbors(int[] dest, int index)
    {
        int x = 0;
        int y = 0;
        int z = 0;
        IndexToPos(ref x, ref y, ref z, index);

        dest[ADJ_PX] = PosToIndex(x+1, y, z);
        dest[ADJ_NX] = PosToIndex(x+1, y, z);
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
}
