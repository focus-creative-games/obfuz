using dnlib.DotNet;
using System;
using System.Collections.Generic;

namespace Obfuz.Emit
{
    public interface IGroupByModuleEntity
    {
        void Init(ModuleDef mod);

        void Done();
    }

    public abstract class GroupByModuleEntityBase : IGroupByModuleEntity
    {
        public abstract void Init(ModuleDef mod);

        public abstract void Done();
    }

    public class GroupByModuleEntityManager
    {
        private readonly Dictionary<(ModuleDef, Type), IGroupByModuleEntity> _moduleEntityManagers = new Dictionary<(ModuleDef, Type), IGroupByModuleEntity>();

        public T GetEntity<T>(ModuleDef mod, Func<T> creator = null) where T : IGroupByModuleEntity
        {
            var key = (mod, typeof(T));
            if (_moduleEntityManagers.TryGetValue(key, out var emitManager))
            {
                return (T)emitManager;
            }
            else
            {
                T newEmitManager;
                if (creator != null)
                {
                    newEmitManager = creator();
                }
                else
                {
                    newEmitManager = (T)Activator.CreateInstance(typeof(T));
                }
                newEmitManager.Init(mod);
                _moduleEntityManagers[key] = newEmitManager;
                return newEmitManager;
            }
        }

        public List<T> GetEntities<T>() where T : IGroupByModuleEntity
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

        public void Done<T>() where T : IGroupByModuleEntity
        {
            var managers = GetEntities<T>();
            foreach (var manager in managers)
            {
                manager.Done();
            }
            _moduleEntityManagers.Remove((default(ModuleDef), typeof(T)));
        }

        public DefaultMetadataImporter GetDefaultModuleMetadataImporter(ModuleDef module, EncryptionScopeProvider encryptionScopeProvider)
        {
            return GetEntity<DefaultMetadataImporter>(module, () => new DefaultMetadataImporter(encryptionScopeProvider));
        }
    }
}
