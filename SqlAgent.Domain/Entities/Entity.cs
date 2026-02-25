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

        // Propiedades Arquitectura v3.0
        public string Alias { get; set; } = string.Empty; // e.g., "o", "od"
        public string Category { get; set; } = "fact"; // fact, dimension
        public string DefaultGrainFields { get; set; } = string.Empty; // e.g., "OrderId,ProductId"

        public ProfileVersion Version { get; set; } = null!;
        public ICollection<Field> Fields { get; set; } = new List<Field>();
    }
}
