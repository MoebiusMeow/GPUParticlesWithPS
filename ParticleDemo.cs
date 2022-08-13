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
		public readonly int MAX_COUNT = 10000;

		public Effect particleShader;
		public Effect behaviorShader;
		public Texture2D particleTexture;
		public RenderTarget2D computeRT;
		public RenderTarget2D computeRTSwap;

		HintVertexInfo[] singleParticleVertex;
		InstanceInfo[] particleInstance;
		HintVertexInfo[] quadVertex;
		VertexBuffer particleVBO = null;
		VertexBuffer singleParticleVBO = null;
		IndexBuffer singleParticleEBO = null;
		VertexBufferBinding[] particleBinding = null;

		public ParticleDemo() { }


		public override void Load()
		{
			particleShader = Assets.Request<Effect>("Effects/particle", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
			behaviorShader = Assets.Request<Effect>("Effects/behavior", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
			particleTexture = Assets.Request<Texture2D>("Textures/Bubble", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
			// particleTexture = Assets.Request<Texture2D>("Textures/gradientW", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
			On.Terraria.Graphics.Effects.FilterManager.EndCapture += ScreenEffectDecorator;

			List<HintVertexInfo> particleVertex;
			List<HintVertexInfo> quadVertex;
			List<InstanceInfo> particleInstance;
			particleVertex = new()
			{
				new HintVertexInfo(new Vector3(-1, -1, 0), new Vector2(0, 0)),
				new HintVertexInfo(new Vector3(-1, +1, 0), new Vector2(0, 1)),
				new HintVertexInfo(new Vector3(+1, -1, 0), new Vector2(1, 0)),
				new HintVertexInfo(new Vector3(+1, +1, 0), new Vector2(1, 1))
			};

			quadVertex = new()
			{
				new HintVertexInfo(new Vector3(-1, -1, 0), new Vector2(0, 1)),
				new HintVertexInfo(new Vector3(-1, +1, 0), new Vector2(0, 0)),
				new HintVertexInfo(new Vector3(+1, -1, 0), new Vector2(1, 1)),
				new HintVertexInfo(new Vector3(+1, +1, 0), new Vector2(1, 0))
			};

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

			bool flag = false;
			if (particleVBO == null || singleParticleVBO == null || singleParticleEBO == null)
			{
				VertexBuffer vbo;
				vbo = new VertexBuffer(graphicsDevice, HintVertexInfo._VertexDeclaration, 4, BufferUsage.WriteOnly);
				vbo.SetData(this.singleParticleVertex, SetDataOptions.None);
				this.singleParticleVBO = vbo;

				vbo = new VertexBuffer(graphicsDevice, InstanceInfo._VertexDeclaration, MAX_COUNT, BufferUsage.WriteOnly);
				vbo.SetData(this.particleInstance, SetDataOptions.None);
				this.particleVBO = vbo;

				particleBinding = new VertexBufferBinding[] {
					new VertexBufferBinding(singleParticleVBO),
					new VertexBufferBinding(particleVBO, 0, 1)
				};

				singleParticleEBO = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, 6, BufferUsage.WriteOnly);
				singleParticleEBO.SetData(new int[] { 0, 1, 2, 3});

				computeRT = new(graphicsDevice, MAX_COUNT, 2, false, SurfaceFormat.Vector2, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
				computeRTSwap = new(graphicsDevice, MAX_COUNT, 2, false, SurfaceFormat.Vector2, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

				flag = true;
			}

			graphicsDevice.SetRenderTarget(screenTarget2);
			graphicsDevice.Clear(Color.Transparent);
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
			Main.spriteBatch.Draw(screenTarget1, Vector2.Zero, Color.White);
			Main.spriteBatch.End();

			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer);
			Main.spriteBatch.Draw(computeRT, new Vector2(100, 100), computeRT.Bounds, Color.White, 0, Vector2.Zero, new Vector2(0.3f, 10), SpriteEffects.None, 0.5f);
			Main.spriteBatch.Draw(computeRTSwap, new Vector2(100, 150), computeRT.Bounds, Color.White, 0, Vector2.Zero, new Vector2(0.3f, 10), SpriteEffects.None, 0.5f);
			Main.spriteBatch.End();

			if (Main.mouseRight || flag)
			{
				graphicsDevice.SetRenderTarget(computeRTSwap);
				graphicsDevice.Clear(Color.Transparent);
				graphicsDevice.SetRenderTarget(computeRT);
				Vector2 position = Main.LocalPlayer.Center;
				position /= new Vector2(Main.maxTilesX, Main.maxTilesY) * 16;
				// graphicsDevice.Clear(new Color(position.X, position.Y, 0.5f));
				Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
				behaviorShader.Parameters["uInitPos"].SetValue(new Vector3(position, 0));
				behaviorShader.Parameters["uInitVel"].SetValue(new Vector3(0.5f, 0.5f, 0));
				behaviorShader.CurrentTechnique.Passes["Init"].Apply();
				graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, quadVertex, 0, 4);
				Main.spriteBatch.End();
			}
			else
			{
				graphicsDevice.SetRenderTarget(computeRTSwap);
				graphicsDevice.Clear(Color.Transparent);
				Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
				particleShader.Parameters["uTex1"].SetValue(computeRT);
				particleShader.CurrentTechnique.Passes["Copy"].Apply();
				graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, quadVertex, 0, 4);
				// Main.spriteBatch.Draw(particleTexture, Vector2.Zero, Color.White);
				Main.spriteBatch.End();

				graphicsDevice.SetRenderTarget(computeRT);
				graphicsDevice.Clear(Color.Red);
				Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
				behaviorShader.Parameters["uTex1"].SetValue(computeRTSwap);
				behaviorShader.Parameters["uWorldSize"].SetValue(new Vector2(Main.maxTilesX, Main.maxTilesY) * 16);
				behaviorShader.Parameters["uDeltaTime"].SetValue(0.1f);
				if (Main.mouseLeft)
                    behaviorShader.Parameters["uTarget"].SetValue(Main.screenPosition + new Vector2(Main.mouseX, Main.mouseY));
				else
                    behaviorShader.Parameters["uTarget"].SetValue(Main.LocalPlayer.Center);
                behaviorShader.Parameters["uDirection"].SetValue(new Vector2((Main.mouseX - 0.5f * Main.screenWidth) * 100.01f,
																			 (0.5f * Main.screenHeight - Main.mouseY) * 10.01f));
				behaviorShader.CurrentTechnique.Passes["Compute"].Apply();
				graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, quadVertex, 0, 4);
				Main.spriteBatch.End();
			}

			graphicsDevice.SetRenderTarget(screenTarget1);
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
			Main.spriteBatch.Draw(screenTarget2, Vector2.Zero, Color.White);
			Main.spriteBatch.End();

			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
								   Main.DefaultSamplerState, DepthStencilState.None,
								   RasterizerState.CullCounterClockwise, null, Matrix.Identity);

			// hookshotHintShader.Parameters["uMVP"].SetValue(mvp);
			particleShader.Parameters["uTex0"].SetValue(particleTexture);
			particleShader.Parameters["uTex1"].SetValue(computeRT);
			// particleShader.Parameters["uWorldSize"].SetValue(new Vector2(Main.screenWidth, Main.screenHeight));
			particleShader.Parameters["uWorldSize"].SetValue(new Vector2(Main.maxTilesX, Main.maxTilesY) * 16);
			// particleShader.Parameters["uScreenPosition"].SetValue(Vector2.Zero);
			particleShader.Parameters["uScreenPosition"].SetValue(Main.screenPosition);
			particleShader.Parameters["uResolution"].SetValue(new Vector2(Main.screenWidth, Main.screenHeight));

			particleShader.Parameters["uScale"].SetValue(Vector2.One * 16);
			particleShader.CurrentTechnique.Passes["Particles"].Apply();
			// graphicsDevice.DrawUserPrimitives(PrimitiveType.PointListEXT, particleVertex, 0, particleVertex.Length);

			graphicsDevice.Indices = singleParticleEBO;
			graphicsDevice.SetVertexBuffers(particleBinding);
			graphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleStrip, 0, 0, 4, 0, 2, MAX_COUNT);
			graphicsDevice.SetVertexBuffers(null);
			graphicsDevice.Indices = null;

			Main.spriteBatch.End();

			orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
		}


		public struct HintVertexInfo : IVertexType
		{
			public static readonly VertexDeclaration _VertexDeclaration = new VertexDeclaration(new VertexElement[2]
			{
				new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
				new VertexElement(3 * sizeof(float), VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
			});
			public Vector3 position;
			public Vector2 texCoord;

			public HintVertexInfo(Vector3 pos, Vector2 uv)
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