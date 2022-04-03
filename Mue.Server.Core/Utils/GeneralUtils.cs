using System;
using System.Collections.Generic;
using System.Linq;

namespace Mue.Server.Core.Utils
{
    public static class GeneralUtils
    {
        public static string GenerateRandomId()
        {
            // TODO: Not this
            return Guid.NewGuid().ToString().Replace("-", String.Empty).Substring(0, 8);
        }

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> value) {
            return value.Where(w => w != null);
        }
    }
}