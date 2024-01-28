﻿using FoxDb;
using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class AddPathsToLibraryTask : LibraryTaskBase
    {
        public const string ID = "972222C8-8F6E-44CF-8EBE-DA4FCFD7CD80";

        public AddPathsToLibraryTask(IEnumerable<string> paths)
            : base(ID)
        {
            this.Paths = paths;
        }

        public override bool Visible
        {
            get
            {
                return true;
            }
        }

        public IEnumerable<string> Paths { get; private set; }

        public ICore Core { get; private set; }

        public IPlaybackManager PlaybackManager { get; private set; }

        public IMetaDataSourceFactory MetaDataSourceFactory { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.PlaybackManager = core.Managers.Playback;
            this.MetaDataSourceFactory = core.Factories.MetaDataSource;
            base.InitializeComponent(core);
        }

        protected override async Task OnRun()
        {
            using (var transaction = this.Database.BeginTransaction())
            {
                this.AddLibraryItems(transaction);
                this.AddOrUpdateMetaData(transaction);
                this.SetLibraryItemsStatus(transaction);
                transaction.Commit();
            }
            await this.SignalEmitter.Send(new Signal(this, CommonSignals.LibraryUpdated));
        }

        private void AddLibraryItems(ITransactionSource transaction)
        {
            this.Name = "Getting file list";
            this.IsIndeterminate = true;
            var query = this.Database.QueryFactory.Build();
            query.Add.AddColumn(this.Database.Tables.LibraryItem.Column("DirectoryName"));
            query.Add.AddColumn(this.Database.Tables.LibraryItem.Column("FileName"));
            query.Add.AddColumn(this.Database.Tables.LibraryItem.Column("Status"));
            query.Add.SetTable(this.Database.Tables.LibraryItem);
            query.Output.AddParameter("DirectoryName", DbType.String, 0, 0, 0, ParameterDirection.Input, false, null, DatabaseQueryParameterFlags.None);
            query.Output.AddParameter("FileName", DbType.String, 0, 0, 0, ParameterDirection.Input, false, null, DatabaseQueryParameterFlags.None);
            query.Output.AddParameter("Status", DbType.Byte, 0, 0, 0, ParameterDirection.Input, false, null, DatabaseQueryParameterFlags.None);
            query.Filter.Expressions.Add(
                query.Filter.CreateUnary(
                    QueryOperator.Not,
                    query.Filter.CreateFunction(
                        QueryFunction.Exists,
                        query.Filter.CreateSubQuery(
                            this.Database.QueryFactory.Build().With(subQuery =>
                            {
                                subQuery.Output.AddOperator(QueryOperator.Star);
                                subQuery.Source.AddTable(this.Database.Tables.LibraryItem);
                                subQuery.Filter.AddColumn(this.Database.Tables.LibraryItem.Column("FileName"));
                            })
                        )
                    )
                )
            );
            using (var command = this.Database.CreateCommand(query.Build(), transaction))
            {
                var addLibraryItem = new Action<string>(fileName =>
                {
                    if (!this.PlaybackManager.IsSupported(fileName))
                    {
                        return;
                    }
                    command.Parameters["directoryName"] = Path.GetDirectoryName(fileName);
                    command.Parameters["fileName"] = fileName;
                    command.Parameters["status"] = LibraryItemStatus.Import;
                    var count = command.ExecuteNonQuery();
                    if (count != 0)
                    {
                        Logger.Write(this, LogLevel.Debug, "Added file to library: {0}", fileName);
                    }
                    else
                    {
                        Logger.Write(this, LogLevel.Debug, "Skipped adding file to library: {0}", fileName);
                    }
                });
                foreach (var path in this.Paths)
                {
                    if (Directory.Exists(path))
                    {
                        foreach (var fileName in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
                        {
                            addLibraryItem(fileName);
                        }
                    }
                    else if (File.Exists(path))
                    {
                        addLibraryItem(path);
                    }
                }
            }
        }

        private void AddOrUpdateMetaData(ITransactionSource transaction)
        {
            using (var metaDataPopulator = new MetaDataPopulator(this.Database, transaction, this.Database.Queries.AddLibraryMetaDataItems, true))
            {
                var query = this.Database
                    .AsQueryable<LibraryItem>(this.Database.Source(new DatabaseQueryComposer<LibraryItem>(this.Database)))
                    .Where(libraryItem => libraryItem.Status == LibraryItemStatus.Import && !libraryItem.MetaDatas.Any());
                metaDataPopulator.InitializeComponent(this.Core);
                metaDataPopulator.NameChanged += (sender, e) => this.Name = metaDataPopulator.Name;
                metaDataPopulator.DescriptionChanged += (sender, e) => this.Description = metaDataPopulator.Description;
                metaDataPopulator.PositionChanged += (sender, e) => this.Position = metaDataPopulator.Position;
                metaDataPopulator.CountChanged += (sender, e) => this.Count = metaDataPopulator.Count;
                metaDataPopulator.Populate(query);
            }
        }

        private void SetLibraryItemsStatus(ITransactionSource transaction)
        {
            this.IsIndeterminate = true;
            var query = this.Database.QueryFactory.Build();
            query.Update.SetTable(this.Database.Tables.LibraryItem);
            query.Update.AddColumn(this.Database.Tables.LibraryItem.Column("Status"));
            this.Database.Execute(query, (parameters, phase) =>
            {
                switch (phase)
                {
                    case DatabaseParameterPhase.Fetch:
                        parameters["status"] = LibraryItemStatus.None;
                        break;
                }
            }, transaction);
        }
    }
}
