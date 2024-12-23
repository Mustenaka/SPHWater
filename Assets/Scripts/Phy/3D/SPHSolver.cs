using Assets.Scripts;
using UnityEngine;

public class SPHSolver : MonoBehaviour
{
    private SPHSimulate simulate;
    private SPHReferenceData referenceData;

    public float dt = 0.002f;
    public float accTime = 0.0f;

    [Header("Particles Param")]
    public int particleCount;       // ��������
    public float particleRadius;    // ���Ӱ뾶
    public Vector3 gravity;         // ����
    public Vector3[] positions;     // λ��
    public Vector3[] velocitys;     // �ٶ�

    [Header("Bound")] 
    [Range(0, 1.0f)] public float collisionDamping;   // ��ײ����
    public Vector3 BoundCenter;     // ��ײ����
    public Vector3 BoundSize;       // ��ײ�д�С

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
        initData.CollisionDamping = collisionDamping;

        positions = new Vector3[particleCount];
        velocitys = new Vector3[particleCount];

        // Fill data
        var particlesPerLayer = Mathf.CeilToInt(Mathf.Pow(particleCount, 1f / 3f));
        var index = 0;

        for (var z = 0; z < particlesPerLayer && index < particleCount; z++)
        {
            for (var y = 0; y < particlesPerLayer && index < particleCount; y++)
            {
                for (var x = 0; x < particlesPerLayer && index < particleCount; x++)
                {
                    // ����ÿ�����ӵ�λ��
                    positions[index] = new Vector3(
                        x * particleRadius * 2,   // X��ļ��
                        y * particleRadius * 2,   // Y��ļ��
                        z * particleRadius * 2    // Z��ļ��
                    );
                    index++;
                }
            }
        }

        initData.Positions = positions;
        initData.Velocitys = velocitys;

        simulate = new SPHSimulate(initData);
    }

    private void Update()
    {
        accTime += Time.deltaTime;
        int cnt = (int)(accTime / dt);

        if (simulate != null)
        {
            // װ��mono��������
            PackageReferenceData();

            // ���ѭ�������Ϊ�˴������������ʱ�����˼������
            for (int i = 0; i < cnt; i++)
            {
                simulate.DataReference(referenceData);
                simulate.Simulate(dt);
            }

            // ��ȡ����λ�� | ��������һ����Ⱦģ�����
            positions = simulate.GetPositions();
        }

        accTime %= dt;
    }

    private void OnDestroy()
    {
        simulate.Dispose();
    }

    private void PackageReferenceData()
    {
        referenceData ??= new SPHReferenceData();
        referenceData.Gravity = gravity;
        referenceData.BoundCenter  = BoundCenter;
        referenceData.BoundSize = BoundSize;
        referenceData.Radius = particleRadius;
        referenceData.CollisionDamping = collisionDamping;
    }

    private void OnDrawGizmos()
    {
        // ���ư�Χ�� 
        if (isRenderBound)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(BoundCenter, BoundSize);
        }

        // ��������
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