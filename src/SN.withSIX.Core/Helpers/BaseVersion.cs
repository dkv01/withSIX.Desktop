// <copyright company="SIX Networks GmbH" file="BaseVersion.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace SN.withSIX.Core.Helpers
{
    // TODO: Stronger validation of each element: Name, Version, Branch (Dependency has version constraint support though..)
    public abstract class BaseVersion : IEquatable<BaseVersion>
    {
        public static readonly Regex RxPackageName =
            new Regex(@"(.*)((\-\d+\.[\d\.]+)(\-\w+)|(\-\d+\.[\d\.]+))",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
        [NotNull]
        public string Name { get; protected set; }
        public virtual string Branch { get; protected set; }
        public string VersionData { get; protected set; }
        public virtual string DisplayName => VersionData;

        public bool Equals(BaseVersion other) => other != null && GetFullName().Equals(other.GetFullName());

        public abstract string GetFullName();

        public override bool Equals(object other) => Equals(other as BaseVersion);

        public override int GetHashCode() => HashCode.Start.Hash(GetFullName());

        public override string ToString() => GetFullName();

        protected static string JoinConstraints(IEnumerable<string> constraints)
            => string.Join("-", constraints.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    public class Dependency : BaseVersion, IComparePK<Dependency>
    {
        private const string StableBranchPostFix = "-" + SpecificVersionInfo.StableBranch;
        static readonly Regex rxDependency =
            new Regex(@"(.*)((\-([\>\<\=\~]*)\s*\d+\.[\d\.]+)(\-\w+)|(\-([\>\<\=\~]*)\s*\d+\.[\d\.]+))",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
        string _fullName;

        public Dependency(string fullyQualifiedName) {
            Contract.Requires<ArgumentNullException>(fullyQualifiedName != null);
            ParseFullyQualifiedName(fullyQualifiedName);
            VersionData = GetVersionData();
        }

        public Dependency(string name, string version) {
            Contract.Requires<ArgumentNullException>(name != null);
            Name = name;
            ParseVersion(version);

            VersionData = GetVersionData();
        }

        public Dependency(string name, string version, string branch) {
            Contract.Requires<ArgumentNullException>(name != null);
            Contract.Requires<NotSupportedException>(branch == null || version != null,
                "Cannot specify a branch if no version is specified");
            Name = name;
            Version = string.IsNullOrWhiteSpace(version) ? null : version;
            Branch = branch ?? SpecificVersionInfo.StableBranch;

            VersionData = GetVersionData();
        }

        public string Version { get; private set; }

        public bool ComparePK(object other) {
            var o = other as Dependency;
            return o != null && ComparePK(o);
        }

        public bool ComparePK(Dependency other) => other != null && other.GetFullName().Equals(GetFullName());

        void ParseFullyQualifiedName(string fullName) {
            var match = rxDependency.Match(fullName);
            if (match.Success) {
                Name = match.Groups[1].Value;
                if (!string.IsNullOrWhiteSpace(match.Groups[5].Value)) {
                    var branch = match.Groups[5].Value.Substring(1).ToLower();
                    Branch = branch;
                }
                if (string.IsNullOrWhiteSpace(match.Groups[6].Value)) {
                    Version = !string.IsNullOrWhiteSpace(match.Groups[3].Value)
                        ? match.Groups[3].Value.Substring(1)
                        : null;
                } else
                    Version = match.Groups[6].Value.Substring(1);
            } else
                Name = fullName;
        }

        void ParseVersion(string version) {
            var v = version.Split('-');
            version = v[0];
            if (v.Length > 1)
                Branch = v[1].ToLower();
            Version = version;
        }

        // TODO: protect VersionData. Why should it ever be an empty string? It is either null, or it is a valid value...
        string GetVersionData() {
            var versionData = Version;
            if (string.IsNullOrWhiteSpace(Branch) || Branch == SpecificVersionInfo.StableBranch)
                return versionData;

            if (Version == null)
                throw new NotSupportedException("Cannot specify a branch if no version is specified");
            versionData += "-" + Branch;
            return versionData;
        }

        static IEnumerable<SpecificVersionInfo> GetOrderedVersions(IEnumerable<SpecificVersionInfo> dependencies)
            => dependencies.OrderByDescending(
                x =>
                    x.Branch == null || x.Branch.ToLower() == SpecificVersionInfo.StableBranch
                        ? SpecificVersionInfo.StableBranch
                        : x.Branch)
                .ThenByDescending(x => x.Version);

        static IEnumerable<SpecificVersion> GetOrderedVersions(IEnumerable<SpecificVersion> dependencies)
            => dependencies.OrderByDescending(
                x =>
                    x.Branch == null || x.Branch.ToLower() == SpecificVersionInfo.StableBranch
                        ? SpecificVersionInfo.StableBranch
                        : x.Branch)
                .ThenByDescending(x => x.Version);

        public static SpecificVersion FindLatestPreferNonBranched(IEnumerable<SpecificVersion> dependencies)
            => GetOrderedVersions(dependencies).FirstOrDefault();

        public static SpecificVersionInfo FindLatestPreferNonBranched(IEnumerable<SpecificVersionInfo> dependencies)
            => GetOrderedVersions(dependencies).FirstOrDefault();

        public static string FindLatestPreferNonBranched(IEnumerable<string> items) {
            var sortedItems = items.OrderByDescending(x => new SpecificVersionInfo(x)).ToArray();
            return sortedItems.FirstOrDefault(x => !x.Contains("-") || x.Contains(StableBranchPostFix)) ??
                   sortedItems.FirstOrDefault();
        }

        public string GetConstraints(IEnumerable<string> inputConstraints = null) {
            var constraints = inputConstraints?.ToList() ?? new List<string>();
            if (Version != null)
                constraints.Add(Version);
            if (string.IsNullOrWhiteSpace(Branch))
                return JoinConstraints(constraints);

            var b = Branch.ToLower();
            if (b != SpecificVersionInfo.StableBranch)
                constraints.Add(b);
            return JoinConstraints(constraints);
        }

        public override string GetFullName() => _fullName ?? (_fullName = GetConstraints(new List<string> {Name}));

        public static SpecificVersion FindLatest(IEnumerable<SpecificVersion> packages)
            => packages.OrderByDescending(x => x).FirstOrDefault();
    }

    public class SpecificVersionInfo : IComparable<SpecificVersionInfo>, IEquatable<SpecificVersionInfo>
    {
        public const string StableBranch = "stable";

        public SpecificVersionInfo(string versionData) {
            var versions = versionData.Split('-');
            Version = Version.Parse(versions[0]);
            if (versions.Length > 1) {
                if (string.IsNullOrWhiteSpace(versions[1]))
                    throw new ArgumentException("version info cannot be empty");
                Branch = versions[1];
            }
            VersionData = GetVersionData();
        }

        public SpecificVersionInfo(Version version) {
            Version = version;
            VersionData = GetVersionData();
        }

        public SpecificVersionInfo(Version version, string branch) {
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(branch));
            Version = version;
            Branch = branch;
            VersionData = GetVersionData();
        }

        public SpecificVersionInfo(string version, string branch) : this(new Version(version), branch) {}

        [NotNull]
        public string Branch { get; } = StableBranch;

        [NotNull]
        public Version Version { get; }

        [NotNull]
        public string VersionData { get; }

        public int CompareTo(SpecificVersionInfo other) {
            if (other == null)
                return 1;
            var versionCompare = Version.CompareTo(other.Version);
            if (versionCompare != 0)
                return versionCompare;

            if (Branch == StableBranch && other.Branch == Branch)
                return 0;
            if (Branch == StableBranch)
                return 1;
            if (other.Branch == StableBranch)
                return -1;
            return Branch.CompareTo(other.Branch);
        }

        public bool Equals(SpecificVersionInfo other) {
            if (other == null)
                return false;
            return CompareTo(other) == 0;
        }

        public override string ToString() => VersionData;

        string GetVersionData() {
            var versionData = Version.ToString();
            if (!string.IsNullOrWhiteSpace(Branch) && Branch != StableBranch)
                versionData += "-" + Branch;
            return versionData;
        }

        public override bool Equals(object other) => Equals(other as SpecificVersionInfo);

        public override int GetHashCode() =>
            HashCode.Start.Hash(Branch).Hash(Version);

        public static bool operator ==(SpecificVersionInfo a, SpecificVersionInfo b) {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(a, b))
                return true;

            // If one is null, but not both, return false.
            if (((object) a == null) || ((object) b == null))
                return false;

            // Return true if the fields match:
            return a.Equals(b);
        }

        public static bool operator !=(SpecificVersionInfo a, SpecificVersionInfo b) => !(a == b);
    }

    public class SpecificVersion : BaseVersion, IComparable<SpecificVersion>, IEquatable<SpecificVersion>,
        IComparePK<SpecificVersion>
    {
        public const string DefaultVersion = "0.0.1";
        public static readonly Version DefaultV = new Version(DefaultVersion);
        string _fullName;

        public SpecificVersion(string fullyQualifiedName) {
            VersionInfo = ParseFullyQualifiedName(fullyQualifiedName);
            VersionData = VersionInfo.ToString();
        }

        public SpecificVersion(string name, string versionData) {
            Contract.Requires<ArgumentNullException>(name != null);
            Contract.Requires<ArgumentNullException>(versionData != null);
            Name = name;
            VersionInfo = new SpecificVersionInfo(versionData);
            VersionData = VersionInfo.ToString();
        }

        public SpecificVersion(string name, Version version, string branch)
            : this(name, new SpecificVersionInfo(version, branch)) {
            Contract.Requires<ArgumentNullException>(branch != null);
        }

        public SpecificVersion(string name, SpecificVersionInfo info) {
            Contract.Requires<ArgumentNullException>(info != null);
            Name = name;
            VersionInfo = info;
            VersionData = VersionInfo.ToString();
        }

        public SpecificVersion(string name, Version version) {
            Contract.Requires<ArgumentNullException>(name != null);
            Contract.Requires<ArgumentNullException>(version != null);

            Name = name;
            VersionInfo = new SpecificVersionInfo(version);
            VersionData = VersionInfo.ToString();
        }

        [NotNull]
        public SpecificVersionInfo VersionInfo { get; }

        [NotNull]
        public Version Version => VersionInfo.Version;
        [NotNull]
        public override string Branch => VersionInfo.Branch;

        public int CompareTo(SpecificVersion other) {
            if (other == null)
                return 1;
            if (!other.Name.Equals(Name))
                return Name.CompareTo(other.Name);
            return VersionInfo.CompareTo(other.VersionInfo);
        }

        public bool ComparePK(object other) {
            var o = other as SpecificVersion;
            return o != null && ComparePK(o);
        }

        public bool ComparePK(SpecificVersion other) => other != null && other.GetFullName().Equals(GetFullName());

        public bool Equals(SpecificVersion other) => other != null && (ReferenceEquals(this, other) || ComparePK(other));
        public override bool Equals(object obj) => Equals(obj as SpecificVersion);

        public override int GetHashCode() => HashCode.Start.Hash(GetFullName());

        public Dependency ToDependency() => new Dependency(GetFullName());

        [NotNull]
        SpecificVersionInfo ParseFullyQualifiedName(string fullName) {
            Version version = null;
            var branch = SpecificVersionInfo.StableBranch;
            var match = RxPackageName.Match(fullName);
            if (match.Success) {
                Name = match.Groups[1].Value;
                if (!string.IsNullOrWhiteSpace(match.Groups[4].Value)) {
                    var b = match.Groups[4].Value.Substring(1).ToLower();
                    branch = b;
                }
                if (string.IsNullOrWhiteSpace(match.Groups[5].Value)) {
                    version = !string.IsNullOrWhiteSpace(match.Groups[3].Value)
                        ? Version.Parse(match.Groups[3].Value.Substring(1))
                        : null;
                } else
                    version = Version.Parse(match.Groups[5].Value.Substring(1));
            } else
                Name = fullName;

            if (version == null)
                version = DefaultV;
            return new SpecificVersionInfo(version, branch);
        }

        public override string GetFullName() => _fullName ?? (_fullName = GetConstraints(new List<string> {Name}));

        public string GetConstraints(IEnumerable<string> inputConstraints = null) {
            var constraints = inputConstraints?.ToList() ?? new List<string>();
            if (Version != null)
                constraints.Add(Version.ToString());
            if (string.IsNullOrWhiteSpace(Branch))
                return JoinConstraints(constraints);

            var b = Branch.ToLower();
            if (b != SpecificVersionInfo.StableBranch)
                constraints.Add(b);
            return JoinConstraints(constraints);
        }

        public static bool operator ==(SpecificVersion a, SpecificVersion b) {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(a, b))
                return true;

            // If one is null, but not both, return false.
            if (((object) a == null) || ((object) b == null))
                return false;

            // Return true if the fields match:
            return a.ComparePK(b);
        }

        public static bool operator !=(SpecificVersion a, SpecificVersion b) => !(a == b);
    }

    public static class VersionExtensions
    {
        public static SpecificVersion ToSpecificVersion(this BaseVersion version)
            => new SpecificVersion(version.GetFullName());
    }

    public struct HashCode
    {
        private readonly int _hashCode;

        public HashCode(int hashCode) {
            _hashCode = hashCode;
        }

        public static HashCode Start => new HashCode(17);

        public static implicit operator int(HashCode hashCode) => hashCode.GetHashCode();

        public HashCode Hash<T>(T obj) {
            var c = EqualityComparer<T>.Default;
            var h = c.Equals(obj, default(T)) ? 0 : obj.GetHashCode();
            unchecked {
                h += _hashCode*31;
            }
            return new HashCode(h);
        }

        public override int GetHashCode() => _hashCode;
    }
}