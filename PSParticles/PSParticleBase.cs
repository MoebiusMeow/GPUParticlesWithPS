using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace ParticleDemo.PSParticles
{
    public class PSParticleBase 
    {
        public virtual string particalShaderPath => "Effects/xxx";
        public virtual string behaviorShaderPath => "Effects/xxx";
        public virtual string particleTexturePath => "Textures/xxx";
        public virtual int computeTextureHeight => 2;   // 每个像素是两个float，增大纹理高度可以存储更多数据

        public Mod Mod = null;


        public virtual int MAX_COUNT => 10000;          // N 

        public Effect particleShader;                   // 包括绘制粒子Pass和拷贝Pass
        public Effect behaviorShader;                   // 虚假的计算着色器，包含粒子行为计算Pass和初始化Pass
        
        public Texture2D particleTexture;               // 粒子图形纹理
        public RenderTarget2D computeRT;                // 记录每个粒子数据的纹理（N * 2），只含红绿通道代表x和y轴，两行分别代表位置和速度
        public RenderTarget2D computeRTSwap;

        public ParticleVertexInfo[] singleParticleVertex;      // 单个粒子的四个顶点
        public InstanceInfo[] particleInstance;                // 粒子实例列表，包含N个点
        public ParticleVertexInfo[] quadVertex;                // 用于绘制平凡的矩形的四个顶点，注意uv方向

        public VertexBuffer particleVBO = null;                // 存储实例（InstanceInfo）的Buffer （长度N）
        public VertexBuffer singleParticleVBO = null;          // 存储单个粒子的四个顶点的Buffer （长度4）
        public IndexBuffer singleParticleEBO = null;           // 存储单个粒子四个顶点的绘制顺序的索引Buffer （因为使用条带所以不需要特别处理）
        public VertexBufferBinding[] particleBinding = null;   // 绑定实例Buffer和单个粒子的Buffer，用于实例绘制

        public PSParticleBase() { }

        public virtual void SetDefault() { }

        public virtual void Load(Mod Mod)
        {
            particleShader = Mod.Assets.Request<Effect>(particalShaderPath, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            behaviorShader = Mod.Assets.Request<Effect>(behaviorShaderPath, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            particleTexture = Mod.Assets.Request<Texture2D>(particleTexturePath, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

            // 初始化顶点数据
            List<ParticleVertexInfo> particleVertex;
            List<ParticleVertexInfo> quadVertex;
            List<InstanceInfo> particleInstance;
            particleVertex = new()
            {
                new ParticleVertexInfo(new Vector3(-1, -1, 0), new Vector2(0, 0)),
                new ParticleVertexInfo(new Vector3(-1, +1, 0), new Vector2(0, 1)),
                new ParticleVertexInfo(new Vector3(+1, -1, 0), new Vector2(1, 0)),
                new ParticleVertexInfo(new Vector3(+1, +1, 0), new Vector2(1, 1))
            };

            quadVertex = new()
            {
                new ParticleVertexInfo(new Vector3(-1, -1, 0), new Vector2(0, 1)),
                new ParticleVertexInfo(new Vector3(-1, +1, 0), new Vector2(0, 0)),
                new ParticleVertexInfo(new Vector3(+1, -1, 0), new Vector2(1, 1)),
                new ParticleVertexInfo(new Vector3(+1, +1, 0), new Vector2(1, 0))
            };

            // 实例Buffer中需要放入不同的东西来区分每个实例
            // 这里会把实例的id映射到0-1的浮点数，在shader通过这个值来索引
            particleInstance = new();
            for (int i = 0; i < MAX_COUNT; i++)
            {
                particleInstance.Add(new InstanceInfo(i / (float)MAX_COUNT));
            }

            this.singleParticleVertex = particleVertex.ToArray();
            this.particleInstance = particleInstance.ToArray();
            this.quadVertex = quadVertex.ToArray();
        }


        public virtual void Unload(Mod Mod)
        {
        }

        public virtual void Init(GraphicsDevice graphicsDevice)
        {
            throw new NotImplementedException();
        }

        public virtual void Compute(GraphicsDevice graphicsDevice)
        {
            throw new NotImplementedException();
        }

        public virtual void Render(GraphicsDevice graphicsDevice)
        {
            throw new NotImplementedException();
        }


        public virtual void SetupRenderTargets(GraphicsDevice graphicsDevice)
        {
            // 初始化相关缓冲区
            VertexBuffer vbo;
            vbo = new VertexBuffer(graphicsDevice, ParticleVertexInfo._VertexDeclaration, 4, BufferUsage.WriteOnly);
            vbo.SetData(this.singleParticleVertex, SetDataOptions.None);
            this.singleParticleVBO = vbo;

            vbo = new VertexBuffer(graphicsDevice, InstanceInfo._VertexDeclaration, MAX_COUNT, BufferUsage.WriteOnly);
            vbo.SetData(this.particleInstance, SetDataOptions.None);
            this.particleVBO = vbo;

            particleBinding = new VertexBufferBinding[] {
                // 单个粒子的4个点
                new VertexBufferBinding(singleParticleVBO),
                // N个粒子的，标记instance frequency来让它被用于实例绘制
                new VertexBufferBinding(particleVBO, 0, 1)
            };

            // 表示实例中条带顶点的顺序就是1234（比如如果绘制的是普通三角形图元的话就要写六个）
            singleParticleEBO = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, 4, BufferUsage.WriteOnly);
            singleParticleEBO.SetData(new int[] { 0, 1, 2, 3 });

            // 注意SurfaceFormat
            // 我们需要用单通道表示一个float
            // 那么需要的精度就是每个通道32位，而不是平时用的8位（0-255）
            // 总大小是2*32=64位（平时用的RGBA是8*4=32位）
            computeRT = new(graphicsDevice, MAX_COUNT, computeTextureHeight, false,
                            SurfaceFormat.Vector2, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            computeRTSwap = new(graphicsDevice, MAX_COUNT, computeTextureHeight, false,
                            SurfaceFormat.Vector2, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        }

        // 顶点属性定义，不作解释
        public struct ParticleVertexInfo : IVertexType
        {
            public static readonly VertexDeclaration _VertexDeclaration = new VertexDeclaration(new VertexElement[2]
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(3 * sizeof(float), VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
            });
            public Vector3 position;
            public Vector2 texCoord;

            public ParticleVertexInfo(Vector3 pos, Vector2 uv)
            {
                position = pos;
                texCoord = uv;
            }

            public VertexDeclaration VertexDeclaration { get => _VertexDeclaration; }
        }
        public struct InstanceInfo : IVertexType
        {
            public static readonly VertexDeclaration _VertexDeclaration = new VertexDeclaration(new VertexElement[]
            {
                new VertexElement(0 * sizeof(float), VertexElementFormat.Single, VertexElementUsage.Position, 0)
            });
            public float texCoord;

            public InstanceInfo(float u)
            {
                texCoord = u;
            }

            public VertexDeclaration VertexDeclaration { get => _VertexDeclaration; }
        }
    }

}