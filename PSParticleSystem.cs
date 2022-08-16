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
using ParticleDemo.PSParticles;

namespace ParticleDemo
{
    public class PSParticleSystem : ModSystem
    {
        public PSParticleBase[] particleClasses;
        public bool inited = false;

        public PSParticleSystem()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Type[] particleTypes = assembly.GetTypes()
                .Where(t => string.Equals(t.Namespace, this.GetType().Namespace + ".PSParticles", StringComparison.Ordinal)).ToArray();
            particleTypes = particleTypes.Where(t => t.IsSubclassOf(typeof(PSParticleBase))).ToArray();
            particleClasses = particleTypes.Select(t => (PSParticleBase)Activator.CreateInstance(t)).ToArray();
        }

        public override void OnWorldLoad()
        {
            inited = false;
            base.OnWorldLoad();
        }

        public override void Load()
        {
            Array.ForEach(particleClasses, s =>
            {
                s.Mod = this.Mod;
                s.Load(Mod);
            });

            On.Terraria.Graphics.Effects.FilterManager.EndCapture += ScreenEffectDecorator;
            Main.QueueMainThreadAction(() => 
            {
                Array.ForEach(particleClasses, s =>
                {
                    s.SetupRenderTargets(Main.graphics.GraphicsDevice);
                });
            });
            base.Load();
        }


        public override void Unload()
        {
            Array.ForEach(particleClasses, s => { s.Unload(Mod); });
            On.Terraria.Graphics.Effects.FilterManager.EndCapture -= ScreenEffectDecorator;
            base.Unload();
        }


        public void ScreenEffectDecorator(On.Terraria.Graphics.Effects.FilterManager.orig_EndCapture orig,
                                          Terraria.Graphics.Effects.FilterManager self,
                                          RenderTarget2D finalTexture, RenderTarget2D screenTarget1,
                                          RenderTarget2D screenTarget2, Color clearColor)
        {
            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;

            // 暂存当前RT
            graphicsDevice.SetRenderTarget(screenTarget2);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
            Main.spriteBatch.Draw(screenTarget1, Vector2.Zero, Color.White);
            Main.spriteBatch.End();

            if (!inited)
            {
                inited = true;
                Array.ForEach(particleClasses, s =>
                {
                    s.Init(graphicsDevice);
                });
            }
            /*
            // 为了展示机制，这里额外地绘制上粒子数据纹理
            // 每一条光谱中的竖线都代表一个粒子
            // 红是x，绿是y
            // 竖线上半是位置，下半是速度
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer);
            Main.spriteBatch.Draw(computeRT, new Vector2(100, 100), computeRT.Bounds, Color.White, 0, Vector2.Zero, new Vector2(0.3f, 10), SpriteEffects.None, 0.5f);
            // 绘制swap是为了调试
            Main.spriteBatch.Draw(computeRTSwap, new Vector2(100, 150), computeRT.Bounds, Color.White, 0, Vector2.Zero, new Vector2(0.3f, 10), SpriteEffects.None, 0.5f);
            Main.spriteBatch.End();
            */

            // 【计算部分】计算每个粒子下一tick的属性，但是通过绘制来计算
            Array.ForEach(particleClasses, s =>
            {
                s.Compute(graphicsDevice);
            });

            // 还原屏幕RT，准备最终绘制
            graphicsDevice.SetRenderTarget(screenTarget1);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            Main.spriteBatch.Draw(screenTarget2, Vector2.Zero, Color.White);
            Main.spriteBatch.End();

            // 【绘制部分】
            Array.ForEach(particleClasses, s =>
            {
                s.Render(graphicsDevice);
            });

            orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
        }

    }

}