// <copyright company="SIX Networks GmbH" file="Publisher.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;
using withSIX.Api.Models.Content;

namespace withSIX.Mini.Core.Games
{
    [DataContract]
    public class ContentPublisher : IEquatable<ContentPublisher>
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

        public bool Equals(ContentPublisher other) => (other != null) &&
                                                      (ReferenceEquals(this, other) ||
                                                       ((other.Publisher == Publisher) &&
                                                        (other.PublisherId == PublisherId)));

        public override int GetHashCode() {
            unchecked {
                return ((int) Publisher*397) ^ (PublisherId?.GetHashCode() ?? 0);
            }
        }

        public override bool Equals(object obj) => Equals(obj as ContentPublisher);
    }
}