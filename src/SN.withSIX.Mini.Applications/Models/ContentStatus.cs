// <copyright company="SIX Networks GmbH" file="ContentStatus.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;

namespace SN.withSIX.Mini.Applications.Models
{
    public class ContentStatus : ContentState
    {
        public ContentStatus() {}

        public ContentStatus(double progress, long speed) {
            if (progress.Equals(double.NaN))
                throw new ArgumentOutOfRangeException(nameof(progress), "NaN");
            if (speed.Equals(double.NaN))
                throw new ArgumentOutOfRangeException(nameof(speed), "NaN");
            if (progress < 0)
                throw new ArgumentOutOfRangeException(nameof(progress), "Below 0");
            if (speed < 0)
                throw new ArgumentOutOfRangeException(nameof(speed), "Below 0");
            Progress = progress;
            Speed = speed;
        }

        public double? Progress { get; }
        public long? Speed { get; }
    }

    public abstract class ContentState
    {
        public Guid Id { get; set; }
        public Guid GameId { get; set; }
        public ItemState State { get; set; }
        public string Version { get; set; }
        public long Size { get; set; }
        public long SizePacked { get; set; }
        public DateTime? LastUsed { get; set; }
        public DateTime? LastInstalled { get; set; }
        public DateTime? LastUpdated { get; set; }
    }

    public interface IHaveItemState
    {
        ItemState State { get; }
    }

    public enum PlayAction
    {
        Play,
        Join,
        Launch,
        Sync // Install or Update. Because who cares?
    }
}