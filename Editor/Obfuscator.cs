using dnlib.DotNet;
using dnlib.Protection;
using Obfuz.Data;
using Obfuz.Emit;
using Obfuz.EncryptionVM;
using Obfuz.ObfusPasses;
using Obfuz.ObfusPasses.CleanUp;
using Obfuz.ObfusPasses.SymbolObfus;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEditor.ObjectChangeEventStream;

namespace Obfuz
{

    public class Obfuscator
    {
        private readonly string _obfuscatedAssemblyOutputDir;

        private readonly List<string> _toObfuscatedAssemblyNames;
        private readonly List<string> _notObfuscatedAssemblyNamesReferencingObfuscated;
        private readonly List<string> _assemblySearchDirs;

        private readonly ConfigurablePassPolicy _passPolicy;

        private readonly Pipeline _pipeline1 = new Pipeline();
        private readonly Pipeline _pipeline2 = new Pipeline();
        private readonly byte[] _byteSecret;
        private readonly int[] _intSecret;
        private readonly int _randomSeed;
        private readonly string _encryptionVmGenerationSecret;
        private readonly int _encryptionVmOpCodeCount;
        private readonly string _encryptionVmCodeFile;

        private ObfuscationPassContext _ctx;

        public Obfuscator(ObfuscatorBuilder builder)
        {
            _byteSecret = KeyGenerator.GenerateKey(builder.Secret, VirtualMachine.SecretKeyLength);
            _intSecret = KeyGenerator.ConvertToIntKey(_byteSecret);
            SaveKey(_byteSecret, builder.SecretOutputPath);
            _randomSeed = builder.RandomSeed;
            _encryptionVmGenerationSecret = builder.EncryptionVmGenerationSecretKey;
            _encryptionVmOpCodeCount = builder.EncryptionVmOpCodeCount;
            _encryptionVmCodeFile = builder.EncryptionVmCodeFile;

            _toObfuscatedAssemblyNames = builder.ToObfuscatedAssemblyNames;
            _notObfuscatedAssemblyNamesReferencingObfuscated = builder.NotObfuscatedAssemblyNamesReferencingObfuscated;
            _obfuscatedAssemblyOutputDir = builder.ObfuscatedAssemblyOutputDir;
            _assemblySearchDirs = builder.AssemblySearchDirs;

            _passPolicy = new ConfigurablePassPolicy(_toObfuscatedAssemblyNames, builder.EnableObfuscationPasses, builder.ObfuscationPassConfigFiles);

            foreach (var pass in builder.ObfuscationPasses)
            {
                if (pass is SymbolObfusPass symbolObfusPass)
                {
                    _pipeline2.AddPass(pass);
                }
                else
                {
                    _pipeline1.AddPass(pass);
                }
            }
            _pipeline1.AddPass(new CleanUpInstructionPass());
            _pipeline2.AddPass(new RemoveObfuzAttributesPass());
        }

        public static void SaveKey(byte[] secret, string secretOutputPath)
        {
            FileUtil.CreateParentDir(secretOutputPath);
            File.WriteAllBytes(secretOutputPath, secret);
            Debug.Log($"Save secret key to {secretOutputPath}, secret length:{secret.Length}");
        }

        public void Run()
        {
            FileUtil.RecreateDir(_obfuscatedAssemblyOutputDir);
            RunPipeline(_pipeline1);
            _assemblySearchDirs.Insert(0, _obfuscatedAssemblyOutputDir);
            RunPipeline(_pipeline2);
        }

        private void RunPipeline(Pipeline pipeline)
        {
            if (pipeline.Empty)
            {
                return;
            }
            OnPreObfuscation(pipeline);
            DoObfuscation(pipeline);
            OnPostObfuscation(pipeline);
        }

        private IEncryptor CreateEncryptionVirtualMachine()
        {
            var vmCreator = new VirtualMachineCreator(_encryptionVmGenerationSecret);
            var vm = vmCreator.CreateVirtualMachine(_encryptionVmOpCodeCount);
            var vmGenerator = new VirtualMachineCodeGenerator(vm);

            if (!File.Exists(_encryptionVmCodeFile))
            {
                throw new Exception($"EncryptionVm CodeFile:`{_encryptionVmCodeFile}` not exists! Please run `Obfuz/GenerateVm` to generate it!");
            }
            if (!vmGenerator.ValidateMatch(_encryptionVmCodeFile))
            {
                throw new Exception($"EncryptionVm CodeFile:`{_encryptionVmCodeFile}` not match with encryptionVM settings! Please run `Obfuz/GenerateVm` to update it!");
            }
            var vms = new VirtualMachineSimulator(vm, _byteSecret);

            var generatedVmTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("Obfuz.EncryptionVM.GeneratedEncryptionVirtualMachine"))
                .Where(type => type != null)
                .ToList();
            if (generatedVmTypes.Count == 0)
            {
                throw new Exception($"class Obfuz.EncryptionVM.GeneratedEncryptionVirtualMachine not found in any assembly! Please run `Obfuz/GenerateVm` to generate it!");
            }
            if (generatedVmTypes.Count > 1)
            {
                throw new Exception($"class Obfuz.EncryptionVM.GeneratedEncryptionVirtualMachine found in multiple assemblies! Please retain only one!");
            }

            var gvmInstance = (IEncryptor)Activator.CreateInstance(generatedVmTypes[0], new object[] { _byteSecret } );

            VerifyVm(vm, vms, gvmInstance);

            return vms;
        }

        private void VerifyVm(VirtualMachine vm, VirtualMachineSimulator vms, IEncryptor gvmInstance)
        {
            int testInt = 11223344;
            long testLong = 1122334455667788L;
            float testFloat = 1234f;
            double testDouble = 1122334455.0;
            string testString = "hello,world";
            for (int i = 0; i < vm.opCodes.Length; i++)
            {
                int ops = i * vm.opCodes.Length + i;
                //int salt = i;
                //int ops = -1135538782;
                int salt = -879409147;
                {
                    int encryptedIntOfVms = vms.Encrypt(testInt, ops, salt);
                    int decryptedIntOfVms = vms.Decrypt(encryptedIntOfVms, ops, salt);
                    if (decryptedIntOfVms != testInt)
                    {
                        throw new Exception($"VirtualMachineSimulator decrypt failed! opCode:{i}, originalValue:{testInt} decryptedValue:{decryptedIntOfVms}");
                    }
                    int encryptedValueOfGvm = gvmInstance.Encrypt(testInt, ops, salt);
                    int decryptedValueOfGvm = gvmInstance.Decrypt(encryptedValueOfGvm, ops, salt);
                    if (encryptedValueOfGvm != encryptedIntOfVms)
                    {
                        throw new Exception($"encryptedValue not match! opCode:{i}, originalValue:{testInt} encryptedValue VirtualMachineSimulator:{encryptedIntOfVms} GeneratedEncryptionVirtualMachine:{encryptedValueOfGvm}");
                    }
                    if (decryptedValueOfGvm != testInt)
                    {
                        throw new Exception($"GeneratedEncryptionVirtualMachine decrypt failed! opCode:{i}, originalValue:{testInt} decryptedValue:{decryptedValueOfGvm}");
                    }
                }
                {
                    long encryptedLongOfVms = vms.Encrypt(testLong, ops, salt);
                    long decryptedLongOfVms = vms.Decrypt(encryptedLongOfVms, ops, salt);
                    if (decryptedLongOfVms != testLong)
                    {
                        throw new Exception($"VirtualMachineSimulator decrypt long failed! opCode:{i}, originalValue:{testLong} decryptedValue:{decryptedLongOfVms}");
                    }
                    long encryptedValueOfGvm = gvmInstance.Encrypt(testLong, ops, salt);
                    long decryptedValueOfGvm = gvmInstance.Decrypt(encryptedValueOfGvm, ops, salt);
                    if (encryptedValueOfGvm != encryptedLongOfVms)
                    {
                        throw new Exception($"encryptedValue not match! opCode:{i}, originalValue:{testLong} encryptedValue VirtualMachineSimulator:{encryptedLongOfVms} GeneratedEncryptionVirtualMachine:{encryptedValueOfGvm}");
                    }
                    if (decryptedValueOfGvm != testLong)
                    {
                        throw new Exception($"GeneratedEncryptionVirtualMachine decrypt long failed! opCode:{i}, originalValue:{testLong} decryptedValue:{decryptedValueOfGvm}");
                    }
                }
                {
                    float encryptedFloatOfVms = vms.Encrypt(testFloat, ops, salt);
                    float decryptedFloatOfVms = vms.Decrypt(encryptedFloatOfVms, ops, salt);
                    if (decryptedFloatOfVms != testFloat)
                    {
                        throw new Exception("encryptedFloat not match");
                    }
                    float encryptedValueOfGvm = gvmInstance.Encrypt(testFloat, ops, salt);
                    float decryptedValueOfGvm = gvmInstance.Decrypt(encryptedFloatOfVms, ops, salt);
                    if (encryptedFloatOfVms != encryptedValueOfGvm)
                    {
                        throw new Exception($"encryptedValue not match! opCode:{i}, originalValue:{testFloat} encryptedValue");
                    }
                    if (decryptedValueOfGvm != testFloat)
                    {
                        throw new Exception($"GeneratedEncryptionVirtualMachine decrypt float failed! opCode:{i}, originalValue:{testFloat}");
                    }
                }
                {
                    double encryptedFloatOfVms = vms.Encrypt(testDouble, ops, salt);
                    double decryptedFloatOfVms = vms.Decrypt(encryptedFloatOfVms, ops, salt);
                    if (decryptedFloatOfVms != testDouble)
                    {
                        throw new Exception("encryptedFloat not match");
                    }
                    double encryptedValueOfGvm = gvmInstance.Encrypt(testDouble, ops, salt);
                    double decryptedValueOfGvm = gvmInstance.Decrypt(encryptedFloatOfVms, ops, salt);
                    if (encryptedFloatOfVms != encryptedValueOfGvm)
                    {
                        throw new Exception($"encryptedValue not match! opCode:{i}, originalValue:{testDouble} encryptedValue");
                    }
                    if (decryptedValueOfGvm != testDouble)
                    {
                        throw new Exception($"GeneratedEncryptionVirtualMachine decrypt float failed! opCode:{i}, originalValue:{testDouble}");
                    }
                }

                {
                    byte[] encryptedStrOfVms = vms.Encrypt(testString, ops, salt);
                    string descryptedStrOfVms = vms.DecryptString(encryptedStrOfVms, 0, encryptedStrOfVms.Length, ops, salt);
                    if (descryptedStrOfVms != testString)
                    {
                        throw new Exception($"VirtualMachineSimulator decrypt string failed! opCode:{i}, originalValue:{testString} decryptedValue:{descryptedStrOfVms}");
                    }
                    byte[] encryptedStrOfGvm = gvmInstance.Encrypt(testString, ops, salt);
                    string descryptedStrOfGvm = gvmInstance.DecryptString(encryptedStrOfGvm, 0, encryptedStrOfGvm.Length, ops, salt);
                    if (!encryptedStrOfGvm.SequenceEqual(encryptedStrOfVms))
                    {
                        throw new Exception($"encryptedValue not match! opCode:{i}, originalValue:{testString} encryptedValue VirtualMachineSimulator:{encryptedStrOfVms} GeneratedEncryptionVirtualMachine:{encryptedStrOfGvm}");
                    }
                    if (descryptedStrOfGvm != testString)
                    {
                        throw new Exception($"GeneratedEncryptionVirtualMachine decrypt string failed! opCode:{i}, originalValue:{testString} decryptedValue:{descryptedStrOfGvm}");
                    }
                }
            }
        }

        private void OnPreObfuscation(Pipeline pipeline)
        {
            AssemblyCache assemblyCache = new AssemblyCache(new PathAssemblyResolver(_assemblySearchDirs.ToArray()));
            List<ModuleDef> toObfuscatedModules = new List<ModuleDef>();
            List<ModuleDef> obfuscatedAndNotObfuscatedModules = new List<ModuleDef>();
            LoadAssemblies(assemblyCache, toObfuscatedModules, obfuscatedAndNotObfuscatedModules);

            var random = new RandomWithKey(_intSecret, _randomSeed);
            RandomCreator localRandomCreator = (seed) => new RandomWithKey(_intSecret, _randomSeed ^ seed);
            var encryptor = CreateEncryptionVirtualMachine();
            var moduleEntityManager = new GroupByModuleEntityManager();
            var rvaDataAllocator = new RvaDataAllocator(random, encryptor, moduleEntityManager);
            var constFieldAllocator = new ConstFieldAllocator(encryptor, localRandomCreator, rvaDataAllocator, moduleEntityManager);
            _ctx = new ObfuscationPassContext
            {
                assemblyCache = assemblyCache,
                toObfuscatedModules = toObfuscatedModules,
                obfuscatedAndNotObfuscatedModules = obfuscatedAndNotObfuscatedModules,
                toObfuscatedAssemblyNames = _toObfuscatedAssemblyNames,
                notObfuscatedAssemblyNamesReferencingObfuscated = _notObfuscatedAssemblyNamesReferencingObfuscated,
                obfuscatedAssemblyOutputDir = _obfuscatedAssemblyOutputDir,
                moduleEntityManager = moduleEntityManager,

                globalRandom = random,
                localRandomCreator = localRandomCreator,
                encryptor = encryptor,
                rvaDataAllocator = rvaDataAllocator,
                constFieldAllocator = constFieldAllocator,
                whiteList = new NotObfuscatedMethodWhiteList(),
                passPolicy = _passPolicy,
            };
            ObfuscationPassContext.Current = _ctx;
            pipeline.Start();
        }

        private void LoadAssemblies(AssemblyCache assemblyCache, List<ModuleDef> toObfuscatedModules, List<ModuleDef> obfuscatedAndNotObfuscatedModules)
        {
            foreach (string assName in _toObfuscatedAssemblyNames.Concat(_notObfuscatedAssemblyNamesReferencingObfuscated))
            {
                ModuleDefMD mod = assemblyCache.TryLoadModule(assName);
                if (mod == null)
                {
                    Debug.Log($"assembly: {assName} not found! ignore.");
                    continue;
                }
                if (_toObfuscatedAssemblyNames.Contains(assName))
                {
                    toObfuscatedModules.Add(mod);
                }
                obfuscatedAndNotObfuscatedModules.Add(mod);
            }
        }

        private void WriteAssemblies()
        {
            foreach (ModuleDef mod in _ctx.obfuscatedAndNotObfuscatedModules)
            {
                string assNameWithExt = mod.Name;
                string outputFile = $"{_obfuscatedAssemblyOutputDir}/{assNameWithExt}";
                mod.Write(outputFile);
                Debug.Log($"save module. name:{mod.Assembly.Name} output:{outputFile}");
            }
        }

        private void DoObfuscation(Pipeline pipeline)
        {
            pipeline.Run();
        }

        private void OnPostObfuscation(Pipeline pipeline)
        {
            pipeline.Stop();

            _ctx.constFieldAllocator.Done();
            _ctx.rvaDataAllocator.Done();
            WriteAssemblies();
        }
    }
}
