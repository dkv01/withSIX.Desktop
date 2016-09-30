﻿// <copyright company="SIX Networks GmbH" file="IInstallProgressMessenger.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;

namespace withSIX.Play.Infra.Server.Services.Messengers.ServerMessengers
{
    public class StatusUpdated : ISyncDomainEvent
    {
        public Guid StatusID { get; set; }
        public string FieldName { get; set; }
        public dynamic Value { get; set; }
    }

    public class StatusRemoved : ISyncDomainEvent
    {
        public Guid StatusID { get; set; }
    }

    public class StatusAdded : ISyncDomainEvent
    {
        public string StatusType { get; set; }
        public dynamic StatusModel { get; set; }
    }

    public abstract class StatusModel
    {
        /// <summary>
        ///     The Type of Status that the caller will use to display the information
        /// </summary>
        public abstract string StatusType { get; }
        public Guid StatusID { get; set; }
        public Guid ParentStatus { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        /// <summary>
        ///     Calculate Progress from Children (Ignore Progress Status)
        /// </summary>
        public bool CalculateProgressFromChildren { get; set; }
        /// <summary>
        ///     Progress weight from 0 to 1000000
        /// </summary>
        public int ProgressWeight { get; set; }
    }

    public class CollectionStatusModel : StatusModel
    {
        public override string StatusType => "collection";
    }

    public class ModStatusModel : StatusModel
    {
        public override string StatusType => "mod";
    }

    public class FileStatusModel : StatusModel
    {
        public override string StatusType => "file";
        public string FileDownloadAmount { get; set; }
    }
}