using System.Collections.Generic;
public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>
	{
		"Obfuz.Runtime.dll",
	};
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// Obfuz.EncryptionService<Obfuz.DefaultDynamicEncryptionScope>
	// }}

	public void RefMethods()
	{
	}
}