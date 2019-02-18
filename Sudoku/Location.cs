using System;

namespace TexiService
{
    [Serializable]
    public class Location
    {
        private int col;
        private int row;

        public int Col
        {
            get => this.col;
            set => this.col = value;
        }
        public int Row
        {
            get => this.row;
            set => this.row = value;
        }

        public Location(int row, int col)
        {
            this.row = row;
            this.col = col;
        }

        public override string ToString() => this.row.ToString() + ", " + this.col.ToString();
        public void Up() => this.Row -= 1;
        public void Down() => this.Row += 1;
        public void Left() => this.Col -= 1;
        public void Right() => this.Col += 1;
        public bool SameAs(Location destination)
        {
            if(destination.Col == this.col && destination.Row == this.row) return true;
            else return false;
        }
        public int GetDistanceTo(Location destination) => Math.Abs(Math.Abs(this.Row - destination.Row) + Math.Abs(this.Col - destination.Col));
    }
}
