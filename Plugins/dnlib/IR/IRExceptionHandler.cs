using dnlib.DotNet;
using dnlib.DotNet.Emit;


namespace dnlib.IR {
	public class IRExceptionHandler {

		public IRBasicBlock TryStart;

		public IRBasicBlock TryEnd;

		public IRBasicBlock FilterStart;

		public IRBasicBlock HandlerStart;

		public IRBasicBlock HandlerEnd;

		public ITypeDefOrRef CatchType;

		public ExceptionHandlerType HandlerType;
	}
}
