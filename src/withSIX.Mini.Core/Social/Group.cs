// <copyright company="SIX Networks GmbH" file="Group.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using withSIX.Core;
using withSIX.Core.Extensions;
using withSIX.Sync.Core.Legacy.SixSync;
using withSIX.Sync.Core.Legacy.Status;
using withSIX.Sync.Core.Transfer;

namespace withSIX.Mini.Core.Social
{
    [DataContract]
    public class Group : BaseEntityGuidId
    {
        // TODO: https
        static readonly Uri baseHost = new Uri("https://m-groups.withsix.com/groups/");

        public Group(Guid id, string name) {
            Id = id;
            Name = name;
        }

        [DataMember]
        public GroupAccess Access { get; set; }

        [DataMember]
        public List<GroupContent> Content { get; set; }

        [DataMember]
        public string Name { get; set; }

        public async Task Load(IW6Api api, CancellationToken ct) {
            Access = await GetGroupAccess(api, ct).ConfigureAwait(false);
            Content = await GetGroupContent(api, ct).ConfigureAwait(false);
        }

        private Task<GroupAccess> GetGroupAccess(IW6Api api, CancellationToken ct) => api.GroupAccess(Id, ct);

        private Task<List<GroupContent>> GetGroupContent(IW6Api api, CancellationToken ct)
            => api.GroupContent(Id, ct);

        public bool HasMod(string name)
            => Content.Any(x => x.PackageName.Equals(name, StringComparison.CurrentCultureIgnoreCase));

        // TODO
        public bool ExistsAndIsRightVersion(string name, IAbsoluteDirectoryPath path) => false;

        public GroupContent GetMod(string name)
            => Content.First(x => x.PackageName.Equals(name, StringComparison.CurrentCultureIgnoreCase));

        // TODO: Id vs PackageName?
        public IReadOnlyCollection<Uri> GetHosts(GroupContent content)
            => new[] {new Uri(baseHost, Id.ToShortId() + "/" + content.GameId.ToShortId() + "/" + content.PackageName)};

        public async Task GetMod(GroupContent mod, IAbsoluteDirectoryPath destination, IAbsoluteDirectoryPath packPath,
            StatusRepo status, IAuthProvider provider, bool force = false) {
            var folder = destination.GetChildDirectoryWithName(mod.PackageName);

            if (!folder.Exists) {
                await InstallNew(mod, provider, GetOpts(packPath, status, mod), folder).ConfigureAwait(false);
                return;
            }

            var rsyncDir = folder.GetChildDirectoryWithName(Repository.RepoFolderName);
            if (!force && rsyncDir.Exists && IsRightVersion(rsyncDir, mod))
                return;
            await UpdateExisting(mod, provider, rsyncDir, folder, GetOpts(packPath, status, mod)).ConfigureAwait(false);
        }

        private async Task UpdateExisting(GroupContent mod, IAuthProvider provider, IAbsoluteDirectoryPath rsyncDir,
            IAbsoluteDirectoryPath folder, Dictionary<string, object> opts) {
            SetupHosts(mod, provider);
            var repo = GetRepo(rsyncDir, folder, opts);
            await repo.Update(opts).ConfigureAwait(false);
        }

        private async Task InstallNew(GroupContent mod, IAuthProvider provider, Dictionary<string, object> opts,
            IAbsoluteDirectoryPath folder) {
            SetupHosts(mod, provider);
            await
                Repository.Factory.Clone((List<Uri>) opts["hosts"], folder.ToString(), opts)
                    .ConfigureAwait(false);
        }

        private void SetupHosts(GroupContent mod, IAuthProvider provider) {
            foreach (var h in GetHosts(mod)) {
                provider.SetNonPersistentAuthInfo(h,
                    new AuthInfo(Access.UserName, Access.Password));
            }
        }

        // pff, better use a real param object!
        Dictionary<string, object> GetOpts(IAbsoluteDirectoryPath packPath, StatusRepo status,
            GroupContent mod) => new Dictionary<string, object> {
            {"hosts", GetHosts(mod).ToList()},
            //{"required_version", mod.Version}, // TODO
            //{"required_guid", @group.Id}, // TODO
            {"pack_path", packPath.GetChildFileWithName(mod.PackageName).ToString()},
            {"status", status}
        };

        static Repository GetRepo(IAbsoluteDirectoryPath rsyncDir, IAbsoluteDirectoryPath folder,
            Dictionary<string, object> opts) => rsyncDir.Exists
            ? Repository.Factory.Open(folder.ToString(), opts)
            : Repository.Factory.Convert(folder.ToString(), opts);

        // TODO
        private bool IsRightVersion(IAbsoluteDirectoryPath rsyncDir, GroupContent mod) => false;
    }

    [DataContract]
    public class GroupContent : BaseEntityGuidId
    {
        public GroupContent(Guid id) {
            Id = id;
        }

        [DataMember]
        public string PackageName { get; set; }
        [DataMember]
        public string Version { get; set; }
        [DataMember]
        public Guid GameId { get; set; }
    }

    [DataContract]
    public class GroupAccess
    {
        //[DataMember] public Guid GroupId { get; set; }
        [DataMember]
        public string UserName { get; set; }
        [DataMember]
        public string Password { get; set; }
    }
}