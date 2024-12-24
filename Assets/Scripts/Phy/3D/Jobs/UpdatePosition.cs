using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Assets.Scripts.Jobs
{
    public struct UpdatePosition : IJobParallelFor
    {
        [NativeDisableUnsafePtrRestriction] public NativeArray<float3> position;        // 粒子位置
        [NativeDisableUnsafePtrRestriction] public NativeArray<float3> velocities;      // 速度

        [ReadOnly] public float dt;        // 时间微分

        public void Execute(int index)
        {
            position[index] += velocities[index] * dt;
        }
    }
}