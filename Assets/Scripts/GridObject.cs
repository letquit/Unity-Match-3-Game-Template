using UnityEngine;

namespace Match3
{
    public class GridObject<T>
    {
        private GridSystem2D<GridObject<T>> grid;
        private int x;
        private int y;
        private T gem;

        public GridObject(GridSystem2D<GridObject<T>> grid, int x, int y)
        {
            this.grid = grid;
            this.x = x;
            this.y = y;
        }

        public void SetValue(T gem)
        { 
            this.gem = gem;
        }
        
        public T GetValue() => gem;
    }
}