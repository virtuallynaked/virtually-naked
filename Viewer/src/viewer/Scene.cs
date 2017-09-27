using Newtonsoft.Json;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.IO;
using Valve.VR;

class Scene : IDisposable {
	private readonly ImageBasedLightingEnvironment iblEnvironment;
	private readonly Backdrop backdrop;
	private readonly PlayspaceFloor floor;
	private readonly RenderModelRenderer renderModelRenderer;
	private readonly QuadMeshRenderer primitiveRenderer;
	private readonly FigureGroup figureGroup;
	private readonly Menu menu;

	public Scene(IArchiveDirectory dataDir, Device device, ShaderCache shaderCache, StandardSamplers standardSamplers, TrackedDevicePose_t[] poses, ControllerManager controllerManager, IMenuLevel toneMappingMenuLevel) {
		iblEnvironment = new ImageBasedLightingEnvironment(device, standardSamplers, dataDir, FigureActiveSettings.Environment);
		backdrop = new Backdrop(device, shaderCache);
		floor = new PlayspaceFloor(device, shaderCache);
		renderModelRenderer = new RenderModelRenderer(device, shaderCache, poses);
		primitiveRenderer = new QuadMeshRenderer(device, shaderCache, Matrix.Translation(0, 1.25f, 0), GeometricPrimitiveFactory.MakeSphere(0.5f, 100));
		figureGroup = FigureGroup.Load(dataDir, device, shaderCache, controllerManager);
		
		var iblMenu = LightingEnvironmentMenu.MakeMenuLevel(dataDir, iblEnvironment);
		var renderSettingsMenuLevel = new StaticMenuLevel(
			new SubLevelMenuItem("Lighting Enviroment", iblMenu),
			new SubLevelMenuItem("Tone Mapping", toneMappingMenuLevel)
		);
		var scenePersistenceMenuLevel = ScenePersistenceMenuLevel.Make(this);
		var appMenuLevel = new StaticMenuLevel(
			new SubLevelMenuItem("Scenes", scenePersistenceMenuLevel),
			new SubLevelMenuItem("Render Settings", renderSettingsMenuLevel)
		);

		var rootMenuLevel = new CombinedMenuLevel(appMenuLevel, figureGroup.MenuLevel);
		menu = new Menu(device, shaderCache, controllerManager, rootMenuLevel);
	}

	public void Dispose() {
		iblEnvironment.Dispose();
		backdrop.Dispose();
		floor.Dispose();
		renderModelRenderer.Dispose();
		primitiveRenderer.Dispose();
		figureGroup.Dispose();
		menu.Dispose();
	}

	public void Update(DeviceContext context, FrameUpdateParameters updateParameters) {
		menu.Update(context);
		iblEnvironment.Predraw(context);
		floor.Update(context);
		figureGroup.Update(context, updateParameters, iblEnvironment);
	}

	public void RenderPass(DeviceContext context, RenderingPass pass) {
		iblEnvironment.Apply(context.PixelShader);

		if (pass.Layer == RenderingLayer.Opaque) {
			//backdrop.Render(context);
			floor.Render(context);
			renderModelRenderer.Render(context);
			//primitiveRenderer.Render(context);
		}
		
		figureGroup.RenderPass(context, pass);
		menu.RenderPass(context, pass);
	}

	public void RenderCompanionWindowUi(DeviceContext context) {
		menu.RenderCompanionWindowUi(context);
	}
	
	public class Recipe {
		[JsonProperty("lighting-environment")]
		public ImageBasedLightingEnvironment.Recipe lightingEnvironment;

		[JsonProperty("actor")]
		public FigureGroup.Recipe actor;
		
		public void Merge(Scene scene) {
			lightingEnvironment?.Merge(scene.iblEnvironment);
			actor?.Merge(scene.figureGroup);
		}
	}

	public Recipe Recipize() {
		return new Recipe {
			lightingEnvironment = iblEnvironment.Recipize(),
			actor = figureGroup.Recipize()
		};
	}
}