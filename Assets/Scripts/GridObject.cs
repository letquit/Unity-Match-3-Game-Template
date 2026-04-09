using UnityEngine;

namespace Match3
{
    /// <summary>
    /// 网格对象（泛型）。
    /// 代表棋盘网格中的一个独立“格子”或“单元格”。
    /// 它作为一个数据容器，负责存储该位置的具体内容（如宝石），并记录自身在网格中的坐标。
    /// </summary>
    /// <typeparam name="T">存储的数据类型，例如 Gem（宝石）或 int（分数）。</typeparam>
    public class GridObject<T>
    {
        // 该格子所属的网格系统引用（用于反向查询或交互）
        private GridSystem2D<GridObject<T>> grid;
        
        // 格子在网格中的 X 坐标
        private int x;
        
        // 格子在网格中的 Y 坐标
        private int y;
        
        // 存储的实际数据（例如：具体的宝石实例）
        private T gem;

        /// <summary>
        /// 构造函数。
        /// 在网格系统初始化时调用，用于绑定坐标和所属网格。
        /// </summary>
        public GridObject(GridSystem2D<GridObject<T>> grid, int x, int y)
        {
            this.grid = grid;
            this.x = x;
            this.y = y;
        }

        /// <summary>
        /// 设置格子内的数据。
        /// 例如：在初始化棋盘或消除后生成新宝石时调用。
        /// </summary>
        public void SetValue(T gem)
        { 
            this.gem = gem;
        }
        
        /// <summary>
        /// 获取格子内的数据。
        /// 用于检测匹配、交换宝石时读取数据。
        /// </summary>
        public T GetValue() => gem;
    }
}