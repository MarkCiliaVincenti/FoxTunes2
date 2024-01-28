﻿using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;

namespace FoxTunes.Utilities
{
    public class PlaylistItemScriptRunner
    {
        public PlaylistItemScriptRunner(IScriptingContext scriptingContext, PlaylistItem playlistItem, string script)
        {
            this.ScriptingContext = scriptingContext;
            this.PlaylistItem = playlistItem;
            this.Script = script;
        }

        public IScriptingContext ScriptingContext { get; private set; }

        public PlaylistItem PlaylistItem { get; private set; }

        public string Script { get; private set; }

        public void Prepare()
        {
            var metaData = new Dictionary<string, object>();
            foreach (var item in this.PlaylistItem.MetaDatas)
            {
                metaData.Add(item.Name.ToLower(), item.Value);
            }

            var properties = new Dictionary<string, object>();
            foreach (var item in this.PlaylistItem.Properties)
            {
                properties.Add(item.Name.ToLower(), item.Value);
            }
            this.ScriptingContext.SetValue("playing", StandardManagers.Instance.Playback.CurrentStream);
            this.ScriptingContext.SetValue("item", this.PlaylistItem);
            this.ScriptingContext.SetValue("tag", metaData);
            this.ScriptingContext.SetValue("stat", properties);
        }

        public object Run()
        {
            const string RESULT = "__result";
            try
            {
                this.ScriptingContext.Run(string.Concat("var ", RESULT, " = ", this.Script, ";"));
            }
            catch (Exception e)
            {
                return e;
            }
            return this.ScriptingContext.GetValue(RESULT);
        }
    }
}
