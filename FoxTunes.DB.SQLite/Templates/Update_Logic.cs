﻿using System.Collections.Generic;

namespace FoxTunes.Templates
{
    public partial class Update
    {
        public Update(string table, IEnumerable<string> fields)
        {
            this.Table = table;
            this.Fields = fields;
        }

        public string Table { get; private set; }

        public IEnumerable<string> Fields { get; private set; }
    }
}
