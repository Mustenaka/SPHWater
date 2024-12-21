using System;
using Unity.Mathematics;
using UnityEngine;
using static SPHWater.Assets.Scripts.Phy._2D.ParticleSpawner2D;
using static UnityEngine.ParticleSystem;

namespace SPHWater.Assets.Scripts.Phy._2D
{
    /// <summary>
    /// Simulate Solver
    ///     
    /// </summary>
    public class Simulate2D : MonoBehaviour
    {
        [Header("Simulation Setting")]
        public float dt = 0.01f;    // Time differential
        public Vector2 gravity = Vector2.down * 0.981f;

        [Header("Particle Setting")]
        public int particleCount;
        public float particleRadius;
        public float particleMass = 0.02f;
        public float smoothingLength = 0.1f;
        public float restDensity = 1000f;
        public float stiffness = 3.0f;
        public float viscosity = 0.1f;

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
            Array.Fill(_mass, particleMass);    // mass default is 1.0f

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
                    if (distance < particleRadius)
                    {
                        float weight = Kernel(distance);    // p_i = sum(M * W)
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
                var pos = _positions[index];

                for (var i = 0; i < particleCount; i++)
                {
                    if (index == i)
                    {
                        continue;
                    }

                    float distance = math.distance(_positions[index], _positions[i]);

                    if (distance < smoothingLength)
                    {
                        float2 direction = _positions[index] - _positions[i];
                        float weight = GradientKernel(distance);

                        // pressure sim
                        var pressureForce = -_mass[index]
                                            * (_pressure[index] / (_density[index] * _density[index]))
                                            + _pressure[i] / (_density[i] * _density[i]) * weight * direction;
                        _force[index] += pressureForce;

                        if (distance < 0.5f * smoothingLength) // 过于靠近
                        {
                            var strength = 100f; // 调整该值以避免粒子重叠
                            var repulsionForce = strength * (smoothingLength - distance) * direction;
                            _force[index] += repulsionForce;
                        }

                        Debug.Log($"Index{index}: f:{_force[index]} ,p:{_positions[index]}, v:{_velocities[index]}, d:{_density[index]}");
                    }
                }

                // 计算压力（理想气体方程）
                //_pressure[index] = Mathf.Max(0f, stiffness * (_density[index] - restDensity));
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
                _velocities[i] += _force[i] / _density[i] * dt;
                _positions[i] += _velocities[i] * dt;

                //_velocities[i] += (float2)gravity * dt;
                //_positions[i] += _velocities[i] * dt;

                ResolveCollisions(ref _positions[i], ref _velocities[i]);
            }
        }

        /// <summary>
        /// 核函数
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
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
        /// 核函数梯度
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        private float GradientKernel(float distance)
        {
            if (distance < smoothingLength)
            {
                float q = distance / smoothingLength;
                return -(45f / (Mathf.PI * Mathf.Pow(smoothingLength, 6))) * Mathf.Pow((smoothingLength - distance), 2);
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