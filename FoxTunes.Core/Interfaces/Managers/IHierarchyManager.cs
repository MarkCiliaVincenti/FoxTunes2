﻿using System;
using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IHierarchyManager : IStandardManager, IBackgroundTaskSource
    {
        HierarchyManagerState State { get; }

        Task Build(bool reset);

        Task Clear();
    }

    [Flags]
    public enum HierarchyManagerState : byte
    {
        None = 0,
        Updating = 1
    }
}
