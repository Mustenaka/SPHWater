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
    public NativeArray<float3> _velocitys;      // 粒子速度
    public NativeArray<float3> _externalForce;  // 额外力（一般是重力） F_total = f_external + f_pressure + f_viscosity
    public NativeArray<float3> _pressureForce;  // 压力
    public NativeArray<float3> _viscosityForce; // 粘度力
    public NativeArray<float> _density;         // 密度
    public NativeReference<float> _raduis;      // 粒子半径

    public NativeReference<float3> _gravity;        // 重力
    public NativeReference<float3> _boundCenter;    // 包围盒中心
    public NativeReference<float3> _boundSize;      // 包围盒尺寸
    public NativeReference<float> _collisionDamping;// 碰撞阻尼

    public SPHSimulate(SPHInitData initData)
    {
        /* 粒子计算参数 */
        _positions = new NativeArray<float3>(initData.ParticleCount, Allocator.Persistent);
        _velocitys = new NativeArray<float3>(initData.ParticleCount, Allocator.Persistent);
        _externalForce = new NativeArray<float3>(initData.ParticleCount, Allocator.Persistent);
        _pressureForce = new NativeArray<float3>(initData.ParticleCount, Allocator.Persistent);
        _viscosityForce = new NativeArray<float3>(initData.ParticleCount, Allocator.Persistent);
        _density = new NativeArray<float>(initData.ParticleCount, Allocator.Persistent);
        _raduis = new NativeReference<float>(initData.Radius, Allocator.Persistent);

        _positions.CopyFrom(Array.ConvertAll(initData.Positions, v => (float3)v));
        _velocitys.CopyFrom(Array.ConvertAll(initData.Velocitys, v => (float3)v));

        /* 环境参数 */
        _gravity = new NativeReference<float3>(initData.Gravity, Allocator.Persistent);
        _boundCenter = new NativeReference<float3>(initData.BoundCenter, Allocator.Persistent);
        _boundSize = new NativeReference<float3>(initData.BoundSize, Allocator.Persistent);
        _collisionDamping = new NativeReference<float>(initData.CollisionDamping, Allocator.Persistent);
    }

    public void Dispose()
    {
        _positions.Dispose();
        _velocitys.Dispose();
        _externalForce.Dispose();
        _pressureForce.Dispose();
        _viscosityForce.Dispose();
        _density.Dispose();
        _raduis.Dispose();

        _gravity.Dispose();
        _boundCenter.Dispose();
        _boundSize.Dispose();
        _collisionDamping.Dispose();
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
        _raduis.Value = reference.Radius;
        _collisionDamping.Value = reference.CollisionDamping;
    }

    /// <summary>
    /// 每一针模拟
    /// </summary>
    public void Simulate(float dt)
    {
        JobHandle handle = new();

        handle = DoExternalForce(handle);
        handle = DoVelocitySolution(dt, handle);
        handle = DoBoundJob(handle);

        handle.Complete();
    }

    #region Jobs

    /// <summary>
    /// 计算扩展力
    /// </summary>
    /// <param name="depend"></param>
    /// <returns></returns>
    private JobHandle DoExternalForce(JobHandle depend)
    {
        var job = new ExternalForce()
        {
            externalForce = _externalForce,
            gravity = _gravity,
        };

        return job.Schedule(ParticleCount, 64, depend);
    }

    /// <summary>
    /// 牛顿法通过速度得到位置更新
    /// </summary>
    /// <param name="dt"></param>
    /// <param name="depend"></param>
    /// <returns></returns>
    private JobHandle DoVelocitySolution(float dt,JobHandle depend)
    {
        var job = new VelocitySolutionJob()
        {
            positions = _positions,
            velocitys = _velocitys,
            
            externalForce = _externalForce,
            pressureForce = _pressureForce,
            viscosityForce = _viscosityForce,

            dt = dt,
        };

        return job.Schedule(ParticleCount,64, depend);
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
            velocitys = _velocitys,
            particleRadius = _raduis,

            boundCenter = _boundCenter,
            boundSize = _boundSize,
            CollisionDamping = _collisionDamping,
        };

        return job.Schedule(ParticleCount, 64, depend);
    }
    #endregion

    /// <summary>
    /// 获取所有粒子的位置参数，以供其他模块提供渲染
    /// </summary>
    /// <returns></returns>
    public Vector3[] GetPositions()
    {
        return Array.ConvertAll(_positions.ToArray(), p => (Vector3)p);
    }
}
