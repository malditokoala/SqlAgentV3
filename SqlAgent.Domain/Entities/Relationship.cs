using System;
using System.Collections.Generic;
using System.Text;

namespace SqlAgent.Domain.Entities
{
    public class Relationship
    {
        public Guid Id { get; set; }
        public Guid VersionId { get; set; }

        // Definición Lógica de la relación
        public string FromEntityLogical { get; set; } = string.Empty;
        public string FromFieldLogical { get; set; } = string.Empty;
        public string ToEntityLogical { get; set; } = string.Empty;
        public string ToFieldLogical { get; set; } = string.Empty;

        public string JoinType { get; set; } = "INNER";
        public bool IsApproved { get; set; } = true;

        // Propiedad calculada o guardada para compatibilidad (opcional)
        public string JoinCondition { get; set; } = string.Empty;

        public ProfileVersion Version { get; set; } = null!;
    }
    
}
