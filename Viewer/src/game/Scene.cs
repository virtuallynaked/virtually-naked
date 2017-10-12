using Newtonsoft.Json;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.IO;
using Valve.VR;

class Scene : IDisposable {
	private readonly ToneMappingSettings toneMappingSettings;
	private readonly ImageBasedLightingEnvironment iblEnvironment;
	private readonly Backdrop backdrop;
	private readonly PlayspaceFloor floor;
	private readonly RenderModelRenderer renderModelRenderer;
	private readonly MeshRenderer primitiveRenderer;
	private readonly Actor actor;
	private readonly Menu menu;

	public Scene(IArchiveDirectory dataDir, Device device, ShaderCache shaderCache, StandardSamplers standardSamplers, TrackedDeviceBufferManager trackedDeviceBufferManager, ControllerManager controllerManager) {
		toneMappingSettings = new ToneMappingSettings();
		iblEnvironment = new ImageBasedLightingEnvironment(device, standardSamplers, dataDir, InitialSettings.Environment);
		backdrop = new Backdrop(device, shaderCache);
		floor = new PlayspaceFloor(device, shaderCache);
		renderModelRenderer = new RenderModelRenderer(device, shaderCache, trackedDeviceBufferManager);
		primitiveRenderer = new MeshRenderer(device, shaderCache, Matrix.Translation(0, 1.25f, 0), GeometricPrimitiveFactory.MakeSphere(0.5f, 100).AsTriMesh());
		actor = Actor.Load(dataDir, device, shaderCache, controllerManager);
		
		var iblMenu = LightingEnvironmentMenu.MakeMenuLevel(dataDir, iblEnvironment);
		var toneMappingMenuLevel = new ToneMappingMenuLevel(toneMappingSettings);
		var renderSettingsMenuLevel = new StaticMenuLevel(
			new SubLevelMenuItem("Lighting Enviroment", iblMenu),
			new SubLevelMenuItem("Tone Mapping", toneMappingMenuLevel)
		);
		var scenePersistenceMenuLevel = ScenePersistenceMenuLevel.Make(this);
		var appMenuLevel = new StaticMenuLevel(
			new SubLevelMenuItem("Scenes", scenePersistenceMenuLevel),
			new SubLevelMenuItem("Render Settings", renderSettingsMenuLevel)
		);

		var rootMenuLevel = new CombinedMenuLevel(appMenuLevel, actor.MenuLevel);
		menu = new Menu(device, shaderCache, trackedDeviceBufferManager, controllerManager, rootMenuLevel);
	}

	public void Dispose() {
		iblEnvironment.Dispose();
		backdrop.Dispose();
		floor.Dispose();
		renderModelRenderer.Dispose();
		primitiveRenderer.Dispose();
		actor.Dispose();
		menu.Dispose();
	}

	public ToneMappingSettings ToneMappingSettings => toneMappingSettings;
	
	public void Update(DeviceContext context, FrameUpdateParameters updateParameters) {
		menu.Update(context);
		renderModelRenderer.Update(updateParameters);
		iblEnvironment.Predraw(context);
		floor.Update(context);
		actor.Update(context, updateParameters, iblEnvironment);
	}

	public void RenderPass(DeviceContext context, RenderingPass pass) {
		iblEnvironment.Apply(context.PixelShader);

		if (pass.Layer == RenderingLayer.Opaque) {
			//backdrop.Render(context);
			floor.Render(context);
			renderModelRenderer.Render(context);
			//primitiveRenderer.Render(context);
		}
		
		actor.RenderPass(context, pass);
		menu.RenderPass(context, pass);
	}
		
	public void DoPrework(DeviceContext context) {
		menu.DoPrework(context);
	}

	public void DoDrawCompanionWindowUi(DeviceContext context) {
		menu.DoDrawCompanionWindowUi(context);
	}

	public void DoPostwork(DeviceContext context) {
		//this can block (while waiting for pose-feedback buffer to copy, so don't place expensive operations after this)
		actor.DoPostwork(context);
		
		//this is OK to go after actor.DoPostwork because its cheap and should happen as late in the frame as possible
		menu.DoPostwork(context);
	}

	public class Recipe {
		[JsonProperty("tone-mapping")]
		public ToneMappingSettings.Recipe toneMapping;

		[JsonProperty("lighting-environment")]
		public ImageBasedLightingEnvironment.Recipe lightingEnvironment;

		[JsonProperty("actor")]
		public Actor.Recipe actor;
		
		public void Merge(Scene scene) {
			toneMapping?.Merge(scene.toneMappingSettings);
			lightingEnvironment?.Merge(scene.iblEnvironment);
			actor?.Merge(scene.actor);
		}
	}

	public Recipe Recipize() {
		return new Recipe {
			toneMapping = toneMappingSettings.Recipize(),
			lightingEnvironment = iblEnvironment.Recipize(),
			actor = actor.Recipize()
		};
	}
}