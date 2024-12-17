using UnityEngine;
using UnityEngine.UIElements;

namespace SPHWater.Phy.Test
{
    public class DensityTest : MonoBehaviour
    {
        public int ParticleCount = 20;
        public float ParticleRadius = 0.1f;
        [Range(0.0f, 1.0f)] public float CollisionDamping = 0.5f;

        public Vector3 Gravity = Vector3.down * 0.981f;
        public Vector3 BoundsSize;

        public Material instancedMaterial;  // GPU INSTANCE MATERIAL

        private Vector3[] _velocitys;
        private Vector3[] _positions;

        private Matrix4x4[] _instanceTransforms;
        private Color[] _instanceColors;

        private void Start()
        {
            _velocitys = new Vector3[ParticleCount];
            _positions = new Vector3[ParticleCount];
            _instanceTransforms = new Matrix4x4[ParticleCount];
            _instanceColors = new Color[ParticleCount];

            // Fill data
            var particlesPerLayer = Mathf.CeilToInt(Mathf.Pow(ParticleCount, 1f / 3f));
            var index = 0;

            for (var z = 0; z < particlesPerLayer && index < ParticleCount; z++)
            {
                for (var y = 0; y < particlesPerLayer && index < ParticleCount; y++)
                {
                    for (var x = 0; x < particlesPerLayer && index < ParticleCount; x++)
                    {
                        // 计算每个粒子的位置
                        _positions[index] = new Vector3(
                            x * ParticleRadius * 2,   // X轴的间距
                            y * ParticleRadius * 2,   // Y轴的间距
                            z * ParticleRadius * 2    // Z轴的间距
                        );

                        // 将每个粒子的位置进行变换矩阵计算
                        _instanceTransforms[index] = Matrix4x4.TRS(_positions[index], Quaternion.identity, Vector3.one * ParticleRadius * 2);

                        // 设置粒子颜色
                        _instanceColors[index] = Color.blue;

                        index++;
                    }
                }
            }

            // bind Material init
            instancedMaterial.SetMatrixArray("_Positions", _instanceTransforms);
            instancedMaterial.SetColorArray("_Colors", _instanceColors);
            instancedMaterial.SetFloat("_ParticleRadius", ParticleRadius);
        }

        private void Update()
        {
            SetInstanceInfo();      // use gpu draw the particles
        }

        private float CalculateDensity(Vector3 samplePoint)
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
            if (instancedMaterial == null)
            {
                Debug.LogError("missing instance materials");
                return;
            }

            instancedMaterial.SetMatrixArray("_Positions", _instanceTransforms);
            instancedMaterial.SetColorArray("_Colors", _instanceColors);
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