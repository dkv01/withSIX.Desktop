// <copyright company="SIX Networks GmbH" file="GameSettingsViewModelFactory.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Applications.Services;
using withSIX.Core.Infra.Services;
using withSIX.Mini.Applications.Factories;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Infra.Data.Services
{
    public class GameSettingsViewModelFactory : IGameSettingsViewModelFactory, IInfrastructureService
    {
        private readonly IAssemblyService _ass;
        readonly Type _gameSettingsType = typeof(GameSettings);
        readonly IDictionary<Type, Type> _apiModelRegistry;

        public GameSettingsViewModelFactory(IAssemblyService ass) {
            _ass = ass;
            _apiModelRegistry = GetApiModelTypeRegistry();
        }

        public IGameSettingsApiModel CreateApiModel(Game game) {
            var sourceType = game.Settings.GetType();
            var settingsTabViewModel =
                (GameSettingsApiModel) game.Settings.MapTo(sourceType, _apiModelRegistry[sourceType]);
            settingsTabViewModel.StartupLine = game.Settings.StartupParameters.StartupLine;
            settingsTabViewModel.Id = game.Id;
            return settingsTabViewModel;
        }

        IDictionary<Type, Type> GetViewModelTypeRegistry()
            => GetGameSettingsTypes().ToDictionary(x => x, GetViewModelType);

        IDictionary<Type, Type> GetApiModelTypeRegistry()
            => GetGameSettingsTypes().ToDictionary(x => x, GetApiModelType);

        // We expect a convention where the settings exist in the same assembly as the game, but in the ViewModels namespace, and are {GameSettingsClassName}ViewModel
        static Type GetViewModelType(Type x) {
            var typeName = MapToViewModelTypeName(x);
            var type = x.GetTypeInfo().Assembly.GetType(typeName);
            if (type == null)
                throw new InvalidOperationException("Cannot find the ViewModelType required for " + x);
            return type;
        }

        static Type GetApiModelType(Type x) {
            var typeName = MapToApiModelTypeName(x);
            var type = x.GetTypeInfo().Assembly.GetType(typeName);
            if (type == null)
                throw new InvalidOperationException("Cannot find the ViewModelType required for " + x);
            return type;
        }

        static string MapToViewModelTypeName(Type x) => x.FullName
            .Replace(".Models", ".ViewModels")
            .Replace("GameSettings", "GameSettingsViewModel");

        static string MapToApiModelTypeName(Type x) => x.FullName
            .Replace(".Models", ".ApiModels")
            .Replace("GameSettings", "GameSettingsApiModel");

        IEnumerable<Type> GetGameSettingsTypes() => _ass.GetAllAssemblies()
            .Where(x => x.GetName().Name.StartsWith("SN.withSIX.Mini.Plugin."))
            .SelectMany(x => _ass.GetTypes(x))
            .Where(IsGameSettingsType);

        Type[] GetTypesSafe(Assembly x) {
            try {
                return _ass.GetTypes(x);
            } catch (ReflectionTypeLoadException) {
                return new Type[0];
            }
        }

        bool IsGameSettingsType(Type x) {
            var ti = x.GetTypeInfo();
            return !ti.IsInterface && !ti.IsAbstract && _gameSettingsType.IsAssignableFrom(x);
        }
    }
}