using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Assets.Scripts.Jobs
{
    [BurstCompile]
    public struct PressureForce : IJobParallelFor
    {
        public NativeArray<float3> positions;       // 位置
        public NativeArray<float2> densities;       // 密度

        public void Execute(int index)
        {

        }
    }
}