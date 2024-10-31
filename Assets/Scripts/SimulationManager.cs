using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class SimulationManager : MonoBehaviour
{
    [SerializeField] private GameObject spherePrefab;

    [SerializeField] private Vector3 sizeOfTestSpace;
    [SerializeField] private float spawnFromBorderOffset;
    [SerializeField] private int spheresCount;
    [SerializeField] private float spheresSpeed;
    [SerializeField] private float sphereRadius;
    [SerializeField] private bool checkCollisions;
    [SerializeField] private int chunksSeparationCount = 3; // min value should be 3

    public event Action<int> OnSpheresAdded;

    private NativeArray<int> chunksJobResult;
    private NativeArray<Chunk> chunks;

    private List<Transform> simulatedSpheres = new();

    private JobHandle spheresJobHandle = default;
    private JobHandle chunksJobHandle;

    private NativeArray<float3> currentPositions;
    private NativeArray<float3> directions;

    private int spheresToAdd = 0;
    private CancellationTokenSource simulationCancel;

    public int SpheresCount => simulatedSpheres.Count;

    public void StartSimulation()
    {
        _ = StartEarlyInitLoop();
    }

    public void StopSimulation()
    {
        if (simulationCancel != null)
        {
            simulationCancel.Cancel();
            spheresJobHandle.Complete();
            chunksJobHandle.Complete();
            TryDisposeAllArrays();
        }
    }

    public void AddSpheres(int count)
    {
        spheresToAdd += count;
    }

    private async UniTask StartEarlyInitLoop()
    {
        simulationCancel = new();
        SpawnSpheres(spheresToAdd);
        spheresToAdd = 0;

        while (true)
        {
            await UniTask.Yield(PlayerLoopTiming.LastInitialization, simulationCancel.Token);

            if (spheresToAdd != 0)
            {
                SpawnSpheres(spheresToAdd);
                spheresToAdd = 0;
            }
            StartChunkCalculations();

            // input events in lifecycle timeline

            await UniTask.Yield(PlayerLoopTiming.Update, simulationCancel.Token);

            FinishChunkCalculations();
            StartSpheresSimulation();

            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, simulationCancel.Token);

            spheresJobHandle.Complete();
            OnSimulationFrameComplited();
        }
    }

    private void OnDestroy()
    {
        StopSimulation();
    }

    private void SpawnSpheres(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var randomizedPosition = new Vector3(Random.Range(-(sizeOfTestSpace.x - spawnFromBorderOffset) / 2, (sizeOfTestSpace.x - spawnFromBorderOffset) / 2), Random.Range(-(sizeOfTestSpace.y - spawnFromBorderOffset) / 2, (sizeOfTestSpace.y - spawnFromBorderOffset) / 2), Random.Range(-(sizeOfTestSpace.z - spawnFromBorderOffset) / 2, (sizeOfTestSpace.z - spawnFromBorderOffset) / 2));
            randomizedPosition += transform.position;
            simulatedSpheres.Add(Instantiate(spherePrefab, transform).transform);
            simulatedSpheres[^1].transform.position = randomizedPosition;
            simulatedSpheres[^1].gameObject.SetActive(true);
        }

        if (currentPositions.IsCreated)
        {
            currentPositions.Dispose();
        }
        currentPositions = new NativeArray<float3>(simulatedSpheres.Count, Allocator.Persistent);
        for (int i = 0; i < simulatedSpheres.Count; i++)
        {
            currentPositions[i] = simulatedSpheres[i].position;
        }
        if (directions.IsCreated)
        {
            directions.Dispose();
        }
        directions = new NativeArray<float3>(currentPositions.Length, Allocator.Persistent);
        for (int i = 0; i < directions.Length; i++)
        {
            directions[i] = Random.insideUnitSphere;
        }

        OnSpheresAdded?.Invoke(spheresToAdd);
    }

    private void StartSpheresSimulation()
    {
        var job = new SpheresJob()
        {
            directions = directions,
            currentPositions = currentPositions,
            movingSpeed = this.spheresSpeed,
            deltaTime = Time.deltaTime,
            simulationSpacePosition = transform.position,
            simulationSpaceSize = sizeOfTestSpace,
            radiusOfSphere = sphereRadius,
            checkCollisions = checkCollisions,
            chunks = this.chunks,
            chunkIdPerSphere = chunksJobResult,
            chunksSeparationCount = this.chunksSeparationCount
        };

        spheresJobHandle = job.Schedule(simulatedSpheres.Count, 64);
    }

    private void StartChunkCalculations()
    {
        chunksJobResult = new NativeArray<int>(currentPositions.Length, Allocator.TempJob);

        var chunksJob = new ChunkJob()
        {
            separationCount = chunksSeparationCount,
            firstChunkPosition = transform.position - sizeOfTestSpace / 2f,
            positions = currentPositions,
            singleChunkSize = sizeOfTestSpace / chunksSeparationCount,
            result = chunksJobResult
        };

        chunksJobHandle = chunksJob.Schedule(simulatedSpheres.Count, 64);
    }

    private void FinishChunkCalculations()
    {
        chunksJobHandle.Complete();

        var chunksArray = new Chunk[chunksSeparationCount * chunksSeparationCount * chunksSeparationCount];
        for (int i = 0; i < chunksArray.Length; i++)
        {
            chunksArray[i] = new Chunk(Allocator.TempJob);
        }
        for (int i = 0; i < chunksJobResult.Length; i++)
        {
            chunksArray[chunksJobResult[i]].Add(i);
        }

        chunks = new NativeArray<Chunk>(chunksArray, Allocator.TempJob);
    }


    private void TryDisposeAllArrays()
    {
        if (currentPositions.IsCreated)
        {
            currentPositions.Dispose();
            directions.Dispose();
        }
        if (chunks.IsCreated)
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                chunks[i].Dispose();
            }
            chunks.Dispose();
        }
        if (chunksJobResult.IsCreated)
        {
            chunksJobResult.Dispose();
        }
    }

    private void OnSimulationFrameComplited()
    {
        for (int i = 0; i < currentPositions.Length; i++)
        {
            simulatedSpheres[i].position = currentPositions[i];
        }

        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i].Dispose();
        }
        chunks.Dispose();
        chunksJobResult.Dispose();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, sizeOfTestSpace);
    }
}
