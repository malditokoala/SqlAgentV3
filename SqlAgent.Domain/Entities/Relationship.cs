using System;
using System.Collections.Generic;
using System.Text;

namespace SqlAgent.Domain.Entities
{
    public class Relationship
    {
        public Guid Id { get; set; }
        public Guid VersionId { get; set; }
        public string FromEntityLogical { get; set; } = string.Empty;
        public string FromFieldLogical { get; set; } = string.Empty;
        public string ToEntityLogical { get; set; } = string.Empty;
        public string ToFieldLogical { get; set; } = string.Empty;
        public string JoinType { get; set; } = "INNER";
        public bool IsApproved { get; set; } = true;
        // ← JoinCondition eliminado

        public ProfileVersion Version { get; set; } = null!;
    }
    
}
