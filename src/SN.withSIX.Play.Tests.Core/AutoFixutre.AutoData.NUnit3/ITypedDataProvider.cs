using System;

namespace AutoDataConnector
{
    public interface ITypedDataProvider
    {
        object CreateFrozenValue(Type type);
        object CreateValue(Type type);
    }
}