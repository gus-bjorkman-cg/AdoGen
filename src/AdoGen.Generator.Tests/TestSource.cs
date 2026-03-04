namespace AdoGen.Generator.Tests;

internal static class TestSource
{
    private const string DtoName = "User";
    
    extension(AdoGenType genType)
    {
        public string UserSource =>
            $$"""
              using {{genType.Namespace}};

              namespace AdoGen.Generator.Tests;

              public sealed partial record User(Guid Id, string Name, string Email) : {{genType.Interface}};

              public sealed class UserProfile : {{genType.ProfileName}}<User>
              {
                  public UserProfile()
                  {
                      RuleFor(x => x.Name).VarChar(20);
                      RuleFor(x => x.Email).VarChar(50);
                  }
              }
              """;

        private string UserFileName => $"{DtoName}.{genType.FileName}.{genType.Provider.ExtensionName}.g.cs";
        public RunResult RunUserGenerator => genType.UserSource.RunGenerator(genType);

        public string GenerateUserFile(AdoGenType forInterface) => 
            genType.UserSource.RunGenerator(genType).Result.GetGeneratedText(forInterface.UserFileName);
    }
}