using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mue.Backend.Storage
{
    public interface IBackendStorage : IBackendStorageOperations
    {
        IBackendStorageTransaction StartTransaction();
    }

    public interface IBackendStorageOperations
    {
        // General operations
        Task<string?> KeyGet(string key);
        Task<bool> KeySet(string key, string value);
        Task<bool> KeyDelete(string key);

        // Set operations
        Task<IEnumerable<string>> SetMembers(string key);
        Task<bool> SetContains(string key, string value);
        Task<bool> SetAdd(string key, string value);
        Task<bool> SetRemove(string key, string value);

        // Hash operations
        Task<IReadOnlyDictionary<string, string>> HashGetAll(string key);
        Task<bool> HashSetAll(string key, IReadOnlyDictionary<string, string> values);
        Task<string?> HashGetField(string key, string field);
        Task<bool> HashSetField(string key, string field, string value);
        Task<bool> HashDeleteField(string key, string field);
    }

    public interface IBackendStorageTransaction : IBackendStorageOperations, IAsyncDisposable
    {
    }
}
