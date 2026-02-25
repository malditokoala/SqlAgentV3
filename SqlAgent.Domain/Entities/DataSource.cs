using System;
using System.Collections.Generic;
using System.Text;

namespace SqlAgent.Domain.Entities
{
    public class DataSource
    {
        public Guid Id { get; set; }
        public Guid VersionId { get; set; }
        public string ConnectionStringName { get; set; } = string.Empty;
        public string Engine { get; set; } = "SqlServer";

        public ProfileVersion Version { get; set; } = null!;

    }
}
