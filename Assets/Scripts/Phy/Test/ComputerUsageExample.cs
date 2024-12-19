using UnityEngine;

namespace SPHWater.Phy.Test
{
    public class ComputerUsageExample : MonoBehaviour
    {
        public ComputeShader compute;
        private ComputeBuffer buffer;

        private void Start()
        {
            int bufferSize = 10;
            buffer = new ComputeBuffer(bufferSize, sizeof(float));
            compute.SetBuffer(0, "bufferData", buffer);

            // 设置不同的操作模式
            compute.SetInt("operationMode", 0); // 使用 MultiplyByTwo
            RunComputeShader(bufferSize);

            compute.SetInt("operationMode", 1); // 使用 MultiplyByThree
            RunComputeShader(bufferSize);

            buffer.Release();
        }

        private void RunComputeShader(int bufferSize)
        {
            float[] results = new float[bufferSize];

            // 查找内核索引
            int kernelIndex = compute.FindKernel("CSMain");

            // 执行 Compute Shader
            compute.Dispatch(kernelIndex, bufferSize, 1, 1);

            // 获取 GPU 数据
            buffer.GetData(results);

            // 打印结果
            Debug.Log("Results:");
            for (int i = 0; i < bufferSize; i++)
            {
                Debug.Log($"Result[{i}] = {results[i]}");
            }
        }
    }
}