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
    public NativeArray<float3> _nextPosition;   // ����λ�ã���һ֡��
    public NativeArray<float3> _velocitys;      // �����ٶ�
    public NativeArray<float3> _externalForce;  // ��������һ���������� F_total = f_external + f_pressure + f_viscosity
    public NativeArray<float3> _pressureForce;  // ѹ��
    public NativeArray<float3> _viscosityForce; // ճ����
    public NativeArray<float2> _density;        // �ܶ�

    public NativeReference<float> _radius;      // ���Ӱ뾶
    public NativeReference<float> _targetDensity;           // Ŀ���ܶ�
    public NativeReference<float> _pressureMultiplier;      // ѹ��ϵ��
    public NativeReference<float> _nearPressureMultiplier;  // �ڽ�ѹ��ϵ��
    public NativeReference<float> _viscosityStrength;       // ճ��ǿ��
    public NativeReference<float> _smoothingRadius; // ƽ���뾶

    public NativeReference<float3> _gravity;        // ����
    public NativeReference<float3> _boundCenter;    // ��Χ������
    public NativeReference<float3> _boundSize;      // ��Χ�гߴ�
    public NativeReference<float> _collisionDamping;// ��ײ����

    public const uint hashK1 = 15823;
    public const uint hashK2 = 9737333;
    public const uint hashK3 = 440817757;
    public NativeArray<uint3> SpatialIndices;
    public NativeArray<uint3> SpatialOffsets;

    public SPHSimulate(SPHInitData initData)
    {
        /* ���Ӽ������ */
        _positions = new NativeArray<float3>(initData.ParticleCount, Allocator.Persistent);
        _velocitys = new NativeArray<float3>(initData.ParticleCount, Allocator.Persistent);
        _nextPosition = new NativeArray<float3>(initData.ParticleCount, Allocator.Persistent);
        _externalForce = new NativeArray<float3>(initData.ParticleCount, Allocator.Persistent);
        _pressureForce = new NativeArray<float3>(initData.ParticleCount, Allocator.Persistent);
        _viscosityForce = new NativeArray<float3>(initData.ParticleCount, Allocator.Persistent);
        _density = new NativeArray<float2>(initData.ParticleCount, Allocator.Persistent);

        _positions.CopyFrom(Array.ConvertAll(initData.Positions, v => (float3)v));
        _velocitys.CopyFrom(Array.ConvertAll(initData.Velocitys, v => (float3)v));

        /* �������� */
        _radius = new NativeReference<float>(initData.Radius, Allocator.Persistent);
        _targetDensity = new NativeReference<float>(initData.TargetDensity, Allocator.Persistent);
        _pressureMultiplier = new NativeReference<float>(initData.PressureMultiplier, Allocator.Persistent);
        _nearPressureMultiplier = new NativeReference<float>(initData.NearPressureMultiplier, Allocator.Persistent);
        _viscosityStrength = new NativeReference<float>(initData.ViscosityStrength, Allocator.Persistent);
        _smoothingRadius = new NativeReference<float>(initData.SmoothingRadius, Allocator.Persistent);

        /* ���Χ�в��� */
        _gravity = new NativeReference<float3>(initData.Gravity, Allocator.Persistent);
        _boundCenter = new NativeReference<float3>(initData.BoundCenter, Allocator.Persistent);
        _boundSize = new NativeReference<float3>(initData.BoundSize, Allocator.Persistent);
        _collisionDamping = new NativeReference<float>(initData.CollisionDamping, Allocator.Persistent);

        /* HashTable �Ż�*/
        SpatialIndices = new NativeArray<uint3>(initData.ParticleCount, Allocator.Persistent);
        SpatialOffsets = new NativeArray<uint3>(initData.ParticleCount, Allocator.Persistent);
    }

    public void Dispose()
    {
        /* ���Ӽ������ */
        _positions.Dispose();
        _velocitys.Dispose();
        _nextPosition.Dispose();
        _externalForce.Dispose();
        _pressureForce.Dispose();
        _viscosityForce.Dispose();
        _density.Dispose();

        /* �������� */
        _radius.Dispose();
        _targetDensity.Dispose();
        _pressureMultiplier.Dispose();
        _nearPressureMultiplier.Dispose();
        _viscosityStrength.Dispose();
        _smoothingRadius.Dispose();

        /* ���Χ�в��� */
        _gravity.Dispose();
        _boundCenter.Dispose();
        _boundSize.Dispose();
        _collisionDamping.Dispose();

        /* HashTable �Ż�*/
        SpatialIndices.Dispose();
        SpatialOffsets.Dispose();
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
        _collisionDamping.Value = reference.CollisionDamping;

        _radius.Value = reference.Radius;
        _targetDensity.Value = reference.TargetDensity;
        _pressureMultiplier.Value = reference.PressureMultiplier;
        _nearPressureMultiplier.Value = reference.NearPressureMultiplier;
        _viscosityStrength.Value = reference.ViscosityStrength;
        _smoothingRadius.Value = reference.SmoothingRadius;
    }

    /// <summary>
    /// ÿһ��ģ��
    /// </summary>
    public void Simulate(float dt)
    {
        JobHandle handle = new();

        handle = DoExternalForce(dt, handle);
        handle = DoCalcDensities(handle);
        handle = DoUpdatePosition(dt, handle);
        //handle = DoVelocitySolution(dt, handle);  // ���������
        handle = DoBoundJob(handle);

        handle.Complete();
    }

    #region Jobs

    /// <summary>
    /// ������չ��
    /// </summary>
    /// <param name="depend"></param>
    /// <returns></returns>
    private JobHandle DoExternalForce(float dt, JobHandle depend)
    {
        var job = new ExternalForce()
        {
            positions = _positions,
            velocities = _velocitys,
            nextPositions = _nextPosition,

            externalForce = _externalForce,
            gravity = _gravity,

            dt = dt,
        };

        return job.Schedule(ParticleCount, 64, depend);
    }

    /// <summary>
    /// �����ܶ�
    /// </summary>
    /// <param name="depend"></param>
    /// <returns></returns>
    private JobHandle DoCalcDensities(JobHandle depend)
    {
        var job = new CalculateDensities()
        {
            positions = _nextPosition,
            densities = _density,

            smoothingRadius = _smoothingRadius,
        };

        return job.Schedule(ParticleCount, 64, depend);
    }

    /// <summary>
    /// ţ�ٷ�ͨ���ٶȵõ�λ�ø���
    /// </summary>
    /// <param name="dt"></param>
    /// <param name="depend"></param>
    /// <returns></returns>
    private JobHandle DoVelocitySolution(float dt, JobHandle depend)
    {
        var job = new VelocitySolutionJob()
        {
            positions = _positions,
            velocities = _velocitys,

            externalForce = _externalForce,
            pressureForce = _pressureForce,
            viscosityForce = _viscosityForce,

            dt = dt,
        };

        return job.Schedule(ParticleCount, 64, depend);
    }

    private JobHandle DoUpdatePosition(float dt, JobHandle depend)
    {
        var job = new UpdatePosition()
        {
            position = _positions,
            velocities = _velocitys,

            dt = dt,
        };

        return job.Schedule(ParticleCount, 64, depend);
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
            particleRadius = _radius,

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
