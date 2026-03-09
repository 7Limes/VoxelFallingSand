using UnityEngine;

public class SandBrush : MonoBehaviour
{
    [SerializeField] private FallingSandVolume sandVolume;
    [SerializeField] private Transform brushPoint;

    public void OnActivate()
    {
        Vector3Int voxelPos = sandVolume.GetPosInVolume(brushPoint.position);
        if (voxelPos.x != -1)
        {
            sandVolume.SetVoxel(voxelPos.x, voxelPos.y, voxelPos.z, FallingSandVolume.Voxel.Sand);
        }
    }
}
