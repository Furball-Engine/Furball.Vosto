using System;
using System.Collections.Generic;

namespace Furball.Vosto.ShadingLanguage {
    public class Consumer<T> {
public Consumer(IEnumerable<T> enumerable)
        {
            Enumerator = enumerable.GetEnumerator();
        }

        protected IEnumerator<T> Enumerator { get; }

        public int ConsumedCount { get; private set; }

        public T? LastConsumed { get; private set; }

        private object? _onHold;

        public virtual bool TryPeekNext(out T? value)
        {
            value = default;

            if (_onHold != null)
            {
                value = (T) _onHold;
                return true;
            }

            if (!Enumerator.MoveNext())
                return false;

            _onHold = value = Enumerator.Current;
            return true;
        }

        public virtual T PeekNext() {
            T value = default;

            if (_onHold != null)
            {
                return (T) _onHold;
            }

            return Enumerator.Current;
        }

        public TResult TryConsumeNextAndThen<TResult>(Func<bool, T, TResult> function)
        {
            bool consumed = TryConsumeNext(out T? value);
            return function(consumed, value);
        }

        public bool SkipOne() => TryConsumeNext(out _);

        public void SkipTill(Func<T, bool> predicate)
        {
            for (;;)
            {
                if (!TryPeekNext(out T t))
                    break;

                if (!predicate(t!))
                    break;

                SkipOne();
            }
        }

        public void SkipTill<TState>(Func<TState, T, (bool, TState)> predicate, TState initialState)
        {
            for (;;)
            {
                if (!TryPeekNext(out T t))
                    break;

                (bool result, TState newState) = predicate(initialState, t!);

                if (!result)
                    break;

                initialState = newState;

                SkipOne();
            }
        }

        public virtual bool TryConsumeNext(out T? value)
        {
            value = default;
            ConsumedCount++;

            if (_onHold != null)
            {
                value = (T) _onHold;
                _onHold = null;
                return true;
            }

            if (!Enumerator.MoveNext())
                return false;

            value = Enumerator.Current;

            LastConsumed = value;
            return true;
        }
    }
}
