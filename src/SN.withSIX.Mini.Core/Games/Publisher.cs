// <copyright company="SIX Networks GmbH" file="Publisher.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;
using withSIX.Api.Models.Content;

namespace SN.withSIX.Mini.Core.Games
{
    [DataContract]
    public class ContentPublisher
    {
        public ContentPublisher(Publisher publisher, string publisherId) {
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(publisherId));
            Publisher = publisher;
            PublisherId = publisherId;
        }

        [DataMember]
        public Publisher Publisher { get; }
        [DataMember]
        public string PublisherId { get; }
    }
}