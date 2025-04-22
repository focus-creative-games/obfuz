using dnlib.DotNet;


namespace dnlib.IR {
	public class VariableInfo {
		public VariableLocation location;
		public int id;
		public int paramOrLocalIndex;
		public string variableName;
		public object paramOrLocalOrConst;
		public TypeSig type;
		public bool expandToInt;
	}
}
