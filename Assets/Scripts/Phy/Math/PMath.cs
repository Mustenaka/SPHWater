using Unity.Mathematics;

namespace SPHWater
{
    public static class PMath
    {
        public static float SmoothingKernel(float radius, float dst)
        {
            var value = math.max(0, radius - dst);
            return value * value * value;
        }
    }
}
