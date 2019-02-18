namespace TexiService
{
    public enum ZoneType
    {
        Administration, Control, Cafeteria, QualityControl, Packing, Storage, Shipping, Service,
        Parking, Loading, Operation, TexiCharge
    }

    public class Zone
    {
        private Location location;
        private ZoneType type;
        private bool hasTexiOn;
        private bool hasEmployeeOn;
        private Texi texi;
        private Employee employee;

        public Location Location { get => this.location; set => this.location = value; }
        public ZoneType Type { get => this.type; set => this.type = value; }
        public bool HasTexiOn
        {
            get => this.hasTexiOn;
            set => this.hasTexiOn = value;
        }
        public bool HasEmployeeOn
        {
            get => this.hasEmployeeOn;
            set => this.hasEmployeeOn = value;
        }
        public Texi Texi
        {
            get => this.texi;
            set
            {
                if(!this.HasTexiOn)
                {
                    this.texi = value;
                    this.HasTexiOn = true;
                }

                this.texi = value;
            }
        }
        public Employee Employee
        {
            get => this.employee;
            set
            {
                if(!this.hasEmployeeOn)
                {
                    this.employee = value;
                    this.hasEmployeeOn = true;
                }

                this.employee = value;
            }
        }

        public Zone(ZoneType type, Location location)
        {
            this.type = type;
            this.location = location;
            this.hasTexiOn = false;
        }

        public void Update(Location location, ZoneType type)
        {
            this.location = location;
            this.type = type;
        }
        public void RemoveTexi()
        {
            if(this.HasTexiOn)
            {
                this.texi = null;
                this.HasTexiOn = false;
            }
        }
    }
}
