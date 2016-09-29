// <copyright company="SIX Networks GmbH" file="IINstallerSessionFactory.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using withSIX.Sync.Core.Legacy.Status;

namespace withSIX.Mini.Core.Games.Services.ContentInstaller
{
    public interface IINstallerSessionFactory
    {
        IInstallerSession Create(IInstallContentAction<IInstallableContent> action,
            Func<ProgressInfo, Task> progress);

        IUninstallSession CreateUninstaller(IUninstallContentAction2<IUninstallableContent> action);
    }


    public class FlatProgressInfo
    {
        public string Title { get; set; }
        public double Progress { get; set; }
        public long? Speed { get; set; }
        public int CurrentStage { get; set; }
        public int ComponentsCount { get; set; }
    }

    public class ProgressInfo : IEquatable<ProgressInfo>
    {
        public ProgressInfo(string text = null, double? progress = null, List<FlatProgressInfo> components = null,
            long? speed = null) {
            if (progress.HasValue) {
                if (progress.Value.Equals(double.NaN))
                    throw new ArgumentOutOfRangeException(nameof(progress), "NaN");
                if (progress.Value < 0)
                    throw new ArgumentOutOfRangeException(nameof(progress), "Below 0");
            }
            if ((speed != null) && (speed < 0))
                throw new ArgumentOutOfRangeException(nameof(speed), "Below 0");

            Text = text;
            Progress = progress;
            Components = components ?? new List<FlatProgressInfo>();
            Speed = speed;
        }

        public List<FlatProgressInfo> Components { get; }

        public static ProgressInfo Default { get; } = new ProgressInfo(Status.Synchronized.ToString());
        public string Text { get; }
        public double? Progress { get; }
        public long? Speed { get; }

        public bool Equals(ProgressInfo other) => (other != null)
                                                  && (ReferenceEquals(this, other)
                                                      || (other.GetHashCode() == GetHashCode()));

        public override int GetHashCode() =>
            HashCode.Start
                .Hash(Text)
                .Hash(Progress)
                .Hash(Speed)
                .Hash(Components);

        public override bool Equals(object obj) => Equals(obj as ProgressInfo);
    }
}