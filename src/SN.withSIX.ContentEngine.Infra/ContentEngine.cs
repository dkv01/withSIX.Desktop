// <copyright company="SIX Networks GmbH" file="ContentEngine.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.IO;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;

using SN.withSIX.ContentEngine.Core;
using SN.withSIX.ContentEngine.Infra.Services;
using SN.withSIX.ContentEngine.Infra.UseCases;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Infra.Services;
using SN.withSIX.Core.Logging;

namespace SN.withSIX.ContentEngine.Infra
{
    public class ContentEngine : IContentEngine, IInfrastructureService, IDisposable
    {
        const string Scripts_Folder = @"TSScripts";
        readonly V8ScriptEngine _engine;
        readonly object _lock = new object();
        readonly ILogger _logger;
        readonly ICEResourceService _resourceService;
        readonly IModScriptRegistry _scriptRegistry;
        readonly IServiceRegistry _serviceRegistry;
        ExpectedMod _expectedMod;

        public ContentEngine(IServiceRegistry serviceRegistry, IModScriptRegistry scriptRegistry,
            ICEResourceService resourceService) {
            _serviceRegistry = serviceRegistry;
            _scriptRegistry = scriptRegistry;
            _resourceService = resourceService;
            _logger = MainLog.Logger;
            _engine = new V8ScriptEngine(V8ScriptEngineFlags.EnableDebugging);

            RegisterServices();
            RegisterDefaultTypes();
        }

        public IModS LoadModS(IContentEngineContent mod, bool overrideMod = false) {
            if (!ModHasScript(mod))
                throw new Exception("This mod does not have a Script");
            RegisteredMod loadedMod;
            if (_scriptRegistry.TryGetMod(mod.NetworkId, out loadedMod)) {
                if (overrideMod)
                    loadedMod.Mod = mod;
                return loadedMod.ModScript;
            }
            var modS = LoadModSFromStream(mod.NetworkId);
            FinalizeLoadMod(mod, modS);
            return modS;
        }

        public bool ModHasScript(IContentEngineContent mod) => ModHasScript(mod.NetworkId);

        public bool ModHasScript(Guid guid) => _resourceService.ResourceExists(GetScriptPath(guid));

        public void Dispose() {
            _engine?.Dispose();
        }

        void RegisterServices() {
            //TODO: Could totally automate thiss at ssome point now, probably move the automation into the serviceRegistry.
            _serviceRegistry.RegisterService<ITeamspeakService>();
            _serviceRegistry.RegisterService<IGameFolderService>();
        }

        void RegisterDefaultTypes() {
            _engine.AddHostObject("GlobalFunctions", HostItemFlags.GlobalMembers,
                new GlobalFunctions(this).Proxify<GlobalFunctions, IGlobalFunctions_TS>(this));
            _engine.AddHostObject("Logger", _logger);
        }

        IModS LoadModSFromFile(Guid guid) {
            RegisteredMod loadedMod;
            if (_scriptRegistry.TryGetMod(guid, out loadedMod))
                return loadedMod.ModScript;

            lock (_lock) {
                try {
                    if (_expectedMod != null) {
                        throw new Exception(
                            "The ContentEngineService can not currently handle more than one script registartion at a time.");
                    }
                    _expectedMod = new ExpectedMod(guid);
                    _engine.Execute(File.ReadAllText(GetScriptPath(guid)));
                    var modS = _expectedMod.Mod;
                    if (modS == null)
                        throw new Exception("Mod being loaded failed to register itself.");
                    return modS;
                } finally {
                    _expectedMod = null;
                }
            }
        }

        IModS LoadModSFromStream(Guid guid) {
            RegisteredMod loadedMod;
            if (_scriptRegistry.TryGetMod(guid, out loadedMod))
                return loadedMod.ModScript;

            lock (_lock) {
                try {
                    if (_expectedMod != null) {
                        throw new Exception(
                            "The ContentEngineService can not currently handle more than one script registartion at a time.");
                    }
                    _expectedMod = new ExpectedMod(guid);
                    using (var streamR = new StreamReader(_resourceService.GetResource(GetScriptPath(guid))))
                        _engine.Execute(streamR.ReadToEnd());

                    var modS = _expectedMod.Mod;
                    if (modS == null)
                        throw new Exception("Mod being loaded failed to register itself.");
                    return modS;
                } finally {
                    _expectedMod = null;
                }
            }
        }

        static string GetScriptPath(Guid guid) => Scripts_Folder + @"\" + guid + ".js";

        void FinalizeLoadMod(IContentEngineContent mod, IModS modS) {
            var registeredMod = new RegisteredMod(mod.NetworkId, mod, modS);
            _scriptRegistry.RegisterMod(registeredMod);
        }

        internal void RegisterMod(Guid guid, IModS mod) {
            if (_expectedMod == null)
                throw new ArgumentException("Content Engine was not expecting a mod to be registered", nameof(guid));
            if (_expectedMod.Guid != guid) {
                throw new ArgumentException(
                    "The mod ID being registered was not expected. Recieved: ({0}) Expected: ({1})".FormatWith(guid,
                        _expectedMod.Guid), nameof(guid));
            }
            _expectedMod.SetMod(mod);
        }

        internal object GetService(string name, string token) => _serviceRegistry.GetServiceForScript(name, token);
    }

    class ExpectedMod
    {
        internal ExpectedMod(Guid guid) {
            Guid = guid;
        }

        public Guid Guid { get; }
        public IModS Mod { get; private set; }

        internal void SetMod(IModS mod) {
            if (Mod != null)
                throw new ArgumentException("The registation for this mod has already been completed.", nameof(mod));
            Mod = mod;
        }
    }

    public class RegisteredMod
    {
        internal RegisteredMod(Guid guid, IContentEngineContent mod, IModS modScript) {
            Guid = guid;
            Mod = mod;
            ModScript = modScript;
            AccessToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray().Combine(guid.ToByteArray()));
            ModScript.setToken(AccessToken);
        }

        public Guid Guid { get; }
        public IContentEngineContent Mod { get; internal set; }
        public IModS ModScript { get; }
        string AccessToken { get; }
        internal bool Unloaded { get; }
        public event Action<RegisteredMod> OnModUnloaded;

        internal void UnloadScript() {
            if (OnModUnloaded != null)
                OnModUnloaded.Invoke(this);
        }

        public string GetAccessToken(bool force = false) {
            if (Unloaded && !force)
                throw new ModTokenInvalidatedException(AccessToken);
            return AccessToken;
        }
    }


    class RegisteredService
    {
        internal RegisteredService(string name, bool restricted, Func<string, object> creationFunc) {
            Name = name;
            Restricted = restricted;
            CreationFunc = creationFunc;
        }

        public string Name { get; }
        public bool Restricted { get; }
        public Func<string, object> CreationFunc { get; }
    }
}