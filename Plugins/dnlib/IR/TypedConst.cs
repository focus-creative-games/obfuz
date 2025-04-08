namespace dnlib.IR {
	public class TypedConst {
		public ConstType type;
		public object value;


		public static TypedConst CreateInt(int value) {
			return new TypedConst {
				type = ConstType.Int32,
				value = value,
			};
		}

		public static TypedConst CreateLong(long value) {
			return new TypedConst {
				type = ConstType.Int64,
				value = value,
			};
		}

		public static TypedConst CreateFloat(float value) {
			return new TypedConst {
				type = ConstType.Float,
				value = value,
			};
		}

		public static TypedConst CreateDouble(double value) {
			return new TypedConst {
				type = ConstType.Double,
				value = value,
			};
		}

		public static TypedConst CreateNull() {
			return new TypedConst {
				type = ConstType.Null,
				value = null,
			};
		}

		public static TypedConst CreateString(string value) {
			return new TypedConst {
				type = ConstType.String,
				value = value,
			};
		}
	}
}
