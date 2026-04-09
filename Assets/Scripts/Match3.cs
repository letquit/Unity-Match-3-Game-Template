using System;
using System.Collections;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Match3
{
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

        private GridSystem2D<GridObject<Gem>> grid;
        
        private InputReader inputReader;
        private Vector2Int selectedGem = Vector2Int.one * -1;

        private void Awake()
        {
            inputReader = GetComponent<InputReader>();
        }

        private void Start()
        {
            InitializeGrid();
            inputReader.FireAt += OnSelectGem;
        }

        private void OnDestroy()
        {
            inputReader.FireAt -= OnSelectGem;
        }
        
        private void OnSelectGem(Vector2 screenPos)
        {
            // var gridPos = grid.GetXY(Camera.main.ScreenToWorldPoint(inputReader.Selected));
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
            }
            else if (selectedGem == Vector2Int.one * -1)
            {
                SelectGem(gridPos);
            }
            else
            {
                StartCoroutine(RunGameLoop(selectedGem, gridPos));
            }
        }
        
        private bool IsValidPosition(Vector2 gridPos) => gridPos.x >= 0 && gridPos.x < width && gridPos.y >= 0 && gridPos.y < height;
        private bool IsEmptyPosition(Vector2Int gridPos) => grid.GetValue(gridPos.x, gridPos.y) == null;

        private IEnumerator RunGameLoop(Vector2Int gridPosA, Vector2Int gridPosB)
        {
            yield return StartCoroutine(SwapGems(gridPosA, gridPosB));

            DeselectGem();
            
            yield return null;
        }

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

        private void CreateGem(int x, int y)
        {
            var gem = Instantiate(gemPrefab, grid.GetWorldPositionCenter(x, y), Quaternion.identity, transform);
            gem.SetType(gemTypes[Random.Range(0, gemTypes.Length)]);
            var gridObject = new GridObject<Gem>(grid, x, y);
            gridObject.SetValue(gem);
            grid.SetValue(x, y, gridObject);
        }

        private void DeselectGem() => selectedGem = new Vector2Int(-1, -1);
        private void SelectGem(Vector2Int gridPos) => selectedGem = gridPos;
    }
}