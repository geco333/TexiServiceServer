using System;

namespace TexiService
{
    internal class RandomGenerator
    {
        private static Random rnd = new Random();

        public static ZoneType ZoneType() => (ZoneType)rnd.Next(11);
        public static Zone[][] Layout(LayoutSize size)
        {
            Zone[][] matrix = new Zone[size.Row][]; // Layout matrix.

            // For each matrix index create a random type zone.
            for(int i = 0; i < size.Row; i++)
            {
                matrix[i] = new Zone[size.Col];

                for(int j = 0; j < size.Col; j++)
                {
                    Location loc = new Location(i, j);
                    ZoneType type = ZoneType();

                    matrix[i][j] = new Zone(type, loc);
                }
            }

            // Create 4 texi charge stations.
            matrix[size.Row / 3][size.Col / 3].Type = TexiService.ZoneType.TexiCharge; // NW
            matrix[size.Row / 3][2 * size.Col / 3].Type = TexiService.ZoneType.TexiCharge; // NE
            matrix[2 * size.Row / 3][size.Col / 3].Type = TexiService.ZoneType.TexiCharge; // SW
            matrix[2 * size.Row / 3][2 * size.Col / 3].Type = TexiService.ZoneType.TexiCharge; // SE

            return matrix;
        }
        public static Location Location(LayoutSize size) => new Location(rnd.Next(1, size.Row), rnd.Next(1, size.Col));
        public static Texi Texi(LayoutSize size, Center center) => new Texi(Location(size), rnd.Next(999), TexiStatus.Available, center);
        public static Employee Employee(LayoutSize size, Center center)
        {
            string[] firstNames = new string[] { "Christi", "Foster", "Glennis", "Davina", "Matilda",
                                                 "Irene", "Millie", "Elissa", "Raeann", "Marilynn",
                                                 "Lowell", "Nada", "Tamekia", "Corrin", "Twana",
                                                 "Jama", "Ariel","Kristi", "Alvaro", "Ellena"};
            string[] lastNames = new string[] { "Balmer", "Lema", "Kuhlmann", "Wheless", "Marte",
                                                "Commander", "Reiher", "Gracey", "Obanion", "Getchell",
                                                "Figg", "Kemble", "Weir", "Dahlke", "Wiest",
                                                "Mcnaught", "Pusey", "Tuma", "Shimek", "Lott"};
            Location loc = Location(size);

            Employee newEmployee = new Employee(firstNames[rnd.Next(20)], lastNames[rnd.Next(20)], rnd.Next(999), center)
            {
                Row = loc.Row,
                Col = loc.Col
            };
            return newEmployee;
        }
    }
}


