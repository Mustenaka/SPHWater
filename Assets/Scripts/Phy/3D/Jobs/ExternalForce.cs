using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Assets.Scripts.Jobs
{
    /// <summary>
    /// 计算扩展力（主要是重力系统），在这个模块中也可以添加一些交互力之类的Job任务
    /// </summary>
    [BurstCompile]
    public struct ExternalForce : IJobParallelFor
    {
        [NativeDisableUnsafePtrRestriction] public NativeArray<float3> positions;
        [NativeDisableUnsafePtrRestriction] public NativeArray<float3> velocities;
        [NativeDisableUnsafePtrRestriction] public NativeArray<float3> nextPositions;

        [NativeDisableUnsafePtrRestriction] public NativeArray<float3> externalForce;
        [ReadOnly] public NativeReference<float3> gravity;

        [ReadOnly] public float dt;

        public void Execute(int index)
        {
            // 计算扩展力 （相当于重力，如果有扩展，比如说风阻，在这里扩展添加）
            externalForce[index] = gravity.Value;

            // 力学使用 | 显式欧拉
            velocities[index] += (externalForce[index] * dt); 

            // 应用力学做第一份nextPosition
            nextPositions[index] = positions[index] + velocities[index] * dt;
        }
    }
}