using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Infra.Services;
using SN.withSIX.Mini.Applications.Factories;
using SN.withSIX.Mini.Core.Games;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Infra.Data.Services
{
    public class GameSettingsViewModelFactory : IGameSettingsViewModelFactory, IInfrastructureService
    {
        static readonly Type gameSettingsType = typeof (GameSettings);
        static readonly IDictionary<Type, Type> apiModelRegistry = GetApiModelTypeRegistry();

        public IGameSettingsApiModel CreateApiModel(Game game) {
            var sourceType = game.Settings.GetType();
            var settingsTabViewModel =
                (GameSettingsApiModel) game.Settings.MapTo(sourceType, apiModelRegistry[sourceType]);
            settingsTabViewModel.StartupLine = game.Settings.StartupParameters.StartupLine;
            settingsTabViewModel.Id = game.Id;
            return settingsTabViewModel;
        }

        static IDictionary<Type, Type> GetViewModelTypeRegistry()
            => GetGameSettingsTypes().ToDictionary(x => x, GetViewModelType);

        static IDictionary<Type, Type> GetApiModelTypeRegistry()
            => GetGameSettingsTypes().ToDictionary(x => x, GetApiModelType);

        // We expect a convention where the settings exist in the same assembly as the game, but in the ViewModels namespace, and are {GameSettingsClassName}ViewModel
        static Type GetViewModelType(Type x) {
            var typeName = MapToViewModelTypeName(x);
            var type = x.Assembly.GetType(typeName);
            if (type == null)
                throw new InvalidOperationException("Cannot find the ViewModelType required for " + x);
            return type;
        }

        static Type GetApiModelType(Type x) {
            var typeName = MapToApiModelTypeName(x);
            var type = x.Assembly.GetType(typeName);
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

        static IEnumerable<Type> GetGameSettingsTypes() => AppDomain.CurrentDomain.GetAssemblies()
            .Where(x => x.GetName().Name.StartsWith("SN.withSIX.Mini.Plugin."))
            .SelectMany(x => x.GetTypes())
            .Where(IsGameSettingsType);

        static Type[] GetTypesSafe(Assembly x) {
            try {
                return x.GetTypes();
            } catch (ReflectionTypeLoadException) {
                return new Type[0];
            }
        }

        static bool IsGameSettingsType(Type x)
            => !x.IsInterface && !x.IsAbstract && SystemExtensions.IsAssignableFrom(gameSettingsType, x);
    }
}