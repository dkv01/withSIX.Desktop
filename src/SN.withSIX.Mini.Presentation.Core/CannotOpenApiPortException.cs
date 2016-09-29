using System;

namespace SN.withSIX.Mini.Presentation.Core
{
    public class CannotOpenApiPortException : Exception
    {
        public CannotOpenApiPortException(string message) : base(message) {}
        public CannotOpenApiPortException(string message, Exception ex) : base(message, ex) {}
    }
}