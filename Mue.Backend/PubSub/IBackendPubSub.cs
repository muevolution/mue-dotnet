using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mue.Backend.PubSub
{
    public interface IBackendPubSub
    {
        Task<ISubscriptionToken> Subscribe(string topic, Action<string, string> callback);
        Task<ISubscriptionToken> Subscribe(string topic, Func<string, string, Task> callback);
        Task Unsubscribe(ISubscriptionToken token);
        Task Publish(string topic, string value);
        Task<uint> GetSubscribeCount(string topic);
        Task<IEnumerable<string>> GetTopicsWildcard(string topic);
    }

    public interface ISubscriptionToken
    {
        Task Subscribe();
        Task Unsubscribe();
    }
}