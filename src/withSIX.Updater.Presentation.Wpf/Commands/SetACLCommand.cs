// <copyright company="SIX Networks GmbH" file="SetACLCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Security.AccessControl;
using NDepend.Path;
using withSIX.Core;

namespace withSIX.Updater.Presentation.Wpf.Commands
{
    public class SetACLCommand : BaseCommand
    {
        bool _createDirectory;
        string _fsr;
        string _user;

        public SetACLCommand() {
            IsCommand("setACL");
            HasRequiredOption("user=", "", a => _user = a);
            HasRequiredOption("rights=", "File System Rights", a => _fsr = a);
            HasOption("createDirectory", "", a => _createDirectory = a != null);
            HasAdditionalArguments(1, "<location>");
        }

        public override int Run(string[] remainingArguments) {
            if (_createDirectory) {
                Tools.FileUtil.Ops.CreateDirectoryAndSetACL(remainingArguments[0].ToAbsoluteDirectoryPath(), _user,
                    ParseFileSystemRights(_fsr));
            } else {
                Tools.FileUtil.Ops.SetACLHandleInvalidOperation(remainingArguments[0].ToAbsoluteDirectoryPath(), _user,
                    ParseFileSystemRights(_fsr));
            }
            return 0;
        }

        static FileSystemRights ParseFileSystemRights(string arg) => (FileSystemRights)Enum.Parse(typeof(FileSystemRights), arg);
    }
}