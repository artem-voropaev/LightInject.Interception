
#load "common.csx"

private const string projectName = "LightInject.Interception";

private const string portableClassLibraryProjectTypeGuid = "{786C830F-07A1-408B-BD7F-6EE04809D6DB}";
private const string csharpProjectTypeGuid = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";

string pathToSourceFile = @"..\src\LightInject.Interception\LightInject.Interception.cs";
string pathToBuildDirectory = @"tmp/";
private string version = GetVersionNumberFromSourceFile(pathToSourceFile);

private string fileVersion = Regex.Match(version, @"(^[\d\.]+)-?").Groups[1].Captures[0].Value;

WriteLine("LightInject.Interception version {0}" , version);

Execute(() => InitializBuildDirectories(), "Preparing build directories");
Execute(() => RenameSolutionFiles(), "Renaming solution files");
Execute(() => PatchAssemblyInfo(), "Patching assembly information");
Execute(() => PatchProjectFiles(), "Patching project files");
Execute(() => PatchPackagesConfig(), "Patching packages config");
Execute(() => InternalizeSourceVersions(), "Internalizing source versions");
Execute(() => RestoreNuGetPackages(), "NuGet");
Execute(() => BuildAllFrameworks(), "Building all frameworks");
Execute(() => RunAllUnitTests(), "Running unit tests");
Execute(() => AnalyzeTestCoverage(), "Analyzing test coverage");
Execute(() => CreateNugetPackages(), "Creating NuGet packages");

private void CreateNugetPackages()
{
	string pathToNuGetBuildDirectory = Path.Combine(pathToBuildDirectory, "NuGetPackages");
	DirectoryUtils.Delete(pathToNuGetBuildDirectory);
	
		
	Execute(() => CopySourceFilesToNuGetLibDirectory(), "Copy source files to NuGet lib directory");		
	Execute(() => CopyBinaryFilesToNuGetLibDirectory(), "Copy binary files to NuGet lib directory");
	
	Execute(() => CreateSourcePackage(), "Creating source package");		
	Execute(() => CreateBinaryPackage(), "Creating binary package");
    string myDocumentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    RoboCopy(pathToBuildDirectory, myDocumentsFolder, "*.nupkg");		
}

private void CopySourceFilesToNuGetLibDirectory()
{				
	CopySourceFile("NETSTANDARD11", "netstandard1.1");				    
}

private void CopyBinaryFilesToNuGetLibDirectory()
{	
	CopyBinaryFile("NETSTANDARD11", "netstandard1.1");	       		
}

private void CreateSourcePackage()
{	    
	string pathToMetadataFile = Path.Combine(pathToBuildDirectory, "NugetPackages/Source/package/LightInject.Interception.Source.nuspec");	
    PatchNugetVersionInfo(pathToMetadataFile, version);		    
	NuGet.CreatePackage(pathToMetadataFile, pathToBuildDirectory);   		
}

private void CreateBinaryPackage()
{	    
	string pathToMetadataFile = Path.Combine(pathToBuildDirectory, "NugetPackages/Binary/package/LightInject.Interception.nuspec");
	PatchNugetVersionInfo(pathToMetadataFile, version);
	NuGet.CreatePackage(pathToMetadataFile, pathToBuildDirectory);
}

private void CopySourceFile(string frameworkMoniker, string packageDirectoryName)
{
	string pathToMetadata = "../src/LightInject.Interception/NuGet";
	string pathToPackageDirectory = Path.Combine(pathToBuildDirectory, "NugetPackages/Source/package");	
	RoboCopy(pathToMetadata, pathToPackageDirectory, "LightInject.Interception.Source.nuspec");	
	string pathToSourceFile = "tmp/" + frameworkMoniker + "/Source/LightInject.Interception";
	string pathToDestination = Path.Combine(pathToPackageDirectory, "content/" + packageDirectoryName + "/LightInject.Interception");
	RoboCopy(pathToSourceFile, pathToDestination, "LightInject.Interception.cs");
	FileUtils.Rename(Path.Combine(pathToDestination, "LightInject.Interception.cs"), "LightInject.Interception.cs.pp");
	ReplaceInFile(@"namespace \S*", "namespace $rootnamespace$.LightInject.Interception", Path.Combine(pathToDestination, "LightInject.Interception.cs.pp"));
}

private void CopyBinaryFile(string frameworkMoniker, string packageDirectoryName)
{
	string pathToMetadata = "../src/LightInject.Interception/NuGet";
	string pathToPackageDirectory = Path.Combine(pathToBuildDirectory, "NugetPackages/Binary/package");
	RoboCopy(pathToMetadata, pathToPackageDirectory, "LightInject.Interception.nuspec");
	string pathToBinaryFile =  ResolvePathToBinaryFile(frameworkMoniker);
	string pathToDestination = Path.Combine(pathToPackageDirectory, "lib/" + packageDirectoryName);
	RoboCopy(pathToBinaryFile, pathToDestination, "LightInject.Interception.*");
}

private string ResolvePathToBinaryFile(string frameworkMoniker)
{
	var pathToBinaryFile = Directory.GetFiles("tmp/" + frameworkMoniker + "/Binary/LightInject.Interception/bin/Release","LightInject.Interception.dll", SearchOption.AllDirectories).First();
	return Path.GetDirectoryName(pathToBinaryFile);		
}

private void BuildAllFrameworks()
{	
	Build("Net46");	    	
	BuildDotNet();
}

private void Build(string frameworkMoniker)
{
	var pathToSolutionFile = Directory.GetFiles(Path.Combine(pathToBuildDirectory, frameworkMoniker + @"\Binary\"),"*.sln").First();	
	WriteLine(pathToSolutionFile);
	MsBuild.Build(pathToSolutionFile);
	pathToSolutionFile = Directory.GetFiles(Path.Combine(pathToBuildDirectory, frameworkMoniker + @"\Source\"),"*.sln").First();
	MsBuild.Build(pathToSolutionFile);
}

private void BuildDotNet()
{
	string pathToProjectFile = Path.Combine(pathToBuildDirectory, @"netstandard11/Binary/LightInject.Interception/project.json");
	DotNet.Build(pathToProjectFile, "netstandard1.1");		
}

private void RestoreNuGetPackages()
{		
	RestoreNuGetPackages("net46");    			
}

private void RestoreNuGetPackages(string frameworkMoniker)
{
	string pathToProjectDirectory = Path.Combine(pathToBuildDirectory, frameworkMoniker + @"/Binary/LightInject.Interception");
	NuGet.Restore(pathToProjectDirectory);
	pathToProjectDirectory = Path.Combine(pathToBuildDirectory, frameworkMoniker + @"/Binary/LightInject.Interception.Tests");
	NuGet.Restore(pathToProjectDirectory);
	pathToProjectDirectory = Path.Combine(pathToBuildDirectory, frameworkMoniker + @"/Source/LightInject.Interception");
	NuGet.Restore(pathToProjectDirectory);
	pathToProjectDirectory = Path.Combine(pathToBuildDirectory, frameworkMoniker + @"/Source/LightInject.Interception.Tests");
	NuGet.Restore(pathToProjectDirectory);
    NuGet.Update(GetFile(Path.Combine(pathToBuildDirectory, frameworkMoniker, "Binary"), "*.sln"));        
}

private void RunAllUnitTests()
{	
	DirectoryUtils.Delete("TestResults");	
	Execute(() => RunUnitTests("Net46"), "Running unit tests for Net46");		
}

private void RunUnitTests(string frameworkMoniker)
{
	string pathToTestAssembly = Path.Combine(pathToBuildDirectory, frameworkMoniker + @"/Binary/LightInject.Interception.Tests/bin/Release/LightInject.Interception.Tests.dll");
	string testAdapterSearchDirectory = Path.Combine(pathToBuildDirectory, frameworkMoniker, @"Binary/packages/");
    string pathToTestAdapterDirectory = ResolveDirectory(testAdapterSearchDirectory, "xunit.runner.visualstudio.testadapter.dll");
	MsTest.Run(pathToTestAssembly, pathToTestAdapterDirectory);	
}

private void AnalyzeTestCoverage()
{		
	Execute(() => AnalyzeTestCoverage("NET46"), "Analyzing test coverage for NET46");
}

private void AnalyzeTestCoverage(string frameworkMoniker)
{	
    string pathToTestAssembly = Path.Combine(pathToBuildDirectory, frameworkMoniker + @"/Binary/LightInject.Interception.Tests/bin/Release/LightInject.Interception.Tests.dll");
	string pathToPackagesFolder = Path.Combine(pathToBuildDirectory, frameworkMoniker, @"Binary/packages/");
    string pathToTestAdapterDirectory = ResolveDirectory(pathToPackagesFolder, "xunit.runner.visualstudio.testadapter.dll");		
    MsTest.RunWithCodeCoverage(pathToTestAssembly, pathToPackagesFolder,pathToTestAdapterDirectory, "LightInject.Interception.dll");
}

private void InitializBuildDirectories()
{
	DirectoryUtils.Delete(pathToBuildDirectory);		
	Execute(() => InitializeNugetBuildDirectory("NET46"), "Preparing Net46");
	Execute(() => InitializeNugetBuildDirectory("NETSTANDARD11"), "Preparing NetStandard1.1");		    						
}

private void InitializeNugetBuildDirectory(string frameworkMoniker)
{
	var pathToBinary = Path.Combine(pathToBuildDirectory, frameworkMoniker +  "/Binary");		
    CreateDirectory(pathToBinary);
	RoboCopy("../src", pathToBinary, "/e /XD bin obj .vs NuGet TestResults packages");	
				
	var pathToSource = Path.Combine(pathToBuildDirectory,  frameworkMoniker +  "/Source");	
	CreateDirectory(pathToSource);
	RoboCopy("../src", pathToSource, "/e /XD bin obj .vs NuGet TestResults packages");
	
	if (frameworkMoniker.StartsWith("NETSTANDARD"))
	{
		var pathToJsonTemplateFile = Path.Combine(pathToBinary, "LightInject.Interception/project.json_");
		var pathToJsonFile = Path.Combine(pathToBinary, "LightInject.Interception/project.json");
		File.Move(pathToJsonTemplateFile, pathToJsonFile);
		pathToJsonTemplateFile = Path.Combine(pathToSource, "LightInject.Interception/project.json_");
		pathToJsonFile = Path.Combine(pathToSource, "LightInject.Interception/project.json");
		File.Move(pathToJsonTemplateFile, pathToJsonFile);
	}				  
}

private void RenameSolutionFile(string frameworkMoniker)
{
	string pathToSolutionFolder = Path.Combine(pathToBuildDirectory, frameworkMoniker +  "/Binary");
	string pathToSolutionFile = Directory.GetFiles(pathToSolutionFolder, "*.sln").First();
	string newPathToSolutionFile = Regex.Replace(pathToSolutionFile, @"(\w+)(.sln)", "$1_" + frameworkMoniker + "_Binary$2");
	File.Move(pathToSolutionFile, newPathToSolutionFile);
	WriteLine("{0} (Binary) solution file renamed to : {1}", frameworkMoniker, newPathToSolutionFile);
	
	pathToSolutionFolder = Path.Combine(pathToBuildDirectory, frameworkMoniker +  "/Source");
	pathToSolutionFile = Directory.GetFiles(pathToSolutionFolder, "*.sln").First();
	newPathToSolutionFile = Regex.Replace(pathToSolutionFile, @"(\w+)(.sln)", "$1_" + frameworkMoniker + "_Source$2");
	File.Move(pathToSolutionFile, newPathToSolutionFile);
	WriteLine("{0} (Source) solution file renamed to : {1}", frameworkMoniker, newPathToSolutionFile);
}

private void RenameSolutionFiles()
{		
	RenameSolutionFile("NET46");
	RenameSolutionFile("NETSTANDARD11");		    
}

private void Internalize(string frameworkMoniker)
{
	string[] exceptTheseTypes = new string[] {
		"IProxy",
		"IInvocationInfo", 
		"IMethodBuilder", 
		"IDynamicMethodSkeleton", 
		"IProxyBuilder", 
		"IInterceptor", 
		"MethodInterceptorFactory",
		"TargetMethodInfo",
		"OpenGenericTargetMethodInfo",
		"DynamicMethodBuilder",
		"CachedMethodBuilder",
		"TargetInvocationInfo",
		"InterceptorInvocationInfo",
		"CompositeInterceptor",
		"InterceptorInfo",
		"ProxyDefinition"		
		}; 
	
	string pathToSourceFile = Path.Combine(pathToBuildDirectory, frameworkMoniker + "/Source/LightInject.Interception/LightInject.Interception.cs");
	Internalizer.Internalize(pathToSourceFile, frameworkMoniker, exceptTheseTypes);
}

private void InternalizeSourceVersions()
{	
	Execute (()=> Internalize("NET46"), "Internalizing NET46");
	Execute (()=> Internalize("NETSTANDARD11"), "Internalizing NetStandard1.1");	
}

private void PatchPackagesConfig()
{	
	PatchPackagesConfig("net46");	
}

private void PatchPackagesConfig(string frameworkMoniker)
{
	string pathToPackagesConfig = Path.Combine(pathToBuildDirectory, frameworkMoniker + @"/Binary/LightInject.Interception/packages.config");
	ReplaceInFile(@"(targetFramework=\"").*(\"".*)", "$1"+ frameworkMoniker + "$2", pathToPackagesConfig);
	
	pathToPackagesConfig = Path.Combine(pathToBuildDirectory, frameworkMoniker + @"/Source/LightInject.Interception/packages.config");
	ReplaceInFile(@"(targetFramework=\"").*(\"".*)", "$1"+ frameworkMoniker + "$2", pathToPackagesConfig);
}

private void PatchAssemblyInfo()
{
	Execute(() => PatchAssemblyInfo("Net46"), "Patching AssemblyInfo (Net46)");	
	Execute(() => PatchAssemblyInfo("NETSTANDARD11"), "Patching AssemblyInfo (NetStandard1.1)");	    
}

private void PatchAssemblyInfo(string framework)
{	
	var pathToAssemblyInfo = Path.Combine(pathToBuildDirectory, framework + @"\Binary\LightInject.Interception\Properties\AssemblyInfo.cs");	
	PatchAssemblyVersionInfo(version, fileVersion, framework, pathToAssemblyInfo);
	pathToAssemblyInfo = Path.Combine(pathToBuildDirectory, framework + @"\Source\LightInject.Interception\Properties\AssemblyInfo.cs");
	PatchAssemblyVersionInfo(version, fileVersion, framework, pathToAssemblyInfo);	
	PatchInternalsVisibleToAttribute(pathToAssemblyInfo);		
}

private void PatchInternalsVisibleToAttribute(string pathToAssemblyInfo)
{
	var assemblyInfo = ReadFile(pathToAssemblyInfo);   
	StringBuilder sb = new StringBuilder(assemblyInfo);
	sb.AppendLine(@"[assembly: InternalsVisibleTo(""LightInject.Interception.Tests"")]");
	WriteFile(pathToAssemblyInfo, sb.ToString());
}

private void PatchProjectFiles()
{	
	Execute(() => PatchProjectFile("NET46", "4.6"), "Patching project file (NET46)");    		
}

private void PatchProjectFile(string frameworkMoniker, string frameworkVersion)
{
	var pathToProjectFile = Path.Combine(pathToBuildDirectory, frameworkMoniker + @"/Binary/LightInject.Interception/LightInject.Interception.csproj");
	PatchProjectFile(frameworkMoniker, frameworkVersion, pathToProjectFile);
	pathToProjectFile = Path.Combine(pathToBuildDirectory, frameworkMoniker + @"/Source/LightInject.Interception/LightInject.Interception.csproj");
	PatchProjectFile(frameworkMoniker, frameworkVersion, pathToProjectFile);
	PatchTestProjectFile(frameworkMoniker);
}
 
private void PatchProjectFile(string frameworkMoniker, string frameworkVersion, string pathToProjectFile)
{
	WriteLine("Patching {0} ", pathToProjectFile);	
	SetProjectFrameworkMoniker(frameworkMoniker, pathToProjectFile);
	SetProjectFrameworkVersion(frameworkVersion, pathToProjectFile);		
	SetHintPath(frameworkMoniker, pathToProjectFile);	
}

private void SetProjectFrameworkVersion(string frameworkVersion, string pathToProjectFile)
{
	XDocument xmlDocument = XDocument.Load(pathToProjectFile);
	var frameworkVersionElement = xmlDocument.Descendants().SingleOrDefault(p => p.Name.LocalName == "TargetFrameworkVersion");
	frameworkVersionElement.Value = "v" + frameworkVersion;
	xmlDocument.Save(pathToProjectFile);
}

private void SetProjectFrameworkMoniker(string frameworkMoniker, string pathToProjectFile)
{
	XDocument xmlDocument = XDocument.Load(pathToProjectFile);
	var defineConstantsElements = xmlDocument.Descendants().Where(p => p.Name.LocalName == "DefineConstants");
	foreach (var defineConstantsElement in defineConstantsElements)
	{
		defineConstantsElement.Value = defineConstantsElement.Value.Replace("NET46", frameworkMoniker); 
	}	
	xmlDocument.Save(pathToProjectFile);
}

private void SetHintPath(string frameworkMoniker, string pathToProjectFile)
{
	if (frameworkMoniker == "PCL_111")
	{
		frameworkMoniker = "portable-net45+netcore45+wpa81";
	}
	ReplaceInFile(@"(.*\\packages\\LightInject.*\\lib\\).*(\\.*)","$1"+ frameworkMoniker + "$2", pathToProjectFile);	
}



private void PatchTestProjectFile(string frameworkMoniker)
{
	var pathToProjectFile = Path.Combine(pathToBuildDirectory, frameworkMoniker + @"\Binary\LightInject.Interception.Tests\LightInject.Interception.Tests.csproj");
	WriteLine("Patching {0} ", pathToProjectFile);	
	SetProjectFrameworkMoniker(frameworkMoniker, pathToProjectFile);
	pathToProjectFile = Path.Combine(pathToBuildDirectory, frameworkMoniker + @"\Source\LightInject.Interception.Tests\LightInject.Interception.Tests.csproj");
	WriteLine("Patching {0} ", pathToProjectFile);
	SetProjectFrameworkMoniker(frameworkMoniker, pathToProjectFile);
}