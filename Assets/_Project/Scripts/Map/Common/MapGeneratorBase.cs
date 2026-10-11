using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

namespace Map.Common
{
    /// <summary>
    /// 런타임에 맵을 생성하는 컴포넌트들의 공통 부모.
    /// 시드, 생성/정리, 시작·도착 지점, 도착 지점 프리팹, 오브젝트 배치, NavMesh 베이크를 맡는다.
    /// 자식 클래스는 Build 에서 지형을 놓고 시작·도착 지점과 배치 가능한 자리만 알려 주면 된다.
    /// 씬이 로드될 때(Awake) 생성하므로, 스테이지에 다시 들어오면 새 맵이 만들어진다.
    /// </summary>
    [DefaultExecutionOrder(-200)]   // 플레이어가 시작 지점을 읽기 전에 맵이 만들어져 있어야 한다
    public abstract class MapGeneratorBase : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Generation")]
        [Tooltip("켜면 씬이 로드될 때 자동으로 생성한다.")]
        [SerializeField] private bool _shouldGenerateOnAwake = true;

        [Tooltip("켜면 생성할 때마다 다른 맵이 나온다. 끄면 Seed 값으로 항상 같은 맵이 나온다.")]
        [SerializeField] private bool _shouldUseRandomSeed = true;

        [Tooltip("Should Use Random Seed 가 꺼져 있을 때 쓰는 시드")]
        [SerializeField] private int _seed;

        [Header("Goal")]
        [Tooltip("도착 지점에 놓을 프리팹 (선택). 예: 로비로 가는 포탈")]
        [SerializeField] private GameObject _goalPrefab;

        [Header("Placements")]
        [Tooltip("맵 위에 무작위로 놓을 장애물·아이템 목록")]
        [SerializeField] private SMapPlacement[] _placements;

        [Header("Navigation")]
        [Tooltip("생성 후 베이크할 NavMeshSurface (선택). 적이 돌아다니는 맵에서만 연결한다.")]
        [SerializeField] private NavMeshSurface _navMeshSurface;

        // 에디터에서 미리 만들어 둔 맵도 다시 생성할 때 지울 수 있도록 저장한다
        [SerializeField, HideInInspector] private Transform _generatedRoot;

        // 씬을 다시 열거나 스크립트를 다시 컴파일해도 미리 만든 맵의 시드를 알 수 있도록 저장한다
        [SerializeField, HideInInspector] private int _lastSeed;
        #endregion

        #region Properties
        /// <summary>맵이 생성되어 있는지 여부.</summary>
        public bool IsGenerated => _generatedRoot != null;

        /// <summary>생성된 맵의 루트. 맵이 없으면 null. (에디터의 Undo 등록에 쓴다)</summary>
        public Transform GeneratedRoot => _generatedRoot;

        /// <summary>마지막 생성에 쓰인 시드. 같은 맵을 다시 만들려면 이 값을 Seed 에 넣는다.</summary>
        public int LastSeed => _lastSeed;

        /// <summary>플레이어가 시작할 위치.</summary>
        public Vector3 StartPosition { get; private set; }

        /// <summary>도착 지점 위치.</summary>
        public Vector3 GoalPosition { get; private set; }
        #endregion

        #region Events
        /// <summary>맵 생성이 끝났을 때 호출된다.</summary>
        public event Action OnGenerated;
        #endregion

        #region Unity Lifecycle
        protected virtual void Awake()
        {
            if (_shouldGenerateOnAwake)
            {
                Generate();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 이전에 만든 맵을 지우고 새로 생성한다.
        /// </summary>
        public void Generate()
        {
            Clear();

            _lastSeed = _shouldUseRandomSeed ? UnityEngine.Random.Range(int.MinValue, int.MaxValue) : _seed;
            var random = new System.Random(_lastSeed);

            _generatedRoot = new GameObject("GeneratedMap").transform;
            _generatedRoot.SetParent(transform, false);

            var placementPoints = new List<Vector3>();
            Build(_generatedRoot, random, placementPoints, out Vector3 startPosition, out Vector3 goalPosition);

            StartPosition = startPosition;
            GoalPosition = goalPosition;

            if (_goalPrefab != null)
            {
                Instantiate(_goalPrefab, goalPosition, Quaternion.identity, _generatedRoot);
            }

            PlaceObjects(placementPoints, random);

            // 에디터에서 미리 만들어 볼 때는 베이크하지 않는다
            if ((_navMeshSurface != null) && Application.isPlaying)
            {
                _navMeshSurface.BuildNavMesh();
            }

            OnGenerated?.Invoke();
        }

        /// <summary>
        /// 생성된 맵을 지운다.
        /// </summary>
        public void Clear()
        {
            if (_generatedRoot == null) return;

            if (Application.isPlaying)
            {
                // Destroy 는 프레임 끝에 처리되므로, 그 사이 새 맵과 겹치지 않게 먼저 꺼 둔다
                _generatedRoot.gameObject.SetActive(false);
                Destroy(_generatedRoot.gameObject);
            }
            else
            {
                DestroyImmediate(_generatedRoot.gameObject);
            }

            _generatedRoot = null;
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// 지형을 root 아래에 만든다.
        /// </summary>
        /// <param name="root">생성물이 들어갈 부모. 이 컴포넌트의 자식이다.</param>
        /// <param name="random">시드가 적용된 난수. 같은 시드에서 같은 맵이 나오도록 이것만 사용한다.</param>
        /// <param name="placementPoints">장애물·아이템을 놓을 수 있는 자리(바닥 중앙, 월드 좌표)를 채운다.</param>
        /// <param name="startPosition">플레이어가 시작할 위치 (월드 좌표)</param>
        /// <param name="goalPosition">도착 지점 위치 (월드 좌표)</param>
        protected abstract void Build(
            Transform root,
            System.Random random,
            List<Vector3> placementPoints,
            out Vector3 startPosition,
            out Vector3 goalPosition);
        #endregion

        #region Private Methods
        // 배치 목록의 순서대로, 남은 자리 가운데 무작위로 골라 놓는다. 자리가 다 차면 남은 것은 놓지 않는다.
        private void PlaceObjects(List<Vector3> placementPoints, System.Random random)
        {
            if (_placements == null) return;

            foreach (SMapPlacement placement in _placements)
            {
                if (placement.Prefab == null) continue;

                for (int i = 0; i < placement.Count; i++)
                {
                    if (placementPoints.Count == 0) return;

                    // 한 자리에는 하나만 놓는다
                    int index = random.Next(placementPoints.Count);
                    Vector3 position = placementPoints[index] + placement.Offset;
                    placementPoints.RemoveAt(index);

                    Instantiate(placement.Prefab, position, Quaternion.identity, _generatedRoot);
                }
            }
        }
        #endregion
    }
}
