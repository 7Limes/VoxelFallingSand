using UnityEngine;

public class SandBrush : MonoBehaviour
{
    [SerializeField] private FallingSandVolume sandVolume;
    [SerializeField] private Transform brushPoint;

    [SerializeField] private FallingSandVolume.Voxel voxelType = FallingSandVolume.Voxel.Sand;
    [SerializeField] private Potentiometer sizePotentiometer;

    public void OnActivate()
    {
        Vector3Int voxelPos = sandVolume.GetPosInVolume(brushPoint.position);
        if (voxelPos.x == -1)
        {
            return;
        }

        float brushSize = sizePotentiometer.ReadValue();
        int halfSize = Mathf.Max((int)brushSize / 2, 1);

        for (int x = voxelPos.x - halfSize; x < voxelPos.x + halfSize; x++)
        {
            for (int y = voxelPos.y - halfSize; y < voxelPos.y + halfSize; y++)
            {
                for (int z = voxelPos.z - halfSize; z < voxelPos.z + halfSize; z++)
                {
                    sandVolume.SetVoxel(x, y, z, voxelType); 
                }
            }
        }
    }
}
