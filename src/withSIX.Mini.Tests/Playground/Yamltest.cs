// <copyright company="SIX Networks GmbH" file="Yamltest.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Presentation.Bridge;
using withSIX.Sync.Core.Legacy.SixSync;

namespace withSIX.Mini.Tests.Playground
{
    [TestFixture]
    public class Yamltest
    {
        [Test]
        public async Task Test() {
            var n = new YamlUtil().NewFromYaml<RepoVersion>(@"
---
:archive_format: .gz
:format_version: 1
:guid: 08d56888-a75d-4e4c-842f-8db5f9d163f4
:version: 1
:pack_size: 28456
:wd_size: 140157
:pack:
  addons/ace_compat_rhs_afrf3.pbo.gz: f9cfb837b5d64f7fcf364911f83b135a
  addons/ace_compat_rhs_usf3.pbo.gz: 09b04627357283cf3fb39c32be6ebcfb
  addons/userconfig/ace/serverconfig.hpp.gz: 0babb2a8d97247947d57b28f4b6e723e
  addons/ace_compat_rhs_afrf3.pbo.ace_3.8.1.11-e1aa5b42.bisign.gz: 63595430b41898d2d338e4fff11332da
  addons/ace_compat_rhs_afrf3.pbo.six_ace3.bisign.gz: 287a8413dadfd4c2e7dc2bc1a1c07bb4
  addons/ace_compat_rhs_afrf3.pbo.six_ace_optionals.bisign.gz: a2d6ccb71acf718acf8fc8efff1a88fc
  addons/ace_compat_rhs_usf3.pbo.ace_3.8.1.11-e1aa5b42.bisign.gz: 1f36f49ff0d5ee9530837c1b14ed936a
  addons/ace_compat_rhs_usf3.pbo.six_ace3.bisign.gz: 2ac922c177f9496d8d300630f75b8572
  addons/ace_compat_rhs_usf3.pbo.six_ace_optionals.bisign.gz: 05ddf47bf34cafd68d2011f1a360a345
  keys/six_ace_optionals.bikey.gz: 03774883cfb9532af70ec2cd8a70fe70
:wd:
  addons/ace_compat_rhs_usf3.pbo: cb8bc8b508581843dc88412bf545a3ac
  addons/ace_compat_rhs_afrf3.pbo: 972b40ddc0a022235fa64d14dd83a621
  addons/userconfig/ace/serverconfig.hpp: 26b6ecd355676c7413f1fa376a57491e
  keys/six_ace_optionals.bikey: c191ba047f8aa5755c83dfb08d62edd9
  addons/ace_compat_rhs_usf3.pbo.six_ace_optionals.bisign: 17cda3e00aa5f05385850a00bbac8c33
  addons/ace_compat_rhs_usf3.pbo.six_ace3.bisign: bae42633fac2bc1f46549b4e9c55f000
  addons/ace_compat_rhs_usf3.pbo.ace_3.8.1.11-e1aa5b42.bisign: a3d55b42f84e22b749787fb036dbcc3d
  addons/ace_compat_rhs_afrf3.pbo.six_ace_optionals.bisign: cf912f0ab2bc94f6277a98b64f2b72dc
  addons/ace_compat_rhs_afrf3.pbo.six_ace3.bisign: 811bf388a7544124387d063624a7bb88
  addons/ace_compat_rhs_afrf3.pbo.ace_3.8.1.11-e1aa5b42.bisign: 81508e234450dffd3c2c88acfb5e0bd6
");
            Console.WriteLine(n.ToJson());
        }

        [Test]
        public async Task Test2() {
            var n = new YamlUtil().NewFromYaml<RepoConfig>(@"
--- 
:pack_path: C:\temp\testpath
:exclude: []
:include: []
:hosts:
- https://testhost.com
");
            Console.WriteLine(n.ToJson());
        }
    }
}