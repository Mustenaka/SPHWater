using System;
using Unity.Mathematics;
using UnityEngine;
using static SPHWater.Assets.Scripts.Phy._2D.ParticleSpawner2D;

namespace SPHWater.Assets.Scripts.Phy._2D
{
    /// <summary>
    /// Simulate Solver
    ///     
    /// </summary>
    public class Simulate2D : MonoBehaviour
    {
        [Header("Simulation Setting")]
        public float dt;    // Time differential
        public Vector2 gravity = Vector2.down * 0.981f;

        [Header("Particle Setting")]
        public int particleCount;
        public float particleRadius;
        public float particleMass = 0.02f;
        public float smoothingLength = 0.1f;
        public float restDensity = 1000f;
        public float stiffness = 3.0f;
        public float viscosity = 0.1f;
        public float deltaTime = 0.01f;

        private float2[] _velocities;
        private float2[] _positions;
        private float2[] _force;
        private float[] _density;
        private float[] _pressure;
        private float[] _mass;

        [Header("Bounding & Collision Setting")]
        public Vector2 boundsSize;
        [Range(0.0f, 1.0f)] public float collisionDamping = 0.5f;

        [Header("Reference")]
        public ParticleSpawner2D particleSpawner;

        [Header("Show(Temp)")]
        public Material InstancedMaterial;  // GPU INSTANCE MATERIAL

        private Matrix4x4[] _instanceTransforms;
        private Color[] _instanceColors;

        #region Unity_Default_Function

        private void Start()
        {
            /* BASE PARTICLES */
            ParticleSpawnData spawnData = particleSpawner.GetSpawnData();
            _positions = spawnData.positions;
            _velocities = spawnData.velocities;
            particleCount = spawnData.positions.Length;
            _force = new float2[particleCount];
            _density = new float[particleCount];
            _pressure = new float[particleCount];
            _mass = new float[particleCount];
            Array.Fill(_mass, 1.0f);    // mass default is 1.0f

            /* PARTICLE RENDER */
            _instanceTransforms = new Matrix4x4[particleCount];
            _instanceColors = new Color[particleCount];

            for (var i = 0; i < _positions.Length; i++)
            {
                // 将每个粒子的位置进行变换矩阵计算
                _instanceTransforms[i] =
                    Matrix4x4.TRS((Vector2)_positions[i], Quaternion.identity, Vector3.one * particleRadius * 2);

                // 设置粒子颜色
                _instanceColors[i] = Color.blue;
            }

            // bind Material init
            InstancedMaterial.SetMatrixArray("_Positions", _instanceTransforms);
            InstancedMaterial.SetColorArray("_Colors", _instanceColors);
            InstancedMaterial.SetFloat("_ParticleRadius", particleRadius);
        }

        private void Update()
        {
            ComputeDensityAndPressure();
            ComputeForces();
            Integrate();

            SetInstanceInfo();      // use gpu draw the particles
        }

        #endregion

        /// <summary>
        /// Compute density and pressure
        /// TODO:
        ///     use HashTable or SpaceDiv function to optimize this.
        /// </summary>
        private void ComputeDensityAndPressure()
        {
            for (var index = 0; index < particleCount; index++)
            {
                _density[index] = 0.0f;
                var pos = _positions[index];

                for (var i = 0; i < particleCount; i++)
                {
                    var neighborPos = _positions[i];

                    float distance = math.distance(pos, neighborPos);
                    if (distance < smoothingLength)
                    {
                        float weight = Kernel(distance);
                        _density[index] += _mass[i] * weight;
                    }
                }

                // 计算压力（理想气体方程）
                _pressure[index] = Mathf.Max(0f, stiffness * (_density[index] - restDensity));

            }
        }

        /// <summary>
        /// Compute forces
        /// TODO:
        ///     Same as ComputeDensityAndPressure(), use HashTable to optimize this.
        /// </summary>
        private void ComputeForces()
        {
            for (var index = 0; index < particleCount; index++)
            {
                _force[index] = 0.0f;
                var pos = _positions[index];

                for (var i = 0; i < particleCount; i++)
                {
                    if (index == i)
                    {
                        continue;
                    }

                    var neighborPos = _positions[i];
                    float distance = math.distance(pos, neighborPos);

                    if (distance < smoothingLength)
                    {
                        float2 direction = pos - neighborPos;
                        float distanceSqr = math.lengthsq(direction);

                        if (distanceSqr > 0.0f)
                        {
                            float distanceCubed = Mathf.Pow(distanceSqr, 1.5f);

                            // pressure sim
                            var pressureForce = (_pressure[index] + _pressure[i]) / (2f * _density[i]) * math.normalize(direction);
                            _force[index] += pressureForce * _mass[i];

                            // viscosity sim
                            var viscosityForce = viscosity * (_velocities[i] - _velocities[index]) / distanceCubed;
                            _force[index] += viscosityForce * _mass[i];
                        }
                    }
                }

                // 计算压力（理想气体方程）
                _pressure[index] = Mathf.Max(0f, stiffness * (_density[index] - restDensity));

            }
        }

        /// <summary>
        /// Euler Integration (Explicit Newton integral)
        /// TODO:
        ///     Change Explicit Newton integral to Implicit Newton integral or RK4 or Verlet integral
        /// </summary>
        private void Integrate()
        {
            for (int i = 0; i < particleCount; i++)
            {
                _velocities[i] += (_force[i] + (float2)gravity) / _density[i] * Time.deltaTime;
                _positions[i] += _velocities[i] * Time.deltaTime;

                //_velocities[i] += (float2)gravity * Time.deltaTime;
                //_positions[i] += _velocities[i] * Time.deltaTime;

                ResolveCollisions(ref _positions[i], ref _velocities[i]);
            }
        }

        private float Kernel(float distance)
        {
            // 核函数（通常使用高斯核函数或者多项式核函数）
            if (distance < smoothingLength)
            {
                float q = distance / smoothingLength;
                return (315f / (64f * Mathf.PI * Mathf.Pow(smoothingLength, 9))) * Mathf.Pow((1f - q * q) * (1f - q * q), 3);
            }
            return 0f;
        }

        /// <summary>
        /// resolver collision: calculate boundary conditions, reflections
        /// </summary>
        private void ResolveCollisions(ref float2 position, ref float2 velocity)
        {
            Vector2 halfBoundsSize = boundsSize / 2 - Vector2.one * particleRadius;

            if (Math.Abs(position.x) > halfBoundsSize.x)
            {
                position.x = halfBoundsSize.x * Math.Sign(position.x);
                velocity.x *= -1 * collisionDamping;
            }

            if (Math.Abs(position.y) > halfBoundsSize.y)
            {
                position.y = halfBoundsSize.y * Math.Sign(position.y);
                velocity.y *= -1 * collisionDamping;
            }
        }

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
                    Gizmos.DrawSphere((Vector2)position, particleRadius);
                }
            }

            // Draw bounding box
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(Vector3.zero, boundsSize);
        }
    }
}