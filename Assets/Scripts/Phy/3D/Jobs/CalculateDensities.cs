using System;
using System.Security.Cryptography;
using SPHWater;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.Scripts.Jobs
{
    /// <summary>
    /// 计算密度
    /// </summary>
    [BurstCompile]
    public struct CalculateDensities : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> positions;    // 位置
        [WriteOnly] public NativeArray<float2> densities;   // 密度

        [ReadOnly] public NativeReference<float> smoothingRadius;  // 平滑半径

        public void Execute(int index)
        {
            var position = positions[index];

            var sqrRadius = smoothingRadius.Value * smoothingRadius.Value;
            float density = 0;
            float nearDensity = 0;

            for (int i = 0; i < positions.Length; i++)
            {
                var neighbourPosition = position[i];

                var offsetToNeighbour = neighbourPosition - position;
                var sqrDstToNeighbour = math.dot(offsetToNeighbour, offsetToNeighbour);

                // skip if not within radius
                if (sqrDstToNeighbour > sqrRadius)
                {
                    continue;
                }

                // calculate density and near density
                var dst = math.sqrt(sqrDstToNeighbour);
                density += PMath.SpikyKernelPow2(dst, smoothingRadius.Value);
                nearDensity += PMath.SpikyKernelPow3(dst, smoothingRadius.Value);
            }

            //if (density > 0)
            //{
            //    Debug.Log($"Index:{index}, density:{density}, nearDensity:{nearDensity}");
            //}

            densities[index] = new float2(density, nearDensity);

            //var tmp = densities[index];
            //tmp[0] = density;
            //tmp[1] = nearDensity;
            //densities[index] = tmp;
        }
    }
}