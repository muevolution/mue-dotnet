using System;
using System.Reactive.Subjects;
using Mue.Server.Core.Models;

namespace Mue.Server.Core.Rx
{
    public class WorldUpdateObservable : IObservable<WorldUpdate>
    {
        private Subject<WorldUpdate> _subject = new Subject<WorldUpdate>();

        public IDisposable Subscribe(IObserver<WorldUpdate> observer)
        {
            return _subject.Subscribe(observer);
        }

        public void PublishEvent<T>(string instanceId, string eventName, ObjectUpdate objUpdate = null)
        {
            var update = new WorldUpdate
            {
                Instance = instanceId,
                EventName = eventName,
                ObjectUpdate = objUpdate,
            };

            _subject.OnNext(update);
        }
    }

    public class WorldUpdate : IGlobalUpdate
    {
        public const string EVENT_JOINED = "joined";
        public const string EVENT_INVALIDATE_SCRIPT = "invalidate_script";
        public const string EVENT_UPDATE_OBJECT = "update_object";

        public string Instance { get; set; }
        public string EventName { get; set; }
        public ObjectUpdate ObjectUpdate { get; set; }
    }
}