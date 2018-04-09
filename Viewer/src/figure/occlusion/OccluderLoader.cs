using System;
using Device = SharpDX.Direct3D11.Device;

public class OccluderLoader {
	private readonly Device device;
	private readonly ShaderCache shaderCache;
	private readonly ChannelSystem channelSystem;
	private readonly IArchiveDirectory unmorphedOcclusionDirectory;
	
	public OccluderLoader(Device device, ShaderCache shaderCache, FigureDefinition figureDefinition) {
		this.device = device;
		this.shaderCache = shaderCache;
		channelSystem = figureDefinition.ChannelSystem;
		unmorphedOcclusionDirectory = figureDefinition.Directory.Subdirectory("occlusion");
	}

	public IArchiveDirectory DefaultDirectory => unmorphedOcclusionDirectory;

	public IOccluder Load(IArchiveDirectory occlusionDirectory) {
		bool isMainFigure = channelSystem.Parent == null;

		if (isMainFigure) {
			IArchiveFile occluderParametersFile = occlusionDirectory.File("occluder-parameters.dat");
			if (occluderParametersFile == null) {
				throw new InvalidOperationException("expected main figure to have occlusion system");
			}
			
			var occluderParameters = Persistance.Load<OccluderParameters>(occluderParametersFile);
			OcclusionInfo[] unmorphedOcclusionInfos = OcclusionInfo.UnpackArray(unmorphedOcclusionDirectory.File("occlusion-infos.array").ReadArray<uint>());
			var occluder = new DeformableOccluder(device, shaderCache, channelSystem, unmorphedOcclusionInfos, occluderParameters);
			return occluder;
		} else {
			OcclusionInfo[] figureOcclusionInfos = OcclusionInfo.UnpackArray(occlusionDirectory.File("occlusion-infos.array").ReadArray<uint>());
			OcclusionInfo[] parentOcclusionInfos = OcclusionInfo.UnpackArray(occlusionDirectory.File("parent-occlusion-infos.array").ReadArray<uint>());
			var occluder = new StaticOccluder(device, figureOcclusionInfos, parentOcclusionInfos);
			return occluder;
		}
	}
}
