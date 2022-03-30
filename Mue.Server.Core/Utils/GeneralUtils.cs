using System;

namespace Mue.Server.Core.Utils
{
    public static class GeneralUtils
    {
        public static string GenerateRandomId()
        {
            // TODO: Not this
            return Guid.NewGuid().ToString().Replace("-", String.Empty).Substring(0, 8);
        }
    }
}