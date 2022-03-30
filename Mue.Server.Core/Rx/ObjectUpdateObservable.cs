using System;
using System.Reactive.Subjects;
using Mue.Server.Core.Models;

namespace Mue.Server.Core.Rx
{
    public class ObjectUpdateObservable : IObservable<ObjectUpdate>
    {
        private Subject<ObjectUpdate> _subject = new Subject<ObjectUpdate>();

        public IDisposable Subscribe(IObserver<ObjectUpdate> observer)
        {
            return _subject.Subscribe(observer);
        }

        public void PublishObjectEvent<T>(ObjectId id, string eventName, T meta) where T : IObjectUpdateResult
        {
            var update = new ObjectUpdate
            {
                Id = id,
                EventName = eventName,
                Meta = meta,
            };

            _subject.OnNext(update);
        }

        public void PublishPlayerEvent<T>(ObjectId id, string eventName, T meta) where T : IPlayerUpdateResult
        {
            var update = new PlayerUpdate
            {
                Id = id,
                EventName = eventName,
                Meta = meta,
            };

            _subject.OnNext(update);
        }
    }
}