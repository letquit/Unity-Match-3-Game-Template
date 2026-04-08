using UnityEngine;

namespace Match3
{
    public class GridObject<T>
    {
        private GridSystem2D<GridObject<T>> grid;
        private int x;
        private int y;

        public GridObject(GridSystem2D<GridObject<T>> grid, int x, int y)
        {
            this.grid = grid;
            this.x = x;
            this.y = y;
        }
    }
}