using System;


namespace dnlib.IR {
	//public class VariableArgumentData : InstructionArgumentData {
	//	public VariableLocation location;
	//	public int index;
	//}

	[Flags]
	public enum ArgumentFlag {
		None = 0x0,
		Volatible = 0x1,
	}
}
