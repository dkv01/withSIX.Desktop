using NUnit.Framework.Interfaces;

namespace AutoDataConnector
{
    public interface IParameterValueProvider
    {
        object Get(IParameterInfo parameterInfo);
    }
}