using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    /// 构造时参数
    /// </summary>
    public class SPHInitData
    {
        /// <summary>
        /// 粒子数量
        /// </summary>
        public int ParticleCount => this.Positions.Length;
        
        /// <summary>
        /// 粒子位置
        /// </summary>
        public Vector3[] Positions { get; set; }
        
        /// <summary>
        /// 粒子初始速度
        /// </summary>
        public Vector3[] Velocitys { get; set; }
        
        /// <summary>
        /// 重力
        /// </summary>
        public Vector3 Gravity { get; set; }

        /// <summary>
        /// 碰撞盒中心
        /// </summary>
        public Vector3 BoundCenter { get; set; }

        /// <summary>
        /// 碰撞盒尺寸
        /// </summary>
        public Vector3 BoundSize { get; set; }

        /// <summary>
        /// 碰撞阻尼
        /// </summary>
        public float CollisionDamping { get; set; }

        /// <summary>
        /// 粒子半径
        /// </summary>
        public float Radius { get; set; }

        /// <summary>
        /// 目标密度
        /// </summary>
        public float TargetDensity { get; set; }

        /// <summary>
        /// 压力系数
        /// </summary>
        public float PressureMultiplier { get; set; }

        /// <summary>
        /// 邻近压力系数
        /// </summary>
        public float NearPressureMultiplier { get; set; }

        /// <summary>
        /// 粘度强度
        /// </summary>
        public float ViscosityStrength { get; set; }

        /// <summary>
        /// 平滑半径
        /// </summary>
        public float SmoothingRadius { get; set; }
    }

    /// <summary>
    /// 每次物理解算时进行的传递参数
    /// </summary>
    public class SPHReferenceData
    {
        /// <summary>
        /// 重力
        /// </summary>
        public Vector3 Gravity { get; set; }

        /// <summary>
        /// 碰撞盒中心
        /// </summary>
        public Vector3 BoundCenter { get; set; }

        /// <summary>
        /// 碰撞盒尺寸
        /// </summary>
        public Vector3 BoundSize { get; set; }

        /// <summary>
        /// 碰撞阻尼
        /// </summary>
        public float CollisionDamping { get; set; }

        /// <summary>
        /// 粒子半径
        /// </summary>
        public float Radius { get; set; }

        /// <summary>
        /// 目标密度
        /// </summary>
        public float TargetDensity { get; set; }

        /// <summary>
        /// 压力系数
        /// </summary>
        public float PressureMultiplier { get; set; }

        /// <summary>
        /// 邻近压力系数
        /// </summary>
        public float NearPressureMultiplier { get; set; }

        /// <summary>
        /// 粘度强度
        /// </summary>
        public float ViscosityStrength { get; set; }

        /// <summary>
        /// 平滑半径
        /// </summary>
        public float SmoothingRadius { get; set; }
    }
}