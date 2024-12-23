using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.Scripts.Jobs
{
    /// <summary>
    /// 计算碰撞边界并反弹
    /// </summary>
    [BurstCompile]
    public struct BoundJob : IJobParallelFor
    {
        [NativeDisableUnsafePtrRestriction] public NativeArray<float3> positions;
        [NativeDisableUnsafePtrRestriction] public NativeArray<float3> velocitys;
        [ReadOnly] public NativeReference<float> particleRadius;


        [ReadOnly] public NativeReference<float3> boundCenter;
        [ReadOnly] public NativeReference<float3> boundSize;
        [ReadOnly] public NativeReference<float> CollisionDamping;

        public void Execute(int index)
        {
            var halfBoundSize = boundSize.Value / 2 - (float3)(Vector3.one * particleRadius.Value);
            var halfBound = boundCenter.Value + halfBoundSize;
            
            var position = positions[index];
            var velocity = velocitys[index];

            if (math.abs(positions[index].x) > halfBound.x)
            {
                position.x = halfBound.x * math.sign(positions[index].x);
                velocity.x *= (-1 * CollisionDamping.Value);
            }

            if (math.abs(positions[index].y) > halfBound.y)
            {
                position.y = halfBound.y * math.sign(positions[index].y);
                velocity.y *= (-1 * CollisionDamping.Value);
            }

            if (math.abs(positions[index].z) > halfBound.z)
            {
                position.z = halfBound.z * math.sign(positions[index].z);
                velocity.z *= (-1 * CollisionDamping.Value);
            }

            positions[index] = position;
            velocitys[index] = velocity;
        }
    }
}