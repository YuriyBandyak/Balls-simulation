using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public partial class SimulationManager
{
    public struct SpheresJob : IJobParallelFor
    {
        public NativeArray<Vector3> directions;
        [NativeDisableParallelForRestriction] public NativeArray<Vector3> currentPositions;
        public float movingSpeed;
        public float deltaTime;
        public Vector3 simulationSpacePosition;
        public Vector3 simulationSpaceSize;
        public float radiusOfSphere;

        public void Execute(int i)
        {
            currentPositions[i] += directions[i] * deltaTime * movingSpeed;
            CheckSimulationSpaceBorder(i);
            CheckCollisionsWithOtherSpheres(i);
        }

        private void CheckSimulationSpaceBorder(int i)
        {
            if (currentPositions[i].x < simulationSpacePosition.x + (-simulationSpaceSize.x / 2))
            {
                Bounce(i, Vector3.right);
            }
            if (currentPositions[i].x > simulationSpacePosition.x + (simulationSpaceSize.x / 2))
            {
                Bounce(i, Vector3.left);
            }
            if (currentPositions[i].y < simulationSpacePosition.y + (-simulationSpaceSize.y / 2))
            {
                Bounce(i, Vector3.up);
            }
            if (currentPositions[i].y > simulationSpacePosition.y + (simulationSpaceSize.y / 2))
            {
                Bounce(i, Vector3.down);
            }
            if (currentPositions[i].z < simulationSpacePosition.z + (-simulationSpaceSize.z / 2))
            {
                Bounce(i, Vector3.forward);
            }
            if (currentPositions[i].z > simulationSpacePosition.z + (simulationSpaceSize.z / 2))
            {
                Bounce(i, Vector3.back);
            }
        }

        private void CheckCollisionsWithOtherSpheres(int i)
        {
            for (int a = 0; a < currentPositions.Length; a++)
            {
                if (a == i)
                {
                    continue;
                }
                if ((currentPositions[i] - currentPositions[a]).sqrMagnitude <= radiusOfSphere * radiusOfSphere)
                {
                    directions[i] = (currentPositions[i] - currentPositions[a]).normalized;
                }
            }
        }

        private void Bounce(int i, Vector3 unbounceDirection)
        {
            directions[i] = Vector3.Reflect(directions[i], unbounceDirection);
        }
    }
}
