using System;

namespace TexiService
{
    [Serializable]
    public class LayoutSize
    {
        private int col;
        private int row;

        public int Col => this.col;
        public int Row => this.row;

        public LayoutSize(int row, int col)
        {
            this.row = row;
            this.col = col;
        }
    }
}
