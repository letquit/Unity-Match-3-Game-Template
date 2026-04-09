using System;
using TMPro;
using UnityEngine;

namespace Match3
{
    /// <summary>
    /// 通用二维网格系统。
    /// 这是一个核心架构类，负责管理网格数据、处理坐标转换（世界坐标 <-> 网格坐标）。
    /// 使用策略模式（CoordinateConverter）支持 2D (XY轴) 和 3D (XZ轴) 两种布局。
    /// </summary>
    /// <typeparam name="T">网格中存储的数据类型（例如 GridObject<Gem>）</typeparam>
    public class GridSystem2D<T>
    {
        // 网格宽度（列数）
        private readonly int width;
        // 网格高度（行数）
        private readonly int height;
        // 每个格子的大小（单位：米）
        private readonly float cellSize;
        // 网格在世界空间中的起始点（通常是左下角）
        private readonly Vector3 origin;
        // 存储数据的二维数组
        private readonly T[,] gridArray;
        
        // 坐标转换器（策略模式：决定是 2D 还是 3D 布局）
        private readonly CoordinateConverter coordinateConverter;

        // 当网格中某个位置的值发生变化时触发的事件
        public event Action<int, int, T> OnValueChangeEvent;

        /// <summary>
        /// 工厂方法：创建一个垂直平面（2D，XY轴）的网格。
        /// </summary>
        public static GridSystem2D<T> VerticalGrid(int width, int height, float cellSize, Vector3 origin, bool debug = false)
        {
            return new GridSystem2D<T>(width, height, cellSize, origin, new VerticalConverter(), debug);
        }
        
        /// <summary>
        /// 工厂方法：创建一个水平平面（3D/伪3D，XZ轴）的网格。
        /// </summary>
        public static GridSystem2D<T> HorizontalGrid(int width, int height, float cellSize, Vector3 origin, bool debug = false)
        {
            return new GridSystem2D<T>(width, height, cellSize, origin, new HorizontalConverter(), debug);
        }

        /// <summary>
        /// 私有构造函数。
        /// </summary>
        public GridSystem2D(int width, int height, float cellSize, Vector3 origin,
            CoordinateConverter coordinateConverter, bool debug)
        {
            this.width = width;
            this.height = height;
            this.cellSize = cellSize;
            this.origin = origin;
            // 如果传入的转换器为空，默认使用垂直转换器
            this.coordinateConverter = coordinateConverter ?? new VerticalConverter();
            
            gridArray = new T[width, height];

            // 如果在编辑器中开启调试，绘制网格线
            if (debug)
            {
                DrawDebugLines();
            }
        }

        /// <summary>
        /// 根据世界坐标设置网格某位置的值。
        /// 自动将世界坐标转换为网格坐标。
        /// </summary>
        public void SetValue(Vector3 worldPosition, T value)
        {
            Vector2Int pos = coordinateConverter.WorldToGrid(worldPosition, cellSize, origin);
            SetValue(pos.x, pos.y, value);
        }

        /// <summary>
        /// 根据网格坐标 (x, y) 设置值。
        /// 如果位置有效，更新数组并触发事件。
        /// </summary>
        public void SetValue(int x, int y, T value)
        {
            if (IsValid(x, y))
            {
                gridArray[x, y] = value;
                OnValueChangeEvent?.Invoke(x, y, value);
            }
        }

        /// <summary>
        /// 根据世界坐标获取网格中的值。
        /// </summary>
        public T GetValue(Vector3 worldPosition)
        {
            Vector2Int pos = GetXY(worldPosition);
            return GetValue(pos.x, pos.y);
        }

        /// <summary>
        /// 根据网格坐标 (x, y) 获取值。
        /// 如果越界则返回默认值（null 或 default(T)）。
        /// </summary>
        public T GetValue(int x, int y)
        {
            return IsValid(x, y) ? gridArray[x, y] : default;
        }

        // 检查坐标是否在网格范围内
        private bool IsValid(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;
        
        /// <summary>
        /// 将世界坐标转换为网格坐标（行列号）。
        /// </summary>
        public Vector2Int GetXY(Vector3 worldPosition) =>
            coordinateConverter.WorldToGrid(worldPosition, cellSize, origin);

        /// <summary>
        /// 获取指定网格坐标的中心点世界位置。
        /// 用于将物体移动到格子中心。
        /// </summary>
        public Vector3 GetWorldPositionCenter(int x, int y) =>
            coordinateConverter.GridToWorldCenter(x, y, cellSize, origin);
        
        // 获取指定网格坐标的左下角世界位置（用于绘制调试线）
        private Vector3 GetWorldPosition(int x, int y) => coordinateConverter.GridToWorld(x, y, cellSize, origin);

        /// <summary>
        /// 在 Scene 视图中绘制调试网格线和坐标文本。
        /// </summary>
        private void DrawDebugLines()
        {
            const float duration = 100f;
            var parent = new GameObject("Debugging");

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // 显示坐标文本
                    CreateWorldText(parent, x + "," + y, GetWorldPositionCenter(x, y), coordinateConverter.Forward);
                    // 绘制格子边框
                    Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x, y + 1), Color.white, duration);
                    Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x + 1, y), Color.white, duration);
                }
            }
            
            // 绘制最右侧和最上侧的边框
            Debug.DrawLine(GetWorldPosition(0, height), GetWorldPosition(width, height), Color.white, duration);
            Debug.DrawLine(GetWorldPosition(width, 0), GetWorldPosition(width, height), Color.white, duration);
        }

        /// <summary>
        /// 在世界空间中创建 3D 文本（用于调试）。
        /// </summary>
        private TextMeshPro CreateWorldText(GameObject parent, string text, Vector3 position, Vector3 dir, int fontSize = 2,
            Color color = default, TextAlignmentOptions textAnchor = TextAlignmentOptions.Center, int sortingOrder = 0)
        {
            GameObject gameObject = new GameObject("DebugText_" + text, typeof(TextMeshPro));
            gameObject.transform.SetParent(parent.transform);
            gameObject.transform.position = position;
            gameObject.transform.forward = dir;
            
            TextMeshPro textMeshPro = gameObject.GetComponent<TextMeshPro>();
            textMeshPro.text = text;
            textMeshPro.fontSize = fontSize;
            textMeshPro.color = color == default ? Color.white : color;
            textMeshPro.alignment = textAnchor;
            textMeshPro.GetComponent<MeshRenderer>().sortingOrder = sortingOrder;
            
            return textMeshPro;
        }
        
        /// <summary>
        /// 坐标转换器抽象类（策略模式）。
        /// 定义了网格坐标与世界坐标转换的接口。
        /// </summary>
        public abstract class CoordinateConverter
        {
            // 网格坐标 -> 世界坐标（左下角）
            public abstract Vector3 GridToWorld(int x, int y, float cellSize, Vector3 origin);

            // 网格坐标 -> 世界坐标（中心点）
            public abstract Vector3 GridToWorldCenter(int x, int y, float cellSize, Vector3 origin);

            // 世界坐标 -> 网格坐标
            public abstract Vector2Int WorldToGrid(Vector3 worldPosition, float cellSize, Vector3 origin);
            
            // 获取该平面的朝向（法线方向）
            public abstract Vector3 Forward { get; }
        }

        /// <summary>
        /// 垂直平面转换器（2D 模式，使用 XY 轴）。
        /// </summary>
        public class VerticalConverter : CoordinateConverter
        {
            public override Vector3 GridToWorld(int x, int y, float cellSize, Vector3 origin)
            {
                return new Vector3(x, y , 0) * cellSize + origin;
            }

            public override Vector3 GridToWorldCenter(int x, int y, float cellSize, Vector3 origin)
            {
                return new Vector3(x * cellSize + cellSize * 0.5f, y * cellSize + cellSize * 0.5f, 0) + origin;
            }

            public override Vector2Int WorldToGrid(Vector3 worldPosition, float cellSize, Vector3 origin)
            {
                Vector3 gridPosition = (worldPosition - origin) / cellSize;
                var x = Mathf.FloorToInt(gridPosition.x);
                var y = Mathf.FloorToInt(gridPosition.y);
                return new Vector2Int(x, y);
            }
            
            public override Vector3 Forward => Vector3.forward;
        }

        /// <summary>
        /// 水平平面转换器（3D 模式，使用 XZ 轴）。
        /// </summary>
        public class HorizontalConverter : CoordinateConverter
        {
            public override Vector3 GridToWorldCenter(int x, int y, float cellSize, Vector3 origin)
            {
                return new Vector3(x * cellSize + cellSize * 0.5f, 0, y * cellSize + cellSize * 0.5f) + origin;
            }

            public override Vector3 GridToWorld(int x, int y, float cellSize, Vector3 origin)
            {
                return new Vector3(x, 0, y) * cellSize + origin;
            }

            public override Vector2Int WorldToGrid(Vector3 worldPosition, float cellSize, Vector3 origin)
            {
                Vector3 gridPosition = (worldPosition - origin) / cellSize;
                var x = Mathf.FloorToInt(gridPosition.x);
                var y = Mathf.FloorToInt(gridPosition.z); // 注意：这里取 Z 轴作为 Y 坐标
                return new Vector2Int(x, y);
            }
            
            public override Vector3 Forward => -Vector3.up;
        }
    }
}