using System.ComponentModel;

namespace TexiService
{
    public enum EmployeeStatus { Working, Waiting, Moving }

    public class Employee : INotifyPropertyChanged
    {
        private int id;
        private string first;
        private string last;
        private int row;
        private int col;
        private EmployeeStatus status;
        private Center center;
        private Location location;
        private Location destination;

        public int Id => this.id;
        public string First => this.first;
        public string Last => this.last;
        public int Row
        {
            get => this.row;
            set
            {
                this.row = value;
                this.location.Row = value;
                this.OnPropertyChanged("Row");
            }
        }
        public int Col
        {
            get => this.col;
            set
            {
                this.col = value;
                this.location.Col = value;
                this.OnPropertyChanged("Col");
            }
        }
        public EmployeeStatus Status
        {
            get => this.status;
            set
            {
                this.status = value;
                this.OnPropertyChanged("Status");
            }
        }
        public Center Center
        {
            get => this.center;
            set => this.center = value;
        }
        public Location Location
        {
            get => this.location;
            set
            {
                this.location = value;
                this.Row = this.location.Row;
                this.Col = this.location.Col;
            }
        }
        public Location Destination
        {
            get => this.destination;
            set
            {
                this.destination = value;
                this.OnPropertyChanged("Destination");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Employee(string first, string last, int id, Center center)
        {
            this.first = first;
            this.last = last;
            this.id = id;
            this.center = center;

            this.status = EmployeeStatus.Working;
            this.location = new Location(this.row, this.col);
        }
        public Employee(SerializableEmployee serEmp, Center center)
        {
            this.id = serEmp.Id;
            this.first = serEmp.First;
            this.last = serEmp.Last;
            this.row = serEmp.Location.Row;
            this.col = serEmp.Location.Col;
            this.status = serEmp.Status;
            this.center = center;

            this.location = new Location(this.row, this.col);
        }
        public SerializableEmployee ToSerializable() => new SerializableEmployee(this.First, this.Last, this.Id, this.Location, this.destination, this.Status);

        private void OnPropertyChanged(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    }
}
