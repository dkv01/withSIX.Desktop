// <copyright company="SIX Networks GmbH" file="ContentFolderLinkContext.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NDepend.Path;
using withSIX.Core;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Infra.Data.Services
{
    public class ContentFolderLinkContext : ContextBase<ContentFolderLink>, IContentFolderLinkContext
    {
        private readonly IAbsoluteFilePath _path;

        public ContentFolderLinkContext(IAbsoluteFilePath path) {
            _path = path;
        }

        public Task<ContentFolderLink> GetFolderLink() => Get();

        protected override async Task<ContentFolderLink> LoadInternal() => _path.Exists
            ? await LoadJsonFromFileAsync().ConfigureAwait(false)
            : new ContentFolderLink(new List<FolderInfo>());

        protected override Task SaveChangesInternal(ContentFolderLink loaded) {
            var dto = new ContentFolderLinkDTO {
                Folders = loaded.Infos.ToDictionary(x => x.Path.ToString(), x => x.ContentInfo)
            };
            return Task.Run(() => Tools.Serialization.Json.SaveJsonToDiskThroughMemory(dto, _path));
        }

        private async Task<ContentFolderLink> LoadJsonFromFileAsync() {
            var dto =
                await
                    Tools.Serialization.Json.LoadJsonFromFileAsync<ContentFolderLinkDTO>(_path)
                        .ConfigureAwait(false);
            return
                new ContentFolderLink(
                    dto.Folders.Select(x => new FolderInfo(x.Key.ToAbsoluteDirectoryPath(), x.Value)).ToList());
        }
    }

    public class ContentFolderLinkDTO
    {
        public Dictionary<string, ContentInfo> Folders { get; set; }
        //public Dictionary<Guid, FolderInfo2> FolderInfo2 { get; set; } = new Dictionary<Guid, FolderInfo2>();
    }

    /*    public class FolderInfo2
        {
            public IAbsoluteDirectoryPath Folder { get; set; }
            public Guid UserId { get; set; }
            public Guid GameId { get; set; }
        }*/
}