using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

// Interface for voxel edits that will modify pre-existing terrain chunk data
public interface IVoxelEdit {
    // Modify pre-generated or pre-modified terrain voxel data
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Voxel Modify(float3 position, Voxel voxel);

    // Get the AABB bounds of this voxel edit
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bounds GetBounds();

    // MUST CALL THE "ApplyGeneric" function because we can't hide away generics
    public JobHandle Apply(float3 offset, NativeArray<Voxel> voxels, NativeMultiCounter counters);

    // Apply any generic voxel edit onto oncoming data
    public static JobHandle ApplyGeneric<T>(T edit, float3 offset, NativeArray<Voxel> voxels, NativeMultiCounter counters) where T : struct, IVoxelEdit {
        VoxelEditJob<T> job = new VoxelEditJob<T> {
            offset = offset,
            edit = edit,
            voxels = voxels,
            //counters = counters
        };
        return job.Schedule(VoxelUtils.Volume, 2048 * VoxelUtils.EditsScheduleCount);
    }
}
