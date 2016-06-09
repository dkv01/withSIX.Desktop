// <copyright company="SIX Networks GmbH" file="AutoMapperWrapper.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using AutoMapper;
using SN.withSIX.Core.Applications.Services;

namespace SN.withSIX.Core.Infra.Services
{
    abstract class AutoMapperWrapper : IAMapper
    {
        protected IMapper Engine;

        public TDestination Map<TDestination>(object source) => Engine.Map<TDestination>(source);

        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
            => Engine.Map(source, destination);

        public TDestination Map<TDestination>(object source, Action<IMappingOperationOptions> opts)
            => Engine.Map<TDestination>(source, opts);

        public object Map(object source, Type sourceType, Type destinationType)
            => Engine.Map(source, sourceType, destinationType);

        public object Map(object source, Type sourceType, Type destinationType, Action<IMappingOperationOptions> opts)
            => Engine.Map(source, sourceType, destinationType, opts);

        public object Map(object source, object destination, Type sourceType, Type destinationType)
            => Engine.Map(source, destination, sourceType, destinationType);

        public object Map(object source, object destination, Type sourceType, Type destinationType,
            Action<IMappingOperationOptions> opts) => Engine.Map(source, destination, sourceType, destinationType, opts);
    }
}