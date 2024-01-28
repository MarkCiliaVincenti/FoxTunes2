﻿using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class RescanLibraryTask : LibraryTaskBase
    {
        public RescanLibraryTask() : this(Enumerable.Empty<string>(), Enumerable.Empty<LibraryItem>())
        {

        }

        public RescanLibraryTask(IEnumerable<string> roots, IEnumerable<LibraryItem> items) : base()
        {
            this.Roots = roots;
            this.Items = items;
        }

        public IEnumerable<string> Roots { get; private set; }

        public IEnumerable<LibraryItem> Items { get; private set; }

        public override bool Visible
        {
            get
            {
                return true;
            }
        }

        public override bool Cancellable
        {
            get
            {
                return true;
            }
        }

        protected override async Task OnStarted()
        {
            this.Name = "Getting file list";
            await base.OnStarted().ConfigureAwait(false);
        }

        protected override async Task OnRun()
        {
            var roots = default(IEnumerable<string>);
            if (this.Roots != null && this.Roots.Any())
            {
                roots = this.Roots;
            }
            else
            {
                roots = await this.GetRoots().ConfigureAwait(false);
            }
            await this.CheckPaths(roots).ConfigureAwait(false);
            await this.RescanLibrary().ConfigureAwait(false);
            await this.RemoveHierarchies(LibraryItemStatus.Remove).ConfigureAwait(false);
            await this.RemoveItems(LibraryItemStatus.Remove).ConfigureAwait(false);
            await this.AddPaths(roots).ConfigureAwait(false);
        }

        protected virtual async Task CheckPaths(IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                if (!NetworkDrive.IsRemotePath(path))
                {
                    continue;
                }
                await NetworkDrive.ConnectRemotePath(path).ConfigureAwait(false);
            }
        }

        protected virtual async Task RescanLibrary()
        {
            var predicate = new Func<LibraryItem, bool>(libraryItem =>
            {
                var file = new FileInfo(libraryItem.FileName);
                if (!file.Exists)
                {
                    Logger.Write(this, LogLevel.Debug, "Removing dead file: {0} => {1}", libraryItem.Id, libraryItem.FileName);
                    return true;
                }
                if (file.LastWriteTimeUtc > libraryItem.GetImportDate())
                {
                    Logger.Write(this, LogLevel.Debug, "Refreshing modified file: {0} => {1}", libraryItem.Id, libraryItem.FileName);
                    return true;
                }
                return false;
            });
            var action = new Func<IDatabaseSet<LibraryItem>, LibraryItem, Task>(async (set, libraryItem) =>
            {
                libraryItem.Status = LibraryItemStatus.Remove;
                //TODO: Writing to IDatabaseSet is not thread safe.
                Monitor.Enter(set);
                try
                {
                    await set.AddOrUpdateAsync(libraryItem).ConfigureAwait(false);
                }
                finally
                {
                    Monitor.Exit(set);
                }
            });
            using (var task = new SingletonReentrantTask(this, ComponentSlots.Database, SingletonReentrantTask.PRIORITY_LOW, async cancellationToken =>
            {
                using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
                {
                    using (var libraryUpdater = new LibraryUpdater(this.Database, this.Items, predicate, action, this.Visible, transaction))
                    {
                        libraryUpdater.InitializeComponent(this.Core);
                        await this.WithSubTask(libraryUpdater,
                            () => libraryUpdater.Populate(cancellationToken)
                        ).ConfigureAwait(false);
                    }
                    if (transaction.HasTransaction)
                    {
                        transaction.Commit();
                    }
                }
            }))
            {
                await task.Run().ConfigureAwait(false);
            }
        }

        protected override async Task OnCompleted()
        {
            await base.OnCompleted().ConfigureAwait(false);
            await this.SignalEmitter.Send(new Signal(this, CommonSignals.LibraryUpdated)).ConfigureAwait(false);
            await this.SignalEmitter.Send(new Signal(this, CommonSignals.HierarchiesUpdated)).ConfigureAwait(false);
        }
    }
}
