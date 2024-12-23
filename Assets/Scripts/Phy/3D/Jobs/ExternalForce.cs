using Unity.Burst;
using Unity.Collections;
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
        [WriteOnly] public NativeArray<float3> externalForce;
        [ReadOnly] public NativeReference<float3> gravity;

        public void Execute(int index)
        {
            externalForce[index] = gravity.Value;
        }
    }
}