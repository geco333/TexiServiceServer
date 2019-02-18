using System;

namespace TexiService
{
    [Serializable]
    public class SerializableEmployee
    {
        private int id;
        private string first;
        private string last;
        private Location location;
        private Location destination;
        private EmployeeStatus status;

        public int Id => this.id;
        public string First => this.first;
        public string Last => this.last;
        public string FullName => this.First + ' ' + this.Last;
        public Location Location { get => this.location; set => this.location = value; }
        public Location Destination { get => this.destination; set => this.destination = value; }
        public EmployeeStatus Status
        {
            get => this.status;
            set => this.status = value;
        }

        public SerializableEmployee(string first, string last, int id)
        {
            this.first = first;
            this.last = last;
            this.id = id;

            this.status = EmployeeStatus.Working;
        }
        public SerializableEmployee(string first, string last, int id, Location location, Location destination, EmployeeStatus status)
        {
            this.id = id;
            this.first = first;
            this.last = last;
            this.location = location;
            this.Destination = destination;
            this.status = status;
        }
    }
}
