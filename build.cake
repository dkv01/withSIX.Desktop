var target = Argument("target", "Build");
var configuration   = Argument<string>("configuration", "Release");

// TODO versioning

Task("Default")
  .Does(() =>
{
  Information("Hello World!");
});

Task("Build")
  .Does(() => {
    DotNetBuild("./src/withSIX.Mini.sln", settings => settings.SetConfiguration(configuration));
  });

Task("Pack")
  .IsDependentOn("Build")
  .Does(() => {
    var nuspecFiles = GetFiles("./**/*.nuspec");
    NuGetPack(nuspecFiles, new NuGetPackSettings()); // , nuGetPackSettings
});

RunTarget(target);