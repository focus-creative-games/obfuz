using System;


namespace dnlib.IR {

	[Flags]
	public enum ArgumentMetaFlag {
		None = 0x0,
		In = 0x1,
		Out = 0x2,
		InOut = In | Out,
		LoadAddress = 0x4,
		Variadic = 0x8,
		Constant = 0x10,
		LoadIndirect = 0x20,
		StoreIndirect = 0x40,
	}
}
