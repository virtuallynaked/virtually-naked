using Newtonsoft.Json;
using SharpDX;
using SharpDX.Direct3D11;
using System;

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
		var textureCache = new TextureCache(device);

		toneMappingSettings = new ToneMappingSettings();
		iblEnvironment = new ImageBasedLightingEnvironment(device, standardSamplers, dataDir, InitialSettings.Environment, InitialSettings.EnvironmentRotation);
		backdrop = new Backdrop(device, shaderCache);
		floor = new PlayspaceFloor(device, shaderCache);
		renderModelRenderer = new RenderModelRenderer(device, shaderCache, trackedDeviceBufferManager);
		primitiveRenderer = new MeshRenderer(device, shaderCache, Matrix.Translation(0, 1.25f, 0), GeometricPrimitiveFactory.MakeSphere(0.5f, 100).AsTriMesh());

		var shapeNormalsLoader = new ShapeNormalsLoader(dataDir, device, textureCache);
		var figureRendererLoader = new FigureRendererLoader(dataDir, device, shaderCache, textureCache);
		var figureLoader = new FigureLoader(dataDir, device, shaderCache, shapeNormalsLoader, figureRendererLoader);
		actor = Actor.Load(dataDir, device, shaderCache, controllerManager, figureLoader);
		
		var iblMenu = LightingEnvironmentMenu.MakeMenuLevel(dataDir, iblEnvironment);
		var toneMappingMenuLevel = new ToneMappingMenuLevel(toneMappingSettings);
		var renderSettingsMenuLevel = new StaticMenuLevel(
			new SubLevelMenuItem("Lighting Enviroment", iblMenu),
			new SubLevelMenuItem("Tone Mapping", toneMappingMenuLevel)
		);
		var scenePersistenceMenuLevel = ScenePersistenceMenuLevel.Make(this);
		var appMenuLevel = new StaticMenuLevel(
			new SubLevelMenuItem("Save/Load", scenePersistenceMenuLevel),
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

		if (pass.Layer == RenderingLayer.OneSidedOpaque) {
			bool depthOnly = pass.OutputMode == OutputMode.FalseDepth;
			//backdrop.Render(context, depthOnly);
			floor.Render(context, depthOnly);
			renderModelRenderer.Render(context, depthOnly);
			//primitiveRenderer.Render(context, depthOnly);
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
