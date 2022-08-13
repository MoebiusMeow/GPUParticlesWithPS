## GPUParticlesWithPS
---
GPU Particle system with pixel shader (rather than compute shader)
+ A Demo in tModLoader

### About 说明
内置使用10000个GPU粒子的Demo

不使用计算着色器（因为HLSL的低版本无法使用）


### Compatibility 兼容性
+ HLSL3.0
+ vs_3_0
+ ps_3_0

不需要引用额外库，可以直接在tml里使用。
编译3.0的fx文件可以直接用这里带的XNBCompiler（在原XNBCompiler的基础上经过修改以支持HLSL3.0）。
