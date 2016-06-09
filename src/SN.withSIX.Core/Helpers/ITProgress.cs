// <copyright company="SIX Networks GmbH" file="ITProgress.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace SN.withSIX.Core.Helpers
{
    public interface ISpeedRead
    {
        long? Speed { get; }
    }
    public interface ISpeed : ISpeedRead
    {
        new long? Speed { get; set; }
    }

    public interface IProgressRead
    {
        double Progress { get; }
    }

    public interface IProgress : IProgressRead
    {
        new double Progress { get; set; }
    }

    public interface ITProgress : ISpeedRead, IProgressRead, IUpdateSpeedAndProgress
    {
    }

    public interface IUpdateSpeedAndProgress
    {
        void Update(long? speed, double progress);
    }
}