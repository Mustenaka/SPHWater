using Unity.Mathematics;

namespace SPHWater
{
    public static class PMath
    {
        /// <summary>
        /// Æ½»¬º¯ÊýºË
        /// </summary>
        /// <param name="radius"></param>
        /// <param name="dst"></param>
        /// <returns></returns>
        public static float SmoothingKernel(float radius, float dst)
        {
            var volume = math.PI * math.pow(radius, 8) / 4;
            var value = math.max(0, radius * radius - dst * dst);
            return value * value * value;
        }
    }
}
