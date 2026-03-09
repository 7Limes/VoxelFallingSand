using UnityEngine;

public class EraserTool : MonoBehaviour
{
    [SerializeField] private FallingSandVolume sandVolume;
    [SerializeField] private Transform erasePoint;

    public void OnActivate()
    {
        Vector3Int voxelPos = sandVolume.GetPosInVolume(erasePoint.position);
        if (voxelPos.x != -1)
        {
            sandVolume.SetVoxel(voxelPos.x, voxelPos.y, voxelPos.z, FallingSandVolume.Voxel.None);
        }
    }
}
