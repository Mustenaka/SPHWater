using Unity.Mathematics;

namespace SPHWater
{
    public static class PMath
    {
        /// <summary>
        /// Poly6 Kernel:
        ///     W_{poly}6(r,h)=\frac{315}{64\pi h^9}\begin{cases}(h^2-r^2)^3,0<=r<=h \\ 0, {otherwise} \end{cases}
        /// </summary>
        /// <param name="dst">当前距离</param>
        /// <param name="radius">范围半径</param>
        /// <returns>平滑核结果</returns>
        public static float SmoothingKernelPoly6(float dst, float radius)
        {
            if (dst >= radius)
            {
                return 0;
            }

            var scale = 315 / (64 * math.PI * math.pow(math.abs(radius), 9));
            var v = radius * radius - dst * dst;
            return v * v * v * scale;
        }

        /// <summary>
        /// Spiky Kernel Pow3:
        ///     W_{spiky}(r,h)=\frac{15}{\pi h^6}\begin{cases}(h-r)^3,0<=r<=h \\ 0, {otherwise} \end{cases}
        /// </summary>
        /// <param name="dst">当前距离</param>
        /// <param name="radius">范围半径</param>
        /// <returns>平滑核结果</returns>
        public static float SpikyKernelPow3(float dst, float radius)
        {
            if (dst >= radius)
            {
                return 0;
            }

            float scale = 15 / (math.PI * math.pow(radius, 6));
            float v = radius - dst;
            return v * v * v * scale;
        }

        /// <summary>
        /// Spiky Kernel Pow2:
        ///     Integrate[(h-r)^2 r^2 Sin[θ], {r, 0, h}, {θ, 0, π}, {φ, 0, 2*π}]
        /// </summary>
        /// <param name="dst">当前距离</param>
        /// <param name="radius">范围半径</param>
        /// <returns>平滑核结果</returns>
        public static float SpikyKernelPow2(float dst, float radius)
        {
            if (dst >= radius)
            {
                return 0;
            }

            float scale = 15 / (2 * math.PI * math.pow(radius, 5));
            float v = radius - dst;
            return v * v * scale;
        }

        /// <summary>
        /// DerivativeSpikyPow3 导数
        /// </summary>
        /// <param name="dst">当前距离</param>
        /// <param name="radius">范围半径</param>
        /// <returns></returns>
        public static float DerivativeSpikyPow3(float dst, float radius)
        {
            if (dst > radius)
            {
                return 0;
            }

            float scale = 45 / (math.PI * math.pow(radius, 6));
            float v = radius - dst;
            return -v * v * scale;
        }

        /// <summary>
        /// DerivativeSpikyPow2 导数
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static float DerivativeSpikyPow2(float dst, float radius)
        {
            if (dst > radius)
            {
                return 0;
            }

            float scale = 15 / (math.PI * math.pow(radius, 5));
            float v = radius - dst;
            return -v * scale;
        }
    }
}
