using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class GroupExportUtils {
	public static T FindExportTarget<T>(T parent, T[] children) {
		if (children.Length == 0) {
			return parent;
		} else {
			return children[children.Length - 1];
		}
	}

	public static Figure FindExportTarget(FigureGroup group) {
		return FindExportTarget(group.Parent, group.Children);
	}

	public static ChannelOutputs FindExportTarget(ChannelOutputsGroup group) {
		return FindExportTarget(group.ParentOutputs, group.ChildOutputs);
	}
}
