using System;
using System.Collections.Generic;
using System.Text;

namespace SqlAgent.Domain.Entities
{
    public class Entity
    {
        public Guid Id { get; set; }
        public Guid VersionId { get; set; }
        public string LogicalName { get; set; } = null!;
        public string PhysicalName { get; set; } = null!;

        public ProfileVersion Version { get; set; } = null!;
        public ICollection<Field> Fields { get; set; } = new List<Field>();
    }
}
