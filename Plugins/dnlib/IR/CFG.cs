using System.Collections.Generic;
using System.Linq;

namespace dnlib.IR {
	class CFG {

		private readonly List<IRBasicBlock> _bbs;
		private readonly List<IRExceptionHandler> _exHandlers;

		public CFG(List<IRBasicBlock> bbs, List<IRExceptionHandler> exHandlers) {
			_bbs = bbs;
			_exHandlers = exHandlers;
		}





		private void BasicBlockForwardDFS(IRBasicBlock bb, Dictionary<IRBasicBlock, StronglyConnectedComponents> bb2scc, HashSet<IRBasicBlock> visited, Stack<IRBasicBlock> stack) {
			if (!bb2scc.TryGetValue(bb, out var scc)) {
				scc = new StronglyConnectedComponents();
				scc.AddBlock(bb);
				bb2scc.Add(bb, scc);
			}

			// existing cycle
			if (!visited.Add(bb)) {
				foreach (var prevBb in stack) {
					if (prevBb == bb) {
						break;
					}
					scc.Merge(bb2scc[prevBb], bb2scc);
				}
				return;
			}

			stack.Push(bb);
			foreach (var next in bb.OutboundBasicBlocks) {
				BasicBlockForwardDFS(next, bb2scc, visited, stack);
			}
			stack.Pop();
		}

		private void ComputeSccFowardFlows(Dictionary<IRBasicBlock, StronglyConnectedComponents> bb2scc) {
			var csss = new HashSet<StronglyConnectedComponents>();
			foreach (var sc in bb2scc.Values) {
				if (csss.Add(sc)) {
					sc.InitOutboundSccs(bb2scc);
				}
			}
		}

		private readonly Dictionary<IRBasicBlock, StronglyConnectedComponents>  _bb2scc = new Dictionary<IRBasicBlock, StronglyConnectedComponents>();

		public StronglyConnectedComponents GetScc(IRBasicBlock bb) {
			return _bb2scc[bb];
		}

		public void ComputeControlFlows() {
			var visited = new HashSet<IRBasicBlock>();
			var stack = new Stack<IRBasicBlock>();
			BasicBlockForwardDFS(_bbs[0], _bb2scc, visited, stack);

			foreach (var ex in _exHandlers) {
				//if (ex.TryStart != null) {
				//	BasicBlockForwardDFS(ex.TryStart, bb2scc, visited, stack);
				//}
				if (ex.HandlerStart != null) {
					BasicBlockForwardDFS(ex.HandlerStart, _bb2scc, visited, stack);
				}
				if (ex.FilterStart != null) {
					BasicBlockForwardDFS(ex.FilterStart, _bb2scc, visited, stack);
				}
			}

			ComputeSccFowardFlows(_bb2scc);
		}

		

		// 		要计算多个基本块（BasicBlocks）的共同最近的前驱基本块（即最近公共祖先，LCA，Lowest Common Ancestor），你可以使用以下方法。这里假设你有一个控制流图（CFG），其中每个基本块是一个节点，边代表控制流。

	}
}
