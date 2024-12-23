using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Assets.Scripts.Jobs
{
    /// <summary>
    /// 速度解算Job
    ///     显式牛顿法驱动计算
    /// </summary>
    [BurstCompile]
    public struct VelocitySolutionJob : IJobParallelFor
    {
        [NativeDisableUnsafePtrRestriction] public NativeArray<float3> positions;
        [NativeDisableUnsafePtrRestriction] public NativeArray<float3> velocitys;

        [NativeDisableUnsafePtrRestriction] public NativeArray<float3> externalForce;
        [NativeDisableUnsafePtrRestriction] public NativeArray<float3> pressureForce;
        [NativeDisableUnsafePtrRestriction] public NativeArray<float3> viscosityForce;

        [ReadOnly] public float dt; 

        public void Execute(int index)
        {
            var totalForce = externalForce[index] + pressureForce[index] + viscosityForce[index];

            velocitys[index] += (totalForce * dt);
            positions[index] += (velocitys[index] * dt);
        }
    }
}