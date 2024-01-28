﻿#pragma warning disable 612, 618
using FoxDb;
using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Database)]
    public class LibraryManager : StandardManager, ILibraryManager
    {
        public LibraryManager()
        {
            this._SelectedItem = new ConcurrentDictionary<LibraryHierarchy, LibraryHierarchyNode>();
        }

        public LibraryManagerState State
        {
            get
            {
                if (global::FoxTunes.BackgroundTask.Active.Any(backgroundTask =>
                {
                    var type = backgroundTask.GetType();
                    return type == typeof(AddPathsToLibraryTask) || type == typeof(ClearLibraryTask) || type == typeof(RescanLibraryTask);
                }))
                {
                    return LibraryManagerState.Updating;
                }
                return LibraryManagerState.None;
            }
        }

        public ICore Core { get; private set; }

        public IDatabaseFactory DatabaseFactory { get; private set; }

        public ILibraryHierarchyBrowser HierarchyBrowser { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        private LibraryHierarchy _SelectedHierarchy { get; set; }

        public LibraryHierarchy SelectedHierarchy
        {
            get
            {
                return this._SelectedHierarchy;
            }
            set
            {
                if (object.Equals(this._SelectedHierarchy, value))
                {
                    return;
                }
                this._SelectedHierarchy = value;
                this.OnSelectedHierarchyChanged();
            }
        }

        protected virtual void OnSelectedHierarchyChanged()
        {
            this.OnSelectedItemChanged();
            if (this.SelectedHierarchyChanged != null)
            {
                this.SelectedHierarchyChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("SelectedHierarchy");
        }

        public event EventHandler SelectedHierarchyChanged;

        private ConcurrentDictionary<LibraryHierarchy, LibraryHierarchyNode> _SelectedItem { get; set; }

        public LibraryHierarchyNode SelectedItem
        {
            get
            {
                var libraryHierarchyNode = default(LibraryHierarchyNode);
                if (this.SelectedHierarchy == null || !this._SelectedItem.TryGetValue(this.SelectedHierarchy, out libraryHierarchyNode))
                {
                    return default(LibraryHierarchyNode);
                }
                return libraryHierarchyNode;
            }
            set
            {
                if (this.SelectedHierarchy == null || object.Equals(this.SelectedItem, value))
                {
                    return;
                }
                this._SelectedItem[this.SelectedHierarchy] = value;
                this.OnSelectedItemChanged();
            }
        }

        protected virtual void OnSelectedItemChanged()
        {
            if (this.SelectedItemChanged != null)
            {
                this.SelectedItemChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("SelectedItem");
        }

        public event EventHandler SelectedItemChanged;

        private bool _CanNavigate { get; set; }

        public bool CanNavigate
        {
            get
            {
                return this._CanNavigate;
            }
        }

        protected Task SetCanNavigate(bool value)
        {
            this._CanNavigate = value;
            return this.OnCanNavigateChanged();
        }

        protected virtual async Task OnCanNavigateChanged()
        {
            if (this.CanNavigateChanged != null)
            {
                var e = new AsyncEventArgs();
                this.CanNavigateChanged(this, e);
                await e.Complete().ConfigureAwait(false);
            }
            this.OnPropertyChanged("CanNavigate");
        }

        public event AsyncEventHandler CanNavigateChanged;

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.HierarchyBrowser = core.Components.LibraryHierarchyBrowser;
            this.DatabaseFactory = core.Factories.Database;
            this.SignalEmitter = core.Components.SignalEmitter;
            this.SignalEmitter.Signal += this.OnSignal;
            //TODO: Bad .Wait().
            this.Refresh().Wait();
            base.InitializeComponent(core);
        }

        protected virtual Task OnSignal(object sender, ISignal signal)
        {
            switch (signal.Name)
            {
                case CommonSignals.LibraryUpdated:
                case CommonSignals.HierarchiesUpdated:
                    return this.Refresh();
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public async Task<bool> HasItems()
        {
            using (var database = this.DatabaseFactory.Create())
            {
                using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                {
                    return await database.ExecuteScalarAsync<bool>(database.QueryFactory.Build().With(query1 =>
                    {
                        query1.Output.AddCase(
                            query1.Output.CreateCaseCondition(
                                query1.Output.CreateFunction(
                                    QueryFunction.Exists,
                                    query1.Output.CreateSubQuery(
                                        database.QueryFactory.Build().With(query2 =>
                                        {
                                            query2.Output.AddOperator(QueryOperator.Star);
                                            query2.Source.AddTable(database.Tables.LibraryItem);
                                        })
                                    )
                                ),
                                query1.Output.CreateConstant(1)
                            ),
                            query1.Output.CreateCaseCondition(
                                query1.Output.CreateConstant(0)
                            )
                        );
                    }), transaction).ConfigureAwait(false);
                }
            }
        }

        public async Task Refresh()
        {
            if (this.SelectedHierarchy != null)
            {
                this.SelectedHierarchy = this.HierarchyBrowser.GetHierarchies().FirstOrDefault(libraryHierarchy => libraryHierarchy.Id == this.SelectedHierarchy.Id);
                if (this.SelectedHierarchy != null)
                {
                    Logger.Write(this, LogLevel.Debug, "Refreshed selected hierarchy: {0} => {1}", this.SelectedHierarchy.Id, this.SelectedHierarchy.Name);
                }
                else
                {
                    Logger.Write(this, LogLevel.Debug, "Failed to refresh selected hierarchy, it was removed or disabled.");
                }
            }
            if (this.SelectedHierarchy == null)
            {
                this.SelectedHierarchy = this.HierarchyBrowser.GetHierarchies().FirstOrDefault();
                if (this.SelectedHierarchy == null)
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to select a hierarchy, perhaps none are enabled?");
                }
                else
                {
                    Logger.Write(this, LogLevel.Debug, "Selected first hierarchy: {0} => {1}", this.SelectedHierarchy.Id, this.SelectedHierarchy.Name);
                }
            }
            Logger.Write(this, LogLevel.Debug, "Refresh was requested, determining whether navigation is possible.");
            await this.SetCanNavigate(this.DatabaseFactory != null && await this.HasItems().ConfigureAwait(false)).ConfigureAwait(false);
            if (this.CanNavigate)
            {
                Logger.Write(this, LogLevel.Debug, "Navigation is possible.");
            }
            else
            {
                Logger.Write(this, LogLevel.Debug, "Navigation is not possible, library is empty.");
            }
        }

        public async Task Add(IEnumerable<string> paths)
        {
            using (var task = new AddPathsToLibraryTask(paths))
            {
                task.InitializeComponent(this.Core);
                await this.OnBackgroundTask(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
                this.OnReport();
            }
        }

        public async Task Clear(LibraryItemStatus? status)
        {
            using (var task = new ClearLibraryTask(status))
            {
                task.InitializeComponent(this.Core);
                await this.OnBackgroundTask(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }

        public async Task Rescan()
        {
            using (var task = new RescanLibraryTask())
            {
                task.InitializeComponent(this.Core);
                await this.OnBackgroundTask(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
                this.OnReport();
            }
        }

        public async Task Rescan(IEnumerable<string> roots, IEnumerable<LibraryItem> items)
        {
            using (var task = new RescanLibraryTask(roots, items))
            {
                task.InitializeComponent(this.Core);
                await this.OnBackgroundTask(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
                this.OnReport();
            }
        }

        public async Task Set(LibraryItemStatus status)
        {
            using (var task = new UpdateLibraryTask(status))
            {
                task.InitializeComponent(this.Core);
                await this.OnBackgroundTask(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }

        public async Task SetRating(LibraryHierarchyNode libraryHierarchyNode, byte rating)
        {
            using (var task = new UpdateLibraryRatingTask(libraryHierarchyNode, rating))
            {
                task.InitializeComponent(this.Core);
                await this.OnBackgroundTask(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }

        protected virtual Task OnBackgroundTask(IBackgroundTask backgroundTask)
        {
            if (this.BackgroundTask == null)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            var e = new BackgroundTaskEventArgs(backgroundTask);
            this.BackgroundTask(this, e);
            return e.Complete();
        }

        public event BackgroundTaskEventHandler BackgroundTask;

        protected virtual void OnReport()
        {
            var report = new LibraryManagerReport(this.DatabaseFactory);
            report.InitializeComponent(this.Core);
            this.OnReport(report);
        }

        protected virtual Task OnReport(IReport Report)
        {
            if (this.Report == null)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            var e = new ReportEventArgs(Report);
            this.Report(this, e);
            return e.Complete();
        }

        public event ReportEventHandler Report;

        public class LibraryManagerReport : BaseComponent, IReport
        {
            public LibraryManagerReport(IDatabaseFactory databaseFactory)
            {
                this.DatabaseFactory = databaseFactory;
            }

            public IDatabaseFactory DatabaseFactory { get; private set; }

            public string Title
            {
                get
                {
                    return "Library Status";
                }
            }

            public string Description
            {
                get
                {
                    return string.Empty;
                }
            }

            public string[] Headers
            {
                get
                {
                    return new[]
                    {
                        "Path",
                        "Tracks"
                    };
                }
            }

            public IEnumerable<IReportRow> Rows
            {
                get
                {
                    using (var database = this.DatabaseFactory.Create())
                    {
                        using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                        {
                            var libraryRoots = database.Set<LibraryRoot>(transaction);
                            foreach (var libraryRoot in libraryRoots)
                            {
                                yield return this.GetRow(database, libraryRoot, transaction);
                            }
                        }
                    }
                }
            }

            protected virtual IReportRow GetRow(IDatabaseComponent database, LibraryRoot libraryRoot, ITransactionSource transaction)
            {
                var table = database.Tables.LibraryItem;
                var builder = database.QueryFactory.Build();
                builder.Output.AddFunction(QueryFunction.Count, builder.Output.CreateOperator(QueryOperator.Star));
                builder.Source.AddTable(table);
                builder.Filter.Add().With(binary =>
                {
                    binary.Left = binary.CreateColumn(table.Column("DirectoryName"));
                    binary.Operator = binary.CreateOperator(QueryOperator.Match);
                    binary.Right = binary.CreateConstant(libraryRoot.DirectoryName + "%");
                });
                var count = database.ExecuteScalar<long>(builder.Build(), transaction);
                return new ReportRow(libraryRoot.DirectoryName, count);
            }

            public Action<Guid> Action
            {
                get
                {
                    return key =>
                    {
                        //Nothing to do.
                    };
                }
            }

            private class ReportRow : IReportRow
            {
                public ReportRow(string directoryName, long count)
                {
                    this.DirectoryName = directoryName;
                    this.Count = count;
                }

                public string DirectoryName { get; private set; }

                public long Count { get; private set; }

                public Guid Id
                {
                    get
                    {
                        return Guid.Empty;
                    }
                }

                public string[] Values
                {
                    get
                    {
                        return new[]
                        {
                            this.DirectoryName,
                            Convert.ToString(this.Count)
                        };
                    }
                }
            }
        }
    }
}
