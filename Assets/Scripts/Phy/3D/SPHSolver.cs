using Assets.Scripts;
using UnityEngine;

public class SPHSolver : MonoBehaviour
{
    private SPHSimulate simulate;
    private SPHReferenceData referenceData;

    public float dt = 0.002f;
    public float accTime = 0.0f;

    [Header("Particles Param")]
    public int particleCount;       // 粒子数量
    public Vector3 gravity;         // 重力
    public Vector3[] positions;     // 位置
    public Vector3[] velocities;    // 速度
    public Vector2[] densities;     // 密度

    public float particleRadius;        // 粒子半径
    public float targetDensity;         // 目标密度
    public float pressureMultiplier;    // 压力系数
    public float nearPressureMultiplier;// 邻近压力系数
    public float viscosityStrength;     // 粘性强度
    public float smoothingRadius;       // 平滑半径

    [Header("Bound")] 
    [Range(0, 1.0f)] public float collisionDamping;   // 碰撞阻尼
    public Vector3 BoundCenter;     // 碰撞中心
    public Vector3 BoundSize;       // 碰撞盒大小

    [Header("Renders")]
    public bool isRenderBound;
    public bool isRenderParticles;

    private void Start()
    {
        SPHInitData initData = new SPHInitData();
        initData.Gravity = gravity;
        initData.BoundCenter = BoundCenter;
        initData.BoundSize = BoundSize;
        initData.Radius = particleRadius;
        initData.TargetDensity = targetDensity;
        initData.PressureMultiplier = pressureMultiplier;
        initData.NearPressureMultiplier = nearPressureMultiplier;
        initData.ViscosityStrength = viscosityStrength;
        initData.CollisionDamping = collisionDamping;

        positions = new Vector3[particleCount];
        velocities = new Vector3[particleCount];
        densities = new Vector2[particleCount];

        // Fill data
        var particlesPerLayer = Mathf.CeilToInt(Mathf.Pow(particleCount, 1f / 3f));
        var index = 0;

        for (var z = 0; z < particlesPerLayer && index < particleCount; z++)
        {
            for (var y = 0; y < particlesPerLayer && index < particleCount; y++)
            {
                for (var x = 0; x < particlesPerLayer && index < particleCount; x++)
                {
                    // 计算每个粒子的位置
                    positions[index] = new Vector3(
                        x * particleRadius * 2,   // X轴的间距
                        y * particleRadius * 2,   // Y轴的间距
                        z * particleRadius * 2    // Z轴的间距
                    );
                    index++;
                }
            }
        }

        initData.Positions = positions;
        initData.Velocitys = velocities;

        simulate = new SPHSimulate(initData);
    }

    private void Update()
    {
        accTime += Time.deltaTime;
        int cnt = (int)(accTime / dt);

        if (simulate != null)
        {
            // 装载mono处的数据
            PackageReferenceData();

            // 这个循环设计是为了处理计算机卡顿时产生了计算畸变
            for (int i = 0; i < cnt; i++)
            {
                simulate.DataReference(referenceData);
                simulate.Simulate(dt);
            }

            ReceiveData();
        }

        accTime %= dt;
    }

    private void OnDestroy()
    {
        simulate.Dispose();
    }

    /// <summary>
    /// 打包传递参数
    /// </summary>
    private void PackageReferenceData()
    {
        referenceData ??= new SPHReferenceData();
        referenceData.Gravity = gravity;
        referenceData.BoundCenter  = BoundCenter;
        referenceData.BoundSize = BoundSize;
        referenceData.CollisionDamping = collisionDamping;
        referenceData.Radius = particleRadius;
        referenceData.TargetDensity = targetDensity;
        referenceData.PressureMultiplier = pressureMultiplier;
        referenceData.NearPressureMultiplier = nearPressureMultiplier;
        referenceData.ViscosityStrength = viscosityStrength;
        referenceData.SmoothingRadius = smoothingRadius;
    }

    /// <summary>
    /// 接受Simulate除传来的同步数据
    /// </summary>
    private void ReceiveData()
    {
        // 获取粒子位置 | 将来可用一个渲染模块承载
        positions = simulate.GetPositions();
        velocities = simulate.GetVelocities();
        densities = simulate.GetDensities();
    }

    private void OnDrawGizmos()
    {
        // 绘制包围盒 
        if (isRenderBound)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(BoundCenter, BoundSize);
        }

        // 绘制粒子
        if (isRenderParticles)
        {
            if (positions != null)
            {
                Gizmos.color = Color.white;
                foreach (var position in positions)
                {
                    Gizmos.DrawSphere(position, particleRadius);
                }
            }
        }
    }
}
