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
    public class Bubble : PSParticleBase
    {
        public override string particalShaderPath => "Effects/particle";
        public override string behaviorShaderPath => "Effects/behavior";
        public override string particleTexturePath => "Textures/Bubble";
        public override int MAX_COUNT => 10000;

        public Bubble() { }

        public override void SetDefault() { }

        public override void Init(GraphicsDevice graphicsDevice)
        {
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

        public override void Compute(GraphicsDevice graphicsDevice)
        {
            if (Main.mouseRight)
            {
                // 按下右键初始化粒子
                Init(graphicsDevice);
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

        }

        public override void Render(GraphicsDevice graphicsDevice)
        {
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
        }
    }

}