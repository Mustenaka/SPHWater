using System;
using UnityEngine;

namespace SPHWater.Phy.Test
{
    /// <summary>
    /// 2D Draw
    /// </summary>
    public class DensityTest : MonoBehaviour
    {
        public int ParticleCount = 20;
        public float ParticleRadius = 0.1f;
        [Range(0.0f, 1.0f)] public float CollisionDamping = 0.5f;

        public Vector2 Gravity = Vector2.down * 0.981f;
        public Vector2 BoundsSize;

        public Material InstancedMaterial;  // GPU INSTANCE MATERIAL

        private Vector2[] _velocities;
        private Vector2[] _positions;

        private Matrix4x4[] _instanceTransforms;
        private Color[] _instanceColors;

        private void Start()
        {
            _velocities = new Vector2[ParticleCount];
            _positions = new Vector2[ParticleCount];
            _instanceTransforms = new Matrix4x4[ParticleCount];
            _instanceColors = new Color[ParticleCount];

            // Fill data
            var particlePerRow = (int)Math.Sqrt(ParticleCount);
            var particlePerCol = (ParticleCount - 1) / particlePerRow + 1;
            for (int i = 0; i < ParticleCount; i++)
            {
                var x = (i % particlePerRow - particlePerRow / 2f + 0.5f) * ParticleRadius * 2;
                var y = (i / particlePerRow - particlePerCol / 2f + 0.5f) * ParticleRadius * 2;

                _positions[i] = new Vector2(x, y);

                // 将每个粒子的位置进行变换矩阵计算
                _instanceTransforms[i] = Matrix4x4.TRS(_positions[i], Quaternion.identity, Vector3.one * ParticleRadius * 2);
                // 设置粒子颜色
                _instanceColors[i] = Color.blue;
            }


            // bind Material init
            InstancedMaterial.SetMatrixArray("_Positions", _instanceTransforms);
            InstancedMaterial.SetColorArray("_Colors", _instanceColors);
            InstancedMaterial.SetFloat("_ParticleRadius", ParticleRadius);
        }

        private void Update()
        {
            for (int i = 0; i < ParticleCount; i++)
            {
                _velocities[i] += Gravity * Time.deltaTime;
                _positions[i] += _velocities[i] * Time.deltaTime;

                ResolveCollisions(ref _positions[i], ref _velocities[i]);
            }

            SetInstanceInfo();      // use gpu draw the particles
        }

        /// <summary>
        /// resolver collision: calculate boundary conditions, reflections
        /// </summary>
        private void ResolveCollisions(ref Vector2 position, ref Vector2 velocity)
        {
            Vector2 halfBoundsSize = BoundsSize / 2 - Vector2.one * ParticleRadius;

            if (Math.Abs(position.x) > halfBoundsSize.x)
            {
                position.x = halfBoundsSize.x * Math.Sign(position.x);
                velocity.x *= -1 * CollisionDamping;
            }

            if (Math.Abs(position.y) > halfBoundsSize.y)
            {
                position.y = halfBoundsSize.y * Math.Sign(position.y);
                velocity.y *= -1 * CollisionDamping;
            }
        }

        private float CalculateDensity(Vector2 samplePoint)
        {
            float desity = 0;
            const float mass = 1;

            foreach (var position in _positions)
            {
                float dst = (position - samplePoint).magnitude;
                float influence = PMath.SmoothingKernel(ParticleRadius, dst);
                desity += influence * mass;
            }

            return desity;
        }

        #region Draw

        /// <summary>
        /// make sure the instance data is updated
        /// </summary>
        private void SetInstanceInfo()
        {
            if (InstancedMaterial == null)
            {
                Debug.LogError("missing instance materials");
                return;
            }

            InstancedMaterial.SetMatrixArray("_Positions", _instanceTransforms);
            InstancedMaterial.SetColorArray("_Colors", _instanceColors);
        }

        private void OnDrawGizmos()
        {
            // Draw Particles
            if (_positions != null)
            {
                Gizmos.color = Color.white;
                foreach (var position in _positions)
                {
                    Gizmos.DrawSphere(position, ParticleRadius);
                }
            }

            // Draw bounding box
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(Vector3.zero, BoundsSize);
        }

        #endregion
    }
}