using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Assets.Scripts.Jobs
{
    /// <summary>
    /// 压力计算
    /// </summary>
    [BurstCompile]
    public struct PressureForce : IJobParallelFor
    {
        public NativeArray<float3> positions;       // 位置
        public NativeArray<float2> densities;       // 密度

        public NativeReference<float> targetDensity;    // 目标密度
        public NativeReference<float> pressureMultiplier;   // 压力系数

        public void Execute(int index)
        {

        }

        /// <summary>
        /// 从密度计算压力
        /// </summary>
        /// <param name="density"></param>
        /// <returns></returns>
        private float PressureFromDensity(float density)
        {
            return (density - targetDensity.Value) * pressureMultiplier.Value;
        }

        /// <summary>
        /// 计算临接压力
        /// </summary>
        /// <param name="nearDensity"></param>
        /// <returns></returns>
        private float NearPressureFromDensity(float nearDensity)
        {
            return nearDensity * pressureMultiplier.Value;
        }
    }
}