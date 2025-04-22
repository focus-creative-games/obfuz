using System.Collections.Generic;
using System.Diagnostics;


namespace dnlib.IR {
	class StronglyConnectedComponents {
		private readonly HashSet<IRBasicBlock> _bbs = new HashSet<IRBasicBlock>();

		private StronglyConnectedComponents _mergeTo;

		private readonly List<StronglyConnectedComponents> _inboundSccs = new List<StronglyConnectedComponents>();
		private readonly List<StronglyConnectedComponents> _outboundSccs = new List<StronglyConnectedComponents>();

		public void AddBlock(IRBasicBlock bb) {
			_bbs.Add(bb);
		}

		public bool Contains(IRBasicBlock bb) {
			return _bbs.Contains(bb);
		}

		public void Merge(StronglyConnectedComponents other, Dictionary<IRBasicBlock, StronglyConnectedComponents> bb2scc) {
			if (other._mergeTo != null) {
				Debug.Assert(other._mergeTo == this);
				return;
			}
			other._mergeTo = this;
			foreach (var bb in other._bbs) {
				if (_bbs.Add(bb)) {
					bb2scc[bb] = this;
				}
			}
		}

		public void InitOutboundSccs(Dictionary<IRBasicBlock, StronglyConnectedComponents> bb2scc) {
			var outboundSccs = new HashSet<StronglyConnectedComponents>();
			foreach (var bb in _bbs) {
				foreach (var next in bb.OutboundBasicBlocks) {
					StronglyConnectedComponents nextScc = bb2scc[next];
					if (nextScc != this) {
						outboundSccs.Add(nextScc);
					}
				}
			}
			foreach (var scc in outboundSccs) {
				_outboundSccs.Add(scc);
				scc._inboundSccs.Add(this);
			}
		}

		public void AddOutboundScc(StronglyConnectedComponents scc) {
			if (_outboundSccs.Contains(scc)) {
				Debug.Assert(scc._inboundSccs.Contains(this));
				return;
			}
			_outboundSccs.Add(scc);
			scc._inboundSccs.Add(this);
		}
	}
}
