using dnlib.DotNet;
using Obfuz.Rename;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;

namespace Obfuz
{

    public class RenameRecordMap
    {
        private enum RenameStatus
        {
            NotRenamed,
            Renamed,
        }

        private class RenameRecord
        {
            public RenameStatus status;
            public string signature;
            public string oldName;
            public string newName;
            public object renameMappingData;
        }

        private class RenameMappingField
        {
            public RenameStatus status;
            public string signature;
            public string newName;
        }

        private class RenameMappingMethod
        {
            public RenameStatus status;
            public string signature;
            public string newName;

            public List<RenameMappingMethodParam> parameters = new List<RenameMappingMethodParam>();
        }

        private class RenameMappingMethodParam
        {
            public RenameStatus status;
            public int index;
            public string newName;
        }

        private class RenameMappingProperty
        {
            public RenameStatus status;
            public string signature;
            public string newName;
        }

        private class RenameMappingEvent
        {
            public RenameStatus status;
            public string signature;
            public string newName;
        }

        private class RenameMappingType
        {
            public RenameStatus status;
            public string oldFullName;
            public string newFullName;

            public Dictionary<string, RenameMappingField> fields = new Dictionary<string, RenameMappingField>();
            public Dictionary<string, RenameMappingMethod> methods = new Dictionary<string, RenameMappingMethod>();
            public Dictionary<string, RenameMappingProperty> properties = new Dictionary<string, RenameMappingProperty>();
            public Dictionary<string, RenameMappingEvent> events = new Dictionary<string, RenameMappingEvent>();
        }

        private class RenameMappingAssembly
        {
            public RenameStatus status;
            public string oldAssName;
            public string newAssName;

            public Dictionary<string, RenameMappingType> types = new Dictionary<string, RenameMappingType>();
        }

        private readonly string _mappingFile;
        private readonly Dictionary<string, RenameMappingAssembly> _assemblies = new Dictionary<string, RenameMappingAssembly>();


        private readonly Dictionary<ModuleDefMD, RenameRecord> _modRenames = new Dictionary<ModuleDefMD, RenameRecord>();
        private readonly Dictionary<TypeDef, RenameRecord> _typeRenames = new Dictionary<TypeDef, RenameRecord>();
        private readonly Dictionary<MethodDef, RenameRecord> _methodRenames = new Dictionary<MethodDef, RenameRecord>();
        private readonly Dictionary<ParamDef, RenameRecord> _paramRenames = new Dictionary<ParamDef, RenameRecord>();
        private readonly Dictionary<FieldDef, RenameRecord> _fieldRenames = new Dictionary<FieldDef, RenameRecord>();
        private readonly Dictionary<PropertyDef, RenameRecord> _propertyRenames = new Dictionary<PropertyDef, RenameRecord>();
        private readonly Dictionary<EventDef, RenameRecord> _eventRenames = new Dictionary<EventDef, RenameRecord>();
        private readonly Dictionary<VirtualMethodGroup, RenameRecord> _virtualMethodGroups = new Dictionary<VirtualMethodGroup, RenameRecord>();


        public RenameRecordMap(string mappingFile)
        {
            _mappingFile = mappingFile;
        }

        public void Init(List<ObfuzAssemblyInfo> assemblies, INameMaker nameMaker)
        {
            LoadXmlMappingFile(_mappingFile);
            foreach (var ObfuzAssemblyInfo in assemblies)
            {
                ModuleDefMD mod = ObfuzAssemblyInfo.module;
                string name = mod.Assembly.Name;
                nameMaker.AddPreservedName(mod, name);

                RenameMappingAssembly rma = _assemblies.GetValueOrDefault(name);
                if (rma != null && rma.status == RenameStatus.Renamed)
                {
                    nameMaker.AddPreservedName(mod, rma.newAssName);
                }

                _modRenames.Add(mod, new RenameRecord
                {
                    status = RenameStatus.NotRenamed,
                    signature = name,
                    oldName = name,
                    newName = null,
                    renameMappingData = rma,
                });

                foreach (TypeDef type in mod.GetTypes())
                {
                    nameMaker.AddPreservedName(type, name);
                    nameMaker.AddPreservedNamespace(type, type.Namespace);
                    string fullTypeName = type.FullName;
                    RenameMappingType rmt = rma?.types.GetValueOrDefault(fullTypeName);
                    if (rmt != null)
                    {
                        var (newNamespace, newName) = MetaUtil.SplitNamespaceAndName(rmt.newFullName);
                        nameMaker.AddPreservedNamespace(type, newNamespace);
                        nameMaker.AddPreservedName(type, newName);
                    }

                    _typeRenames.Add(type, new RenameRecord
                    {
                        status = RenameStatus.NotRenamed,
                        signature = fullTypeName,
                        oldName = fullTypeName,
                        newName = null,
                        renameMappingData = rmt,
                    });
                    foreach (MethodDef method in type.Methods)
                    {
                        nameMaker.AddPreservedName(method, method.Name);
                        string methodSig = TypeSigUtil.ComputeMethodDefSignature(method);
                        nameMaker.AddPreservedName(method, method.Name);

                        RenameMappingMethod rmm = rmt?.methods.GetValueOrDefault(methodSig);
                        if (rmm != null)
                        {
                            nameMaker.AddPreservedName(method, rmm.newName);
                        }
                        _methodRenames.Add(method, new RenameRecord
                        {
                            status = RenameStatus.NotRenamed,
                            signature = methodSig,
                            oldName = method.Name,
                            newName = null,
                            renameMappingData = rmm,
                        });
                        foreach (Parameter param in method.Parameters)
                        {
                            if (param.ParamDef != null)
                            {
                                _paramRenames.Add(param.ParamDef, new RenameRecord
                                {
                                    status = RenameStatus.NotRenamed,
                                    signature = param.Name,
                                    oldName = param.Name,
                                    newName = null,
                                });
                            }
                        }
                    }
                    foreach (FieldDef field in type.Fields)
                    {
                        nameMaker.AddPreservedName(field, field.Name);
                        string fieldSig = TypeSigUtil.ComputeFieldDefSignature(field);
                        RenameMappingField rmf = rmt?.fields.GetValueOrDefault(fieldSig);
                        if (rmf != null)
                        {
                            nameMaker.AddPreservedName(field, rmf.newName);
                        }
                        _fieldRenames.Add(field, new RenameRecord
                        {
                            status = RenameStatus.NotRenamed,
                            signature = fieldSig,
                            oldName = field.Name,
                            newName = null,
                            renameMappingData = rmf,
                        });
                    }
                    foreach (PropertyDef property in type.Properties)
                    {
                        nameMaker.AddPreservedName(property, property.Name);
                        string propertySig = TypeSigUtil.ComputePropertyDefSignature(property);
                        RenameMappingProperty rmp = rmt?.properties.GetValueOrDefault(propertySig);
                        if (rmp != null)
                        {
                            nameMaker.AddPreservedName(property, rmp.newName);
                        }
                        _propertyRenames.Add(property, new RenameRecord
                        {
                            status = RenameStatus.NotRenamed,
                            signature = propertySig,
                            oldName = property.Name,
                            newName = null,
                            renameMappingData = rmp,
                        });
                    }
                    foreach (EventDef eventDef in type.Events)
                    {
                        nameMaker.AddPreservedName(eventDef, eventDef.Name);
                        string eventSig = TypeSigUtil.ComputeEventDefSignature(eventDef);
                        RenameMappingEvent rme = rmt?.events.GetValueOrDefault(eventSig);
                        if (rme != null)
                        {
                            nameMaker.AddPreservedName(eventDef, rme.newName);
                        }
                        _eventRenames.Add(eventDef, new RenameRecord
                        {
                            status = RenameStatus.NotRenamed,
                            signature = eventSig,
                            oldName = eventDef.Name,
                            newName = null,
                            renameMappingData = rme,
                        });
                    }
                }
            }
        }

        private void LoadXmlMappingFile(string mappingFile)
        {
            if (!File.Exists(mappingFile))
            {
                return;
            }
            var doc = new XmlDocument();
            doc.Load(mappingFile);
            var root = doc.DocumentElement;
            foreach (XmlNode node in root.ChildNodes)
            {
                if (node is not XmlElement element)
                {
                    continue;
                }
                LoadAssemblyMapping(element);
            }
        }

        private void LoadAssemblyMapping(XmlElement ele)
        {
            if (ele.Name != "assembly")
            {
                throw new System.Exception($"Invalid node name: {ele.Name}. Expected 'assembly'.");
            }

            var assemblyName = ele.Attributes["name"].Value;
            var newAssemblyName = ele.Attributes["newName"].Value;
            var rma = new RenameMappingAssembly
            {
                oldAssName = assemblyName,
                newAssName = newAssemblyName,
                status = System.Enum.Parse<RenameStatus>(ele.Attributes["status"].Value),
            };
            foreach (XmlNode node in ele.ChildNodes)
            {
                if (node is not XmlElement element)
                {
                    continue;
                }
                if (element.Name != "type")
                {
                    throw new System.Exception($"Invalid node name: {element.Name}. Expected 'type'.");
                }
                LoadTypeMapping(element, rma);
            }
            _assemblies.Add(assemblyName, rma);
        }

        private void LoadTypeMapping(XmlElement ele, RenameMappingAssembly ass)
        {
            var typeName = ele.Attributes["fullName"].Value;
            var newTypeName = ele.Attributes["newFullName"].Value;
            var rmt = new RenameMappingType
            {
                oldFullName = typeName,
                newFullName = newTypeName,
                status = System.Enum.Parse<RenameStatus>(ele.Attributes["status"].Value),
            };
            foreach (XmlNode node in ele.ChildNodes)
            {
                if (node is not XmlElement c)
                {
                    continue;
                }
                switch (node.Name)
                {
                    case "field": LoadFieldMapping(c, rmt); break;
                    case "event": LoadEventMapping(c, rmt); break;
                    case "property": LoadPropertyMapping(c, rmt); break;
                    case "method": LoadMethodMapping(c, rmt); break;
                    default: throw new System.Exception($"Invalid node name:{node.Name}");
                }
            }
            ass.types.Add(typeName, rmt);
        }

        private void LoadMethodMapping(XmlElement ele, RenameMappingType type)
        {
            string signature = ele.Attributes["signature"].Value;
            string newName = ele.Attributes["newName"].Value;
            var rmm = new RenameMappingMethod
            {
                signature = signature,
                newName = newName,
                status = RenameStatus.Renamed,
            };
            foreach (XmlNode node in ele.ChildNodes)
            {
                if (node is not XmlElement c)
                {
                    continue;
                }
                switch (node.Name)
                {
                    case "param": LoadMethodParamMapping(c, rmm); break;
                    default: throw new System.Exception($"unknown node name:{node.Name}, expect 'param'");
                }
            }
            type.methods.Add(signature, rmm);
        }

        private void LoadMethodParamMapping(XmlElement ele, RenameMappingMethod method)
        {
            string index = ele.Attributes["index"].Value;
            string newName = ele.Attributes["newName"].Value;
            var rmp = new RenameMappingMethodParam
            {
                index = int.Parse(index),
                newName = newName,
                status = RenameStatus.Renamed,
            };
            method.parameters.Add(rmp);
        }

        private void LoadFieldMapping(XmlElement ele, RenameMappingType type)
        {
            string signature = ele.Attributes["signature"].Value;
            string newName = ele.Attributes["newName"].Value;
            var rmf = new RenameMappingField
            {
                signature = signature,
                newName = newName,
                status = RenameStatus.Renamed,
            };
            type.fields.Add(signature, rmf);
        }

        private void LoadPropertyMapping(XmlElement ele, RenameMappingType type)
        {
            string signature = ele.Attributes["signature"].Value;
            string newName = ele.Attributes["newName"].Value;
            var rmp = new RenameMappingProperty
            {
                signature = signature,
                newName = newName,
                status = RenameStatus.Renamed,
            };
            type.properties.Add(signature, rmp);
        }

        private void LoadEventMapping(XmlElement ele, RenameMappingType type)
        {
            string signature = ele.Attributes["signature"].Value;
            string newName = ele.Attributes["newName"].Value;
            var rme = new RenameMappingEvent
            {
                signature = signature,
                newName = newName,
                status = RenameStatus.Renamed,
            };
            type.events.Add(signature, rme);
        }

        public void WriteXmlMappingFile()
        {
            var doc = new XmlDocument();
            var root = doc.CreateElement("mapping");
            doc.AppendChild(root);
            foreach (var kvp in _modRenames)
            {
                ModuleDefMD mod = kvp.Key;
                RenameRecord record = kvp.Value;
                var assemblyNode = doc.CreateElement("assembly");
                assemblyNode.SetAttribute("name", mod.Assembly.Name);
                assemblyNode.SetAttribute("newName", record.status == RenameStatus.Renamed ? record.newName : "");
                assemblyNode.SetAttribute("status", record.status.ToString());
                foreach (TypeDef type in mod.GetTypes())
                {
                    WriteTypeMapping(assemblyNode, type);
                }
                root.AppendChild(assemblyNode);
            }
            foreach (RenameMappingAssembly ass in _assemblies.Values)
            {
                if (_modRenames.Keys.Any(m => m.Assembly.Name == ass.oldAssName))
                {
                    continue;
                }
                var assemblyNode = doc.CreateElement("assembly");
                assemblyNode.SetAttribute("name", ass.oldAssName);
                assemblyNode.SetAttribute("newName", ass.status == RenameStatus.Renamed ? ass.newAssName : "");
                assemblyNode.SetAttribute("status", ass.status.ToString());
                foreach (var e in ass.types)
                {
                    WriteTypeMapping(assemblyNode, e.Key, e.Value);
                }
                root.AppendChild(assemblyNode);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(_mappingFile));
            doc.Save(_mappingFile);
            Debug.Log($"Mapping file saved to {Path.GetFullPath(_mappingFile)}");
        }

        private void WriteTypeMapping(XmlElement assNode, TypeDef type)
        {
            _typeRenames.TryGetValue(type, out var record);
            var typeNode = assNode.OwnerDocument.CreateElement("type");
            typeNode.SetAttribute("fullName", record?.signature ?? type.FullName);
            typeNode.SetAttribute("newFullName", record != null && record.status == RenameStatus.Renamed ? record.newName : "");
            typeNode.SetAttribute("status", record != null ? record.status.ToString() : RenameStatus.NotRenamed.ToString());

            foreach (FieldDef field in type.Fields)
            {
                WriteFieldMapping(typeNode, field);
            }
            foreach (PropertyDef property in type.Properties)
            {
                WritePropertyMapping(typeNode, property);
            }
            foreach (EventDef eventDef in type.Events)
            {
                WriteEventMapping(typeNode, eventDef);
            }
            foreach (MethodDef method in type.Methods)
            {
                WriteMethodMapping(typeNode, method);
            }
            if ((record != null && record.status == RenameStatus.Renamed) || typeNode.ChildNodes.Count > 0)
            {
                assNode.AppendChild(typeNode);
            }
        }

        private void WriteTypeMapping(XmlElement assNode, string fullName, RenameMappingType type)
        {
            var typeNode = assNode.OwnerDocument.CreateElement("type");
            typeNode.SetAttribute("fullName", fullName);
            typeNode.SetAttribute("newFullName", type.status == RenameStatus.Renamed ? type.newFullName : "");
            typeNode.SetAttribute("status", type.status.ToString());

            foreach (var e in type.fields)
            {
                string signature = e.Key;
                RenameMappingField field = e.Value;
                WriteFieldMapping(typeNode, e.Key, e.Value);
            }
            foreach (var e in type.properties)
            {
                WritePropertyMapping(typeNode, e.Key, e.Value);
            }
            foreach (var e in type.events)
            {
                WriteEventMapping(typeNode, e.Key, e.Value);
            }
            foreach (var e in type.methods)
            {
                WriteMethodMapping(typeNode, e.Key, e.Value);
            }

            assNode.AppendChild(typeNode);
        }

        private void WriteFieldMapping(XmlElement typeEle, FieldDef field)
        {
            if (!_fieldRenames.TryGetValue(field, out var record) || record.status == RenameStatus.NotRenamed)
            {
                return;
            }
            var fieldNode = typeEle.OwnerDocument.CreateElement("field");
            fieldNode.SetAttribute("signature", record?.signature);
            fieldNode.SetAttribute("newName", record.newName);
            //fieldNode.SetAttribute("status", record.status.ToString());
            typeEle.AppendChild(fieldNode);
        }

        private void WriteFieldMapping(XmlElement typeEle, string signature, RenameMappingField field)
        {
            var fieldNode = typeEle.OwnerDocument.CreateElement("field");
            fieldNode.SetAttribute("signature", signature);
            fieldNode.SetAttribute("newName", field.newName);
            //fieldNode.SetAttribute("status", record.status.ToString());
            typeEle.AppendChild(fieldNode);
        }

        private void WritePropertyMapping(XmlElement typeEle, PropertyDef property)
        {
            if (!_propertyRenames.TryGetValue(property, out var record) || record.status == RenameStatus.NotRenamed)
            {
                return;
            }
            var propertyNode = typeEle.OwnerDocument.CreateElement("property");
            propertyNode.SetAttribute("signature", record.signature);
            propertyNode.SetAttribute("newName", record.newName);
            //propertyNode.SetAttribute("status", record.status.ToString());
            typeEle.AppendChild(propertyNode);
        }

        private void WritePropertyMapping(XmlElement typeEle, string signature, RenameMappingProperty property)
        {
            var propertyNode = typeEle.OwnerDocument.CreateElement("property");
            propertyNode.SetAttribute("signature", signature);
            propertyNode.SetAttribute("newName", property.newName);
            //propertyNode.SetAttribute("status", record.status.ToString());
            typeEle.AppendChild(propertyNode);
        }

        private void WriteEventMapping(XmlElement typeEle, EventDef eventDef)
        {
            if (!_eventRenames.TryGetValue(eventDef, out var record) || record.status == RenameStatus.NotRenamed)
            {
                return;
            }
            var eventNode = typeEle.OwnerDocument.CreateElement("event");
            eventNode.SetAttribute("signature", record.signature);
            eventNode.SetAttribute("newName", record.newName);
            typeEle.AppendChild(eventNode);
        }

        private void WriteEventMapping(XmlElement typeEle, string signature, RenameMappingEvent eventDef)
        {
            var eventNode = typeEle.OwnerDocument.CreateElement("event");
            eventNode.SetAttribute("signature", signature);
            eventNode.SetAttribute("newName", eventDef.newName);
            typeEle.AppendChild(eventNode);
        }

        private void WriteMethodMapping(XmlElement typeEle, MethodDef method)
        {
            if (!_methodRenames.TryGetValue(method, out var record) || record.status == RenameStatus.NotRenamed)
            {
                return;
            }
            var methodNode = typeEle.OwnerDocument.CreateElement("method");
            methodNode.SetAttribute("signature", record.signature);
            methodNode.SetAttribute("newName", record.newName);
            //methodNode.SetAttribute("status", record != null ? record.status.ToString() : RenameStatus.NotRenamed.ToString());
            foreach (Parameter param in method.Parameters)
            {
                if (param.ParamDef != null)
                {
                    WriteMethodParamMapping(methodNode, param.ParamDef);
                }
            }
            typeEle.AppendChild(methodNode);
        }

        private void WriteMethodMapping(XmlElement typeEle, string signature, RenameMappingMethod method)
        {
            var methodNode = typeEle.OwnerDocument.CreateElement("method");
            methodNode.SetAttribute("signature", signature);
            methodNode.SetAttribute("newName", method.newName);
            //methodNode.SetAttribute("status", record != null ? record.status.ToString() : RenameStatus.NotRenamed.ToString());
            foreach (RenameMappingMethodParam param in method.parameters)
            {
                WriteMethodParamMapping(methodNode, param);
            }
            typeEle.AppendChild(methodNode);
        }

        private void WriteMethodParamMapping(XmlElement methodEle, ParamDef param)
        {
            if (!_paramRenames.TryGetValue(param, out var record) || record.status == RenameStatus.NotRenamed)
            {
                return;
            }
            var paramNode = methodEle.OwnerDocument.CreateElement("param");
            paramNode.SetAttribute("index", param.Sequence.ToString());
            paramNode.SetAttribute("newName", record.newName);
            //paramNode.SetAttribute("status", record.status.ToString());
            methodEle.AppendChild(paramNode);
        }

        private void WriteMethodParamMapping(XmlElement methodEle, RenameMappingMethodParam param)
        {
            var paramNode = methodEle.OwnerDocument.CreateElement("param");
            paramNode.SetAttribute("index", param.index.ToString());
            paramNode.SetAttribute("newName", param.newName);
            //paramNode.SetAttribute("status", record.status.ToString());
            methodEle.AppendChild(paramNode);
        }

        public void AddRename(ModuleDefMD mod, string newName)
        {
            RenameRecord record = _modRenames[mod];
            record.status = RenameStatus.Renamed;
            record.newName = newName;
        }

        public void AddRename(TypeDef type, string newName)
        {
            RenameRecord record = _typeRenames[type];
            record.status = RenameStatus.Renamed;
            record.newName = newName;
        }

        public void AddRename(MethodDef method, string newName)
        {
            RenameRecord record = _methodRenames[method];
            record.status = RenameStatus.Renamed;
            record.newName = newName;
        }

        public void AddRename(ParamDef paramDef, string newName)
        {
            RenameRecord record = _paramRenames[paramDef];
            record.status = RenameStatus.Renamed;
            record.newName = newName;
        }

        public void InitAndAddRename(VirtualMethodGroup methodGroup, string newName)
        {
            RenameRecord methodRecord = _methodRenames[methodGroup.methods[0]];
            _virtualMethodGroups.Add(methodGroup, new RenameRecord
            {
                status = RenameStatus.Renamed,
                signature = methodRecord.signature,
                oldName = methodRecord.oldName,
                newName = newName,
            });
        }

        public void AddRename(FieldDef field, string newName)
        {
            RenameRecord record = _fieldRenames[field];
            record.status = RenameStatus.Renamed;
            record.newName = newName;
        }

        public void AddRename(PropertyDef property, string newName)
        {
            RenameRecord record = _propertyRenames[property];
            record.status = RenameStatus.Renamed;
            record.newName = newName;
        }

        public void AddRename(EventDef eventDef, string newName)
        {
            RenameRecord record = _eventRenames[eventDef];
            record.status = RenameStatus.Renamed;
            record.newName = newName;
        }

        public bool TryGetExistRenameMapping(ModuleDefMD mod, out string newName)
        {
            if (_modRenames.TryGetValue(mod, out var record) && record.renameMappingData != null)
            {
                newName = ((RenameMappingAssembly)record.renameMappingData).newAssName;
                return true;
            }
            newName = null;
            return false;
        }

        public bool TryGetExistRenameMapping(TypeDef type, out string newNamespace, out string newName)
        {
            if (_typeRenames.TryGetValue(type, out var record) && record.renameMappingData != null)
            {
                var rmt = (RenameMappingType)record.renameMappingData;
                (newNamespace, newName) = MetaUtil.SplitNamespaceAndName(rmt.newFullName);
                return true;
            }
            newNamespace = null;
            newName = null;
            return false;
        }

        public bool TryGetExistRenameMapping(MethodDef method, out string newName)
        {
            if (_methodRenames.TryGetValue(method, out var record) && record.renameMappingData != null)
            {
                newName = ((RenameMappingMethod)record.renameMappingData).newName;
                return true;
            }
            newName = null;
            return false;
        }

        public bool TryGetExistRenameMapping(FieldDef field, out string newName)
        {
            if (_fieldRenames.TryGetValue(field, out var record) && record.renameMappingData != null)
            {
                newName = ((RenameMappingField)record.renameMappingData).newName;
                return true;
            }
            newName = null;
            return false;
        }

        public bool TryGetExistRenameMapping(PropertyDef property, out string newName)
        {
            if (_propertyRenames.TryGetValue(property, out var record) && record.renameMappingData != null)
            {
                newName = ((RenameMappingProperty)record.renameMappingData).newName;
                return true;
            }
            newName = null;
            return false;
        }

        public bool TryGetExistRenameMapping(EventDef eventDef, out string newName)
        {
            if (_eventRenames.TryGetValue(eventDef, out var record) && record.renameMappingData != null)
            {
                newName = ((RenameMappingEvent)record.renameMappingData).newName;
                return true;
            }
            newName = null;
            return false;
        }

        public bool TryGetRename(VirtualMethodGroup group, out string newName)
        {
            if (_virtualMethodGroups.TryGetValue(group, out var record))
            {
                newName = record.newName;
                return true;
            }
            newName = null;
            return false;
        }
    }
}
