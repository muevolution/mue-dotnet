using System.Dynamic;

namespace Mue.Scripting;

public class DynamicDictionary : DynamicObject
{
    // The inner dictionary.
    private Dictionary<string, object?> _dictionary;

    public DynamicDictionary()
    {
        _dictionary = new Dictionary<string, object?>();
    }

    public DynamicDictionary(IReadOnlyDictionary<string, object?> root)
    {
        _dictionary = new Dictionary<string, object?>(root);
    }

    public static DynamicDictionary From<T1, T2>(IReadOnlyDictionary<T1, T2> root)
    {
        return new DynamicDictionary(root.ToDictionary(k => k.Key?.ToString()!, v => (object?)v.Value));
    }

    // This property returns the number of elements
    // in the inner dictionary.
    public int Count
    {
        get
        {
            return _dictionary.Count;
        }
    }

    // If you try to get a value of a property
    // not defined in the class, this method is called.
    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        // Converting the property name to lowercase
        // so that property names become case-insensitive.
        string name = binder.Name.ToLower();

        // If the property name is found in a dictionary,
        // set the result parameter to the property value and return true.
        // Otherwise, return false.
        return _dictionary.TryGetValue(name, out result);
    }

    // If you try to set a value of a property that is
    // not defined in the class, this method is called.
    public override bool TrySetMember(SetMemberBinder binder, object? value)
    {
        if (Locked)
        {
            return false;
        }

        // Converting the property name to lowercase
        // so that property names become case-insensitive.
        _dictionary[binder.Name.ToLower()] = value;

        // You can always add a value to a dictionary,
        // so this method always returns true.
        return true;
    }

    public dynamic? Get(string key)
    {
        var lvk = key.ToLower();
        if (_dictionary.ContainsKey(lvk))
        {
            return _dictionary[lvk];
        }
        return null;
    }

    public void Set(string key, object value)
    {
        if (Locked)
        {
            return;
        }

        var lvk = key.ToLower();
        if (_dictionary.ContainsKey(lvk))
        {
            _dictionary[lvk] = value;
        }
        else
        {
            _dictionary.Add(lvk, value);
        }
    }

    public bool Has(string key)
    {
        return _dictionary.ContainsKey(key.ToLower());
    }

    public bool Locked { get; private set; }

    public void Lock()
    {
        this.Locked = true;
    }
}
