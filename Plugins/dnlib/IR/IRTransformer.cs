using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;


namespace dnlib.IR {

	public class IRTransformer {
		public IRMethodBody Transform(MethodDef method) {

			Console.WriteLine($"Method: {method.FullName}");
			if (!method.HasBody) {
				return null;
			}
			var ctx = new TransformContext(method);
			return ctx.Transform();
		}
	}
}
