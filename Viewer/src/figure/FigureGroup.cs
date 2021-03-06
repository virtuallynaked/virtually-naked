using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Linq;

class FigureGroup : IDisposable {
	private readonly FigureFacade parentFigure;
	private FigureFacade[] childFigures;

	private readonly CoordinateNormalMatrixPairConstantBufferManager modelToWorldTransform;
	private readonly InOutStructuredBufferManager<Vector3> parentDeltas;

	public FigureGroup(Device device, FigureFacade parentFigure, FigureFacade[] childFigures) {
		this.parentFigure = parentFigure;
		this.childFigures = childFigures;

		this.modelToWorldTransform = new CoordinateNormalMatrixPairConstantBufferManager(device);
		this.parentDeltas = new InOutStructuredBufferManager<Vector3>(device, parentFigure.VertexCount);
		
		parentFigure.RegisterChildren(childFigures.ToList());
	}

	public FigureFacade Parent => parentFigure;
	public FigureFacade[] Children => childFigures;
		
	public void Dispose() {
		modelToWorldTransform.Dispose();
		parentDeltas.Dispose();
		//don't dispose figures because they are not owned by the figure group
	}

	public void SetChildFigures(FigureFacade[] childFigures) {
		this.childFigures = childFigures;
		parentFigure.RegisterChildren(childFigures.ToList());
	}

	public void Update(DeviceContext context, FrameUpdateParameters updateParameters, ImageBasedLightingEnvironment lightingEnvironment) {
		modelToWorldTransform.Update(context, Matrix.Scaling(0.01f));
		
		foreach (var figure in childFigures) {
			figure.SyncWithModel();
		}
		parentFigure.SyncWithModel();

		var parentOutputs = parentFigure.UpdateFrame(context, updateParameters, null);
		foreach (var figure in childFigures) {
			figure.UpdateFrame(context, updateParameters, parentOutputs);
		}
		
		parentFigure.UpdateVertexPositionsAndGetDeltas(context, parentDeltas.OutView);
		foreach (var figure in childFigures) {
			figure.UpdateVertexPositions(context, parentDeltas.InView);
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

	public void DoPostwork(DeviceContext context) {
		parentFigure.ReadbackPosedControlVertices(context);
	}
}
