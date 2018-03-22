using System;

[Flags]
public enum SetMask {
	ApplyClamp = 0x1,
	VisibleOnly = 0x2,

	Any = 0,
	ApplyClampAndVisibleOnly = ApplyClamp | VisibleOnly
}
