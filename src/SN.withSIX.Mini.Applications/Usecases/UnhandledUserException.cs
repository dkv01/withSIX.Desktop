using System;

namespace SN.withSIX.Mini.Applications.Usecases
{
    public class UnhandledUserException : Exception
    {
        public UnhandledUserException(string s, Exception exception) : base(s, exception) {}
    }
}