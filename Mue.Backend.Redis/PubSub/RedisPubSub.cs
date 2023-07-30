namespace Mue.Backend.PubSub;

class RedisPubSub : IBackendPubSub
{
    private readonly RedisBackend _backend;
    private ISubscriber? _pubsub;

    public RedisPubSub(RedisBackend backend)
    {
        _backend = backend;
    }

    // TODO: This should probably be better (but is fine for most use as we're not clustered by default)
    private ISubscriber Subscriber { get { return _pubsub ?? (_pubsub = _backend.Connection?.GetSubscriber()) ?? throw new Exception("Connection was not open!"); } }

    public async Task<ISubscriptionToken> Subscribe(string topic, Action<string, string> callback)
    {
        var token = new RedisSubscriptionToken(Subscriber, topic, callback);
        await token.Subscribe();
        return token;
    }

    public async Task<ISubscriptionToken> Subscribe(string topic, Func<string, string, Task> callback)
    {
        var token = new RedisSubscriptionToken(Subscriber, topic, callback);
        await token.Subscribe();
        return token;
    }

    public Task Unsubscribe(ISubscriptionToken token)
    {
        return token.Unsubscribe();
    }

    public Task Publish(string topic, string value)
    {
        return Subscriber.PublishAsync(RedisPubSubUtil.GetChannel(topic), new RedisValue(value));
    }

    public async Task<uint> GetSubscribeCount(string topic)
    {
        var server = _backend.GetServer();
        if (server == null)
        {
            throw new NullReferenceException("Server was null.");
        }

        return (uint)(await server.SubscriptionSubscriberCountAsync(RedisPubSubUtil.GetChannel(topic)));
    }

    public async Task<IEnumerable<string>> GetTopicsWildcard(string topic)
    {
        var server = _backend.GetServer();
        if (server == null)
        {
            throw new NullReferenceException("Server was null.");
        }

        return (await server.SubscriptionChannelsAsync(RedisPubSubUtil.GetChannel(topic))).Select(s => s.ToString());
    }
}

public class RedisSubscriptionToken : ISubscriptionToken, IAsyncDisposable
{
    private readonly ISubscriber _pubsub;
    private readonly string _topic;
    private readonly Action<ChannelMessage>? _syncCallback;
    private readonly Func<ChannelMessage, Task>? _asyncCallback;
    private ChannelMessageQueue? _mq;

    public RedisSubscriptionToken(ISubscriber pubsub, string topic, Action<string, string> callback)
    {
        _pubsub = pubsub;
        _topic = topic;
        _syncCallback = (cm) => callback(cm.Channel.ToString(), cm.Message.ToString());
    }

    public RedisSubscriptionToken(ISubscriber pubsub, string topic, Func<string, string, Task> callback)
    {
        _pubsub = pubsub;
        _topic = topic;
        _asyncCallback = (cm) => callback(cm.Channel.ToString(), cm.Message.ToString());
    }

    public async ValueTask DisposeAsync()
    {
        await Unsubscribe();
    }

    public async Task Subscribe()
    {
        if (_mq == null)
        {
            _mq = await _pubsub.SubscribeAsync(RedisPubSubUtil.GetChannel(_topic));

            if (_syncCallback != null)
            {
                _mq.OnMessage(_syncCallback);
            }
            else if (_asyncCallback != null)
            {
                _mq.OnMessage(_asyncCallback);
            }
            else
            {
                throw new Exception("Illegal RedisPubSub callback state");
            }
        }
    }

    public async Task Unsubscribe()
    {
        if (_mq != null)
        {
            await _mq.UnsubscribeAsync();
            _mq = null;
        }
    }
}

static class RedisPubSubUtil
{
    public static RedisChannel GetChannel(string topic) => new RedisChannel(topic, RedisChannel.PatternMode.Auto);
}
