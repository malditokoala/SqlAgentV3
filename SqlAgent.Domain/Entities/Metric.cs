using System;
using System.Collections.Generic;
using System.Text;

namespace SqlAgent.Domain.Entities
{
    public class Metric
    {
        public Guid Id { get; set; }
        public Guid VersionId { get; set; }
        public string LogicalName { get; set; } = string.Empty; // "Revenue"
        public string ExpressionAst { get; set; } = string.Empty; // JSON del árbol
        public string Alias { get; set; } = string.Empty; // "TotalRevenue"
        public string ValidAggregations { get; set; } = "SUM,AVG";

        public ProfileVersion Version { get; set; } = null!;
    }
}
