using System;
using System.Collections.Generic;
using System.Text;

namespace SqlAgent.Domain.Entities
{
    public class Relationship
    {
        public Guid Id { get; set; }
        public Guid VersionId { get; set; }
        public string SourceLogicalName { get; set; } = null!;
        public string TargetLogicalName { get; set; } = null!;
        public string JoinCondition { get; set; } = null!;
        public string Multiplicity { get; set; } = "1:N";

        public ProfileVersion Version { get; set; } = null!;
    }
}
