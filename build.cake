var target = Argument("target", "Default");
var configuration   = Argument<string>("configuration", "Release");

Task("Default")
  .Does(() =>
{
  Information("Hello World!");
  DotNetBuild("./src/withSIX.Mini.sln", settings => settings.SetConfiguration(configuration));
});

RunTarget(target);