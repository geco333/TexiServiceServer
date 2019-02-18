using System;
using System.ComponentModel;
using System.Windows;

namespace TexiService
{
    public enum TexiStatus { Available, AnsweringCall, HavePassenger, Charging }

    public class Texi : INotifyPropertyChanged
    {
        private int number;
        private int charge;
        private int row;
        private int col;
        private Location destination;
        private Location location;
        private TexiStatus status;
        private Center center;
        private Employee passenger;
        private bool havePassenger;
        private MainWindow mw;

        public int Number { get => this.number; set => this.number = value; }
        public int Charge
        {
            get => this.charge;
            set
            {
                this.charge = value;
                this.OnPropertyChanged("Charge");
            }
        }
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
        public Location Destination
        {
            get => this.destination;
            set
            {
                this.destination = value;
                this.OnPropertyChanged("Destination");
            }
        }
        public Location Location { get => this.location; set => this.location = value; }
        public TexiStatus Status
        {
            get => this.status;
            set
            {
                this.status = value;
                this.OnPropertyChanged("Status");
            }
        }
        public Center Center => this.center;
        public Employee Passenger
        {
            get => this.passenger;
            set
            {
                this.passenger = value;
                this.OnPropertyChanged("Caller");
            }
        }
        public bool HavePassenger
        {
            get => this.havePassenger;
            set
            {
                this.havePassenger = value;
                this.OnPropertyChanged("HavePassenger");
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public Texi(Location location, int number, TexiStatus status, Center center)
        {
            this.location = location;
            this.row = location.Row;
            this.col = location.Col;
            this.number = number;
            this.status = status;
            this.center = center;

            this.mw = Application.Current.MainWindow as MainWindow;
            this.Charge = 100;
        }

        public void MoveTimerCallback(object sender, EventArgs e)
        {
            // Clear texi from the layout zone.
            this.center.Layout[this.row][this.col].Texi = null;
            this.center.Layout[this.row][this.col].HasTexiOn = false;

            // If the texi has a passenger on clear the 'HasEmployeeOn' flag from the layout zone.
            if(this.HavePassenger)
                this.center.Layout[this.row][this.col].HasEmployeeOn = false;

            // Calculate absolute row and column distance to destination.
            int deltaRow = Math.Abs(this.destination.Row - this.Row);
            int deltaCol = Math.Abs(this.destination.Col - this.Col);

            // Move.
            if(deltaRow > 0 &&
               deltaRow <= deltaCol ||
               this.Col == this.destination.Col)
            {
                if(this.Destination.Row < this.Row)
                {
                    this.Row--;
                }
                else
                    this.Row++;
            }
            else
            {
                if(this.Destination.Col < this.Col)
                    this.Col--;
                else
                    this.Col++;
            }

            // Decrement charge.
            this.Charge--;

            // Change next location zone to hasTexiOn = true
            this.center.Layout[this.Row][this.Col].Texi = this;

            // If a passenger is on update the row and col in the layout.
            if(this.HavePassenger)
            {
                this.Passenger.Row = this.Row;
                this.Passenger.Col = this.Col;
                this.center.Layout[this.Row][this.Col].Employee = this.Passenger;
                this.center.Layout[this.Row][this.Col].HasEmployeeOn = true;
            }

            // If the texi has reached its destination.
            if(this.Row == this.destination.Row &&
               this.Col == this.destination.Col)
            {
                // If texi has no passenger on.
                if(!this.HavePassenger)
                {
                    // If the zone has an employee on and the employee is the passenger.
                    if(this.center.Layout[this.row][this.col].HasEmployeeOn &&
                       this.center.Layout[this.row][this.col].Employee == this.Passenger)
                    {
                        this.Destination = this.passenger.Destination; // Change desination to passengers destination.
                        this.Status = TexiStatus.HavePassenger; // Change texi status to 'HavePassenger'.
                        this.Passenger.Status = EmployeeStatus.Moving;// Change passenger status to 'Moving'.
                        this.HavePassenger = true;
                    }
                }
                // If the texi has a passanger on.
                else if(this.HavePassenger)
                {
                    // Update texi properties.
                    this.Destination = null; // Clear texi destination.
                    this.Status = TexiStatus.Available; // Change texi status to 'Available'.
                    this.Passenger.Status = EmployeeStatus.Working; // Change passenger status to 'Working'.
                    this.Passenger = null; // Clear texi passenger.
                    this.HavePassenger = false; // Clear texi 'HavePassenger' flag.
                }

                // Unsubscribe from gloabalTimer tick.
                if(this.Status != TexiStatus.HavePassenger)
                {
                    this.mw.globalTimer.Tick -= this.MoveTimerCallback;

                    if(this.Status == TexiStatus.Charging)
                    {
                        this.mw.globalTimer.Tick -= this.mw.UpdateLayoutView;
                        this.mw.globalTimer.Tick += this.ChargeTimerCallback;
                        this.mw.globalTimer.Tick += this.mw.UpdateLayoutView;
                    }
                }
            }

            this.center.Layout[this.row][this.col].Texi = this; // Place the texi in the layout zone.
            this.center.Layout[this.row][this.col].HasTexiOn = true; // Change 'HasTexiOn' flag to true.
        }
        private void ChargeTimerCallback(object sender, EventArgs e)
        {
            if(this.Charge < 100) this.Charge++;

            if(this.Charge >= 100 || this.Center.Layout[this.Row][this.Col].Type != ZoneType.TexiCharge)
                this.mw.globalTimer.Tick -= this.ChargeTimerCallback;
        }

        public void Move(Location destination)
        {
            int disToDest = this.Location.GetDistanceTo(destination);
            int disToChargeFromDest = destination.GetDistanceTo(this.Center.GetClosestCharger(destination));

            // If there is enough charge move to destination.
            if(this.Charge - (disToDest + disToChargeFromDest) >= 0)
            {
                this.Destination = destination;
            }
            // Move to closest charger.
            else
            {
                // Change status to charging.
                this.Status = TexiStatus.Charging;
                // Dispaly a message saying the texi is heading to a charger.
                (Application.Current.MainWindow as MainWindow).PrintTexiMessage("Not enough charge to destination, going to recharge.");
                // Change destination to closest charger.
                this.Destination = this.Center.GetClosestCharger(this.Location);
            }

            this.mw.globalTimer.Tick -= this.mw.UpdateLayoutView;
            this.mw.globalTimer.Tick += this.MoveTimerCallback;
            this.mw.globalTimer.Tick += this.mw.UpdateLayoutView;
        }
        public SerializableTexi ToSerializable() => new SerializableTexi(this.Number, this.Destination, this.Location, this.Status);

        private void OnPropertyChanged(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    }
}
