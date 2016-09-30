// <copyright company="SIX Networks GmbH" file="BundleList.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using ReactiveUI;
using withSIX.Api.Models;
using withSIX.Play.Core.Glue.Helpers;

namespace withSIX.Play.Core.Games.Legacy
{
    public class BundleList : SelectionList<BundleItem>
    {
        readonly RepositoryHandler _handler;

        public BundleList(RepositoryHandler handler) {
            _handler = handler;
        }

        public void ProcessBundles() {
            var repository = _handler.Repository;
            var cm = _handler.BundleManager;

            Items.Clear();
            if (repository == null)
                return;
            var dic = cm.GetBundlesAsVersions(_handler.Remote);
            Items.AddRange(dic.Select(
                x =>
                    new BundleItem(x.Key, _handler, new ReactiveList<SpecificVersion>(x.Value))).ToArray());
        }
    }
}