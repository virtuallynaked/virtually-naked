using System.Collections.Generic;

public class MultiUvTextureMask {
	private readonly Dictionary<UvSet, TextureMask> masksByUvSet = new Dictionary<UvSet, TextureMask>();

	public IEnumerable<TextureMask> PerUvMasks => masksByUvSet.Values;

	public void Merge(TextureMask mask) {
		if (masksByUvSet.TryGetValue(mask.UvSet, out var existingMask)) {
			existingMask.Merge(mask);
		} else {
			masksByUvSet.Add(mask.UvSet, mask);
		}
	}
}
