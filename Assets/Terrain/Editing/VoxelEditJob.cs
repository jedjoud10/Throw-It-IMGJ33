using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

// Edit job that will modify the voxel chunk data DIRECTLY
[BurstCompile(CompileSynchronously = true)]
struct VoxelEditJob<T> : IJobParallelFor
    where T : struct, IVoxelEdit {
    [ReadOnly] public float3 offset;

    public T edit;
    public NativeArray<Voxel> voxels;

    //public NativeMultiCounter.Concurrent counters;

    public void Execute(int index) {
        uint3 id = VoxelUtils.IndexToPos(index);
        float3 position = (math.float3(id));

        // Needed for voxel size reduction
        position *= VoxelUtils.VoxelSizeFactor;
        position -= 1.5f * VoxelUtils.VoxelSizeFactor;

        //position -= math.float3(1);
        position *= VoxelUtils.VertexScaling;
        position += offset;

        // Read, modify, write
        Voxel oldVoxel = voxels[index];
        Voxel newVoxel = edit.Modify(position, oldVoxel);
        voxels[index] = newVoxel;

        /*
        // Keep track the number of valid voxels of each materials
        half oldDensity = oldVoxel.density;
        half newDensity = newVoxel.density;
        if (newDensity > 0.0f && oldDensity < 0.0f) {
            counters.Increment(newVoxel.material);
        } else if (newDensity < 0.0f && oldDensity > 0.0f) {
            counters.Decrement(newVoxel.material);
        }
        */
    }
}