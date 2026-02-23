using UnityEngine;

public class CuboidVolumeIterator
{
	private int dimX, dimY, dimZ, area;
    private int currentIndex;
    private Vector3Int currentPos;

    public CuboidVolumeIterator(Vector3Int volumeSize)
    {
        currentIndex = 0;
        dimX = volumeSize.x;
        dimY = volumeSize.y;
        dimZ = volumeSize.z;
        area = dimX * dimY * dimZ;
        currentPos = new Vector3Int(0, 0, 0);
    }

    public Vector3Int Next()
    {
        currentPos.x = currentIndex % dimX;
        currentPos.y = currentIndex / dimX % dimZ;
        currentPos.z = currentIndex / (dimX * dimZ);
        currentIndex++;

        return currentPos;
    }

    public bool HasNext()
    {
        return currentIndex < area;
    }

    public void Reset()
    {
        currentIndex = 0;
    }
}