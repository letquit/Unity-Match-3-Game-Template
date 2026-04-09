using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Match3
{
    /// <summary>
    /// 三消游戏主控制器，处理游戏逻辑、输入响应、消除匹配等核心功能
    /// </summary>
    public class Match3 : MonoBehaviour
    {
        [SerializeField] private int width = 8;
        [SerializeField] private int height = 8;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector3 originPosition = Vector3.zero;
        [SerializeField] private bool debug = true;
        
        [SerializeField] private Gem gemPrefab;
        [SerializeField] private GemType[] gemTypes;
        [SerializeField] private Ease ease = Ease.InQuad;
        [SerializeField] private GameObject explosion;
        
        [SerializeField] private float selectedScaleMultiplier = 1.15f;
        [SerializeField] private float selectAnimDuration = 0.15f;
        
        private AudioManager audioManager;

        private GridSystem2D<GridObject<Gem>> grid;
        
        private InputReader inputReader;
        private Vector2Int selectedGem = Vector2Int.one * -1;
        private bool isBusy = false;
        
        private Tween selectedTween;
        private Transform selectedTransform;
        private Vector3 selectedGemOriginalScale = Vector3.one;

        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void Awake()
        {
            inputReader = GetComponent<InputReader>();
            audioManager = GetComponent<AudioManager>();
        }

        /// <summary>
        /// 游戏开始时初始化网格并解析初始匹配
        /// </summary>
        private void Start()
        {
            InitializeGrid();
            StartCoroutine(ResolveBoardAtStart());
            inputReader.FireAt += OnSelectGem;
        }

        /// <summary>
        /// 解析游戏开始时的初始匹配，直到没有匹配为止
        /// </summary>
        /// <returns>协程枚举器</returns>
        private IEnumerator ResolveBoardAtStart()
        {
            isBusy = true;

            while (true)
            {
                var matches = FindMatches(false);
                if (matches.Count == 0) break;

                yield return StartCoroutine(ResolveMatches(matches));
            }

            isBusy = false;
        }

        /// <summary>
        /// 销毁时移除事件监听器
        /// </summary>
        private void OnDestroy()
        {
            inputReader.FireAt -= OnSelectGem;
        }
        
        /// <summary>
        /// 处理宝石选择事件
        /// </summary>
        /// <param name="screenPos">屏幕坐标位置</param>
        private void OnSelectGem(Vector2 screenPos)
        {
            if (isBusy) return;

            var world = Camera.main.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, -Camera.main.transform.position.z)
            );
            world.z = 0f;

            var gridPos = grid.GetXY(world);

            if (!IsValidPosition(gridPos) || IsEmptyPosition(gridPos))
            {
                return;
            }

            if (selectedGem == gridPos)
            {
                DeselectGem();
                audioManager.PlayDeselect();
            }
            else if (selectedGem == Vector2Int.one * -1)
            {
                SelectGem(gridPos);
                audioManager.PlayClick();
            }
            else
            {
                if (!IsAdjacent(selectedGem, gridPos))
                {
                    SelectGem(gridPos);
                    audioManager.PlayClick();
                    return;
                }

                StartCoroutine(RunGameLoop(selectedGem, gridPos));
            }
        }
        
        /// <summary>
        /// 检查两个位置是否相邻（上下左右）
        /// </summary>
        /// <param name="a">第一个位置</param>
        /// <param name="b">第二个位置</param>
        /// <returns>是否相邻</returns>
        private bool IsAdjacent(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
        }
        
        /// <summary>
        /// 验证网格位置是否有效
        /// </summary>
        /// <param name="gridPos">网格坐标</param>
        /// <returns>位置是否有效</returns>
        private bool IsValidPosition(Vector2 gridPos) => gridPos.x >= 0 && gridPos.x < width && gridPos.y >= 0 && gridPos.y < height;
        
        /// <summary>
        /// 检查指定位置是否为空
        /// </summary>
        /// <param name="gridPos">网格坐标</param>
        /// <returns>位置是否为空</returns>
        private bool IsEmptyPosition(Vector2Int gridPos) => grid.GetValue(gridPos.x, gridPos.y) == null;

        /// <summary>
        /// 运行主要游戏循环：交换宝石、查找匹配、解析匹配
        /// </summary>
        /// <param name="gridPosA">第一个宝石位置</param>
        /// <param name="gridPosB">第二个宝石位置</param>
        /// <returns>协程枚举器</returns>
        private IEnumerator RunGameLoop(Vector2Int gridPosA, Vector2Int gridPosB)
        {
            if (isBusy) yield break;
            isBusy = true;

            yield return StartCoroutine(SwapGems(gridPosA, gridPosB));

            var firstMatches = FindMatches(true);

            if (firstMatches.Count == 0)
            {
                yield return StartCoroutine(SwapGems(gridPosB, gridPosA));
                DeselectGem();
                isBusy = false;
                yield break;
            }

            yield return StartCoroutine(ResolveMatches(firstMatches));

            while (true)
            {
                var matches = FindMatches(false);
                if (matches.Count == 0) break;
                yield return StartCoroutine(ResolveMatches(matches));
            }

            DeselectGem();
            isBusy = false;
        }
        
        /// <summary>
        /// 解析匹配的宝石：爆炸、下落、填充空位
        /// </summary>
        /// <param name="matches">匹配的宝石列表</param>
        /// <returns>协程枚举器</returns>
        private IEnumerator ResolveMatches(List<Vector2Int> matches)
        {
            yield return StartCoroutine(ExplodeGems(matches));
            yield return StartCoroutine(MakeGemsFall());
            yield return StartCoroutine(FillEmptySpots());
        }

        /// <summary>
        /// 填充空位，创建新的宝石
        /// </summary>
        /// <returns>协程枚举器</returns>
        private IEnumerator FillEmptySpots()
        {
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    if (grid.GetValue(x, y) == null)
                    {
                        CreateGem(x, y);
                        audioManager.PlayPop();
                        yield return new WaitForSeconds(0.1f);
                    }
                }
            }
        }

        /// <summary>
        /// 让宝石下落到正确位置
        /// </summary>
        /// <returns>协程枚举器</returns>
        private IEnumerator MakeGemsFall()
        {
            var fallingTweens = new List<Tween>();
    
            for (int column = 0; column < width; column++)
            {
                CompactColumn(column, fallingTweens);
            }
    
            if (fallingTweens.Count > 0)
            {
                audioManager.PlayWhoosh();
            }
    
            if (fallingTweens.Count > 0)
            {
                yield return fallingTweens[fallingTweens.Count - 1].WaitForCompletion();
            }
        }
        
        /// <summary>
        /// 压缩指定列，将非空宝石移动到连续位置
        /// </summary>
        /// <param name="column">列索引</param>
        /// <param name="fallingTweens">下落动画列表</param>
        private void CompactColumn(int column, List<Tween> fallingTweens)
        {
            int writePos = 0;
    
            for (int readPos = 0; readPos < height; readPos++)
            {
                var gridObject = grid.GetValue(column, readPos);
        
                if (gridObject != null)
                {
                    if (readPos != writePos)
                    {
                        MoveGemDown(column, readPos, writePos, fallingTweens);
                    }
                    writePos++;
                }
            }
        }
        
        /// <summary>
        /// 将宝石从一个位置移动到另一个位置
        /// </summary>
        /// <param name="column">列索引</param>
        /// <param name="fromRow">起始行</param>
        /// <param name="toRow">目标行</param>
        /// <param name="fallingTweens">下落动画列表</param>
        private void MoveGemDown(int column, int fromRow, int toRow, List<Tween> fallingTweens)
        {
            var gridObject = grid.GetValue(column, fromRow);
            var gem = gridObject.GetValue();
    
            grid.SetValue(column, toRow, gridObject);
            grid.SetValue(column, fromRow, null);
    
            var targetPosition = grid.GetWorldPositionCenter(column, toRow);
            var tween = gem.transform
                .DOLocalMove(targetPosition, 0.5f)
                .SetEase(ease);
    
            fallingTweens.Add(tween);
        }

        /// <summary>
        /// 爆炸指定位置的宝石
        /// </summary>
        /// <param name="matches">要爆炸的宝石位置列表</param>
        /// <returns>协程枚举器</returns>
        private IEnumerator ExplodeGems(List<Vector2Int> matches)
        {
            audioManager.PlayPop();
            
            foreach (var match in matches)
            {
                if (selectedGem == match)
                {
                    DeselectGem();
                }
                
                var gem = grid.GetValue(match.x, match.y).GetValue();
                grid.SetValue(match.x, match.y, null);

                ExplodeVFX(match);

                gem.transform.DOPunchScale(Vector3.one * 0.1f, 0.1f, 1, 0.5f);
                
                yield return new WaitForSeconds(0.1f);

                gem.DestroyGem();
            }
        }

        /// <summary>
        /// 创建爆炸视觉效果
        /// </summary>
        /// <param name="match">爆炸位置</param>
        private void ExplodeVFX(Vector2Int match)
        {
            var fx = Instantiate(explosion, transform);
            fx.transform.position = grid.GetWorldPositionCenter(match.x, match.y);
            Destroy(fx, 5f);
        }

        /// <summary>
        /// 查找匹配的宝石（水平和垂直方向）
        /// </summary>
        /// <param name="playSfx">是否播放音效</param>
        /// <returns>匹配的宝石位置列表</returns>
        private List<Vector2Int> FindMatches(bool playSfx = true)
        {
            HashSet<Vector2Int> matches = new();
            
            // 水平匹配检测
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width - 2; x++)
                {
                    var gemA = grid.GetValue(x, y);
                    var gemB = grid.GetValue(x + 1, y);
                    var gemC = grid.GetValue(x + 2, y);
                    
                    if (gemA == null || gemB == null || gemC == null) continue;

                    if (gemA.GetValue().GetType() == gemB.GetValue().GetType() &&
                        gemB.GetValue().GetType() == gemC.GetValue().GetType())
                    {
                        matches.Add(new Vector2Int(x, y));
                        matches.Add(new Vector2Int(x + 1, y));
                        matches.Add(new Vector2Int(x + 2, y));
                    }
                }
            }
            
            // 垂直匹配检测
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height - 2; y++)
                {
                    var gemA = grid.GetValue(x, y);
                    var gemB = grid.GetValue(x, y + 1);
                    var gemC = grid.GetValue(x, y + 2);
                    
                    if (gemA == null || gemB == null || gemC == null) continue;
                    
                    if (gemA.GetValue().GetType() == gemB.GetValue().GetType() &&
                        gemB.GetValue().GetType() == gemC.GetValue().GetType())
                    {
                        matches.Add(new Vector2Int(x, y));
                        matches.Add(new Vector2Int(x, y + 1));
                        matches.Add(new Vector2Int(x, y + 2));
                    }
                }
            }

            if (playSfx)
            {
                if (matches.Count == 0) audioManager.PlayNoMatch();
                else audioManager.PlayMatch();
            }

            
            return new List<Vector2Int>(matches);
        }

        /// <summary>
        /// 交换两个位置的宝石
        /// </summary>
        /// <param name="gridPosA">第一个位置</param>
        /// <param name="gridPosB">第二个位置</param>
        /// <returns>协程枚举器</returns>
        private IEnumerator SwapGems(Vector2Int gridPosA, Vector2Int gridPosB)
        {
            var gridObjectA = grid.GetValue(gridPosA.x, gridPosA.y);
            var gridObjectB = grid.GetValue(gridPosB.x, gridPosB.y);
            
            gridObjectA.GetValue().transform.DOLocalMove(grid.GetWorldPositionCenter(gridPosB.x, gridPosB.y), 0.5f)
                .SetEase(ease);
            gridObjectB.GetValue().transform.DOLocalMove(grid.GetWorldPositionCenter(gridPosA.x, gridPosA.y), 0.5f)
                .SetEase(ease);
            
            grid.SetValue(gridPosA.x, gridPosA.y, gridObjectB);
            grid.SetValue(gridPosB.x, gridPosB.y, gridObjectA);
            
            yield return new WaitForSeconds(0.5f);
        }
        
        /// <summary>
        /// 初始化游戏网格并创建宝石
        /// </summary>
        private void InitializeGrid()
        {
            grid = GridSystem2D<GridObject<Gem>>.VerticalGrid(width, height, cellSize, originPosition, debug);
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    CreateGem(x, y);
                }
            }
        }

        /// <summary>
        /// 在指定位置创建新宝石
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        private void CreateGem(int x, int y)
        {
            var gem = Instantiate(gemPrefab, grid.GetWorldPositionCenter(x, y), Quaternion.identity, transform);
            gem.SetType(GetRandomGemTypeWithoutInitialMatch(x, y));
            var gridObject = new GridObject<Gem>(grid, x, y);
            gridObject.SetValue(gem);
            grid.SetValue(x, y, gridObject);
        }
        
        /// <summary>
        /// 获取随机宝石类型，避免形成初始匹配
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>宝石类型</returns>
        private GemType GetRandomGemTypeWithoutInitialMatch(int x, int y)
        {
            var candidates = new List<GemType>(gemTypes);

            if (x >= 2)
            {
                var left1 = grid.GetValue(x - 1, y)?.GetValue();
                var left2 = grid.GetValue(x - 2, y)?.GetValue();
                if (left1 != null && left2 != null && left1.GetType() == left2.GetType())
                {
                    candidates.Remove(left1.GetType());
                }
            }

            if (y >= 2)
            {
                var down1 = grid.GetValue(x, y - 1)?.GetValue();
                var down2 = grid.GetValue(x, y - 2)?.GetValue();
                if (down1 != null && down2 != null && down1.GetType() == down2.GetType())
                {
                    candidates.Remove(down1.GetType());
                }
            }

            if (candidates.Count == 0)
                candidates.AddRange(gemTypes);

            return candidates[Random.Range(0, candidates.Count)];
        }

        /// <summary>
        /// 选择指定位置的宝石
        /// </summary>
        /// <param name="gridPos">网格位置</param>
        private void SelectGem(Vector2Int gridPos)
        {
            if (selectedGem != Vector2Int.one * -1)
            {
                RestoreSelectedAnimation();
            }

            selectedGem = gridPos;
            PlaySelectAnimation(gridPos);
        }

        /// <summary>
        /// 取消当前选中的宝石
        /// </summary>
        private void DeselectGem()
        {
            RestoreSelectedAnimation();
            selectedGem = new Vector2Int(-1, -1);
        }
        
        /// <summary>
        /// 播放选中动画
        /// </summary>
        /// <param name="gridPos">网格位置</param>
        private void PlaySelectAnimation(Vector2Int gridPos)
        {
            var gemObj = grid.GetValue(gridPos.x, gridPos.y);
            if (gemObj == null) return;
            var gem = gemObj.GetValue();
            if (gem == null) return;
    
            selectedTween?.Kill();

            selectedTransform = gem.transform;
            selectedTransform.DOKill();

            selectedGemOriginalScale = selectedTransform.localScale;

            selectedTween = selectedTransform
                .DOScale(selectedGemOriginalScale * selectedScaleMultiplier, selectAnimDuration)
                .SetEase(Ease.OutBack);
        }

        /// <summary>
        /// 恢复选中动画到原始状态
        /// </summary>
        private void RestoreSelectedAnimation()
        {
            selectedTween?.Kill();
            selectedTween = null;

            if (selectedTransform != null)
            {
                selectedTransform.DOKill();
                selectedTransform
                    .DOScale(selectedGemOriginalScale, selectAnimDuration)
                    .SetEase(Ease.OutQuad);
                selectedTransform = null;
            }
        }
    }
}