// <copyright company="SIX Networks GmbH" file="KeyCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Linq;

using withSIX.Core.Extensions;
using withSIX.Sync.Core.Keys;

namespace withSIX.Sync.Presentation.Console.Commands
{

    public class KeyCommand : BaseCommand
    {
        readonly SshKeyFactory _keyFactory;
        int _bits = 2048;
        bool _force;
        string _type = "rsa";

        public KeyCommand(SshKeyFactory keyFactory) {
            _keyFactory = keyFactory;
            IsCommand("key", "SSH Key Management");
            HasOption("f|force", "Force creation even if already exists", s => _force = s != null);
            HasOption("b|bits=", "key size, defaults to 2048", s => _bits = s.TryInt());
            HasOption("t|type=", "key type, defaults to rsa", s => _type = s);
            HasAdditionalArguments(1, "path/to/new_key_file");
        }

        public override int Run(params string[] remainingArguments) {
            var file = remainingArguments.First();
            _keyFactory.Create(file, _force, _bits, _type);

            return 0;
        }
    }
}