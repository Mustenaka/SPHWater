using System;
using Assets.Scripts;
using Assets.Scripts.Jobs;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// SPH模拟器
///     SPHSimulate():      构造基本参数
///     Dispose():          析构参数 
///     DataReference():    传递参数引用
///     Simulate():         执行单步模拟
///
///     GetPositions():     获取所有粒子位置参数（用于其他模块提供渲染）
/// </summary>
public class SPHSimulate
{
    public int ParticleCount => _positions.Length;  // 粒子数量
    public NativeArray<float3> _positions;      // 粒子位置
    public NativeArray<float3> _nextPosition;   // 粒子位置（下一帧）
    public NativeArray<float3> _velocities;     // 粒子速度
    public NativeArray<float3> _externalForce;  // 额外力（一般是重力） F_total = f_external + f_pressure + f_viscosity
    public NativeArray<float3> _pressureForce;  // 压力
    public NativeArray<float3> _viscosityForce; // 粘度力
    public NativeArray<float2> _densities;      // 密度

    public NativeReference<float> _radius;      // 粒子半径
    public NativeReference<float> _targetDensity;           // 目标密度
    public NativeReference<float> _pressureMultiplier;      // 压力系数
    public NativeReference<float> _nearPressureMultiplier;  // 邻近压力系数
    public NativeReference<float> _viscosityStrength;       // 粘度强度
    public NativeReference<float> _smoothingRadius; // 平滑半径

    public NativeReference<float3> _gravity;        // 重力
    public NativeReference<float3> _boundCenter;    // 包围盒中心
    public NativeReference<float3> _boundSize;      // 包围盒尺寸
    public NativeReference<float> _collisionDamping;// 碰撞阻尼

    public const int threadCount = 128;
    public const uint hashK1 = 15823;
    public const uint hashK2 = 9737333;
    public const uint hashK3 = 440817757;
    public NativeArray<uint3> SpatialIndices;
    public NativeArray<uint3> SpatialOffsets;

    public SPHSimulate(SPHInitData initData)
    {
        /* 粒子计算参数 */
        _positions = new NativeArray<float3>(initData.ParticleCount, Allocator.Persistent);
        _velocities = new NativeArray<float3>(initData.ParticleCount, Allocator.Persistent);
        _nextPosition = new NativeArray<float3>(initData.ParticleCount, Allocator.Persistent);
        _externalForce = new NativeArray<float3>(initData.ParticleCount, Allocator.Persistent);
        _pressureForce = new NativeArray<float3>(initData.ParticleCount, Allocator.Persistent);
        _viscosityForce = new NativeArray<float3>(initData.ParticleCount, Allocator.Persistent);
        _densities = new NativeArray<float2>(initData.ParticleCount, Allocator.Persistent);

        _positions.CopyFrom(Array.ConvertAll(initData.Positions, v => (float3)v));
        _velocities.CopyFrom(Array.ConvertAll(initData.Velocitys, v => (float3)v));

        /* 环境参数 */
        _radius = new NativeReference<float>(initData.Radius, Allocator.Persistent);
        _targetDensity = new NativeReference<float>(initData.TargetDensity, Allocator.Persistent);
        _pressureMultiplier = new NativeReference<float>(initData.PressureMultiplier, Allocator.Persistent);
        _nearPressureMultiplier = new NativeReference<float>(initData.NearPressureMultiplier, Allocator.Persistent);
        _viscosityStrength = new NativeReference<float>(initData.ViscosityStrength, Allocator.Persistent);
        _smoothingRadius = new NativeReference<float>(initData.SmoothingRadius, Allocator.Persistent);

        /* 外包围盒参数 */
        _gravity = new NativeReference<float3>(initData.Gravity, Allocator.Persistent);
        _boundCenter = new NativeReference<float3>(initData.BoundCenter, Allocator.Persistent);
        _boundSize = new NativeReference<float3>(initData.BoundSize, Allocator.Persistent);
        _collisionDamping = new NativeReference<float>(initData.CollisionDamping, Allocator.Persistent);

        /* HashTable 优化*/
        SpatialIndices = new NativeArray<uint3>(initData.ParticleCount, Allocator.Persistent);
        SpatialOffsets = new NativeArray<uint3>(initData.ParticleCount, Allocator.Persistent);
    }

    public void Dispose()
    {
        /* 粒子计算参数 */
        _positions.Dispose();
        _velocities.Dispose();
        _nextPosition.Dispose();
        _externalForce.Dispose();
        _pressureForce.Dispose();
        _viscosityForce.Dispose();
        _densities.Dispose();

        /* 环境参数 */
        _radius.Dispose();
        _targetDensity.Dispose();
        _pressureMultiplier.Dispose();
        _nearPressureMultiplier.Dispose();
        _viscosityStrength.Dispose();
        _smoothingRadius.Dispose();

        /* 外包围盒参数 */
        _gravity.Dispose();
        _boundCenter.Dispose();
        _boundSize.Dispose();
        _collisionDamping.Dispose();

        /* HashTable 优化*/
        SpatialIndices.Dispose();
        SpatialOffsets.Dispose();
    }

    /// <summary>
    /// 动态传递值，主要是传递一些环境因素，会导致流体模拟发生变化的属性
    /// </summary>
    /// <param name="reference"></param>
    public void DataReference(SPHReferenceData reference)
    {
        _gravity.Value = reference.Gravity;
        _boundCenter.Value = reference.BoundCenter;
        _boundSize.Value = reference.BoundSize;
        _collisionDamping.Value = reference.CollisionDamping;

        _radius.Value = reference.Radius;
        _targetDensity.Value = reference.TargetDensity;
        _pressureMultiplier.Value = reference.PressureMultiplier;
        _nearPressureMultiplier.Value = reference.NearPressureMultiplier;
        _viscosityStrength.Value = reference.ViscosityStrength;
        _smoothingRadius.Value = reference.SmoothingRadius;
    }

    /// <summary>
    /// 每一针模拟
    /// </summary>
    public void Simulate(float dt)
    {
        JobHandle handle = new();

        handle = DoExternalForce(dt, handle);
        handle = DoCalcDensities(handle);
        handle = DoUpdatePosition(dt, handle);
        //handle = DoVelocitySolution(dt, handle);  // 不用这个了
        handle = DoBoundJob(handle);

        handle.Complete();
    }

    #region Jobs

    /// <summary>
    /// 计算扩展力
    /// </summary>
    /// <param name="depend"></param>
    /// <returns></returns>
    private JobHandle DoExternalForce(float dt, JobHandle depend)
    {
        var job = new ExternalForce()
        {
            positions = _positions,
            velocities = _velocities,
            nextPositions = _nextPosition,

            externalForce = _externalForce,
            gravity = _gravity,

            dt = dt,
        };

        return job.Schedule(ParticleCount, threadCount, depend);
    }

    /// <summary>
    /// 计算密度
    /// </summary>
    /// <param name="depend"></param>
    /// <returns></returns>
    private JobHandle DoCalcDensities(JobHandle depend)
    {
        var job = new CalculateDensities()
        {
            positions = _nextPosition,
            densities = _densities,

            smoothingRadius = _smoothingRadius,
        };

        return job.Schedule(ParticleCount, threadCount, depend);
    }

    /// <summary>
    /// 牛顿法通过速度得到位置更新
    /// </summary>
    /// <param name="dt"></param>
    /// <param name="depend"></param>
    /// <returns></returns>
    private JobHandle DoVelocitySolution(float dt, JobHandle depend)
    {
        var job = new VelocitySolutionJob()
        {
            positions = _positions,
            velocities = _velocities,

            externalForce = _externalForce,
            pressureForce = _pressureForce,
            viscosityForce = _viscosityForce,

            dt = dt,
        };

        return job.Schedule(ParticleCount, threadCount, depend);
    }

    /// <summary>
    /// 更新位置
    /// </summary>
    /// <param name="dt"></param>
    /// <param name="depend"></param>
    /// <returns></returns>
    private JobHandle DoUpdatePosition(float dt, JobHandle depend)
    {
        var job = new UpdatePosition()
        {
            position = _positions,
            velocities = _velocities,

            dt = dt,
        };

        return job.Schedule(ParticleCount, threadCount, depend);
    }

    /// <summary>
    /// 计算碰撞边界
    /// </summary>
    /// <param name="depend"></param>
    /// <returns></returns>
    private JobHandle DoBoundJob(JobHandle depend)
    {
        var job = new BoundJob()
        {
            positions = _positions,
            velocitys = _velocities,
            particleRadius = _radius,

            boundCenter = _boundCenter,
            boundSize = _boundSize,
            CollisionDamping = _collisionDamping,
        };

        return job.Schedule(ParticleCount, threadCount, depend);
    }
    #endregion

    #region  OuputInterface

    /// <summary>
    /// 获取所有粒子的位置参数，以供其他模块提供渲染
    /// </summary>
    /// <returns></returns>
    public Vector3[] GetPositions()
    {
        return Array.ConvertAll(_positions.ToArray(), p => (Vector3)p);
    }

    /// <summary>
    /// 获取所有的速度参数
    /// </summary>
    /// <returns></returns>
    public Vector3[] GetVelocities()
    {
        return Array.ConvertAll(_velocities.ToArray(), p => (Vector3)p);
    }

    /// <summary>
    /// 获取所有粒子的密度信息
    /// </summary>
    /// <returns></returns>
    public Vector2[] GetDensities()
    {
        return Array.ConvertAll(_densities.ToArray(), p => (Vector2)p);
    }

    #endregion

}
