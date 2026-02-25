using System;
using System.Collections.Generic;
using System.Text;

namespace SqlAgent.Domain.Entities
{
    public class Metric
    {
        public Guid Id { get; set; }
        public Guid VersionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Formula { get; set; } = string.Empty;

        public ProfileVersion Version { get; set; } = null!;
    }
}
