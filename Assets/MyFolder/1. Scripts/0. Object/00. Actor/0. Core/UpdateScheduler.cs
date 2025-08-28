using System.Collections.Generic;

namespace MyFolder._1._Scripts._0._Object._00._Actor._0._Core
{
    /// <summary>
    /// 우선순위 기반 업데이트 스케줄러
    /// </summary>
    public sealed class UpdateScheduler
    {
        private readonly List<IActorUpdatable> _items = new List<IActorUpdatable>();
        private readonly List<IActorUpdatable> _toAdd = new List<IActorUpdatable>();
        private readonly List<IActorUpdatable> _toRemove = new List<IActorUpdatable>();
        private readonly Dictionary<IActorUpdatable, int> _registrationOrder = new Dictionary<IActorUpdatable, int>();
        private int _counter = 0;

        /// <summary>
        /// 업데이트 가능한 컴포넌트 추가
        /// </summary>
        public void Add(IActorUpdatable updatable)
        {
            if (updatable == null) return;
            _toAdd.Add(updatable);
        }

        /// <summary>
        /// 업데이트 가능한 컴포넌트 제거
        /// </summary>
        public void Remove(IActorUpdatable updatable)
        {
            if (updatable == null) return;
            _toRemove.Add(updatable);
        }

        /// <summary>
        /// 추가/제거 대기 중인 항목들을 실제로 반영
        /// </summary>
        private void Flush()
        {
            // 제거 처리
            if (_toRemove.Count > 0)
            {
                for (int i = 0; i < _toRemove.Count; i++)
                {
                    var item = _toRemove[i];
                    _items.Remove(item);
                    _registrationOrder.Remove(item);
                }
                _toRemove.Clear();
            }

            // 추가 처리
            if (_toAdd.Count > 0)
            {
                for (int i = 0; i < _toAdd.Count; i++)
                {
                    var item = _toAdd[i];
                    _items.Add(item);
                    _registrationOrder[item] = _counter++; // 등록 순서 기록
                }
                _toAdd.Clear();

                // 우선순위로 정렬 (Priority 낮은 것부터, 동일하면 등록 순서대로)
                _items.Sort(Compare);
            }
        }

        /// <summary>
        /// 우선순위 비교 함수
        /// </summary>
        private int Compare(IActorUpdatable a, IActorUpdatable b)
        {
            int priorityComparison = a.Priority.CompareTo(b.Priority);
            if (priorityComparison != 0)
                return priorityComparison;

            // 우선순위가 같으면 등록 순서로 안정 정렬
            return _registrationOrder[a].CompareTo(_registrationOrder[b]);
        }

        /// <summary>
        /// Update 실행
        /// </summary>
        public void RunUpdate()
        {
            Flush();
            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].Update();
            }
        }

        /// <summary>
        /// FixedUpdate 실행
        /// </summary>
        public void RunFixedUpdate()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].FixedUpdate();
            }
        }

        /// <summary>
        /// LateUpdate 실행
        /// </summary>
        public void RunLateUpdate()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].LateUpdate();
            }
        }

        /// <summary>
        /// 현재 등록된 컴포넌트 수
        /// </summary>
        public int Count => _items.Count;

        /// <summary>
        /// 모든 컴포넌트 제거
        /// </summary>
        public void Clear()
        {
            _items.Clear();
            _toAdd.Clear();
            _toRemove.Clear();
            _registrationOrder.Clear();
            _counter = 0;
        }
    }
}
