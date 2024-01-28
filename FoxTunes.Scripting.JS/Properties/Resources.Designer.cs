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
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("FoxTunes.Scripting.JS.Properties.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to (function () {
        ///    if (tag.iscompilation || tag.__ft_variousartists) {
        ///        return strings.general_variousartists;
        ///    }
        ///    return tag.artist || strings.general_noartist;
        ///})().
        /// </summary>
        internal static string Artist {
            get {
                return ResourceManager.GetString("Artist", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to (function () {
        ///    var parts = [tag.artist || strings.general_noartist];
        ///    if (tag.album) {
        ///        parts.push(tag.album);
        ///    }
        ///    return parts.join(&quot; - &quot;);
        ///})().
        /// </summary>
        internal static string Artist_Album {
            get {
                return ResourceManager.GetString("Artist_Album", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to (function () {
        ///    var parts = [];
        ///    if (tag.performer) {
        ///        parts.push(tag.performer);
        ///    }
        ///    else if (tag.artist) {
        ///        parts.push(tag.artist);
        ///    }
        ///    else {
        ///        parts.push(strings.general_noartist);
        ///    }
        ///    if (tag.title) {
        ///        parts.push(tag.title);
        ///    }
        ///    else {
        ///        parts.push(strings.general_notitle);
        ///    }
        ///    if (tag.beatsperminute) {
        ///        parts.push(&quot;[&quot; + tag.beatsperminute + &quot;]&quot;);
        ///    }
        ///    else {
        ///        parts.push(&quot;[&quot; + strings.general_un [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string Artist_Title_BPM {
            get {
                return ResourceManager.GetString("Artist_Title_BPM", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to (extension(file) || &quot;&quot;).toUpperCase();.
        /// </summary>
        internal static string Codec {
            get {
                return ResourceManager.GetString("Codec", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to (function () {
        ///    if (tag.title) {
        ///        var parts = [];
        ///        if (parseInt(tag.disccount) != 1 &amp;&amp; parseInt(tag.disc)) {
        ///            parts.push(tag.disc);
        ///        }
        ///        if (tag.track) {
        ///            parts.push(zeropad2(tag.track, tag.trackcount, 2));
        ///        }
        ///        parts.push(tag.title);
        ///        return parts.join(&quot; - &quot;);
        ///    } return filename(file);
        ///})().
        /// </summary>
        internal static string Disk_Track_Title {
            get {
                return ResourceManager.GetString("Disk_Track_Title", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to timestamp(property.duration).
        /// </summary>
        internal static string Duration {
            get {
                return ResourceManager.GetString("Duration", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ucfirst(tag.genre) || strings.general_nogenre.
        /// </summary>
        internal static string Genre {
            get {
                return ResourceManager.GetString("Genre", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to (function () {
        ///    if (tag.rating) {
        ///        return tag.rating;
        ///    }
        ///    return strings.general_norating;
        ///})().
        /// </summary>
        internal static string Rating {
            get {
                return ResourceManager.GetString("Rating", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to (function () {
        ///    var parts = [];
        ///    if (tag.title) {
        ///        parts.push(tag.title);
        ///    }
        ///    if (tag.performer &amp;&amp; tag.performer != tag.artist) {
        ///        parts.push(tag.performer);
        ///    }
        ///    if (parts.length) {
        ///        return parts.join(&quot; - &quot;);
        ///    }
        ///    else {
        ///        return filename(file);
        ///    }
        ///})().
        /// </summary>
        internal static string Title_Performer {
            get {
                return ResourceManager.GetString("Title_Performer", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to (function () {
        ///    var parts = [];
        ///    if (tag.disccount != 1 &amp;&amp; tag.disc) {
        ///        parts.push(tag.disc);
        ///    }
        ///    if (tag.track) {
        ///        parts.push(zeropad2(tag.track, tag.trackcount, 2));
        ///    }
        ///    return parts.join(&quot; - &quot;);
        ///})().
        /// </summary>
        internal static string Track {
            get {
                return ResourceManager.GetString("Track", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to function version() {
        ///    //The actual version could be returned.
        ///    //return Publication.Product + &quot; &quot; + Publication.Version;
        ///    return &quot;Fox Tunes&quot;;
        ///}
        ///
        ///function timestamp(value) {
        ///
        ///    if (!value) {
        ///        return value;
        ///    }
        ///
        ///    var s = parseInt((value / 1000) % 60);
        ///    var m = parseInt((value / (1000 * 60)) % 60);
        ///    var h = parseInt((value / (1000 * 60 * 60)) % 24);
        ///
        ///    var parts = [];
        ///
        ///    if (h &gt; 0) {
        ///        if (h &lt; 10) {
        ///            h = &quot;0&quot; + h;
        ///        }
        ///        parts.pu [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string Utils {
            get {
                return ResourceManager.GetString("Utils", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to (function () {
        ///    if (tag.album) {
        ///        var parts = [];
        ///        if (tag.year) {
        ///            parts.push(tag.year);
        ///        }
        ///        parts.push(tag.album);
        ///        return parts.join(&quot; - &quot;);
        ///    }
        ///    return strings.general_noalbum;
        ///})().
        /// </summary>
        internal static string Year_Album {
            get {
                return ResourceManager.GetString("Year_Album", resourceCulture);
            }
        }
    }
}
