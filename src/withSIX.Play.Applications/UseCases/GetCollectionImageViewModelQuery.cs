// <copyright company="SIX Networks GmbH" file="GetCollectionImageViewModelQuery.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using ReactiveUI;
using MediatR;
using withSIX.Play.Applications.Extensions;
using withSIX.Play.Applications.ViewModels.Games.Dialogs;
using withSIX.Play.Core.Games.Legacy;
using withSIX.Api.Models.Extensions;

namespace withSIX.Play.Applications.UseCases
{
    public class GetCollectionImageViewModelQuery : IRequest<CollectionImageViewModel>
    {
        public GetCollectionImageViewModelQuery(Guid collectionId) {
            CollectionId = collectionId;
        }

        public Guid CollectionId { get; }
    }


    public class GetCollectionImageViewModelQueryHandler :
        IRequestHandler<GetCollectionImageViewModelQuery, CollectionImageViewModel>
    {
        readonly IContentManager _contentList;
        readonly Func<CollectionImageViewModel> _factory;

        public GetCollectionImageViewModelQueryHandler(Func<CollectionImageViewModel> factory,
            IContentManager contentList) {
            _factory = factory;
            _contentList = contentList;
        }

        public CollectionImageViewModel Handle(GetCollectionImageViewModelQuery request) {
            var collection = _contentList.CustomCollections.First(x => x.Id == request.CollectionId);
            var vm = _factory();
            var collectionImageDataModel = collection.MapTo<CollectionImageDataModel>();

            // TODO: We might want to dispose of this sometime?
            collection.WhenAnyValue(x => x.Image)
                .Subscribe(x => collectionImageDataModel.Image = x);

            vm.SetContent(collectionImageDataModel);
            return vm;
        }
    }
}