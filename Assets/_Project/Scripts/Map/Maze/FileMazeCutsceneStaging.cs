using System.Collections;
using System.Collections.Generic;
using Core;
using Core.Stage;
using TMPro;
using UIandCinematic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Map.Maze
{
    /// <summary>파일 미로 컷신의 관리자, 파일 전달 및 도둑 군중 연출.</summary>
    public class FileMazeCutsceneStaging : MonoBehaviour
    {
        #region Constants
        private const int FILE_COLUMNS = 16;
        private const int FILE_ROWS = 22;
        private const float FILE_PIXEL_SIZE = 0.0364f;
        private const float FILE_BACK_Z = 0.015f;
        private const float FILE_BASE_FRONT_Z = -0.015f;
        private const float FILE_RAISED_FRONT_Z = -0.03f;
        private const int PIXEL_DARK = 1;
        private const int PIXEL_SHADE = 2;
        private const int PIXEL_EDGE = 3;
        private const int PIXEL_LIGHT = 4;
        private const float FILE_GLOW_MARGIN = 0.28f;
        private const float FILE_GLOW_STRENGTH = 0.6f;
        private const int FILE_GLOW_TEXTURE_SIZE = 64;
        private static readonly int[] PixelCubeTriangles =
        {
            0, 3, 2, 0, 2, 1,
            5, 6, 7, 5, 7, 4,
            4, 7, 3, 4, 3, 0,
            1, 2, 6, 1, 6, 5,
            3, 7, 6, 3, 6, 2,
            1, 5, 4, 1, 4, 0
        };
        #endregion

        #region Serialized Fields
        [SerializeField] private GameObject _administratorPrefab;
        [SerializeField] private GameObject _thiefPrefab;
        [SerializeField] private SOCutscene _round2Cutscene;
        [SerializeField] private SOCutscene _round3Cutscene;
        [SerializeField] private FileApp _fileApp;
        #endregion

        #region Private Fields
        private readonly List<GameObject> _actors = new();
        private readonly List<Material> _materials = new();
        private readonly List<Mesh> _meshes = new();
        private Texture2D _fileGlowTexture;
        private CinemachineCamera _camera;
        private GameObject _administrator;
        private PlayableGraph _idleGraph;
        private AnimationClipPlayable _idlePlayable;
        private float _idleDuration;
        private float _idleTime;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            CinematicManager.OnCutsceneStepStarted += HandleStep;
            CinematicManager.OnPlayFinished += HandleFinished;
        }

        private void OnDisable()
        {
            CinematicManager.OnCutsceneStepStarted -= HandleStep;
            CinematicManager.OnPlayFinished -= HandleFinished;
            StopAllCoroutines();
            Cleanup(true);
        }

        private void Update()
        {
            if (!_idleGraph.IsValid() || _idleDuration <= 0f) return;
            _idleTime = (_idleTime + Time.unscaledDeltaTime) % _idleDuration;
            _idlePlayable.SetTime(_idleTime);
            _idleGraph.Evaluate(0f);
        }
        #endregion

        #region Private Methods
        private void HandleStep(SOCutscene cutscene, int index)
        {
            if (cutscene == _round2Cutscene && index == 0) SetupAdministrator();
            if (cutscene != _round3Cutscene) return;

            if (index == 0) SetupAdministrator();
            else if (index == 2) StartCoroutine(CoFiles());
            else if (index == 8)
            {
                PlaceCamera(true);
                StartCoroutine(CoThieves());
            }
        }

        private Transform PlayerTransform()
        {
            return GameManager.Instance != null && GameManager.Instance.CurrentPlayerFacade != null
                ? GameManager.Instance.CurrentPlayerFacade.transform : null;
        }

        private static Vector3 Forward(Transform player)
        {
            Vector3 forward = Vector3.ProjectOnPlane(player.forward, Vector3.up).normalized;
            return forward.sqrMagnitude < 0.01f ? Vector3.forward : forward;
        }

        private void SetupCamera()
        {
            if (_camera != null)
            {
                PlaceCamera(false);
                return;
            }
            GameObject cameraObject = new GameObject("FileMazeCutsceneCamera");
            _camera = cameraObject.AddComponent<CinemachineCamera>();
            _camera.Priority = 50;
            cameraObject.AddComponent<CinematicCamera>().SetKey("FileMazeStaging");
            PlaceCamera(false);
        }

        private void PlaceCamera(bool isFront)
        {
            Transform player = PlayerTransform();
            if (player == null || _camera == null) return;
            Vector3 forward = Forward(player);
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 target = player.position + Vector3.up * 1.25f;
            Vector3 desired = isFront
                ? player.position + forward * 5.0f + right * 1.0f + Vector3.up * 2.2f
                : player.position - forward * 4.5f + Vector3.up * 2.4f;
            Vector3 offset = desired - target;
            float distance = offset.magnitude;
            foreach (RaycastHit hit in Physics.SphereCastAll(target, 0.15f, offset.normalized, distance, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(player) || hit.distance <= 0.05f) continue;
                distance = Mathf.Min(distance, Mathf.Max(0.25f, hit.distance - 0.2f));
            }
            Vector3 position = target + offset.normalized * distance;
            Vector3 lookAt = isFront ? target : target + forward * 1.5f;
            _camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(lookAt - position));
        }

        private void SetupAdministrator()
        {
            Transform player = PlayerTransform();
            if (player == null || _administratorPrefab == null) return;
            SetupCamera();
            Vector3 forward = Forward(player);
            _administrator = Instantiate(_administratorPrefab, player.position + forward * 3.0f + Vector3.up * 1.4f, Quaternion.LookRotation(-forward));
            foreach (Animator animator in _administrator.GetComponentsInChildren<Animator>())
            {
                AnimationClip idle = null;
                if (animator.runtimeAnimatorController != null)
                {
                    foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                        if (clip != null && clip.name == "Idle") { idle = clip; break; }
                }
                if (idle == null) continue;
                // 컷신 카메라 전환 중에도 바인드 포즈에 머물지 않도록 항상 본을 갱신한다.
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                _idleGraph = PlayableGraph.Create("FileAdministratorIdle");
                _idleGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                _idlePlayable = AnimationClipPlayable.Create(_idleGraph, idle);
                AnimationPlayableOutput.Create(_idleGraph, "Idle", animator).SetSourcePlayable(_idlePlayable);
                _idleDuration = idle.length;
                _idleTime = 0f;
                _idleGraph.Play();
                _idleGraph.Evaluate(0f);
                break;
            }
            _actors.Add(_administrator);
        }

        private void HandleFinished(CinematicManager manager)
        {
            StopAllCoroutines();
            Cleanup(false);
        }

        private void Cleanup(bool shouldDestroyCamera)
        {
            if (_idleGraph.IsValid()) _idleGraph.Destroy();
            foreach (GameObject actor in _actors) if (actor != null) Destroy(actor);
            _actors.Clear();
            foreach (Material material in _materials) if (material != null) Destroy(material);
            _materials.Clear();
            foreach (Mesh mesh in _meshes) if (mesh != null) Destroy(mesh);
            _meshes.Clear();
            if (_fileGlowTexture != null) Destroy(_fileGlowTexture);
            _fileGlowTexture = null;
            // 복귀 블렌드가 진행되는 동안 마지막 카메라를 유지한다.
            if (shouldDestroyCamera && _camera != null)
            {
                Destroy(_camera.gameObject);
                _camera = null;
            }
            _administrator = null;
        }
        #endregion

        #region Coroutines
        private GameObject CreateMusicFile(string label, Color color, int type)
        {
            var card = new GameObject("MusicFile_" + label);
            int[,] pixels = BuildFilePixels(type);
            Color dark = Color.Lerp(Color.black, color, 0.16f);
            AddPixelLayer(card.transform, pixels, PIXEL_DARK, dark, FILE_BASE_FRONT_Z);
            AddPixelLayer(card.transform, pixels, PIXEL_SHADE, Color.Lerp(Color.black, color, 0.55f), FILE_BASE_FRONT_Z);
            AddPixelLayer(card.transform, pixels, PIXEL_EDGE, color, FILE_RAISED_FRONT_Z);
            AddPixelLayer(card.transform, pixels, PIXEL_LIGHT, Color.Lerp(color, Color.white, 0.65f), FILE_RAISED_FRONT_Z);
            AddFileGlow(card.transform, color);

            GameObject tag = new GameObject("FileType");
            tag.transform.SetParent(card.transform, false);
            tag.transform.localPosition = new Vector3(0f, -0.255f, FILE_RAISED_FRONT_Z - 0.005f);
            tag.transform.localRotation = Quaternion.identity;
            TextMeshPro text = tag.AddComponent<TextMeshPro>();
            text.text = type == 1 ? ".WAV" : ".TXT";
            text.fontSize = 1.2f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = dark;
            text.rectTransform.sizeDelta = new Vector2(0.44f, 0.145f);
            return card;
        }

        /// <summary>시안 D의 픽셀 파일 아이콘을 행·열 격자로 만든다. 0행이 맨 위다.</summary>
        private static int[,] BuildFilePixels(int type)
        {
            var pixels = new int[FILE_ROWS, FILE_COLUMNS];
            for (int y = 0; y < FILE_ROWS; y++)
            {
                for (int x = 0; x < FILE_COLUMNS; x++)
                {
                    if (x > 11 && y < x - 11) continue;
                    bool isEdge = x == 0 || x == FILE_COLUMNS - 1 || y == 0 || y == FILE_ROWS - 1 || (x > 11 && y == x - 11);
                    if (isEdge) pixels[y, x] = PIXEL_EDGE;
                    else if (x >= 11 && y <= 4) pixels[y, x] = PIXEL_LIGHT;
                    else pixels[y, x] = x < 2 || y < 2 ? PIXEL_SHADE : PIXEL_DARK;
                }
            }

            // 접힌 모서리 안쪽 선과 확장자 띠
            for (int y = 1; y <= 4; y++) pixels[y, 11] = PIXEL_EDGE;
            for (int x = 12; x <= 14; x++) pixels[4, x] = PIXEL_EDGE;
            for (int y = 16; y <= 19; y++)
                for (int x = 2; x <= 13; x++) pixels[y, x] = PIXEL_EDGE;

            if (type == 0)
            {
                int[] widths = { 10, 10, 9, 10, 6 };
                for (int i = 0; i < widths.Length; i++)
                    for (int x = 0; x < widths[i]; x++) pixels[6 + i * 2, 3 + x] = PIXEL_LIGHT;
            }
            else if (type == 1)
            {
                int[] heights = { 2, 6, 10, 4, 8 };
                for (int i = 0; i < heights.Length; i++)
                    for (int y = 0; y < heights[i]; y++) pixels[10 - heights[i] / 2 + y, 3 + i * 2] = PIXEL_LIGHT;
            }
            else
            {
                string[] note = { "..#####", "..#...#", "..#...#", "..#...#", "###.###", "###.###" };
                for (int y = 0; y < note.Length; y++)
                    for (int x = 0; x < note[y].Length; x++)
                        if (note[y][x] == '#') pixels[7 + y, 4 + x] = PIXEL_LIGHT;
            }
            return pixels;
        }

        /// <summary>맵 Bloom이 약해 네온 빛 번짐은 파일 뒤의 가산 혼합 판으로 표현한다.</summary>
        private void AddFileGlow(Transform parent, Color color)
        {
            float halfWidth = FILE_COLUMNS * FILE_PIXEL_SIZE * 0.5f + FILE_GLOW_MARGIN;
            float halfHeight = FILE_ROWS * FILE_PIXEL_SIZE * 0.5f + FILE_GLOW_MARGIN;
            if (_fileGlowTexture == null) _fileGlowTexture = CreateFileGlowTexture(halfWidth, halfHeight);

            var mesh = new Mesh { name = "RewardFileGlow" };
            mesh.vertices = new[]
            {
                new Vector3(-halfWidth, -halfHeight, FILE_BACK_Z), new Vector3(halfWidth, -halfHeight, FILE_BACK_Z),
                new Vector3(halfWidth, halfHeight, FILE_BACK_Z), new Vector3(-halfWidth, halfHeight, FILE_BACK_Z)
            };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            mesh.triangles = new[] { 0, 3, 2, 0, 2, 1 };
            mesh.RecalculateBounds();
            _meshes.Add(mesh);

            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 2f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.mainTexture = _fileGlowTexture;
            material.color = color * FILE_GLOW_STRENGTH;
            _materials.Add(material);

            var glow = new GameObject("FileGlow");
            glow.transform.SetParent(parent, false);
            glow.AddComponent<MeshFilter>().sharedMesh = mesh;
            glow.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Texture2D CreateFileGlowTexture(float halfWidth, float halfHeight)
        {
            var texture = new Texture2D(FILE_GLOW_TEXTURE_SIZE, FILE_GLOW_TEXTURE_SIZE, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < FILE_GLOW_TEXTURE_SIZE; y++)
            {
                for (int x = 0; x < FILE_GLOW_TEXTURE_SIZE; x++)
                {
                    // 파일 외곽에서 가장자리까지 빛이 줄어들도록 파일 밖 거리로 밝기를 정한다.
                    float offsetX = Mathf.Abs((x + 0.5f) / FILE_GLOW_TEXTURE_SIZE * 2f - 1f) * halfWidth;
                    float offsetY = Mathf.Abs((y + 0.5f) / FILE_GLOW_TEXTURE_SIZE * 2f - 1f) * halfHeight;
                    float distanceX = Mathf.Max(0f, offsetX - (halfWidth - FILE_GLOW_MARGIN));
                    float distanceY = Mathf.Max(0f, offsetY - (halfHeight - FILE_GLOW_MARGIN));
                    float distance = Mathf.Sqrt(distanceX * distanceX + distanceY * distanceY);
                    float brightness = Mathf.Pow(Mathf.Clamp01(1f - distance / FILE_GLOW_MARGIN), 2f);
                    texture.SetPixel(x, y, new Color(brightness, brightness, brightness, brightness));
                }
            }
            texture.Apply();
            return texture;
        }

        private void AddPixelLayer(Transform parent, int[,] pixels, int pixel, Color color, float frontZ)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int y = 0; y < FILE_ROWS; y++)
            {
                for (int x = 0; x < FILE_COLUMNS; x++)
                {
                    if (pixels[y, x] != pixel) continue;
                    float left = (x - FILE_COLUMNS * 0.5f) * FILE_PIXEL_SIZE;
                    float right = left + FILE_PIXEL_SIZE;
                    float top = (FILE_ROWS * 0.5f - y) * FILE_PIXEL_SIZE;
                    float bottom = top - FILE_PIXEL_SIZE;
                    int start = vertices.Count;
                    vertices.Add(new Vector3(left, bottom, frontZ));
                    vertices.Add(new Vector3(right, bottom, frontZ));
                    vertices.Add(new Vector3(right, top, frontZ));
                    vertices.Add(new Vector3(left, top, frontZ));
                    vertices.Add(new Vector3(left, bottom, FILE_BACK_Z));
                    vertices.Add(new Vector3(right, bottom, FILE_BACK_Z));
                    vertices.Add(new Vector3(right, top, FILE_BACK_Z));
                    vertices.Add(new Vector3(left, top, FILE_BACK_Z));
                    foreach (int index in PixelCubeTriangles) triangles.Add(start + index);
                }
            }

            var mesh = new Mesh { name = "RewardFilePixels" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            _meshes.Add(mesh);

            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.color = color;
            _materials.Add(material);

            var layer = new GameObject("FilePixels");
            layer.transform.SetParent(parent, false);
            layer.AddComponent<MeshFilter>().sharedMesh = mesh;
            layer.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        /// <summary>관리자 전달과 마지막 전송에서 같은 파일 디자인을 사용한다.</summary>
        public GameObject CreateRewardFile(int type)
        {
            string[] labels = { "가사", "멜로디", "코드" };
            Color[] colors = { new Color(1f, 0.3f, 0.6f), new Color(0.2f, 0.8f, 1f), new Color(0.7f, 0.4f, 1f) };
            if (type < 0 || type >= labels.Length) return null;
            GameObject card = CreateMusicFile(labels[type], colors[type], type);
            _actors.Add(card);
            return card;
        }

        private IEnumerator CoFiles()
        {
            Transform player = PlayerTransform();
            if (player == null) yield break;
            Vector3 forward = Forward(player);
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            string[] labels = { "가사", "멜로디", "코드" };
            Color[] colors = { new Color(1f, 0.3f, 0.6f), new Color(0.2f, 0.8f, 1f), new Color(0.7f, 0.4f, 1f) };
            var cards = new List<GameObject>();
            for (int i = 0; i < 3; i++)
            {
                GameObject card = CreateMusicFile(labels[i], colors[i], i);
                card.transform.position = player.position + forward * 2.5f + right * ((i - 1) * 0.8f) + Vector3.up * 1.75f;
                card.transform.rotation = Quaternion.LookRotation(forward);
                cards.Add(card);
                _actors.Add(card);
            }
            float elapsed = 0f;
            while (elapsed < 2.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                if (elapsed > 1f)
                    foreach (GameObject card in cards) if (card != null) card.transform.position = Vector3.Lerp(card.transform.position, player.position + Vector3.up, Time.unscaledDeltaTime * 2f);
                yield return null;
            }
            foreach (GameObject card in cards) if (card != null) Destroy(card);
            if (_idleGraph.IsValid()) _idleGraph.Destroy();
            if (_administrator != null) Destroy(_administrator);
            _administrator = null;
        }

        private IEnumerator CoThieves()
        {
            Transform player = PlayerTransform();
            if (player == null || _thiefPrefab == null) yield break;
            Transform visual = _thiefPrefab.transform.Find("Robot_Enemy_Prefab");
            if (visual == null) yield break;
            Vector3 forward = Forward(player);
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 origin = player.position;
            Vector3 portalPosition = origin - forward * 6.5f;
            GameObject portal = null;
            GameObject portalPrefab = Resources.Load<GameObject>("FileMaze/ThiefPortal");
            if (portalPrefab != null)
            {
                portal = Instantiate(portalPrefab, portalPosition + Vector3.up * 1.5f, Quaternion.LookRotation(forward));
                portal.name = "ThiefPortal";
                portal.transform.localScale = Vector3.one * 3.4f;
                foreach (MonoBehaviour spinner in portal.GetComponentsInChildren<MonoBehaviour>()) spinner.enabled = false;
                foreach (Renderer renderer in portal.GetComponentsInChildren<Renderer>())
                {
                    Material[] originals = renderer.sharedMaterials;
                    var darkMaterials = new Material[originals.Length];
                    for (int i = 0; i < originals.Length; i++)
                    {
                        if (originals[i] == null) continue;
                        var material = new Material(originals[i]);
                        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.black);
                        if (material.HasProperty("_AlarmImmune")) material.SetFloat("_AlarmImmune", 1f);
                        if (material.HasProperty("_DarkImmune")) material.SetFloat("_DarkImmune", 1f);
                        if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        darkMaterials[i] = material;
                        _materials.Add(material);
                    }
                    renderer.sharedMaterials = darkMaterials;
                }
                Transform core = portal.transform.Find("Core");
                if (core != null) core.gameObject.SetActive(false);
                CreatePortalDarkCenter(portal.transform);
                _actors.Add(portal);
            }
            var thieves = new List<GameObject>();
            var speeds = new List<float>();
            var swayPhases = new List<float>();
            var spawnTimes = new List<float>();
            for (int i = 0; i < 24; i++)
            {
                float across = Random.Range(-1.25f, 1.25f);
                Vector3 position = portalPosition + forward * 0.1f + right * across;
                GameObject actor = Instantiate(visual.gameObject, position, Quaternion.LookRotation(forward));
                actor.transform.localScale = Vector3.Scale(_thiefPrefab.transform.localScale, visual.localScale);
                foreach (MonoBehaviour behaviour in actor.GetComponentsInChildren<MonoBehaviour>()) behaviour.enabled = false;
                float speed = Random.Range(4.5f, 6f);
                foreach (Animator animator in actor.GetComponentsInChildren<Animator>())
                {
                    animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    animator.SetBool("IsMoving", true);
                    animator.SetFloat("MoveAnimSpeed", speed / 3f);
                }
                thieves.Add(actor);
                speeds.Add(speed);
                swayPhases.Add(Random.Range(0f, Mathf.PI * 2f));
                spawnTimes.Add(0.15f + i * 0.09f + Random.Range(0f, 0.12f));
                actor.SetActive(false);
                _actors.Add(actor);
            }
            float elapsed = 0f;
            while (elapsed < 4f)
            {
                float delta = Time.unscaledDeltaTime;
                elapsed += delta;
                if (portal != null)
                {
                    float opening = Mathf.Clamp01(elapsed / 0.25f);
                    float closing = Mathf.Clamp01((4f - elapsed) / 0.35f);
                    portal.transform.localScale = Vector3.one * (3.4f * Mathf.Min(opening, closing));
                    for (int i = 1; i < portal.transform.childCount; i++)
                        portal.transform.GetChild(i).Rotate(0f, 0f, (i % 2 == 0 ? 32f : -25f) * delta, Space.Self);
                }
                for (int i = 0; i < thieves.Count; i++)
                {
                    GameObject actor = thieves[i];
                    if (actor == null) continue;
                    if (elapsed < spawnTimes[i]) continue;
                    if (!actor.activeSelf)
                    {
                        actor.SetActive(true);
                        foreach (Animator animator in actor.GetComponentsInChildren<Animator>())
                        {
                            animator.SetBool("IsMoving", true);
                            animator.SetFloat("MoveAnimSpeed", speeds[i] / 3f);
                        }
                    }
                    float emergence = Mathf.Clamp01((elapsed - spawnTimes[i]) / 0.3f);
                    actor.transform.localScale = Vector3.Scale(_thiefPrefab.transform.localScale, visual.localScale) * emergence;
                    float lateralSpeed = Mathf.Sin(elapsed * 1.7f + swayPhases[i]) * 0.55f;
                    float across = Vector3.Dot(actor.transform.position - origin, right);
                    float nextAcross = Mathf.Clamp(across + lateralSpeed * delta, -1.8f, 1.8f);
                    Vector3 movement = forward * (speeds[i] * delta) + right * (nextAcross - across);
                    actor.transform.position += movement;
                    if (movement.sqrMagnitude > 0.000001f) actor.transform.rotation = Quaternion.LookRotation(movement);
                }
                yield return null;
            }
            foreach (GameObject actor in thieves) if (actor != null) Destroy(actor);
            if (portal != null) Destroy(portal);
        }

        private void CreatePortalDarkCenter(Transform portal)
        {
            const int SEGMENTS = 48;
            var vertices = new Vector3[SEGMENTS + 1];
            var triangles = new int[SEGMENTS * 6];
            for (int i = 0; i < SEGMENTS; i++)
            {
                float angle = 2f * Mathf.PI * i / SEGMENTS;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * 0.39f, Mathf.Sin(angle) * 0.39f, 0f);
                int next = (i + 1) % SEGMENTS + 1;
                int at = i * 6;
                triangles[at] = 0; triangles[at + 1] = i + 1; triangles[at + 2] = next;
                triangles[at + 3] = 0; triangles[at + 4] = next; triangles[at + 5] = i + 1;
            }
            var mesh = new Mesh { name = "ThiefPortalDarkCenter" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.color = Color.black;
            var center = new GameObject("BlackCenter");
            center.transform.SetParent(portal, false);
            center.transform.localPosition = new Vector3(0f, 0f, -0.03f);
            center.AddComponent<MeshFilter>().sharedMesh = mesh;
            center.AddComponent<MeshRenderer>().sharedMaterial = material;
            _meshes.Add(mesh);
            _materials.Add(material);
        }
        #endregion
    }
}
