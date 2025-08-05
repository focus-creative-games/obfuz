using dnlib.DotNet;
using System;
using System.Collections.Generic;

namespace Obfuz.Emit
{
    public interface IGroupByModuleEntity
    {
        GroupByModuleEntityManager Manager { get; set; }

        ModuleDef Module { get; set; }

        EncryptionScopeProvider EncryptionScopeProvider { get; }

        EncryptionScopeInfo EncryptionScope { get; set; }

        void Init();

        void Done();
    }

    public abstract class GroupByModuleEntityBase : IGroupByModuleEntity
    {
        public GroupByModuleEntityManager Manager { get; set; }

        public ModuleDef Module { get; set; }

        public EncryptionScopeInfo EncryptionScope { get; set; }

        public EncryptionScopeProvider EncryptionScopeProvider => Manager.EncryptionScopeProvider;

        public T GetEntity<T>() where T : IGroupByModuleEntity, new()
        {
            return Manager.GetEntity<T>(Module);
        }

        public abstract void Init();

        public abstract void Done();
    }

    public class GroupByModuleEntityManager
    {
        private readonly Dictionary<(ModuleDef, Type), IGroupByModuleEntity> _moduleEntityManagers = new Dictionary<(ModuleDef, Type), IGroupByModuleEntity>();

        public EncryptionScopeProvider EncryptionScopeProvider { get; set; }

        public T GetEntity<T>(ModuleDef mod) where T : IGroupByModuleEntity, new()
        {
            var key = (mod, typeof(T));
            if (_moduleEntityManagers.TryGetValue(key, out var emitManager))
            {
                return (T)emitManager;
            }
            else
            {
                T newEmitManager = new T();
                newEmitManager.Manager = this;
                newEmitManager.Module = mod;
                newEmitManager.EncryptionScope = EncryptionScopeProvider.GetScope(mod);
                newEmitManager.Init();
                _moduleEntityManagers[key] = newEmitManager;
                return newEmitManager;
            }
        }

        public List<T> GetEntities<T>() where T : IGroupByModuleEntity, new()
        {
            var managers = new List<T>();
            foreach (var kv in _moduleEntityManagers)
            {
                if (kv.Key.Item2 == typeof(T))
                {
                    managers.Add((T)kv.Value);
                }
            }
            return managers;
        }

        public void Done<T>() where T : IGroupByModuleEntity, new()
        {
            var managers = GetEntities<T>();
            foreach (var manager in managers)
            {
                manager.Done();
            }
        }
    }
}
