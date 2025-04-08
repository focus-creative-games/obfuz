namespace dnlib.Protection {
	public class MyRandom {
		private int _seed;

		public MyRandom(int seed) {
			_seed = seed;
		}

		public int Next() {
			// linear congruential generator
			_seed = 214013 * _seed + 2531011;
			return _seed;
		}

		public uint NextU() {
			return (uint)Next();
		}

		public int Next(int maxValue) {
			return (int)((uint)Next() % (uint)maxValue);
		}
	}
}
