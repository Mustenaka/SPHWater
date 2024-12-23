using System;
using Assets.Scripts;
using Assets.Scripts.Jobs;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// SPHģ����
///     SPHSimulate():      �����������
///     Dispose():          �������� 
///     DataReference():    ���ݲ�������
///     Simulate():         ִ�е���ģ��
///
///     GetPositions():     ��ȡ��������λ�ò�������������ģ���ṩ��Ⱦ��
/// </summary>
public class SPHSimulate
{
    public int ParticleCount => _positions.Length;  // ��������
    public NativeArray<float3> _positions;      // ����λ��
    public NativeArray<float3> _velocitys;      // �����ٶ�
    public NativeArray<float3> _externalForce;  // ��������һ���������� F_total = f_external + f_pressure + f_viscosity
    public NativeArray<float3> _pressureForce;  // ѹ��
    public NativeArray<float3> _viscosityForce; // ճ����
    public NativeArray<float> _density;         // �ܶ�
    public NativeReference<float> _raduis;      // ���Ӱ뾶

    public NativeReference<float3> _gravity;        // ����
    public NativeReference<float3> _boundCenter;    // ��Χ������
    public NativeReference<float3> _boundSize;      // ��Χ�гߴ�
    public NativeReference<float> _collisionDamping;// ��ײ����

    public SPHSimulate(SPHInitData initData)
    {
        /* ���Ӽ������ */
        _positions = new NativeArray<float3>(initData.ParticleCount, Allocator.Persistent);
        _velocitys = new NativeArray<float3>(initData.ParticleCount, Allocator.Persistent);
        _externalForce = new NativeArray<float3>(initData.ParticleCount, Allocator.Persistent);
        _pressureForce = new NativeArray<float3>(initData.ParticleCount, Allocator.Persistent);
        _viscosityForce = new NativeArray<float3>(initData.ParticleCount, Allocator.Persistent);
        _density = new NativeArray<float>(initData.ParticleCount, Allocator.Persistent);
        _raduis = new NativeReference<float>(initData.Radius, Allocator.Persistent);

        _positions.CopyFrom(Array.ConvertAll(initData.Positions, v => (float3)v));
        _velocitys.CopyFrom(Array.ConvertAll(initData.Velocitys, v => (float3)v));

        /* �������� */
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
    /// ��̬����ֵ����Ҫ�Ǵ���һЩ�������أ��ᵼ������ģ�ⷢ���仯������
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
    /// ÿһ��ģ��
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
    /// ������չ��
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
    /// ţ�ٷ�ͨ���ٶȵõ�λ�ø���
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
    /// ������ײ�߽�
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
    /// ��ȡ�������ӵ�λ�ò������Թ�����ģ���ṩ��Ⱦ
    /// </summary>
    /// <returns></returns>
    public Vector3[] GetPositions()
    {
        return Array.ConvertAll(_positions.ToArray(), p => (Vector3)p);
    }
}
