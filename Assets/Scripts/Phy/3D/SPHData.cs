using UnityEngine;

namespace Assets.Scripts
{
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
        /// 粒子半径
        /// </summary>
        public float Radius { get; set; }

        /// <summary>
        /// 碰撞阻尼
        /// </summary>
        public float CollisionDamping { get; set; }
    }

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
        /// 粒子半径
        /// </summary>
        public float Radius { get; set; }

        /// <summary>
        /// 碰撞阻尼
        /// </summary>
        public float CollisionDamping { get; set; }
    }
}