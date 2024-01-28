﻿using FoxTunes.Interfaces;
using Microsoft.ClearScript.V8;
using System.CodeDom;

namespace FoxTunes
{
    [ComponentPreference(ComponentPreferenceAttribute.NORMAL)]
    [PlatformDependency(Major = 6, Minor = 0)]
    [Component(ID, ComponentSlots.ScriptingRuntime)]
    public class JSScriptingRuntime : ScriptingRuntime
    {
        const string ID = "BA421DD1-22AB-4E39-82FA-55BFD95EE768";

        public JSScriptingRuntime() : base(ID, Strings.JSScriptingRuntime_Name, string.Format(Strings.JSScriptingRuntime_Description, /*TODO: Read this from the assembly.*/"7.3.4"))
        {
        }

        public ICore Core { get; private set; }

        public override ICoreScripts CoreScripts
        {
            get
            {
                return JSCoreScripts.Instance;
            }
        }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            base.InitializeComponent(core);
        }

        public override IScriptingContext CreateContext()
        {
            Logger.Write(this, LogLevel.Debug, "Creating javascript scripting context.");
            var context = new JSScriptingContext(new V8ScriptEngine());
            context.InitializeComponent(this.Core);
            return context;
        }
    }
}