using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Assets.Scripts.Jobs
{
    [BurstCompile]
    public struct PressureForce : IJobParallelFor
    {
        public void Execute(int index)
        {
        }
    }
}