using System;
using System.Collections.Generic;
using System.Text;

namespace SqlAgent.Domain.Entities
{
    public class Policy
    {
        public Guid Id { get; set; }
        public Guid VersionId { get; set; }
        public int MaxRowsDefault { get; set; } = 100;
        public int MaxRowsAbsolute { get; set; } = 1000;
        public int MaxJoinHops { get; set; } = 2;
        public int TimeoutSeconds { get; set; } = 30;
        public bool RequireFiltersForFacts { get; set; } = true;
        public string AllowedOperations { get; set; } = "list,count,aggregate,search";
        public string? GlobalDenyFields { get; set; }

        public ProfileVersion Version { get; set; } = null!;
    }
}
