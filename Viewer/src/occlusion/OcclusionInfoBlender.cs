using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public struct OcclusionInfoBlender {
	private float frontRevealage;
	private float backRevealage;

	public void Init(OcclusionInfo occlusionInfo) {
		frontRevealage = (1 - occlusionInfo.Front);
		backRevealage = (1 - occlusionInfo.Back);
	}

	public void Add(OcclusionInfo occlusionInfo, OcclusionInfo baseOcclusionInfo) {
		if (frontRevealage != 0) {
			frontRevealage *= (1 - occlusionInfo.Front) / (1 - baseOcclusionInfo.Front);
		}
		if (backRevealage != 0) {
			backRevealage *= (1 - occlusionInfo.Back) / (1 - baseOcclusionInfo.Back); 
		}
	}

	public OcclusionInfo GetResult() {
		return new OcclusionInfo(1 - frontRevealage, 1 - backRevealage);
	}
}
