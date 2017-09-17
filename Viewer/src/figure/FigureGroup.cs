using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Linq;

class FigureGroup : IDisposable {
	public static FigureGroup Load(IArchiveDirectory dataDir, Device device, ShaderCache shaderCache, ControllerManager controllerManager) {
		var parentFigure = FigureFacade.Load(dataDir, device, shaderCache, controllerManager, FigureActiveSettings.Parent, null);

		var hairFigure = FigureActiveSettings.Hair != null ? FigureFacade.Load(dataDir, device, shaderCache, controllerManager, FigureActiveSettings.Hair, parentFigure) : null;

		var clothingFigures = FigureActiveSettings.Clothing
			.Select(figureName => FigureFacade.Load(dataDir, device, shaderCache, controllerManager, figureName, parentFigure))
			.ToArray();
		
		return new FigureGroup(device, controllerManager, parentFigure, hairFigure, clothingFigures);
	}
	
	private readonly FigureFacade parentFigure;
	private readonly FigureFacade hairFigure;
	private readonly FigureFacade[] clothingFigures;
	
	private readonly FigureFacade[] childFigures;

	private readonly CoordinateNormalMatrixPairConstantBufferManager modelToWorldTransform;
	private readonly InOutStructuredBufferManager<Vector3> parentDeltas;

	public FigureGroup(Device device, ControllerManager controllerManager, FigureFacade parentFigure, FigureFacade hairFigure, FigureFacade[] clothingFigures) {
		this.parentFigure = parentFigure;
		this.hairFigure = hairFigure;
		this.clothingFigures = clothingFigures;

		childFigures = Enumerable.Repeat(hairFigure, hairFigure == null ? 0 : 1)
			.Concat(clothingFigures)
			.ToArray();

		this.modelToWorldTransform = new CoordinateNormalMatrixPairConstantBufferManager(device);
		this.parentDeltas = new InOutStructuredBufferManager<Vector3>(device, parentFigure.VertexCount);
		
		parentFigure.RegisterChildren(childFigures.ToList());
	}

	public FigureFacade Parent => parentFigure;
	public FigureFacade Hair => hairFigure;

	public IMenuLevel MenuLevel => FigureGroupMenuProvider.MakeRootMenuLevel(this);
	
	public void Dispose() {
		modelToWorldTransform.Dispose();

		parentFigure.Dispose();
		foreach (var figure in childFigures) {
			figure.Dispose();
		}

		parentDeltas.Dispose();
	}

	public void Update(DeviceContext context, float frameTime, ImageBasedLightingEnvironment lightingEnvironment) {
		modelToWorldTransform.Update(context, Matrix.Scaling(0.01f));
		
		var parentOutputs = parentFigure.UpdateFrame(null, frameTime);
		foreach (var figure in childFigures) {
			figure.UpdateFrame(parentOutputs, frameTime);
		}
		
		parentFigure.UpdateVertexPositionsAndGetDeltas(parentDeltas.OutView);
		foreach (var figure in childFigures) {
			figure.UpdateVertexPositions(parentDeltas.InView);
		}
		
		parentFigure.Update(context, lightingEnvironment);
		foreach (var figure in childFigures) {
			figure.Update(context, lightingEnvironment);
		}
	}

	public void RenderPass(DeviceContext context, RenderingPass pass) {
		context.VertexShader.SetConstantBuffer(1, modelToWorldTransform.Buffer);

		parentFigure.RenderPass(context, pass);
		foreach (var figure in childFigures) {
			figure.RenderPass(context, pass);
		}
	}
}
