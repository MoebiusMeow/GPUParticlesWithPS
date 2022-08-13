using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace ParticleDemo
{
    public class ParticleDemo : Mod
    {
        public readonly int MAX_COUNT = 10000;          // N 

        public Effect particleShader;                   // 包括绘制粒子Pass和拷贝Pass
        public Effect behaviorShader;                   // 虚假的计算着色器，包含粒子行为计算Pass和初始化Pass
        
        public Texture2D particleTexture;               // 粒子图形纹理
        public RenderTarget2D computeRT;                // 记录每个粒子数据的纹理（N * 2），只含红绿通道代表x和y轴，两行分别代表位置和速度
        public RenderTarget2D computeRTSwap;

        ParticleVertexInfo[] singleParticleVertex;      // 单个粒子的四个顶点
        InstanceInfo[] particleInstance;                // 粒子实例列表，包含N个点
        ParticleVertexInfo[] quadVertex;                // 用于绘制平凡的矩形的四个顶点，注意uv方向

        VertexBuffer particleVBO = null;                // 存储实例（InstanceInfo）的Buffer （长度N）
        VertexBuffer singleParticleVBO = null;          // 存储单个粒子的四个顶点的Buffer （长度4）
        IndexBuffer singleParticleEBO = null;           // 存储单个粒子四个顶点的绘制顺序的索引Buffer （因为使用条带所以不需要特别处理）
        VertexBufferBinding[] particleBinding = null;   // 绑定实例Buffer和单个粒子的Buffer，用于实例绘制

        public ParticleDemo() { }


        public override void Load()
        {
            particleShader = Assets.Request<Effect>("Effects/particle", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            behaviorShader = Assets.Request<Effect>("Effects/behavior", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            particleTexture = Assets.Request<Texture2D>("Textures/Bubble", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            On.Terraria.Graphics.Effects.FilterManager.EndCapture += ScreenEffectDecorator;

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

            base.Load();
        }


        public override void Unload()
        {
            On.Terraria.Graphics.Effects.FilterManager.EndCapture -= ScreenEffectDecorator;
            base.Unload();
        }


        public void ScreenEffectDecorator(On.Terraria.Graphics.Effects.FilterManager.orig_EndCapture orig,
                                          Terraria.Graphics.Effects.FilterManager self,
                                          RenderTarget2D finalTexture, RenderTarget2D screenTarget1,
                                          RenderTarget2D screenTarget2, Color clearColor)
        {
            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;

            // 初始化，flag用来标记是否是初始化
            bool flag = false;
            if (particleVBO == null || singleParticleVBO == null || singleParticleEBO == null)
            {
                this.SetupRenderTargets(graphicsDevice);
                flag = true;
            }

            // 暂存当前RT
            graphicsDevice.SetRenderTarget(screenTarget2);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
            Main.spriteBatch.Draw(screenTarget1, Vector2.Zero, Color.White);
            Main.spriteBatch.End();

            // 为了展示机制，这里额外地绘制上粒子数据纹理
            // 每一条光谱中的竖线都代表一个粒子
            // 红是x，绿是y
            // 竖线上半是位置，下半是速度
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer);
            Main.spriteBatch.Draw(computeRT, new Vector2(100, 100), computeRT.Bounds, Color.White, 0, Vector2.Zero, new Vector2(0.3f, 10), SpriteEffects.None, 0.5f);
            // 绘制swap是为了调试
            Main.spriteBatch.Draw(computeRTSwap, new Vector2(100, 150), computeRT.Bounds, Color.White, 0, Vector2.Zero, new Vector2(0.3f, 10), SpriteEffects.None, 0.5f);
            Main.spriteBatch.End();

            if (Main.mouseRight || flag)
            {
                // 按下右键初始化粒子
                graphicsDevice.SetRenderTarget(computeRTSwap);
                graphicsDevice.Clear(Color.Transparent);

                graphicsDevice.SetRenderTarget(computeRT);
                Vector2 position = Main.LocalPlayer.Center;
                position /= new Vector2(Main.maxTilesX, Main.maxTilesY) * 16;
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
                behaviorShader.Parameters["uInitPos"].SetValue(new Vector3(position, 0));
                behaviorShader.Parameters["uInitVel"].SetValue(new Vector3(0.5f, 0.5f, 0));
                behaviorShader.CurrentTechnique.Passes["Init"].Apply();
                graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, quadVertex, 0, 4);
                Main.spriteBatch.End();
            }
            else
            {
                // 【计算部分】计算每个粒子下一tick的属性，但是通过绘制来计算

                // 暂存粒子数据
                graphicsDevice.SetRenderTarget(computeRTSwap);
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
                // 不使用一般draw是因为我们的computeRT的surface种类可能不常规（比如这里用的是vector2而不是RGBA）
                particleShader.Parameters["uTex1"].SetValue(computeRT);
                particleShader.CurrentTechnique.Passes["Copy"].Apply();
                graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, quadVertex, 0, 4);
                Main.spriteBatch.End();

                graphicsDevice.SetRenderTarget(computeRT);
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
                behaviorShader.Parameters["uTex1"].SetValue(computeRTSwap);
                // 用于把0-1的颜色值映射到世界坐标
                behaviorShader.Parameters["uWorldSize"].SetValue(new Vector2(Main.maxTilesX, Main.maxTilesY) * 16);
                // 运动相关参数，例子中的例子会去试图追踪目标点
                behaviorShader.Parameters["uDeltaTime"].SetValue(0.1f);
                if (Main.mouseLeft)
                    behaviorShader.Parameters["uTarget"].SetValue(Main.screenPosition + new Vector2(Main.mouseX, Main.mouseY));
                else
                    behaviorShader.Parameters["uTarget"].SetValue(Main.LocalPlayer.Center);
                // 这个暂时没用到，做不同的粒子行为时可以用
                behaviorShader.Parameters["uDirection"].SetValue(new Vector2((Main.mouseX - 0.5f * Main.screenWidth) * 100.01f,
                                                                             (0.5f * Main.screenHeight - Main.mouseY) * 10.01f));
                behaviorShader.CurrentTechnique.Passes["Compute"].Apply();
                graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, quadVertex, 0, 4);
                Main.spriteBatch.End();
            }

            // 还原屏幕RT，准备最终绘制
            graphicsDevice.SetRenderTarget(screenTarget1);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            Main.spriteBatch.Draw(screenTarget2, Vector2.Zero, Color.White);
            Main.spriteBatch.End();

            // 【绘制部分】
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                                   Main.DefaultSamplerState, DepthStencilState.None,
                                   RasterizerState.CullCounterClockwise, null, Matrix.Identity);

            particleShader.Parameters["uTex0"].SetValue(particleTexture);
            particleShader.Parameters["uTex1"].SetValue(computeRT);

            // 可以自由发挥，粒子的位置会被bound在uWorldSize里面（因为颜色值有0-1的限制）
            // 比如如下方式可以让粒子都画在一个屏幕里
            // particleShader.Parameters["uWorldSize"].SetValue(new Vector2(Main.screenWidth, Main.screenHeight));
            // particleShader.Parameters["uScreenPosition"].SetValue(Vector2.Zero);
            particleShader.Parameters["uWorldSize"].SetValue(new Vector2(Main.maxTilesX, Main.maxTilesY) * 16);
            particleShader.Parameters["uScreenPosition"].SetValue(Main.screenPosition);

            // 屏幕分辨率 以及 粒子大小（这里用的是16*16）
            // 注意粒子大小会影响效率，因为使用AlphaBlend，重叠的粒子也会多次绘制
            // 粒子大小上升会使得总共的像素量上升
            particleShader.Parameters["uResolution"].SetValue(new Vector2(Main.screenWidth, Main.screenHeight));
            particleShader.Parameters["uScale"].SetValue(Vector2.One * 16);
            particleShader.CurrentTechnique.Passes["Particles"].Apply();

            // 实例绘制
            graphicsDevice.Indices = singleParticleEBO;
            graphicsDevice.SetVertexBuffers(particleBinding);
            // 一个粒子4个点，2个三角条带图元，绘制N个实例
            graphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleStrip, 0, 0, 4, 0, 2, MAX_COUNT);
            graphicsDevice.SetVertexBuffers(null);
            graphicsDevice.Indices = null;

            Main.spriteBatch.End();

            orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
        }


        public void SetupRenderTargets(GraphicsDevice graphicsDevice)
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
            computeRT = new(graphicsDevice, MAX_COUNT, 2, false, SurfaceFormat.Vector2, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            computeRTSwap = new(graphicsDevice, MAX_COUNT, 2, false, SurfaceFormat.Vector2, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
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