using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct SpheresJob : IJobParallelFor
{
    public readonly static float3 RIGHT = new(1f, 0, 0);
    public readonly static float3 UP = new(0, 1f, 0);
    public readonly static float3 FORWARD = new(0, 0, 1f);

    public NativeArray<float3> directions;
    [NativeDisableParallelForRestriction] public NativeArray<float3> currentPositions; // TODO: change to two  arrays, one simple and one readonly
    public float movingSpeed;
    public float deltaTime;
    public float3 simulationSpacePosition;
    public float3 simulationSpaceSize;
    public float radiusOfSphere;
    public bool checkCollisions;
    public int chunksSeparationCount;

    [ReadOnly]
    public NativeArray<Chunk> chunks;
    [ReadOnly]
    public NativeArray<int> chunkIdPerSphere;

    public void Execute(int i)
    {
        currentPositions[i] += deltaTime * movingSpeed * directions[i];
        if (checkCollisions)
        {
            CheckCollisionsWithOtherSpheres(i);
        }
        CheckSimulationSpaceBorder(i);
    }

    private void CheckSimulationSpaceBorder(int i)
    {
        var currentPosition = currentPositions[i];
        var localPosition = currentPosition - simulationSpacePosition;

        if (math.abs(localPosition.x) > math.abs(simulationSpaceSize.x / 2f))
        {
            Bounce(i, RIGHT * math.sign(localPosition.x));
        }
        if (math.abs(localPosition.y) > math.abs(simulationSpaceSize.y / 2f))
        {
            Bounce(i, UP * math.sign(localPosition.y));
        }
        if (math.abs(localPosition.z) > math.abs(simulationSpaceSize.z / 2f))
        {
            Bounce(i, FORWARD * math.sign(localPosition.z));
        }
    }

    private void CheckCollisionsWithOtherSpheres(int i)
    {
        int currentChunk = chunkIdPerSphere[i];

        var closeChunks = new NativeArray<int>(27, Allocator.Temp);
        int index = 0;

        int chunkX = currentChunk % chunksSeparationCount;
        int chunkY = (currentChunk / chunksSeparationCount) % chunksSeparationCount;
        int chunkZ = currentChunk / (chunksSeparationCount * chunksSeparationCount);

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    int newX = chunkX + x;
                    int newY = chunkY + y;
                    int newZ = chunkZ + z;

                    if (newX >= 0 && newX < chunksSeparationCount &&
                        newY >= 0 && newY < chunksSeparationCount &&
                        newZ >= 0 && newZ < chunksSeparationCount)
                    {
                        int newChunk = newX + newY * chunksSeparationCount + newZ * chunksSeparationCount * chunksSeparationCount;
                        closeChunks[index++] = newChunk;
                    }
                    else
                    {
                        closeChunks[index++] = -1;
                    }
                }
            }
        }

        int closestSphereIndex = -1;
        float closestDistance = float.MaxValue;

        for (int j = 0; j < closeChunks.Length; j++)
        {
            if (closeChunks[j] == -1)
            {
                continue;
            }
            for (int a = 0; a < chunks[closeChunks[j]].Length; a++)
            {
                int sphereToCheck = chunks[closeChunks[j]].GetSphereId(a);
                if (i == sphereToCheck)
                {
                    continue;
                }
                var distance = math.distance(currentPositions[i], currentPositions[sphereToCheck]);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestSphereIndex = sphereToCheck;
                }
            }
        }

        if (closestSphereIndex != -1 && closestDistance < radiusOfSphere * 2f)
        {
            directions[i] = math.normalize(currentPositions[i] - currentPositions[closestSphereIndex]);
        }
    }

    private void Bounce(int i, float3 unbounceDirection)
    {
        directions[i] = math.reflect(directions[i], unbounceDirection);
    }
}
