﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace FoxTunes {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Strings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Strings() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("FoxTunes.Output.Bass.Dsd.Properties.Strings", typeof(Strings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to PCM Gain (dB).
        /// </summary>
        internal static string BassDsdBehaviourConfiguration_Gain {
            get {
                return ResourceManager.GetString("BassDsdBehaviourConfiguration.Gain", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Play From Memory.
        /// </summary>
        internal static string BassDsdBehaviourConfiguration_Memory {
            get {
                return ResourceManager.GetString("BassDsdBehaviourConfiguration.Memory", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to DSD.
        /// </summary>
        internal static string BassDsdBehaviourConfiguration_Path {
            get {
                return ResourceManager.GetString("BassDsdBehaviourConfiguration.Path", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to PCM Rate (Hz).
        /// </summary>
        internal static string BassDsdBehaviourConfiguration_Rate {
            get {
                return ResourceManager.GetString("BassDsdBehaviourConfiguration.Rate", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Memory.
        /// </summary>
        internal static string BassDsdMemoryStreamComponent_Name {
            get {
                return ResourceManager.GetString("BassDsdMemoryStreamComponent.Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Open SACD.
        /// </summary>
        internal static string BassSacdBehaviour_Open {
            get {
                return ResourceManager.GetString("BassSacdBehaviour.Open", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Creating memory stream.
        /// </summary>
        internal static string LoadingTask_Name {
            get {
                return ResourceManager.GetString("LoadingTask.Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Reading image.
        /// </summary>
        internal static string SacdItemFactory_Name {
            get {
                return ResourceManager.GetString("SacdItemFactory.Name", resourceCulture);
            }
        }
    }
}
