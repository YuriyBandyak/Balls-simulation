using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct ChunkJob : IJobParallelFor
{
    public int separationCount;
    public float3 firstChunkPosition;
    public NativeArray<float3> positions;
    public float3 singleChunkSize;

    public NativeArray<int> result;

    public void Execute(int index)
    {
        result[index] = GetChunkId(index);
    }

    public int GetChunkId(int sphereIndex)
    {
        float3 localPosition = positions[sphereIndex] - firstChunkPosition;
        int3 result = new()
        {
            x = (int)(localPosition.x / singleChunkSize.x),
            y = (int)(localPosition.y / singleChunkSize.y),
            z = (int)(localPosition.z / singleChunkSize.z)
        };
        for (int i = 0; i < 3; i++)
        {
            if (result[i] == separationCount)
            {
                result[i] -= 1;
            }
        }

        return result.x + result.y * separationCount + result.z * separationCount * separationCount;
    }
}
