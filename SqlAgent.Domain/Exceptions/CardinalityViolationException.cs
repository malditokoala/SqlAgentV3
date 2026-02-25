using System;
using System.Collections.Generic;
using System.Text;

namespace SqlAgent.Domain.Exceptions
{
    public class CardinalityViolationException : Exception
    {
        public CardinalityViolationException(string message) : base(message) { }
    }
}
