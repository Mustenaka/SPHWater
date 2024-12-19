using Unity.Mathematics;
using UnityEngine;

namespace SPHWater.Assets.Scripts.Phy._2D
{
    /// <summary>
    /// Generate particle 2d
    /// </summary>
    public class ParticleSpawner2D : MonoBehaviour
    {
        [Header("粒子数量")]
        public int particleCount;

        [Header("生成范围")]
        public Vector2 spawnSize;
        public Vector2 spawnCenter;
        public Vector2 spawnVelocity;

        public float jitterStr;
        public bool showSpawnBoundsGizmos;

        private void Start()
        {
            Debug.Log($"ParticleSpawner2D: particleCount: {particleCount}");
        }

        public ParticleSpawnData GetSpawnData()
        {
            ParticleSpawnData data = new ParticleSpawnData(particleCount);
            var rng = new Unity.Mathematics.Random(42);

            float2 s = spawnSize;
            int numX = Mathf.CeilToInt(Mathf.Sqrt(s.x / s.y * particleCount + (s.x - s.y) * (s.x - s.y) / (4 * s.y * s.y)) - (s.x - s.y) / (2 * s.y));
            int numY = Mathf.CeilToInt(particleCount / (float)numX);
            int i = 0;

            for (int y = 0; y < numY; y++)
            {
                for (int x = 0; x < numX; x++)
                {
                    if (i >= particleCount) break;

                    float tx = numX <= 1 ? 0.5f : x / (numX - 1f);
                    float ty = numY <= 1 ? 0.5f : y / (numY - 1f);

                    float angle = (float)rng.NextDouble() * 3.14f * 2;
                    Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    Vector2 jitter = dir * jitterStr * ((float)rng.NextDouble() - 0.5f);
                    data.positions[i] = new Vector2((tx - 0.5f) * spawnSize.x, (ty - 0.5f) * spawnSize.y) + jitter + spawnCenter;
                    data.velocities[i] = spawnVelocity;
                    i++;
                }
            }

            return data;
        }

        public struct ParticleSpawnData
        {
            public float2[] positions;
            public float2[] velocities;

            public ParticleSpawnData(int num)
            {
                positions = new float2[num];
                velocities = new float2[num];
            }
        }

        private void OnDrawGizmos()
        {
            if (showSpawnBoundsGizmos)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(spawnCenter, Vector2.one * spawnSize);
            }
        }
    }
}