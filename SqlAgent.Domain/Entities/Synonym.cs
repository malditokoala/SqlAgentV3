using System;
using System.Collections.Generic;
using System.Text;

namespace SqlAgent.Domain.Entities
{
    public class Synonym
    {
        public Guid Id { get; set; }
        public Guid VersionId { get; set; }         // ← FK a ProfileVersion
        public string Term { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty; // "entity","field","metric"
        public string TargetLogicalName { get; set; } = string.Empty;
        public string? EntityContext { get; set; }
        public decimal Confidence { get; set; } = 1.0m;

        public ProfileVersion Version { get; set; } = null!;
    }
}
