using Mue.Server.Core.Objects;
using Mue.Server.Core.System;

namespace Mue.Server.Core.Models;

public class ObjectIdPropListHelper
{
    private IGameObject? _obj;
    private IWorld? _world;
    private ObjectId? _objId;
    private string _propKey;

    public ObjectIdPropListHelper(IGameObject obj, string propKey)
    {
        _obj = obj;
        _propKey = propKey;
    }

    public ObjectIdPropListHelper(IWorld world, ObjectId id, string propKey)
    {
        _world = world;
        _objId = id;
        _propKey = propKey;
    }

    public async Task<bool> Add(ObjectId id)
    {
        var hs = await this.GetValue();
        if (hs.Add(id))
        {
            await SetValue(hs);
            return true;
        }
        return false;
    }

    public async Task<bool> Remove(ObjectId id)
    {
        var hs = await this.GetValue();
        if (hs.Remove(id))
        {
            await SetValue(hs);
            return true;
        }
        return false;
    }

    public async Task<bool> Contains(ObjectId id)
    {
        var hs = await this.GetValue();
        return hs.Contains(id);
    }

    public async Task<IList<ObjectId>> All()
    {
        var hs = await this.GetValue();
        return hs.ToList();
    }

    private async Task<HashSet<ObjectId>> GetValue()
    {
        var propVal = await GetProp();
        var listVal = propVal?.ListValue;
        if (listVal == null)
        {
            return new HashSet<ObjectId>();
        }

        var existingIds = listVal.Where(w => w.ValueType == FlatPropValueType.ObjectId).Select(s => s.ObjectIdValue).WhereNotNull();
        if (existingIds == null)
        {
            return new HashSet<ObjectId>();
        }

        return new HashSet<ObjectId>(existingIds);
    }

    private async Task SetValue(HashSet<ObjectId> hs)
    {
        var newListVals = hs.Select(s => new FlatPropValue(s));
        var propVal = new PropValue(newListVals);
        await SetProp(propVal);
    }

    private Task<PropValue> GetProp()
    {
        if (_obj != null)
        {
            return _obj.GetProp(_propKey);
        }
        else if (_world != null && _objId != null)
        {
            return _world.StorageManager.GetProp(_objId, _propKey);
        }
        else
        {
            throw new InvalidOperationException();
        }
    }

    private Task SetProp(PropValue val)
    {
        if (_obj != null)
        {
            return _obj.SetProp(_propKey, val);
        }
        else if (_world != null && _objId != null)
        {
            return _world.StorageManager.SetProp(_objId, _propKey, val);
        }
        else
        {
            throw new InvalidOperationException();
        }
    }
}
