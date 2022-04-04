using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Mue.Backend.Storage
{
    class RedisStorage : RedisStorageOps, IBackendStorage
    {
        public RedisStorage(RedisBackend backend) : base(backend) { }

        public IBackendStorageTransaction StartTransaction()
        {
            var transact = _db.CreateTransaction();
            return new RedisStorageTransactionOps(_backend, transact);
        }
    }

    class RedisStorageOps : IBackendStorageOperations
    {
        protected RedisBackend _backend;
        protected IDatabase _db { get { return _backend.Connection?.GetDatabase() ?? throw new Exception("Connection was not open!"); } }

        public RedisStorageOps(RedisBackend backend)
        {
            _backend = backend;
        }

        public Task<string?> KeyGet(string key) => AsStr(_db.StringGetAsync(key));
        public Task<bool> KeySet(string key, string value) => _db.StringSetAsync(key, value);
        public Task<bool> KeyDelete(string key) => _db.KeyDeleteAsync(key);

        public Task<IEnumerable<string>> SetMembers(string key) => AsStrNotNull(_db.SetMembersAsync(key));
        public Task<bool> SetContains(string key, string value) => _db.SetContainsAsync(key, value);
        public Task<bool> SetAdd(string key, string value) => _db.SetAddAsync(key, value);
        public Task<bool> SetRemove(string key, string value) => _db.SetRemoveAsync(key, value);

        public async Task<IReadOnlyDictionary<string, string>> HashGetAll(string key)
        {
            var val = await _db.HashGetAllAsync(key);
            return val.ToDictionary(s => (string)s.Name, s => (string)s.Value);
        }

        public async Task<bool> HashSetAll(string key, IReadOnlyDictionary<string, string> values)
        {
            await _db.HashSetAsync(key, values.Select(s => new HashEntry(s.Key, s.Value)).ToArray());
            return true;
        }

        public Task<string?> HashGetField(string key, string field) => AsStr(_db.HashGetAsync(key, field));
        public Task<bool> HashSetField(string key, string field, string value) => _db.HashSetAsync(key, field, value);
        public Task<bool> HashDeleteField(string key, string field) => _db.HashDeleteAsync(key, field);

        private async Task<string?> AsStr(Task<RedisValue> val)
        {
            return await val;
        }

        private async Task<IEnumerable<string?>> AsStr(Task<IEnumerable<RedisValue>> val)
        {
            var items = await val;
            return items.Select(s => (string)s);
        }

        private async Task<IEnumerable<string?>> AsStr(Task<RedisValue[]> val)
        {
            var items = await val;
            return items.Select(s => (string)s);
        }

        private async Task<IEnumerable<string>> AsStrNotNull(Task<RedisValue[]> val)
        {
            var items = await val;
            return items.Select(s => (string)s);
        }
    }

    class RedisStorageTransactionOps : RedisStorageOps, IBackendStorageTransaction
    {
        private ITransaction _transaction;

        public RedisStorageTransactionOps(RedisBackend backend, ITransaction transaction) : base(backend)
        {
            _transaction = transaction;
        }

        public async ValueTask DisposeAsync()
        {
            await _transaction.ExecuteAsync();
        }
    }
}
