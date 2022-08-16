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
        public virtual int computeTextureHeight => 2;   // ÿ������������float����������߶ȿ��Դ洢��������

        public Mod Mod = null;


        public virtual int MAX_COUNT => 10000;          // N 

        public Effect particleShader;                   // ������������Pass�Ϳ���Pass
        public Effect behaviorShader;                   // ��ٵļ�����ɫ��������������Ϊ����Pass�ͳ�ʼ��Pass
        
        public Texture2D particleTexture;               // ����ͼ������
        public RenderTarget2D computeRT;                // ��¼ÿ���������ݵ�����N * 2����ֻ������ͨ������x��y�ᣬ���зֱ����λ�ú��ٶ�
        public RenderTarget2D computeRTSwap;

        public ParticleVertexInfo[] singleParticleVertex;      // �������ӵ��ĸ�����
        public InstanceInfo[] particleInstance;                // ����ʵ���б�����N����
        public ParticleVertexInfo[] quadVertex;                // ���ڻ���ƽ���ľ��ε��ĸ����㣬ע��uv����

        public VertexBuffer particleVBO = null;                // �洢ʵ����InstanceInfo����Buffer ������N��
        public VertexBuffer singleParticleVBO = null;          // �洢�������ӵ��ĸ������Buffer ������4��
        public IndexBuffer singleParticleEBO = null;           // �洢���������ĸ�����Ļ���˳�������Buffer ����Ϊʹ���������Բ���Ҫ�ر���
        public VertexBufferBinding[] particleBinding = null;   // ��ʵ��Buffer�͵������ӵ�Buffer������ʵ������

        public PSParticleBase() { }

        public virtual void SetDefault() { }

        public virtual void Load(Mod Mod)
        {
            particleShader = Mod.Assets.Request<Effect>(particalShaderPath, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            behaviorShader = Mod.Assets.Request<Effect>(behaviorShaderPath, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            particleTexture = Mod.Assets.Request<Texture2D>(particleTexturePath, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

            // ��ʼ����������
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

            // ʵ��Buffer����Ҫ���벻ͬ�Ķ���������ÿ��ʵ��
            // ������ʵ����idӳ�䵽0-1�ĸ���������shaderͨ�����ֵ������
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
            // ��ʼ����ػ�����
            VertexBuffer vbo;
            vbo = new VertexBuffer(graphicsDevice, ParticleVertexInfo._VertexDeclaration, 4, BufferUsage.WriteOnly);
            vbo.SetData(this.singleParticleVertex, SetDataOptions.None);
            this.singleParticleVBO = vbo;

            vbo = new VertexBuffer(graphicsDevice, InstanceInfo._VertexDeclaration, MAX_COUNT, BufferUsage.WriteOnly);
            vbo.SetData(this.particleInstance, SetDataOptions.None);
            this.particleVBO = vbo;

            particleBinding = new VertexBufferBinding[] {
                // �������ӵ�4����
                new VertexBufferBinding(singleParticleVBO),
                // N�����ӵģ����instance frequency������������ʵ������
                new VertexBufferBinding(particleVBO, 0, 1)
            };

            // ��ʾʵ�������������˳�����1234������������Ƶ�����ͨ������ͼԪ�Ļ���Ҫд������
            singleParticleEBO = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, 4, BufferUsage.WriteOnly);
            singleParticleEBO.SetData(new int[] { 0, 1, 2, 3 });

            // ע��SurfaceFormat
            // ������Ҫ�õ�ͨ����ʾһ��float
            // ��ô��Ҫ�ľ��Ⱦ���ÿ��ͨ��32λ��������ƽʱ�õ�8λ��0-255��
            // �ܴ�С��2*32=64λ��ƽʱ�õ�RGBA��8*4=32λ��
            computeRT = new(graphicsDevice, MAX_COUNT, computeTextureHeight, false,
                            SurfaceFormat.Vector2, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            computeRTSwap = new(graphicsDevice, MAX_COUNT, computeTextureHeight, false,
                            SurfaceFormat.Vector2, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        }

        // �������Զ��壬��������
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