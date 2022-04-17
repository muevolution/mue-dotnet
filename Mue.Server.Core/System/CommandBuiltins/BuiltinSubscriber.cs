namespace Mue.Server.Core.System.CommandBuiltins;

public class BuiltinSubscriber : Attribute
{
    public Type? UpdateType { get; init; }
    public List<string> SubscriptionTerms { get; init; } = new List<string>();

    public BuiltinSubscriber() { }

    public BuiltinSubscriber(params string[] events)
    {
        SubscriptionTerms.AddRange(events);
    }

    public BuiltinSubscriber(Type updateType, params string[] events)
    {
        UpdateType = updateType;
        SubscriptionTerms.AddRange(events);
    }
}
