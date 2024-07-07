using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

// Contains the allocation data for a single job
// There are multiple instances of this class stored inside the voxel mesher to saturate the other threads
public class MeshJobHandler {
    // Temp copy for the voxel data
    public NativeArray<Voxel> voxels;

    // Native buffers for mesh data
    public NativeArray<float3> vertices;
    public NativeArray<float2> uvs;
    public NativeArray<int> tempTriangles;
    public NativeArray<int> permTriangles;

    // Native buffer for mesh generation data
    public NativeArray<int> indices;
    public NativeArray<byte> enabled;
    public NativeMultiCounter countersQuad;
    public NativeCounter counter;
    public NativeMultiCounter voxelCounters;

    // Native buffer for handling multiple materials
    public NativeParallelHashMap<ushort, int> materialHashMap;
    public NativeParallelHashSet<ushort> materialHashSet;
    public NativeArray<int> materialSegmentOffsets;
    public NativeCounter materialCounter;
    public JobHandle finalJobHandle;
    public VoxelChunk chunk;
    public bool colissions = false;
    public int startingFrame = 0;
    public int maxFrames = 0;
    public VertexAttributeDescriptor[] vertexAttributeDescriptors;

    public MeshJobHandler() {
        // Native buffers for mesh data
        int materialCount = VoxelUtils.MaxMaterialCount;
        vertices = new NativeArray<float3>(VoxelUtils.Volume, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        uvs = new NativeArray<float2>(VoxelUtils.Volume, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        tempTriangles = new NativeArray<int>(VoxelUtils.Volume * 6, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        permTriangles = new NativeArray<int>(VoxelUtils.Volume * 6, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        voxelCounters = new NativeMultiCounter(materialCount, Allocator.Persistent);
        voxels = new NativeArray<Voxel>(VoxelUtils.Volume, Allocator.Persistent, NativeArrayOptions.UninitializedMemory); 

        // Native buffer for mesh generation data
        indices = new NativeArray<int>(VoxelUtils.Volume, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        enabled = new NativeArray<byte>(VoxelUtils.Volume, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        countersQuad = new NativeMultiCounter(materialCount, Allocator.Persistent);
        counter = new NativeCounter(Allocator.Persistent);

        // Native buffer for handling multiple materials
        materialHashMap = new NativeParallelHashMap<ushort, int>(materialCount, Allocator.Persistent);
        materialHashSet = new NativeParallelHashSet<ushort>(materialCount, Allocator.Persistent);
        materialSegmentOffsets = new NativeArray<int>(materialCount, Allocator.Persistent);
        materialCounter = new NativeCounter(Allocator.Persistent);

        VertexAttributeDescriptor positionDesc = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0);
        VertexAttributeDescriptor uvDesc = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, 1);

        List<VertexAttributeDescriptor> descriptors = new List<VertexAttributeDescriptor> {
            positionDesc, uvDesc
        };
        
        vertexAttributeDescriptors = descriptors.ToArray();
    }
    public bool Free { get; private set; } = true;

    // Begin the vertex + quad job that will generate the mesh based on the current copied voxels
    // Copying of the voxel data must be done outside of this function
    internal JobHandle BeginJob(JobHandle dependency) {
        countersQuad.Reset();
        counter.Count = 0;
        materialCounter.Count = 0;
        materialHashSet.Clear();
        materialHashMap.Clear();
        Free = false;

        // Handles fetching MC corners for the SN edges
        CornerJob cornerJob = new CornerJob {
            voxels = voxels,
            enabled = enabled,
            size = VoxelUtils.Size,
        };

        // Calculates the number of materials within the mesh
        MaterialJob materialJob = new MaterialJob {
            voxels = voxels,
            materialHashSet = materialHashSet.AsParallelWriter(),
            materialHashMap = materialHashMap.AsParallelWriter(),
            materialCounter = materialCounter,
        };

        // Generate the vertices of the mesh
        // Executed only onces, and shared by multiple submeshes
        VertexJob vertexJob = new VertexJob {
            enabled = enabled,
            voxels = voxels,
            indices = indices,
            vertices = vertices,
            uvs = uvs,
            counter = counter,
        };

        // Generate the quads of the mesh (handles materials internally)
        QuadJob quadJob = new QuadJob {
            enabled = enabled,
            voxels = voxels,
            vertexIndices = indices,
            counters = countersQuad,
            triangles = tempTriangles,
            materialHashMap = materialHashMap.AsReadOnly(),
            materialCounter = materialCounter,
        };

        // Create sum job to calculate offsets for each material type 
        SumJob sumJob = new SumJob {
            materialCounter = materialCounter,
            materialSegmentOffsets = materialSegmentOffsets,
            countersQuad = countersQuad
        };

        // Create a copy job that will copy temp memory to perm memory
        CopyJob copyJob = new CopyJob {
            materialSegmentOffsets = materialSegmentOffsets,
            tempTriangles = tempTriangles,
            permTriangles = permTriangles,
            materialCounter = materialCounter,
            counters = countersQuad,
        };

        // Start the corner job
        JobHandle cornerJobHandle = cornerJob.Schedule(VoxelUtils.Volume, 2048 * VoxelUtils.SchedulingInnerloopBatchCount, dependency);

        // Start the material job
        JobHandle materialJobHandle = materialJob.Schedule(VoxelUtils.Volume, 2048 * VoxelUtils.SchedulingInnerloopBatchCount, dependency);

        // Start the vertex job
        JobHandle vertexDep = JobHandle.CombineDependencies(cornerJobHandle, dependency);
        JobHandle vertexJobHandle = vertexJob.Schedule(VoxelUtils.Volume, 2048 * VoxelUtils.SchedulingInnerloopBatchCount, vertexDep);

        // Start the quad job
        JobHandle merged = JobHandle.CombineDependencies(materialJobHandle, vertexJobHandle, cornerJobHandle);
        JobHandle quadJobHandle = quadJob.Schedule(VoxelUtils.Volume, 2048 * VoxelUtils.SchedulingInnerloopBatchCount, merged);

        // Start the sum job 
        JobHandle sumJobHandle = sumJob.Schedule(VoxelUtils.MaxMaterialCount, 32, quadJobHandle);

        // Start the copy job
        JobHandle copyJobHandle = copyJob.Schedule(VoxelUtils.MaxMaterialCount, 1, sumJobHandle);

        finalJobHandle = copyJobHandle;
        return finalJobHandle;
    }

    // Complete the jobs and return a mesh
    internal VoxelMesh Complete(Mesh mesh) {
        if (chunk == null) {
            return VoxelMesh.Empty;
        }

        finalJobHandle.Complete();
        Free = true;

        // Get the max number of materials we generated for this mesh
        int maxMaterials = materialCounter.Count;

        // Get the max number of vertices (shared by submeshes)
        int maxVertices = counter.Count;

        // Count the max number of indices (sum of all submesh indices)
        int maxIndices = 0;

        // Count the number of indices we will have in maximum (all material indices combined)
        for (int i = 0; i < maxMaterials; i++) {
            maxIndices += countersQuad[i] * 6;
        }

        // Set mesh shared vertices
        mesh.Clear();

        mesh.SetVertexBufferParams(maxVertices, vertexAttributeDescriptors);
        mesh.SetVertexBufferData(vertices.Reinterpret<Vector3>(), 0, 0, maxVertices, 0, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds);
        mesh.SetVertexBufferData(uvs.Reinterpret<Vector2>(), 0, 0, maxVertices, 1, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);


        // Set mesh indices
        mesh.SetIndexBufferParams(maxIndices, IndexFormat.UInt32);
        mesh.SetIndexBufferData(permTriangles, 0, 0, maxIndices);
        mesh.subMeshCount = maxMaterials;

        // Create a material array for the new materials
        int[] lookup = new int[maxMaterials];

        // Convert material index to material *count* index
        foreach (var item in materialHashMap) {
            lookup[item.Value] = item.Key;
        }

        // Keep this so we can do material mesh intersection lookups later
        (byte, int)[] triangleOffsetLocalMaterials = new (byte, int)[maxMaterials];

        // Set mesh submeshes
        for (int i = 0; i < maxMaterials; i++) {
            int countIndices = countersQuad[i] * 6;
            int segmentOffset = materialSegmentOffsets[i];
            triangleOffsetLocalMaterials[i] = ((byte)i, segmentOffset / 3);

            if (countIndices > 0) {
                mesh.SetSubMesh(i, new SubMeshDescriptor {
                    indexStart = segmentOffset,
                    indexCount = countIndices,
                    topology = MeshTopology.Triangles,
                }, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds);
            }
        }

        mesh.RecalculateBounds(MeshUpdateFlags.DontValidateIndices);
        triangleOffsetLocalMaterials = triangleOffsetLocalMaterials.OrderBy(x => x.Item2).ToArray();

        chunk = null;
        return new VoxelMesh {
            VoxelMaterialsLookup = lookup,
            ComputeCollisions = colissions,
            VertexCount = maxVertices,
            TriangleCount = maxIndices / 3,
            TriangleOffsetLocalMaterials = triangleOffsetLocalMaterials,
        };
    }

    // Dispose of the underlying memory allocations
    internal void Dispose() {
        voxels.Dispose();
        indices.Dispose();
        uvs.Dispose();
        vertices.Dispose();
        counter.Dispose();
        countersQuad.Dispose();
        tempTriangles.Dispose();
        permTriangles.Dispose();
        materialCounter.Dispose();
        materialHashMap.Dispose();
        materialHashSet.Dispose();
        materialSegmentOffsets.Dispose();
        enabled.Dispose();
        voxelCounters.Dispose();
    }
}