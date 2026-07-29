using System;

using System.Collections.Generic;

using System.Text;

namespace UserManagementPoC.Shared.Helpers
{
    public static class KeyGen
    {
        public static string GenerateKey() => Guid.NewGuid().ToString();

    }
}