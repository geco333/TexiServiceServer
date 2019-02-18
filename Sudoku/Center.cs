using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace TexiService
{
    public class Center
    {
        private ObservableCollection<Texi> texis;
        private ObservableCollection<Employee> employees;
        private LayoutSize size;
        private Zone[][] layout;
        private List<Zone> chargers;
        private IList selectedEmployees;
        private IList selectedTexis;
        private FrameworkElement log;
        private FrameworkElement tbEmployees;
        private Timer messageTimer;
        private Dispatcher dispatcher;
        private MainWindow mw;

        public ObservableCollection<Texi> Texis => this.texis;
        public ObservableCollection<Employee> Employees => this.employees;
        public LayoutSize Size => this.size;
        public Zone[][] Layout => this.layout;
        public List<Zone> Chargers { get => this.chargers; set => this.chargers = value; }
        public IList SelectedEmployees
        {
            get => this.selectedEmployees;
            set => this.selectedEmployees = value;
        }
        public IList SelectedTexis
        {
            get => this.selectedTexis;
            set => this.selectedTexis = value;
        }

        public Center(LayoutSize size)
        {
            this.layout = RandomGenerator.Layout(size);
            this.texis = new ObservableCollection<Texi>();
            this.employees = new ObservableCollection<Employee>();
            this.size = size;

            this.log = (Application.Current.MainWindow as MainWindow).FindName("icLog") as ItemsControl;
            this.tbEmployees = (Application.Current.MainWindow as MainWindow).FindName("tbEmployees") as TextBlock;
            this.dispatcher = Application.Current.Dispatcher;
            this.mw = Application.Current.MainWindow as MainWindow;

            this.GetChargersLocations();
        }

        public void AddTexi()
        {
            Texi texi = RandomGenerator.Texi(this.Size, this);
            int row = texi.Row;
            int col = texi.Col;

            this.Layout[row][col].HasTexiOn = true;
            this.Layout[row][col].Texi = texi;
            this.Texis.Add(texi);

            // Log.
            Application.Current.Dispatcher.Invoke(() => (this.log as ItemsControl).Items.Add(new { message = "# Texi added: " + texi.Number }));
        }
        public void RemoveTexi()
        {
            int count = this.SelectedTexis.Count;

            do
            {
                Texi t = this.SelectedTexis[0] as Texi;
                int row = t.Row;
                int col = t.Col;

                this.layout[row][col].HasTexiOn = false;
                this.Texis.Remove(t);

                // Log.
                Application.Current.Dispatcher.Invoke(() => (this.log as ItemsControl).Items.Add(new { message = "# Texi removed: " + t.Number }));
            } while(this.SelectedTexis.Count > 0);

        }
        public void AddRandomEmployee()
        {
            Employee employee = RandomGenerator.Employee(this.Size, this);
            int row = employee.Row;
            int col = employee.Col;

            this.layout[row][col].HasEmployeeOn = true;
            this.layout[row][col].Employee = employee;
            this.Employees.Add(employee);

            // Log.
            Application.Current.Dispatcher.Invoke(() => (this.log as ItemsControl).Items.Add(new { message = "# Employee added: " + employee.Id }));
        }
        public void AddEmployee(Employee emp)
        {
            int row = emp.Row;
            int col = emp.Col;

            this.layout[row][col].HasEmployeeOn = true;
            this.layout[row][col].Employee = emp;
            Application.Current.Dispatcher.Invoke(() => this.Employees.Add(emp));

            // Log.
            Application.Current.Dispatcher.Invoke(() => (this.log as ItemsControl).Items.Add(new { message = "# Employee added: " + emp.Id }));
        }
        public void AddEmployee(SerializableEmployee serEmp)
        {
            int row = serEmp.Location.Row;
            int col = serEmp.Location.Col;

            Employee emp = new Employee(serEmp, this);

            this.layout[row][col].HasEmployeeOn = true;
            this.layout[row][col].Employee = emp;
            Application.Current.Dispatcher.Invoke(() => this.Employees.Add(emp));

            // Log.
            Application.Current.Dispatcher.Invoke(() => (this.log as ItemsControl).Items.Add(new { message = "# Employee added: " + emp.Id }));
        }
        public void RemoveEmployee()
        {
            int count = this.Employees.Count;

            do
            {
                Employee e = this.SelectedEmployees[0] as Employee;
                int row = e.Row;
                int col = e.Col;

                this.layout[row][col].HasEmployeeOn = false;
                this.Employees.Remove(e);

                // Log.
                Application.Current.Dispatcher.Invoke(() => (this.log as ItemsControl).Items.Add(new { message = "# Employee removed: " + e.Id }));
            } while(this.SelectedEmployees.Count > 0);
        }
        public void RemoveEmployee(Employee emp)
        {
            int row = emp.Row;
            int col = emp.Col;

            this.layout[row][col].HasEmployeeOn = false;
            Application.Current.Dispatcher.Invoke(() => this.Employees.Remove(emp));

            // Log.
            Application.Current.Dispatcher.Invoke(() => (this.log as ItemsControl).Items.Add(new { message = "# Employee removed: " + emp.Id }));
        }
        public Texi FindClosestTexi(Employee employee)
        {
            Texi texi = null;
            int minDistance = int.MaxValue;
            int empRow = employee.Row;
            int empCol = employee.Col;

            foreach(Texi t in this.Texis)
            {
                int texiRow = t.Row;
                int texiCol = t.Col;

                int dToEmployee = Math.Abs((empRow + empCol) - (texiRow + texiCol));
                int dToDestination = Math.Abs((empRow + empCol) - (employee.Destination.Row + employee.Destination.Col));
                int distanceToCharger = this.Size.Row / 2 + this.Size.Col / 2;

                if(dToEmployee < minDistance &&
                   t.Charge > (dToEmployee + dToDestination + this.Size.Row / 3 + this.Size.Col / 3) &&
                   t.Status == TexiStatus.Available)
                {
                    minDistance = dToEmployee;
                    texi = t;
                }
            }

            if(texi != null)
            {
                texi.Passenger = employee;
                texi.Status = TexiStatus.AnsweringCall;
            }

            return texi;
        }
        public Texi CallTexi(Employee employee, Location destination)
        {
            Texi texi = null;

            // Find a texi closest to the employee and
            //  send it to the employee location,
            //  add the texi's move function to the global ticker.
            try
            {
                employee.Destination = destination;
                texi = this.FindClosestTexi(employee);
                texi.Destination = employee.Location;

                (Application.Current.MainWindow as MainWindow).globalTimer.Tick += texi.MoveTimerCallback;

                // Log.
                this.mw.Log($"@ Employee {employee.Id} calld a texi.");
                this.mw.Log($"@ Texi {texi.Number} respondes.");
            }
            // No texi in layout.
            catch(NullReferenceException)
            {
                this.tbEmployees.Visibility = Visibility.Visible;

                this.messageTimer = new Timer(o => this.dispatcher.Invoke(() => this.tbEmployees.Visibility = Visibility.Hidden), null, 2000, Timeout.Infinite);
                (this.tbEmployees as TextBlock).Text = "No texi was found.";
            }

            return texi;
        }
        public Location GetClosestCharger(Location destination)
        {
            Location loc = null;
            int dis;
            int min = int.MaxValue;

            foreach(Zone z in this.Chargers)
            {
                dis = destination.GetDistanceTo(z.Location);

                if(dis < min)
                {
                    loc = z.Location;
                    min = dis;
                }
            }

            return loc;
        }

        private void GetChargersLocations()
        {
            this.Chargers = new List<Zone>()
            {
                this.Layout[this.size.Row / 3][this.size.Col / 3], // NW
                this.Layout[this.size.Row / 3][2 * this.size.Col / 3], // NE
                this.Layout[2 * this.size.Row / 3][this.size.Col / 3], // SW
                this.Layout[2 * this.size.Row / 3][2 * this.size.Col / 3] // SE
            };
        }
    }
}
