using System;

namespace TexiService
{
    [Serializable]
    public class SerializableTexi
    {
        private int number;
        private Location destination;
        private Location location;
        private TexiStatus status;

        public SerializableTexi(int number, Location destination, Location location, TexiStatus status)
        {
            this.Number = number;
            this.Destination = destination;
            this.Location = location;
            this.Status = status;
        }

        public int Number { get => this.number; set => this.number = value; }
        public Location Destination
        {
            get => this.destination;
            set => this.destination = value;
        }
        public Location Location { get => this.location; set => this.location = value; }
        public TexiStatus Status
        {
            get => this.status;
            set => this.status = value;
        }
    }
}
