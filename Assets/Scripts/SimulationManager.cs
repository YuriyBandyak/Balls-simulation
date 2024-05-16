using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public partial class SimulationManager : MonoBehaviour
{
    [SerializeField] private GameObject spherePrefab;

    #region OldStuff
    [SerializeField] private Vector3 sizeOfTestSpace;
    [SerializeField] private float spawnFromBorderOffset;
    [SerializeField] private int spheresCount;
    [SerializeField] private float spheresSpeed;
    [SerializeField] private float sphereRadius;

    private List<Transform> simulatedSpheres = new();

    private JobHandle jobHandle = default;
    private NativeArray<Vector3> currentPositions;
    private NativeArray<Vector3> directions;

    private Vector3[] currentPositionsDebug;
    private Vector3[] directionsDebug;
    #endregion

    private bool isSimulationRunning = false;
    private int currentSpheresCount = 0;

    public void StartSimulation()
    {
        Debug.Log("Starting simulation");
        isSimulationRunning = true;
    }

    public void StopSimulation()
    {
        Debug.Log("Stopping simulation");
        currentSpheresCount = 0;
        isSimulationRunning = false;
    }

    public void AddSpheres(int count)
    {
        currentSpheresCount += count;
        Debug.Log($"Adding {count} spheres. Current count: {currentSpheresCount}");
        SpawnSpheres(count);
    }

    private void Update()
    {
        if (isSimulationRunning)
        {
            RunSimulationInFrame();
        }
    }

    private void LateUpdate()
    {
        if (isSimulationRunning)
        {
            jobHandle.Complete();
            OnSimulationFrameComplited();
        }
    }

    private void OnDestroy()
    {
        currentPositions.Dispose();
        directions.Dispose();
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
    }

    private void RunSimulationInFrame()
    {
        if (currentPositions.Length != simulatedSpheres.Count)
        {
            currentPositions = new NativeArray<Vector3>(simulatedSpheres.Select(x => x.position).ToArray(), Allocator.Persistent);
            directions = new NativeArray<Vector3>(currentPositions.Length, Allocator.Persistent);
            for (int i = 0; i < directions.Length; i++)
            {
                directions[i] = Random.insideUnitSphere;
            }
        }

        var job = new SpheresJob()
        {
            directions = directions,
            currentPositions = currentPositions,
            movingSpeed = this.spheresSpeed,
            deltaTime = Time.deltaTime,
            simulationSpacePosition = transform.position,
            simulationSpaceSize = sizeOfTestSpace,
            radiusOfSphere = sphereRadius
        };

        jobHandle = job.Schedule(simulatedSpheres.Count, 64);
    }

    private void OnSimulationFrameComplited()
    {
        for (int i = 0; i < currentPositions.Length; i++)
        {
            simulatedSpheres[i].position = currentPositions[i];
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, sizeOfTestSpace);
    }
}
