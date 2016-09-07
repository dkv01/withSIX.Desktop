// <copyright company="SIX Networks GmbH" file="ContentSpec.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;
using SN.withSIX.Core;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;
using SN.withSIX.Sync.Core.Legacy.Status;
using withSIX.Api.Models.Content.v3;

namespace SN.withSIX.Mini.Core.Games
{
    public interface IContentSpec<out T> where T : IContent
    {
        T Content { get; }
        string Constraint { get; set; }
    }

    [DataContract]
    public class ContentSpec<T> : IEquatable<ContentSpec<T>>, IContentSpec<T> where T : IContent
    {
        public ContentSpec(T content, string constraint = null) {
            Contract.Requires<ArgumentNullException>(content != null);
            Content = content;
            Constraint = constraint;
        }

        [DataMember]
        public T Content { get; }
        [DataMember]
        public string Constraint { get; set; }
        // TODO: Consider if ContentSpecs are equal even if Constraint doesn't matches, but Content matches.
        // As we only want a single entry even when Constraints differ, one might lean towards only checking the Content object??
        // If we keep the current implementation then:
        // - We can have the same Content multiple times processed
        // - JSON.NET does not properly detect self referencing problems

        // TODO: This would also be a problem for Entity Framework, etc, or not?
        public bool Equals(ContentSpec<T> other) => other?.GetHashCode() == GetHashCode();

        public override int GetHashCode() => HashCode.Start.Hash(Content).Hash(Constraint);

        public override bool Equals(object other) => Equals(other as ContentSpec<T>);

        //public ItemState GetState() => Content.GetState(Constraint);
    }

    public static class ContentSpecExtensions
    {
        public static ItemState GetState<T>(this IContentSpec<T> spec) where T : IContent
            => spec.Content.GetState(spec.Constraint);
    }

    [DataContract]
    public class ContentSpec : ContentSpec<Content>
    {
        public ContentSpec(Content content, string constraint = null) : base(content, constraint) {}
    }

    public interface IHaveOrder
    {
        int Order { get; }
    }

    [DataContract]
    public class OrderedContentSpec : ContentSpec, IHaveOrder
    {
        public OrderedContentSpec(Content content, string constraint = null, int order = -1) : base(content, constraint) {
            Order = order;
        }

        [DataMember]
        public int Order { get; protected set; }
    }

    [DataContract]
    public class CollectionContentsSpec : OrderedContentSpec
    {
        public CollectionContentsSpec(Content content, string constraint = null, int order = -1)
            : base(content, constraint, order) {}

        [DataMember]
        public bool IsOptional { get; set; }

        [DataMember]
        public ContentScope Scope { get; set; }
    }

    public enum ContentScope
    {
        All,
        Client,
        Server
    }

    [DataContract]
    public class LocalContentSpec : ContentSpec<LocalContent>
    {
        public LocalContentSpec(LocalContent content, string constraint = null) : base(content, constraint) {}
    }

    [DataContract]
    public class ModRepoContentSpec : ContentSpec<ModRepoContent>
    {
        public ModRepoContentSpec(ModRepoContent content, string constraint = null) : base(content, constraint) {}
    }

    [DataContract]
    public class NetworkContentSpec : ContentSpec<NetworkContent>
    {
        public NetworkContentSpec(NetworkContent content, string constraint = null) : base(content, constraint) {}
    }

    [DataContract]
    public class NetworkContentRelation : NetworkContentSpec
    {
        public NetworkContentRelation(NetworkContent content, NetworkContent self, string constraint = null)
            : base(content, constraint) {
            Self = self;
        }

        public NetworkContent Self { get; }
    }

    [DataContract]
    public class CollectionContentSpec : ContentSpec<Collection>
    {
        public CollectionContentSpec(Collection content, string constraint = null) : base(content, constraint) {}
    }

    [DataContract]
    public class PackagedContentSpec : ContentSpec<PackagedContent>
    {
        public PackagedContentSpec(PackagedContent content, string constraint = null) : base(content, constraint) {}
    }

    public class UninstallContentSpec : ContentSpec<IUninstallableContent>
    {
        public UninstallContentSpec(IUninstallableContent content, string constraint = null) : base(content, constraint) {}
    }


    public class InstallContentSpec : ContentSpec<IInstallableContent>
    {
        public InstallContentSpec(IInstallableContent content, string constraint = null) : base(content, constraint) {}
    }


    public interface IContentIdSpec<out T> : IHaveId<T>
    {
        string Constraint { get; }
    }

    [DataContract]
    public abstract class ContentIdSpec<T> : IContentIdSpec<T>, IEquatable<ContentIdSpec<T>>
    {
        protected ContentIdSpec(T id, string constraint = null) {
            Contract.Requires<ArgumentNullException>(id != null);
            Id = id;
            Constraint = constraint;
        }

        [DataMember]
        public string Constraint { get; }
        [DataMember]
        public T Id { get; }

        public bool Equals(ContentIdSpec<T> other) => other != null && (ReferenceEquals(this, other) ||
                                                                        EqualityComparer<T>.Default.Equals(other.Id, Id) &&
                                                                        other.Constraint == Constraint);

        public override bool Equals(object obj) => Equals(obj as ContentIdSpec<T>);

        public override int GetHashCode() {
            unchecked {
                return ((Constraint?.GetHashCode() ?? 0)*397) ^ EqualityComparer<T>.Default.GetHashCode(Id);
            }
        }
    }

    [DataContract]
    public class ContentGuidSpec : ContentIdSpec<Guid>
    {
        public ContentGuidSpec(Guid id, string constraint = null) : base(id, constraint) {}
    }

    [DataContract]
    public class ContentIntSpec : ContentIdSpec<ulong>
    {
        public ContentIntSpec(ulong id, string constraint = null) : base(id, constraint) {}
    }
}