﻿using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace FoxTunes
{
    public static class Publication
    {
        public static readonly string Company = "RaimuSoft";

        public static readonly string Product = "FoxTunes";

        public static readonly string Version = GetVersion();

        private static string GetVersion()
        {
            var version = typeof(Publication).Assembly.GetName().Version;
            return string.Format(
                "{0}.{1}.{2}",
                version.Major,
                version.Minor,
                version.Build
            );
        }

        public static Lazy<bool> _IsPortable = new Lazy<bool>(() =>
        {
            //Wow. This is complicated isn't it? Probably simpler and more reliable 
            //to just attempt to write to the folder and handle the error but I try to 
            //avoid exceptions..
            try
            {
                //Grab some informations.
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                var directoryInfo = new DirectoryInfo(ComponentScanner.Instance.Location);
                var directorySecurity = directoryInfo.GetAccessControl();
                var authorizationRules = directorySecurity.GetAccessRules(true, true, typeof(NTAccount));
                for (var a = 0; a < authorizationRules.Count; a++)
                {
                    //We want FileSystemAccessRules where the IdentityReference is an NTAccount.
                    var authorizationRule = authorizationRules[a] as FileSystemAccessRule;
                    if (authorizationRule == null)
                    {
                        continue;
                    }
                    var account = authorizationRule.IdentityReference as NTAccount;
                    if (account == null)
                    {
                        continue;
                    }
                    //Check we have the role (are the user, in the group, whatever..)
                    if (!principal.IsInRole(account.Value))
                    {
                        continue;
                    }
                    //Check it's write access.
                    if (!authorizationRule.FileSystemRights.HasFlag(FileSystemRights.Write))
                    {
                        continue;
                    }
                    //Check it's allow.
                    if (authorizationRule.AccessControlType != AccessControlType.Allow)
                    {
                        continue;
                    }
                    //Looks like we can write here. Must be portable.
                    return true;
                }
            }
            catch
            {
                //Nothing can be done, assume the worst.
            }
            //Either we can't write or we failed to determine.
            //In any case, not portable.
            return false;
        });

        public static bool IsPortable
        {
            get
            {
                return _IsPortable.Value;
            }
        }

        private static Lazy<string> _StoragePath = new Lazy<string>(() =>
        {
            if (IsPortable)
            {
                //If we're portable we store data with the application.
                return ComponentScanner.Instance.Location;
            }
            else
            {
                //Otherwise we use a directory AppData\Company\Product\Version
                var directoryName = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Company,
                    Product
                );
                if (!Directory.Exists(directoryName))
                {
                    //TODO: If this fails bad things are about to happen...
                    Directory.CreateDirectory(directoryName);
                }
                return directoryName;
            }
        });

        public static string StoragePath
        {
            get
            {
                return _StoragePath.Value;
            }
        }
    }
}
