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
        public int ParticleCount;

        [Header("生成范围")] 
        public Vector2 SpawnSize;
        public Vector2 SpawnCenter;
        public Vector2 SpawnVelocity;

        public float JitterStr;
        public bool ShowSpawnBoundsGizmos;

        public ParticleSpawnData GetSpawnData()
        {
            ParticleSpawnData data = new ParticleSpawnData(ParticleCount);
            var rng = new Unity.Mathematics.Random(42);

            float2 s = SpawnSize;
            int numX = Mathf.CeilToInt(Mathf.Sqrt(s.x / s.y * ParticleCount + (s.x - s.y) * (s.x - s.y) / (4 * s.y * s.y)) - (s.x - s.y) / (2 * s.y));
            int numY = Mathf.CeilToInt(ParticleCount / (float)numX);
            int i = 0;

            for (int y = 0; y < numY; y++)
            {
                for (int x = 0; x < numX; x++)
                {
                    if (i >= ParticleCount) break;

                    float tx = numX <= 1 ? 0.5f : x / (numX - 1f);
                    float ty = numY <= 1 ? 0.5f : y / (numY - 1f);

                    float angle = (float)rng.NextDouble() * 3.14f * 2;
                    Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    Vector2 jitter = dir * JitterStr * ((float)rng.NextDouble() - 0.5f);
                    data.positions[i] = new Vector2((tx - 0.5f) * SpawnSize.x, (ty - 0.5f) * SpawnSize.y) + jitter + SpawnCenter;
                    data.velocities[i] = SpawnVelocity;
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

        void OnDrawGizmos()
        {
            if (ShowSpawnBoundsGizmos && !Application.isPlaying)
            {
                Gizmos.color = new Color(1, 1, 0, 0.5f);
                Gizmos.DrawWireCube(SpawnCenter, Vector2.one * SpawnSize);
            }
        }
    }
}