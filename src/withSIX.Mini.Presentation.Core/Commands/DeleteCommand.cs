﻿// <copyright company="SIX Networks GmbH" file="DeleteCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using withSIX.Core;

namespace withSIX.Mini.Presentation.Core.Commands
{
    public class DeleteCommand : BaseCommand
    {
        public DeleteCommand() {
            IsCommand(UpdaterCommands.Delete);
            HasAdditionalArguments(1, "<file|folder>");
        }

        public override int Run(string[] remainingArguments) {
            Tools.FileUtil.Ops.DeleteWithRetry(remainingArguments[0]);
            return 0;
        }
    }
}