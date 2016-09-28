using System;
using System.Reflection;

namespace SN.withSIX.Mini.Infra.Data.Services
{
    public interface IAssemblyService
    {
        Assembly[] GetAllAssemblies();
        Type[] GetTypes(Assembly assembly);
    }
}