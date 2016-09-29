using System.Linq;
using NUnit.Framework.Interfaces;

namespace AutoDataConnector
{
    public class ParameterValueProvider : IParameterValueProvider
    {
        private readonly ITypedDataProvider _typedDataProvider;

        public ParameterValueProvider(ITypedDataProvider typedDataProvider)
        {
            this._typedDataProvider = typedDataProvider;
        }

        public object Get(IParameterInfo parameterInfo) => IsFrozen(parameterInfo)
    ? this._typedDataProvider.CreateFrozenValue(parameterInfo.ParameterType)
    : this._typedDataProvider.CreateValue(parameterInfo.ParameterType);

        private static bool IsFrozen(IReflectionInfo reflectionInfo) => reflectionInfo.GetCustomAttributes<FrozenAttribute>(true).Any();
    }
}