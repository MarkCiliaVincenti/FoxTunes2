﻿using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;

namespace FoxTunes
{
    public class ComponentScanner : IComponentScanner
    {
        const FileSystemHelper.SearchOption SEARCH_OPTIONS =
            FileSystemHelper.SearchOption.Recursive |
            FileSystemHelper.SearchOption.UseSystemCache |
            FileSystemHelper.SearchOption.UseSystemExclusions;

        private static Type[] STANDARD_COMPONENT_TYPES = new[]
        {
            typeof(IStandardComponent),
            typeof(IStandardManager),
            typeof(IStandardFactory),
            typeof(IStandardBehaviour)
        };

        protected static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        public static string Location
        {
            get
            {
                return Path.GetDirectoryName(typeof(ComponentScanner).Assembly.Location);
            }
        }

        private static Lazy<IEnumerable<Type>> _AllComponents = new Lazy<IEnumerable<Type>>(GetAllComponents);

        private static Lazy<IEnumerable<Type>> _AllStandardComponents = new Lazy<IEnumerable<Type>>(GetAllStandardComponents);

        private static Lazy<IEnumerable<Type>> _AllStandardResolvedComponents = new Lazy<IEnumerable<Type>>(GetAllStandardResolvedComponents);

        private static IEnumerable<string> GetFileNames()
        {
            return Enumerable.Concat(
                FileSystemHelper.EnumerateFiles(Location, "FoxTunes*.dll", SEARCH_OPTIONS),
                FileSystemHelper.EnumerateFiles(Location, "FoxTunes*.exe", SEARCH_OPTIONS)
            ).ToArray();
        }

        private static IEnumerable<Type> GetTypes(string fileName)
        {
            var assemblyName = AssemblyRegistry.Instance.GetAssemblyName(fileName);
            if (assemblyName == null)
            {
                return Enumerable.Empty<Type>();
            }
            var assembly = default(Assembly);
            try
            {
                assembly = AssemblyRegistry.Instance.GetOrLoadReflectionAssembly(fileName);
            }
            catch
            {
                return Enumerable.Empty<Type>();
            }
            try
            {
                return assembly.GetExportedTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                Logger.Write(typeof(ComponentScanner), LogLevel.Trace, "Error was handled while getting exported types for assembly {0}: {1}", fileName, e.Message);
                return e.Types;
            }
            catch (Exception e)
            {
                Logger.Write(typeof(ComponentScanner), LogLevel.Warn, "Failed to get exported types for assembly {0}: {1}", fileName, e.Message);
                return Enumerable.Empty<Type>();
            }
        }

        private static IEnumerable<Type> GetAllComponents()
        {
            return GetFileNames()
                .SelectMany(GetTypes)
                .Where(ComponentFilter.IsCompatable)
                .ToArray();
        }

        private static IEnumerable<Type> GetAllStandardComponents()
        {
            return _AllComponents.Value
                .Where(ComponentFilter.IsStandardComponent)
                .ToArray();
        }

        private static IEnumerable<Type> GetAllStandardResolvedComponents()
        {
            try
            {
                return _AllStandardComponents.Value
                    .OrderBy(ComponentSorter.Type)
                    .ThenBy(ComponentSorter.Preference)
                    .ThenBy(ComponentSorter.Priority)
                    .Where(ComponentFilter.IsSelected)
                    .ToArray();
            }
            finally
            {
                ComponentResolver.Instance.Save();
            }
        }

        public IEnumerable<Type> GetComponents()
        {
            return _AllStandardResolvedComponents.Value;
        }

        public IEnumerable<Type> GetComponents(Type type)
        {
            return _AllComponents.Value
                .Where(ComponentFilter.IsComponent(type))
                .OrderBy(ComponentSorter.Type)
                .ThenBy(ComponentSorter.Preference)
                .ThenBy(ComponentSorter.Priority)
                .ToArray();
        }

        public static readonly IComponentScanner Instance = new ComponentScanner();

        public static bool ImplementsInterface(Type type, Type @interface)
        {
            return ImplementsInterface(type, @interface.FullName);
        }

        public static bool ImplementsInterface(Type type, string @interface)
        {
            return type.GetInterfaces().Any(interfaceType => string.Equals(interfaceType.FullName, @interface, StringComparison.OrdinalIgnoreCase));
        }

        public static bool ImplementsInterface(Type type, IEnumerable<Type> interfaces)
        {
            return ImplementsInterface(type, interfaces.Select(@interface => @interface.FullName));
        }

        public static bool ImplementsInterface(Type type, IEnumerable<string> interfaces)
        {
            return type.GetInterfaces().Any(interfaceType => interfaces.Contains(interfaceType.FullName, StringComparer.OrdinalIgnoreCase));
        }

        private static class ComponentFilter
        {
            public static bool IsSelected(Type type)
            {
                var component = default(ComponentAttribute);
                if (type.HasCustomAttribute<ComponentAttribute>(out component))
                {
                    if (string.IsNullOrEmpty(component.Slot) || string.Equals(component.Slot, ComponentSlots.None, StringComparison.OrdinalIgnoreCase))
                    {
                        //Component does not target slot.
                    }
                    else
                    {
                        var id = default(string);
                        if (!ComponentResolver.Instance.Get(component.Slot, out id))
                        {
                            //Slot is not configured, update it.
                            ComponentResolver.Instance.Add(component.Slot, component.Id);
                        }
                        else
                        {
                            if (string.Equals(component.Id, id, StringComparison.OrdinalIgnoreCase))
                            {
                                //Slot is configured for this component.
                            }
                            else
                            {
                                //We just handled an ambiguous slot, inform the resolver that it should save the resolution for next time.
                                ComponentResolver.Instance.AddConflict(component.Slot);
                                return false;
                            }
                        }
                    }
                }
                return true;
            }

            public static Func<Type, bool> IsComponent(Type @interface)
            {
                return type => type.IsClass && !type.IsAbstract && ImplementsInterface(type, @interface);
            }

            public static bool IsStandardComponent(Type type)
            {
                return type.IsClass && !type.IsAbstract && ImplementsInterface(type, STANDARD_COMPONENT_TYPES);
            }

            public static bool IsCompatable(Type type)
            {
                return HasPlatform(type);
            }

            public static bool HasPlatform(Type type)
            {
                var dependencies = default(IEnumerable<PlatformDependencyAttribute>);
                if (type.HasCustomAttributes<PlatformDependencyAttribute>(out dependencies))
                {
                    foreach (var dependency in dependencies)
                    {
                        var version = new Version(dependency.Major, dependency.Minor);
                        if (Environment.OSVersion.Version < version)
                        {
                            Logger.Write(typeof(ComponentScanner), LogLevel.Debug, "Not loading component \"{0}\": Requires platform {1}.{2}.", type.FullName, dependency.Major, dependency.Minor);
                            return false;
                        }
                        if (dependency.Architecture != ProcessorArchitecture.None)
                        {
                            var is64BitProcess = Environment.Is64BitProcess;
                            var is34BitProcess = !is64BitProcess;
                            if (dependency.Architecture == ProcessorArchitecture.X86 && is64BitProcess)
                            {
                                Logger.Write(typeof(ComponentScanner), LogLevel.Debug, "Not loading component \"{0}\": Requires platform X86.", type.FullName);
                                return false;
                            }
                            if (dependency.Architecture == ProcessorArchitecture.X64 && !is34BitProcess)
                            {
                                Logger.Write(typeof(ComponentScanner), LogLevel.Debug, "Not loading component \"{0}\": Requires platform X64.", type.FullName);
                                return false;
                            }
                        }
                    }
                }
                return true;
            }
        }

        private static class ComponentSorter
        {
            public static byte Type(Type type)
            {
                //TODO: This is awful, basically all component loading goes through this ordering before any other sorting strategies (e.g priority)
                //TODO: The load order is always: Components, Factories, Managers, Behaviours.
                //TODO: I don't have the energy to fix it, removing this code causes issues with component inter-dependency during start up.
                var interfaces = type.GetInterfaces().Select(
                    @interface => @interface.FullName
                ).ToArray();
                if (interfaces.Contains(typeof(IStandardComponent).FullName, StringComparer.OrdinalIgnoreCase))
                {
                    return 0;
                }
                if (interfaces.Contains(typeof(IStandardFactory).FullName, StringComparer.OrdinalIgnoreCase))
                {
                    return 1;
                }
                if (interfaces.Contains(typeof(IStandardManager).FullName, StringComparer.OrdinalIgnoreCase))
                {
                    return 2;
                }
                if (interfaces.Contains(typeof(IStandardBehaviour).FullName, StringComparer.OrdinalIgnoreCase))
                {
                    return 3;
                }
                return byte.MaxValue;
            }

            public static byte Priority(Type type)
            {
                var priority = default(ComponentPriorityAttribute);
                if (!type.HasCustomAttribute<ComponentPriorityAttribute>(out priority))
                {
                    return ComponentPriorityAttribute.NORMAL;
                }
                return priority.Priority;
            }

            public static byte Preference(Type type)
            {
                var preference = default(ComponentPreferenceAttribute);
                if (!type.HasCustomAttribute<ComponentPreferenceAttribute>(out preference))
                {
                    return ComponentPreferenceAttribute.NORMAL;
                }
                return preference.Priority;
            }
        }
    }
}
