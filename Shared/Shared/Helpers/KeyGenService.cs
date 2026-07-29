using UserManagementPoC.Shared.Abstractions;

namespace UserManagementPoC.Shared.Helpers;

public class KeyGenService : IKeyGen
{
    public string GenerateKey() => KeyGen.GenerateKey();

}