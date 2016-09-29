using System;

namespace AutoDataConnector
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class FrozenAttribute : Attribute
    {
    }
}