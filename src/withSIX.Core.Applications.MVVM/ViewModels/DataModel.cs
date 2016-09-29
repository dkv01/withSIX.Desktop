// <copyright company="SIX Networks GmbH" file="DataModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using withSIX.Api.Models.Content.v3;

namespace withSIX.Core.Applications.MVVM.ViewModels
{
    public abstract class DataModel : ReactiveValidatableObject {}

    public abstract class DataModel<TId> : DataModel, IHaveId<TId>
    {
        public TId Id { get; set; }
    }
}