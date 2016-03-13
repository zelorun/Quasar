﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security;
using Microsoft.Win32;
using System.Threading;
using xClient.Core.Extensions;

namespace xClient.Core.Registry
{
    /*
    * Derived and Adapted from CrackSoft's Reg Explore.
    * Reg Explore v1.1 (Release Date: June 24, 2011)
    * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    * This is a work that is not of the original. It
    * has been modified to suit the needs of another
    * application.
    * (This has been taken from Justin Yanke's branch)
    * First Modified by Justin Yanke on August 15, 2015
    * Second Modified by StingRaptor on January 21, 2016
    * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    * Unmodified Source:
    * https://regexplore.codeplex.com/SourceControl/latest#Registry/RegSearcher.cs
    */

    public class MatchFoundEventArgs : EventArgs
    {
        public RegSeekerMatch Match { get; private set; }

        public MatchFoundEventArgs(RegSeekerMatch match)
        {
            Match = match;
        }
    }

    public class SearchCompletedEventArgs : EventArgs
    {
        public List<RegSeekerMatch> Matches { get; private set; }

        public SearchCompletedEventArgs(List<RegSeekerMatch> matches)
        {
            Matches = matches;
        }
    }

    public class RegistrySeeker
    {
        #region CONSTANTS

        /// <summary>
        /// An array containing all of the root keys for the registry.
        /// </summary>
        public static readonly RegistryKey[] ROOT_KEYS = new RegistryKey[]
        {
            Microsoft.Win32.Registry.ClassesRoot,
            Microsoft.Win32.Registry.CurrentUser,
            Microsoft.Win32.Registry.LocalMachine,
            Microsoft.Win32.Registry.Users,
            Microsoft.Win32.Registry.CurrentConfig
        };

        #endregion

        #region Fields

        /// <summary>
        /// The lock used to ensure thread safety.
        /// </summary>
        private readonly object locker = new object();

        /// <summary>
        /// The list containing the matches found during the search.
        /// </summary>
        private List<RegSeekerMatch> matches;

        public RegSeekerMatch[] Matches
        {
            get
            {
                if (matches != null)
                    return matches.ToArray();
                return null;
            }
        }

        #endregion

        public RegistrySeeker()
        {
            matches = new List<RegSeekerMatch>();
        }

        public void BeginSeeking(string rootKeyName)
        {
            if (!String.IsNullOrEmpty(rootKeyName))
            {
                using(RegistryKey root = GetRootKey(rootKeyName))
                {
                    //Check if this is a root key or not
                    if (root != null && root.Name != rootKeyName)
                    {
                        //Must get the subKey name by removing root and '\'
                        string subKeyName = rootKeyName.Substring(root.Name.Length + 1);
                        using(RegistryKey subroot = root.OpenReadonlySubKeySafe(subKeyName))
                        {
                            if(subroot != null)
                                Seek(subroot);
                        } 
                    }
                    else
                    {
                        Seek(root);
                    }
                }
            }
            else
            {
                Seek(null);
            }
        }

        private void Seek(RegistryKey rootKey)
        {
            // Get root registrys
            if (rootKey == null)
            {
                foreach (RegistryKey key in RegistrySeeker.ROOT_KEYS)
                    //Just need root key so process it
                    ProcessKey(key, key.Name);
            }
            else
            {
                //searching for subkeys to root key
                Search(rootKey);
            }
        }

        private void Search(RegistryKey rootKey)
        {
            foreach(string subKeyName in rootKey.GetSubKeyNames())
            {
                RegistryKey subKey = rootKey.OpenReadonlySubKeySafe(subKeyName);
                ProcessKey(subKey, subKeyName);
            }
        }

        private void ProcessKey(RegistryKey key, string keyName)
        {
            if (key != null)
            {
                List<RegValueData> values = new List<RegValueData>();

                foreach (string valueName in key.GetValueNames())
                {
                    RegistryValueKind valueType = key.GetValueKind(valueName);
                    object valueData = key.GetValue(valueName);
                    values.Add(new RegValueData(valueName, valueType, valueData));
                }

                AddMatch(keyName, values, key.SubKeyCount);
            }
            else
            {
                AddMatch(keyName, null, 0);
            }

        }

        private void AddMatch(string key, List<RegValueData> values, int subkeycount)
        {
            RegSeekerMatch match = new RegSeekerMatch(key, values, subkeycount);

            matches.Add(match);
        }

        public static RegistryKey GetRootKey(string subkey_fullpath)
        {
            string[] path = subkey_fullpath.Split('\\');

            switch (path[0]) // <== root;
            {
                case "HKEY_CLASSES_ROOT":
                    return Microsoft.Win32.Registry.ClassesRoot;
                case "HKEY_CURRENT_USER":
                    return Microsoft.Win32.Registry.CurrentUser;
                case "HKEY_LOCAL_MACHINE":
                    return Microsoft.Win32.Registry.LocalMachine;
                case "HKEY_USERS":
                    return Microsoft.Win32.Registry.Users;
                case "HKEY_CURRENT_CONFIG":
                    return Microsoft.Win32.Registry.CurrentConfig;
                default:
                    /* If none of the above then the key must be invalid */
                    throw new Exception("Invalid rootkey, could not be found");
            }
        }
    }
}
