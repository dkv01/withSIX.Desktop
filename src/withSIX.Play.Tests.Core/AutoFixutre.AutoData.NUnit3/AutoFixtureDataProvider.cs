using System;
using System.Reflection;
using Ploeh.AutoFixture;

namespace AutoDataConnector
{
    public class AutoFixtureDataProvider : ITypedDataProvider
    {
        protected IFixture Fixture { get; }

        public AutoFixtureDataProvider(params ICustomization[] customizations)
        {
            Fixture = new Fixture();

            foreach (var c in customizations)
            {
                Fixture.Customize(c);
            }
        }

        public object CreateFrozenValue(Type type) => this.CallGeneric(type, "Freeze");

        public object CreateValue(Type type) => this.CallGeneric(type, "Create");

        private object CallGeneric(Type type, string methodName)
        {
            var methodInfo = this.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);

            var generic = methodInfo.MakeGenericMethod(type);

            return generic.Invoke(this, null);
        }

        private T Create<T>()
        {
            return this.Fixture.Create<T>();
        }

        private T Freeze<T>() => this.Fixture.Freeze<T>();
    }
}