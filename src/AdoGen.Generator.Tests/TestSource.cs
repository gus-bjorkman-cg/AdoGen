namespace AdoGen.Generator.Tests;

internal static class TestSource
{
    private static readonly List<ITestTypeSource> TestTypeSources =
    [
        UserSourceHandler.Instance,
        TestTypeSourceHandler.Instance,
        AuditEventSourceHandler.Instance
    ];
    
    extension(AdoGenType genType)
    {
        private string FileName(TestTypes testType) => $"{testType.Name}.{genType.FileName}.{genType.Provider.ExtensionName}.g.cs";
        public RunResult RunUserGenerator(TestTypes testType) => 
            TestTypeSources.First(x => x.IsMatch(testType)).Handle(genType).RunGenerator(genType);

        public string GenerateFile(TestTypes testType, AdoGenType forInterface) => 
            genType.RunUserGenerator(testType).Result.GetGeneratedText(forInterface.FileName(testType));
    }
}