// <copyright company="SIX Networks GmbH" file="ImageCacheManager.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using Akavache;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Infra.Services;
using Splat;

namespace SN.withSIX.Core.Infra.Cache
{
    public class ImageCacheManager : IImageCacheManager, IInfrastructureService
    {
        readonly IImageCache _cache;

        public ImageCacheManager(IImageCache cache) {
            _cache = cache;
        }

        public IObservable<IBitmap> GetImage(Uri uri, DesiredImageSize desiredDimensions)
            => _cache.LoadImageFromUrl(GetDimensionKey(uri, desiredDimensions), uri.ToString(), false,
                desiredDimensions.Width, desiredDimensions.Height);

        public IObservable<IBitmap> GetImage(Uri uri, TimeSpan offset, DesiredImageSize desiredDimensions) {
            var url = uri.ToString();
            return _cache.LoadImageFromUrl(GetDimensionKey(uri, desiredDimensions), url, false, desiredDimensions.Width,
                desiredDimensions.Height,
                offset.GetAbsoluteUtc());
        }

        public IObservable<IBitmap> GetImage(Uri uri) => _cache.LoadImageFromUrl(uri.ToString());

        public IObservable<IBitmap> GetImage(Uri uri, TimeSpan offset)
            => _cache.LoadImageFromUrl(uri.ToString(), false, null, null, offset.GetAbsoluteUtc());

        static string GetDimensionKey(Uri uri, DesiredImageSize desiredDimensions)
            => uri + "??dimensions=" + desiredDimensions;
    }
}