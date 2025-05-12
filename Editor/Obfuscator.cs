using dnlib.DotNet;
using dnlib.Protection;
using Obfuz.Data;
using Obfuz.Emit;
using Obfuz.EncryptionVM;
using Obfuz.ObfusPasses;
using Obfuz.ObfusPasses.CleanUp;
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
        private readonly AssemblyCache _assemblyCache;

        private readonly List<string> _toObfuscatedAssemblyNames;
        private readonly List<string> _notObfuscatedAssemblyNamesReferencingObfuscated;
        private readonly List<ModuleDef> _toObfuscatedModules = new List<ModuleDef>();
        private readonly List<ModuleDef> _obfuscatedAndNotObfuscatedModules = new List<ModuleDef>();

        private readonly Pipeline _pipeline = new Pipeline();
        private readonly byte[] _secret;
        private readonly int _randomSeed;
        private readonly string _encryptionVmGenerationSecret;
        private readonly int _encryptionVmOpCodeCount;
        private readonly string _encryptionVmCodeFile;

        private ObfuscationPassContext _ctx;

        public Obfuscator(ObfuscatorBuilder builder)
        {
            _secret = KeyGenerator.GenerateKey(builder.Secret, VirtualMachine.SecretKeyLength);
            SaveKey(_secret, builder.SecretOutputPath);
            _randomSeed = builder.RandomSeed;
            _encryptionVmGenerationSecret = builder.EncryptionVmGenerationSecretKey;
            _encryptionVmOpCodeCount = builder.EncryptionVmOpCodeCount;
            _encryptionVmCodeFile = builder.EncryptionVmCodeFile;

            _toObfuscatedAssemblyNames = builder.ToObfuscatedAssemblyNames;
            _notObfuscatedAssemblyNamesReferencingObfuscated = builder.NotObfuscatedAssemblyNamesReferencingObfuscated;
            _obfuscatedAssemblyOutputDir = builder.ObfuscatedAssemblyOutputDir;

            GroupByModuleEntityManager.Reset();
            _assemblyCache = new AssemblyCache(new PathAssemblyResolver(builder.AssemblySearchDirs.ToArray()));
            foreach (var pass in builder.ObfuscationPasses)
            {
                _pipeline.AddPass(pass);
            }
            _pipeline.AddPass(new CleanUpInstructionPass());
        }

        public static void SaveKey(byte[] secret, string secretOutputPath)
        {
            FileUtil.CreateParentDir(secretOutputPath);
            File.WriteAllBytes(secretOutputPath, secret);
            Debug.Log($"Save secret key to {secretOutputPath}, secret length:{secret.Length}");
        }

        public void Run()
        {
            OnPreObfuscation();
            DoObfuscation();
            OnPostObfuscation();
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
            var vms = new VirtualMachineSimulator(vm, _secret);

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

            var gvmInstance = (IEncryptor)Activator.CreateInstance(generatedVmTypes[0], new object[] { _secret } );

            int testValue = 11223344;
            for (int i = 0; i < vm.opCodes.Length; i++)
            {
                int encryptedValueOfVms = vms.Encrypt(testValue, i, i);
                int decryptedValueOfVms = vms.Decrypt(encryptedValueOfVms, i, i);
                if (decryptedValueOfVms != testValue)
                {
                    throw new Exception($"VirtualMachineSimulator decrypt failed! opCode:{i}, originalValue:{testValue} decryptedValue:{decryptedValueOfVms}");
                }
                int encryptedValueOfGvm = gvmInstance.Encrypt(testValue, i, i);
                int decryptedValueOfGvm = gvmInstance.Decrypt(encryptedValueOfGvm, i, i);
                if (encryptedValueOfGvm != encryptedValueOfVms)
                {
                    throw new Exception($"encryptedValue not match! opCode:{i}, originalValue:{testValue} encryptedValue VirtualMachineSimulator:{encryptedValueOfVms} GeneratedEncryptionVirtualMachine:{encryptedValueOfGvm}");
                }
                if (decryptedValueOfGvm != testValue)
                {
                    throw new Exception($"GeneratedEncryptionVirtualMachine decrypt failed! opCode:{i}, originalValue:{testValue} decryptedValue:{decryptedValueOfGvm}");
                }
            }

            return vms;
        }

        private void OnPreObfuscation()
        {
            LoadAssemblies();


            var random = new RandomWithKey(_secret, _randomSeed);
            var encryptor = CreateEncryptionVirtualMachine();
            var rvaDataAllocator = new RvaDataAllocator(random, encryptor);
            var constFieldAllocator = new ConstFieldAllocator(encryptor, random, rvaDataAllocator);
            _ctx = new ObfuscationPassContext
            {
                assemblyCache = _assemblyCache,
                toObfuscatedModules = _toObfuscatedModules,
                obfuscatedAndNotObfuscatedModules = _obfuscatedAndNotObfuscatedModules,
                toObfuscatedAssemblyNames = _toObfuscatedAssemblyNames,
                notObfuscatedAssemblyNamesReferencingObfuscated = _notObfuscatedAssemblyNamesReferencingObfuscated,
                obfuscatedAssemblyOutputDir = _obfuscatedAssemblyOutputDir,

                random = random,
                encryptor = encryptor,
                rvaDataAllocator = rvaDataAllocator,
                constFieldAllocator = constFieldAllocator,
            };
            _pipeline.Start(_ctx);
        }

        private void LoadAssemblies()
        {
            foreach (string assName in _toObfuscatedAssemblyNames.Concat(_notObfuscatedAssemblyNamesReferencingObfuscated))
            {
                ModuleDefMD mod = _assemblyCache.TryLoadModule(assName);
                if (mod == null)
                {
                    Debug.Log($"assembly: {assName} not found! ignore.");
                    continue;
                }
                if (_toObfuscatedAssemblyNames.Contains(assName))
                {
                    _toObfuscatedModules.Add(mod);
                }
                _obfuscatedAndNotObfuscatedModules.Add(mod);
            }
        }

        private void DoObfuscation()
        {
            FileUtil.RecreateDir(_obfuscatedAssemblyOutputDir);

            _pipeline.Run(_ctx);
        }

        private void OnPostObfuscation()
        {
            _pipeline.Stop(_ctx);

            foreach (ModuleDef mod in _obfuscatedAndNotObfuscatedModules)
            {
                string assNameWithExt = mod.Name;
                string outputFile = $"{_obfuscatedAssemblyOutputDir}/{assNameWithExt}";
                mod.Write(outputFile);
                Debug.Log($"save module. name:{mod.Assembly.Name} output:{outputFile}");
            }
        }
    }
}
