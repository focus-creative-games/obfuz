using System.Collections.Generic;
using System.Diagnostics;


namespace dnlib.IR {
	public class EvalStack {
		private readonly List<VariableInfo> _stack = new List<VariableInfo>();


		public int Count => _stack.Count;

		public VariableInfo GetTop() {
			return _stack[_stack.Count - 1];
		}

		public VariableInfo GetTop(int index) {
			return _stack[_stack.Count - 1 - index];
		}

		public void Push(VariableInfo var) {
			_stack.Add(var);
		}

		public VariableInfo Pop() {
			var v = _stack[_stack.Count - 1];
			_stack.RemoveAt(_stack.Count - 1);
			return v;
		}

		public void Pop(int n) {
			_stack.RemoveRange(_stack.Count - n, n);
		}

		public VariableInfo[] PopWithValue(int n) {
			var vars = _stack.GetRange(_stack.Count - n, n).ToArray();
			_stack.RemoveRange(_stack.Count - n, n);
			return vars;
		}

		public void Clear() {
			_stack.Clear();
		}

		public void LoadInbound(IRBasicBlock bb) {
			_stack.Clear();
			_stack.AddRange(bb.InboundVariables);
		}

		public void SaveInbound(IRBasicBlock bb) {
			if (bb.InboundVariables.Count == 0) {
				if (_stack.Count > 0) {
					bb.SetInboundVariables(_stack);
				}
			} else {
				Debug.Assert(bb.InboundVariables.Count == _stack.Count);
			}
		}

		public void SaveOutbound(IRBasicBlock bb) {
			bb.SetOutboundVariables(_stack);
		}
	}
}
