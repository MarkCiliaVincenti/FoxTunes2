﻿using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class ReleaseLookupReport : ReportComponent
    {
        public ReleaseLookupReport(Discogs.ReleaseLookup[] releaseLookups)
        {
            this.ReleaseLookups = releaseLookups;
        }

        public Discogs.ReleaseLookup[] ReleaseLookups { get; private set; }

        public IUserInterface UserInterface { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.UserInterface = core.Components.UserInterface;
            base.InitializeComponent(core);
        }

        public override string Title
        {
            get
            {
                return Strings.ReleaseLookupReport_Title;
            }
        }

        public override string Description
        {
            get
            {
                return string.Join(
                    Environment.NewLine,
                    this.ReleaseLookups.Select(
                        releaseLookup => this.GetDescription(releaseLookup)
                    )
                );
            }
        }

        protected virtual string GetDescription(Discogs.ReleaseLookup releaseLookup)
        {
            var builder = new StringBuilder();
            builder.AppendFormat("{0} - {1}", releaseLookup.Artist, releaseLookup.Album);
            if (releaseLookup.Status != Discogs.ReleaseLookupStatus.Complete && releaseLookup.Errors.Any())
            {
                builder.AppendLine(" -> Error");
                foreach (var error in releaseLookup.Errors)
                {
                    builder.AppendLine('\t' + error);
                }
            }
            else
            {
                builder.AppendLine(" -> OK");
            }
            return builder.ToString();
        }

        public override string[] Headers
        {
            get
            {
                return new[]
                {
                    Strings.ReleaseLookupReport_Album,
                    Strings.ReleaseLookupReport_Artist,
                    Strings.ReleaseLookupReport_Status,
                    Strings.ReleaseLookupReport_Release
                };
            }
        }

        public override IEnumerable<IReportComponentRow> Rows
        {
            get
            {
                return this.ReleaseLookups.Select(releaseLookup => new ReleaseLookupReportRow(this, releaseLookup));
            }
        }

        public class ReleaseLookupReportRow : ReportComponentRow
        {
            public ReleaseLookupReportRow(ReleaseLookupReport report, Discogs.ReleaseLookup releaseLookup)
            {
                this.Report = report;
                this.ReleaseLookup = releaseLookup;
            }

            public ReleaseLookupReport Report { get; private set; }

            public Discogs.ReleaseLookup ReleaseLookup { get; private set; }

            public override string[] Values
            {
                get
                {
                    var url = default(string);
                    if (this.ReleaseLookup.Release != null)
                    {
                        url = this.ReleaseLookup.Release.Url;
                    }
                    return new[]
                    {
                        this.ReleaseLookup.Artist,
                        this.ReleaseLookup.Album,
                        Enum.GetName(typeof(Discogs.ReleaseLookupStatus), this.ReleaseLookup.Status),
                        url
                    };
                }
            }

            public override Task<bool> Action()
            {
                var url = new Uri(new Uri("https://www.discogs.com"), this.ReleaseLookup.Release.Url).ToString();
                this.Report.UserInterface.OpenInShell(url);
                return base.Action();
            }
        }
    }
}
