using System;
using System.Collections.Generic;
using UnityEngine;

namespace AlexSamGame.Core
{
    /// <summary>Список таймеров, которые обновляются вместе. Аналог group в knife.timer.</summary>
    public sealed class TimerGroup
    {
        internal readonly List<TimerHandle> Items = new List<TimerHandle>();
        public int Count => Items.Count;
    }

    /// <summary>Порт knife.timer: Every / After / Tween + Update / Clear.</summary>
    public static class Timer
    {
        public static readonly TimerGroup DefaultGroup = new TimerGroup();

        // Timer.after(delay, callback)
        public static TimerHandle After(float delay, Action callback)
        {
            return new AfterTimer(delay, callback).AttachTo(DefaultGroup);
        }

        // Timer.every(interval, callback)
        public static EveryTimer Every(float interval, Action callback)
        {
            var timer = new EveryTimer(interval, callback);
            timer.AttachTo(DefaultGroup);
            return timer;
        }

        // Timer.tween(duration, definition): поля добавляются вызовами .To(...)
        public static TweenTimer Tween(float duration)
        {
            var timer = new TweenTimer(duration);
            timer.AttachTo(DefaultGroup);
            return timer;
        }

        // Timer.update(dt, group)
        public static void Update(float dt, TimerGroup group = null)
        {
            List<TimerHandle> items = (group ?? DefaultGroup).Items;

            // С конца, как в knife: удаление текущего таймера (или Clear из колбэка)
            // не ломает обход, а таймеры, созданные во время обхода, стартуют со следующего кадра.
            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (i >= items.Count)
                    continue;
                items[i].Tick(dt);
            }
        }

        // Timer.clear(group)
        public static void Clear(TimerGroup group = null)
        {
            List<TimerHandle> items = (group ?? DefaultGroup).Items;
            foreach (TimerHandle timer in items)
                timer.OnDetached();
            items.Clear();
        }

        // Статические поля переживают перезапуск Play, если в Enter Play Mode Options
        // выключена перезагрузка домена. Чистим default-группу при каждом старте.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Clear();
        }
    }

    /// <summary>То, что возвращают Every / After / Tween. Аналог объекта-таймера в knife.</summary>
    public abstract class TimerHandle
    {
        private TimerGroup group;   // где таймер сейчас; null — удалён
        private int index;          // позиция в group.Items
        private Action finish;
        protected float Elapsed;

        public bool IsActive => group != null;

        // :finish(callback)
        public TimerHandle Finish(Action callback)
        {
            finish = callback;
            return this;
        }

        // :remove()
        public TimerHandle Remove()
        {
            if (group == null)
                return this;

            // Удаление за O(1), как detach в knife: на место удаляемого ставим последний.
            List<TimerHandle> items = group.Items;
            int last = items.Count - 1;
            items[index] = items[last];
            items[index].index = index;
            items.RemoveAt(last);
            group = null;
            return this;
        }

        // :group(group) — перенести таймер в другую группу
        public TimerHandle Group(TimerGroup newGroup)
        {
            return AttachTo(newGroup ?? Timer.DefaultGroup);
        }

        internal TimerHandle AttachTo(TimerGroup newGroup)
        {
            Remove();
            index = newGroup.Items.Count;
            newGroup.Items.Add(this);
            group = newGroup;
            return this;
        }

        internal void OnDetached()
        {
            group = null;
        }

        internal abstract void Tick(float dt);

        // Сначала finish, потом удаление — порядок как в knife.
        protected void Complete()
        {
            if (finish != null)
                finish();
            Remove();
        }
    }

    internal sealed class AfterTimer : TimerHandle
    {
        private readonly float delay;
        private readonly Action callback;

        public AfterTimer(float delay, Action callback)
        {
            this.delay = delay;
            this.callback = callback;
        }

        internal override void Tick(float dt)
        {
            Elapsed += dt;
            if (Elapsed < delay)
                return;

            if (callback != null)
                callback();
            if (IsActive)
                Complete();
        }
    }

    public sealed class EveryTimer : TimerHandle
    {
        private readonly float interval;
        private readonly Action callback;
        private int limit;   // 0 — без ограничения

        public EveryTimer(float interval, Action callback)
        {
            if (interval <= 0f)
                throw new ArgumentException("Timer.Every: interval must be > 0");
            this.interval = interval;
            this.callback = callback;
        }

        // :limit(n) — сработать n раз и завершиться (тогда вызовется Finish)
        public EveryTimer Limit(int count)
        {
            limit = count;
            return this;
        }

        internal override void Tick(float dt)
        {
            Elapsed += dt;

            // while, а не if: при длинном кадре таймер «догоняет» пропущенные срабатывания.
            while (Elapsed >= interval)
            {
                Elapsed -= interval;

                if (callback != null)
                    callback();
                if (!IsActive)          // колбэк удалил таймер или очистил группу
                    return;

                if (limit > 0 && --limit == 0)
                {
                    Complete();
                    return;
                }
            }
        }
    }

    public sealed class TweenTimer : TimerHandle
    {
        private readonly float duration;
        private readonly List<Action<float>> steps = new List<Action<float>>();
        private readonly List<Action> finals = new List<Action>();
        private Func<float, float> ease = t => t;   // линейно, как easeLinear в knife

        public TweenTimer(float duration)
        {
            this.duration = duration;
        }

        // Одна строка «плана» knife: {target, key, initial, final}.
        // Начальное значение читается сразу — как planTween в момент создания твина.
        public TweenTimer To(Func<float> get, Action<float> set, float final)
        {
            float initial = get();
            steps.Add(t => set(Mathf.LerpUnclamped(initial, final, t)));
            finals.Add(() => set(final));
            return this;
        }

        public TweenTimer To(Func<Vector2> get, Action<Vector2> set, Vector2 final)
        {
            Vector2 initial = get();
            steps.Add(t => set(Vector2.LerpUnclamped(initial, final, t)));
            finals.Add(() => set(final));
            return this;
        }

        public TweenTimer To(Func<Vector3> get, Action<Vector3> set, Vector3 final)
        {
            Vector3 initial = get();
            steps.Add(t => set(Vector3.LerpUnclamped(initial, final, t)));
            finals.Add(() => set(final));
            return this;
        }

        public TweenTimer To(Func<Color> get, Action<Color> set, Color final)
        {
            Color initial = get();
            steps.Add(t => set(Color.LerpUnclamped(initial, final, t)));
            finals.Add(() => set(final));
            return this;
        }

        // :ease(fn) — функция от доли времени 0..1
        public TweenTimer Ease(Func<float, float> easing)
        {
            ease = easing;
            return this;
        }

        internal override void Tick(float dt)
        {
            Elapsed += dt;

            if (Elapsed >= duration)
            {
                // В конце — точно конечные значения, без погрешности интерполяции.
                foreach (Action setFinal in finals)
                    setFinal();
                Complete();
                return;
            }

            float t = ease(Elapsed / duration);
            foreach (Action<float> step in steps)
                step(t);
        }
    }
}
