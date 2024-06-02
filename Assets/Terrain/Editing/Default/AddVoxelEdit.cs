using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[assembly: RegisterGenericJobType(typeof(VoxelEditJob<AddVoxelEdit>))]

// Will either add / remove matter from the terrain
public struct AddVoxelEdit : IVoxelEdit {
    [ReadOnly] public float3 center;
    [ReadOnly] public float strength;
    [ReadOnly] public float radius;
    [ReadOnly] public byte material;
    [ReadOnly] public bool writeMaterial;

    public JobHandle Apply(float3 offset, NativeArray<Voxel> voxels) {
        return IVoxelEdit.ApplyGeneric(this, offset, voxels);
    }

    public Bounds GetBounds() {
        return new Bounds {
            center = center,
            extents = new Vector3(radius, radius, radius)
        };
    }

    public Voxel Modify(float3 position, Voxel voxel) {
        float density = math.length(position - center) - radius;
        voxel.material = (density < 1.0F && writeMaterial && strength < 0) ? material : voxel.material;
        voxel.density += (half)(density < 0.0F ? strength : 0f);
        return voxel;
    }
}