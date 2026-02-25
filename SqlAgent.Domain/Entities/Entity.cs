using System;
using System.Collections.Generic;
using System.Text;

namespace SqlAgent.Domain.Entities
{
    public class Entity
    {
        public Guid Id { get; set; }
        public Guid VersionId { get; set; }
        public string LogicalName { get; set; } = string.Empty;
        public string PhysicalName { get; set; } = string.Empty;
        public string Alias { get; set; } = string.Empty;
        public string Category { get; set; } = EntityCategory.Dimension;
        public string DefaultGrainFields { get; set; } = string.Empty;

        public ProfileVersion Version { get; set; } = null!;
        public ICollection<Field> Fields { get; set; } = new List<Field>();
    }
}
