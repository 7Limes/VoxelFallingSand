using UnityEngine;

public class FallingSandVolume : MonoBehaviour
{
    [SerializeField] private Vector3Int volumeSize;
    [SerializeField] private float voxelSize = 0.01f;

    enum Voxel : byte
    {
        None,
        Sand
    }

    private Voxel[] volume;
    private CuboidVolumeIterator volumeIterator;

    void Start()
    {
        volume = new Voxel[volumeSize.x * volumeSize.y * volumeSize.z];
        volumeIterator = new CuboidVolumeIterator(volumeSize);
    }

    private void FixedUpdate()
    {
        
    }

    private void OnDrawGizmos()
    {
        Vector3 fullVolumeSize = new Vector3(volumeSize.x, volumeSize.y, volumeSize.z) * voxelSize;
        Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 0.75f);
        Gizmos.DrawWireCube(transform.position, fullVolumeSize);
    }
}
