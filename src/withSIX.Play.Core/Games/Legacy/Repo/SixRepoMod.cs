// <copyright company="SIX Networks GmbH" file="SixRepoMod.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using withSIX.Core;
using withSIX.Core.Helpers;
using withSIX.Play.Core.Games.Legacy.Mods;
using withSIX.Sync.Core.Legacy;

namespace withSIX.Play.Core.Games.Legacy.Repo
{
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Sync.Core.Models.Repositories.SixSync"
        )]
    public class SixRepoMod : IBaseYaml, IHaveType<string>
    {
        public SixRepoMod() {
            Dependencies = new string[0];
            Aliases = new string[0];
        }

        [DataMember]
        public string Image { get; set; }
        [DataMember]
        public string ImageLarge { get; set; }
        [DataMember]
        public string Author { get; set; }
        public string[] Categories { get; set; }
        [DataMember]
        public long Version { get; set; }
        [DataMember]
        public string[] Dependencies { get; set; }
        [DataMember]
        public long Size { get; set; }
        [DataMember]
        public long WdSize { get; set; }
        [DataMember]
        public string[] Aliases { get; set; }
        [DataMember]
        public string FullName { get; set; }
        [DataMember]
        public string CppName { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public string Homepage { get; set; }
        [DataMember]
        public string Guid { get; set; }
        [DataMember]
        public string License { get; set; }
        [DataMember]
        public string LicenseUrl { get; set; }
        [DataMember]
        public DateTime UpdatedVersion { get; set; }
        [DataMember]
        public string ModVersion { get; set; }

        public string ToYaml() {
            throw new NotImplementedException();
        }

        [DataMember]
        public string Type { get; set; }

        public CustomRepoMod ToMod(string name, Network network, IEnumerable<Mod> networkMods) {
            var n = network;
            var type = Type;
            if (Type == null)
                type = "RvMod";
            else if (!Type.EndsWith("Mod"))
                type = Type + "Mod";

            var mod = ConstructMod(name, type, n,
                networkMods.FirstOrDefault(x => x.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase)));
            return mod;
        }

        CustomRepoMod ConstructMod(string name, string type, Network n, IMod existingMod) {
            var defaultGuid = System.Guid.Empty;
            return new CustomRepoMod(defaultGuid) {
                Name = name,
                FullName = FullName ?? (existingMod == null ? null : existingMod.FullName),
                CppName = CppName ?? (existingMod == null ? null : existingMod.CppName),
                Description = Description ?? (existingMod == null ? null : existingMod.Description),
                Aliases = GetAliasesWithFallback(existingMod),
                Dependencies = GetDependenciesWithFallback(existingMod),
                HomepageUrl = Homepage ?? (existingMod == null ? null : existingMod.HomepageUrl),
                Categories = GetCategoriesWithFallback(existingMod),
                Guid = Guid,
                Size = Size*FileSizeUnits.KB,
                Author = Author ?? (existingMod == null ? null : existingMod.Author),
                SizeWd = WdSize*FileSizeUnits.KB,
                Version = Version.ToString(),
                ModVersion = ModVersion,
                Type =
                    !string.IsNullOrWhiteSpace(type) || existingMod == null
                        ? (GameModType) Enum.Parse(typeof (GameModType), type)
                        : existingMod.Type,
                Image = Image ?? (existingMod == null ? null : existingMod.Image),
                ImageLarge = ImageLarge ?? (existingMod == null ? null : existingMod.ImageLarge),
                Networks = new[] {n}.ToList(),
                Mirrors = n.Mirrors.Select(x => x.Url).ToArray(),
                UpdatedVersion = UpdatedVersion,
                NetworkId = GetNetworkId(name, defaultGuid)
            };
        }

        Guid GetNetworkId(string name, Guid defaultGuid) {
            switch (name.ToUpper()) {
            case "@ACRE2":
                return System.Guid.Parse("efa97a10-29ef-11e4-864e-001517bd964c");
            case "@ACRE_A3":
                return System.Guid.Parse("efa97a10-29ef-11e4-864e-001517bd964c");
            case "@task_force_radio":
                return System.Guid.Parse("436b875a-56e3-11e3-ba1d-001517bd964c");
            case "@tfar_co_a2":
                return System.Guid.Parse("436b875a-56e3-11e3-ba1d-001517bd964c");
            case "@ACRE":
                return System.Guid.Parse("43a33648-962d-11df-8d10-001517bd964c");
            default:
                return defaultGuid;
            }
        }

        string[] GetCategoriesWithFallback(IMod existingMod) {
            string[] categories;
            if (Categories == null || !Categories.Any()) {
                var m = existingMod as Mod;
                if (m == null || m.Categories == null || !m.Categories.Any())
                    categories = new[] {Common.DefaultCategory}.ToArray();
                else
                    categories = m.Categories.ToArray();
            } else
                categories = Categories.ToArray();
            return categories;
        }

        string[] GetAliasesWithFallback(IMod existingMod) {
            string[] aliases;
            if (Aliases != null)
                aliases = Aliases.ToArray();
            else {
                aliases = existingMod != null && existingMod.Aliases != null
                    ? existingMod.Aliases.ToArray()
                    : new string[0];
            }
            return aliases;
        }

        string[] GetDependenciesWithFallback(IMod existingMod) {
            string[] dependencies;
            if (Dependencies != null)
                dependencies = Dependencies.ToArray();
            else {
                dependencies = existingMod != null && existingMod.Dependencies != null
                    ? existingMod.Dependencies.ToArray()
                    : new string[0];
            }
            return dependencies;
        }

        [OnDeserialized]
        protected void OnDeserialized(StreamingContext context) {
            if (Dependencies == null)
                Dependencies = new string[0];
            if (Categories == null)
                Categories = new string[0];
            if (Aliases == null)
                Aliases = new string[0];
        }
    }
}