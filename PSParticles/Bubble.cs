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
                // �����Ҽ���ʼ������
                Init(graphicsDevice);
            }
            else
            {
                // �����㲿�֡�����ÿ��������һtick�����ԣ�����ͨ������������

                // �ݴ���������
                graphicsDevice.SetRenderTarget(computeRTSwap);
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
                // ��ʹ��һ��draw����Ϊ���ǵ�computeRT��surface������ܲ����棨���������õ���vector2������RGBA��
                particleShader.Parameters["uTex1"].SetValue(computeRT);
                particleShader.CurrentTechnique.Passes["Copy"].Apply();
                graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, quadVertex, 0, 4);
                Main.spriteBatch.End();

                graphicsDevice.SetRenderTarget(computeRT);
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
                behaviorShader.Parameters["uTex1"].SetValue(computeRTSwap);
                // ���ڰ�0-1����ɫֵӳ�䵽��������
                behaviorShader.Parameters["uWorldSize"].SetValue(new Vector2(Main.maxTilesX, Main.maxTilesY) * 16);
                // �˶���ز����������е����ӻ�ȥ��ͼ׷��Ŀ���
                behaviorShader.Parameters["uDeltaTime"].SetValue(0.1f);
                if (Main.mouseLeft)
                    behaviorShader.Parameters["uTarget"].SetValue(Main.screenPosition + new Vector2(Main.mouseX, Main.mouseY));
                else
                    behaviorShader.Parameters["uTarget"].SetValue(Main.LocalPlayer.Center);
                // �����ʱû�õ�������ͬ��������Ϊʱ������
                behaviorShader.Parameters["uDirection"].SetValue(new Vector2((Main.mouseX - 0.5f * Main.screenWidth) * 100.01f,
                                                                             (0.5f * Main.screenHeight - Main.mouseY) * 10.01f));
                behaviorShader.CurrentTechnique.Passes["Compute"].Apply();
                graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, quadVertex, 0, 4);
                Main.spriteBatch.End();
            }

        }

        public override void Render(GraphicsDevice graphicsDevice)
        {
            // �����Ʋ��֡�
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                                   Main.DefaultSamplerState, DepthStencilState.None,
                                   RasterizerState.CullCounterClockwise, null, Matrix.Identity);

            particleShader.Parameters["uTex0"].SetValue(particleTexture);
            particleShader.Parameters["uTex1"].SetValue(computeRT);

            // �������ɷ��ӣ����ӵ�λ�ûᱻbound��uWorldSize���棨��Ϊ��ɫֵ��0-1�����ƣ�
            // �������·�ʽ���������Ӷ�����һ����Ļ��
            // particleShader.Parameters["uWorldSize"].SetValue(new Vector2(Main.screenWidth, Main.screenHeight));
            // particleShader.Parameters["uScreenPosition"].SetValue(Vector2.Zero);
            particleShader.Parameters["uWorldSize"].SetValue(new Vector2(Main.maxTilesX, Main.maxTilesY) * 16);
            particleShader.Parameters["uScreenPosition"].SetValue(Main.screenPosition);

            // ��Ļ�ֱ��� �Լ� ���Ӵ�С�������õ���16*16��
            // ע�����Ӵ�С��Ӱ��Ч�ʣ���Ϊʹ��AlphaBlend���ص�������Ҳ���λ���
            // ���Ӵ�С������ʹ���ܹ�������������
            particleShader.Parameters["uResolution"].SetValue(new Vector2(Main.screenWidth, Main.screenHeight));
            particleShader.Parameters["uScale"].SetValue(Vector2.One * 16);
            particleShader.CurrentTechnique.Passes["Particles"].Apply();

            // ʵ������
            graphicsDevice.Indices = singleParticleEBO;
            graphicsDevice.SetVertexBuffers(particleBinding);
            // һ������4���㣬2����������ͼԪ������N��ʵ��
            graphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleStrip, 0, 0, 4, 0, 2, MAX_COUNT);
            graphicsDevice.SetVertexBuffers(null);
            graphicsDevice.Indices = null;

            Main.spriteBatch.End();
        }
    }

}