using System;
using System.Collections.Generic;

namespace MiniGame.StarterKit.Runtime.Core
{
    /// <summary>
    /// 全局事件总线。支持按事件类型订阅与发布，无反射开销。
    /// </summary>
    public static class MGSEventBus
    {
        private static readonly Dictionary<Type, object> _handlers = new Dictionary<Type, object>();

        /// <summary>
        /// 订阅事件。
        /// </summary>
        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var obj))
            {
                obj = new EventHandlerList<T>();
                _handlers[type] = obj;
            }

            var list = (EventHandlerList<T>)obj;
            list.Add(handler);
        }

        /// <summary>
        /// 取消订阅事件。
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var obj)) return;

            var list = (EventHandlerList<T>)obj;
            list.Remove(handler);
        }

        /// <summary>
        /// 发布事件。
        /// </summary>
        public static void Publish<T>(T evt) where T : struct
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var obj)) return;

            var list = (EventHandlerList<T>)obj;
            list.Invoke(evt);
        }

        /// <summary>
        /// 清空所有订阅。
        /// </summary>
        public static void Clear()
        {
            foreach (var pair in _handlers)
            {
                if (pair.Value is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _handlers.Clear();
        }

        private class EventHandlerList<T> : IDisposable where T : struct
        {
            private readonly List<Action<T>> _actions = new List<Action<T>>();

            public void Add(Action<T> action)
            {
                if (!_actions.Contains(action))
                {
                    _actions.Add(action);
                }
            }

            public void Remove(Action<T> action)
            {
                _actions.Remove(action);
            }

            public void Invoke(T evt)
            {
                // 复制一份避免遍历时订阅变化导致异常
                var copy = new List<Action<T>>(_actions);
                for (int i = 0; i < copy.Count; i++)
                {
                    copy[i]?.Invoke(evt);
                }
            }

            public void Dispose()
            {
                _actions.Clear();
            }
        }
    }
}
