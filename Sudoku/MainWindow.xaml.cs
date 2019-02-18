using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace TexiService
{
    public partial class MainWindow : Window
    {
        private Center center;
        private Rectangle texiHighlight;
        private Rectangle employeeHighlight;
        private Timer utilityTimer;
        private Dispatcher dispatcher;

        public DispatcherTimer globalTimer;

        public MainWindow()
        {
            this.InitializeComponent();

            this.center = new Center(new LayoutSize(31, 21)); // Create a center object with random layout.
            this.dispatcher = Application.Current.Dispatcher;
            this.globalTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 100) }; // DispacthTimer to update view.
            this.globalTimer.Tick += this.UpdateLayoutView;

            // Log.
            this.icLog.Items.Add(new { message = "* Center object created." });
            this.icLog.Items.Add(new { message = "* globalTimer object created." });

            this.texiList.DataContext = this.center.Texis;
            this.employeeList.DataContext = this.center.Employees;

            this.CreateMainGrid();
            this.StartListener();
            this.globalTimer.Start();

            // Log.
            this.icLog.Items.Add(new { message = "* globalTimer started." });
        }

        public void UpdateLayoutView(object sender, EventArgs e)
        {
            for(int i = 1; i < this.center.Size.Row; i++)
                for(int j = 1; j < this.center.Size.Col; j++)
                {
                    string imgName = "I_" + i.ToString() + '_' + j.ToString();
                    string recName = "Z_" + i.ToString() + '_' + j.ToString();
                    Rectangle rec = LogicalTreeHelper.FindLogicalNode(this.mainGrid, recName) as Rectangle;

                    if(this.center.Layout[i][j].HasTexiOn)
                        rec.Fill = new SolidColorBrush(Colors.Yellow);
                    else
                        switch(this.center.Layout[i][j].Type)
                        {
                            case ZoneType.Operation:
                                rec.Fill = new SolidColorBrush(Colors.Green);
                                break;
                            case ZoneType.TexiCharge:
                                rec.Fill = new SolidColorBrush(Colors.Red);
                                break;
                            default:
                                rec.Fill = new SolidColorBrush(Colors.AliceBlue);
                                break;
                        }

                    if(this.center.Layout[i][j].HasEmployeeOn)
                        (LogicalTreeHelper.FindLogicalNode(this.mainGrid, imgName) as Image).Visibility = Visibility.Visible;
                    else
                        (LogicalTreeHelper.FindLogicalNode(this.mainGrid, imgName) as Image).Visibility = Visibility.Hidden;
                }
        }
        public void Log(string msg) => this.dispatcher.Invoke(() => this.icLog.Items.Add(new { message = msg }));
        public void PrintEmployeeMessage(string msg)
        {
            (this.tbEmployees as TextBlock).Visibility = Visibility.Visible;

            this.utilityTimer = new Timer(o => this.dispatcher.Invoke(() => this.tbEmployees.Visibility = Visibility.Hidden), null, 2000, Timeout.Infinite);
            (this.tbEmployees as TextBlock).Text = msg;
        }
        public void PrintTexiMessage(string msg)
        {
            (this.texiTb as TextBlock).Visibility = Visibility.Visible;

            this.utilityTimer = new Timer(o => this.dispatcher.Invoke(() => this.texiTb.Visibility = Visibility.Hidden), null, 2000, Timeout.Infinite);
            (this.texiTb as TextBlock).Text = msg;

        }

        private void StartListener()
        {
            BackgroundWorker listenerWorker = new BackgroundWorker();
            listenerWorker.DoWork += this.ListenWorker;
            listenerWorker.RunWorkerAsync();

            // Log.
            this.icLog.Items.Add(new { message = "* Started listening to clients." });
        }
        private void ListenWorker(object sender, DoWorkEventArgs e)
        {
            // Setup a socket to recive ClientData objects.
            IPHostEntry ipHostInfo = Dns.GetHostEntry("localhost");
            IPAddress ipAddress = ipHostInfo.AddressList[1];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 9999);
            Socket socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            socket.Bind(localEndPoint);
            socket.Listen(999);

            while(true)
            {
                // Accept client connection and statrt a backgroundworker
                //  thet listens to it.
                Socket recSoc = socket.Accept();
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += this.ClientConnectionWorker;
                bw.RunWorkerAsync(recSoc);
            }
        }
        private void ClientConnectionWorker(object sender, DoWorkEventArgs e)
        {
            Socket recSoc = (Socket)e.Argument; // Connection socket.
            BinaryFormatter bf = new BinaryFormatter();
            Employee employee = null; // Thread employee.

            // Log
            Application.Current.Dispatcher.Invoke(() => this.icLog.Items.Add(new { message = "+ Client connection received..." }));

            // Keep listen to data from client.
            while(true)
            {
                byte[] data = new byte[8192];
                int len;

                try
                {
                    len = recSoc.Receive(data); // Receive client data.
                    Array.Resize(ref data, len); // Resize data array from 8192 to the datas' real size.

                    // Dserialize the received object.
                    var deserializedData = this.DeserializeClientData(data, bf, employee);

                    if(deserializedData.GetType() == typeof(string))
                    {

                        // Send layout size.
                        if((string)deserializedData == "Send layout size please.")
                            using(MemoryStream sizeStrem = new MemoryStream())
                            {
                                // Serialize the size object.
                                bf.Serialize(sizeStrem, this.center.Size);
                                // Send the data.
                                recSoc.Send(sizeStrem.ToArray());

                                // Log
                                this.Log("+ Sending layout size to client.");
                            }
                    }
                    else
                    {
                        // Add an new employee.
                        if(deserializedData.GetType() == typeof(SerializableEmployee))
                        {
                            employee = new Employee((SerializableEmployee)deserializedData, this.center);
                            this.center.AddEmployee(employee);

                            this.dispatcher.Invoke(() => this.icLog.Items.Add(new { message = "+ Received employee information: Id " + employee.Id })); // Log
                        }
                        // Call a texi for the employee.
                        else if(deserializedData.GetType() == typeof(Location))
                        {
                            Texi texi = this.dispatcher.Invoke(() => this.center.CallTexi(employee, (Location)deserializedData));

                            if(texi != null)
                            {
                                this.globalTimer.Tick -= this.UpdateLayoutView;
                                this.globalTimer.Tick += (o, arg) => this.UpdateClient(recSoc, employee, texi, bf);
                                this.globalTimer.Tick += this.UpdateLayoutView;
                            }
                            else
                            {
                                // TODO
                            }
                        }
                    }
                }
                // Client data is null.
                catch(NullReferenceException)
                {
                    Application.Current.Dispatcher.Invoke(() => this.icLog.Items.Add(new { message = "- Employee " + employee.Id + "- Client data is null." })); // Log
                }
                // If the socket connection ended close the connection.
                catch(SocketException)
                {
                    this.center.RemoveEmployee(employee); // Remove employee from employees list.
                    Application.Current.Dispatcher.Invoke(() => this.icLog.Items.Add(new { message = "- Employee " + employee.Id + " has disconnected." })); // Log
                    Thread.CurrentThread.Abort(); // Close thread.
                }
            }
        }
        private object DeserializeClientData(byte[] data, BinaryFormatter bf, Employee employee)
        {
            using(MemoryStream dataStream = new MemoryStream(data))
            {
                try
                {
                    return bf.Deserialize(dataStream);
                }
                catch(ArgumentNullException exp)
                {
                    Application.Current.Dispatcher.Invoke(() => this.icLog.Items.Add(new { message = "- Error while attampeting to deserialize client data: Datastream is null." })); // Log
                }
                catch(SerializationException exp)
                {
                    Application.Current.Dispatcher.Invoke(() => this.icLog.Items.Add(new { message = "- Error while attampeting to deserialize client data." })); // Log
                }
                catch(SecurityException exp)
                {
                    Application.Current.Dispatcher.Invoke(() => this.icLog.Items.Add(new { message = "- Error while attampeting to deserialize client data: Security error." })); // Log
                }

                return null;
            }
        }
        private void UpdateClient(Socket recSoc, Employee employee, Texi texi, BinaryFormatter bf)
        {
            // Convert employee and texi to serializable objects.
            SerializableEmployee serEmp = employee.ToSerializable();
            SerializableTexi serTexi = texi.ToSerializable();

            // Serialize and send the employee object.
            using(MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, serEmp);
                recSoc.Send(ms.ToArray());
            }

            // Serialize and send the texi object.
            using(MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, serTexi);
                recSoc.Send(ms.ToArray());
            }

            // If the employee has reached his detination remove thred from globalTimer.
            if(employee.Location == employee.Destination)
            {
                using(MemoryStream ms = new MemoryStream())
                {
                    bf.Serialize(ms, Encoding.ASCII.GetBytes("Destination reached."));
                    recSoc.Send(ms.ToArray());
                }

                this.globalTimer.Tick -= (o, arg) => this.UpdateClient(recSoc, employee, texi, bf);
            }
        }

        private void CreateMainGrid()
        {
            this.texiHighlight = new Rectangle
            {
                Stroke = null,
                StrokeThickness = 3
            };
            this.employeeHighlight = new Rectangle
            {
                Stroke = null,
                StrokeThickness = 3
            };

            Panel.SetZIndex(this.texiHighlight, 99);
            Panel.SetZIndex(this.employeeHighlight, 99);

            this.mainGrid.Children.Add(this.texiHighlight);
            this.mainGrid.Children.Add(this.employeeHighlight);

            // Build rows.
            for(int i = 1; i <= this.center.Size.Row; i++)
            {
                RowDefinition rd = new RowDefinition { Height = new GridLength(20) };
                this.mainGrid.RowDefinitions.Add(rd);
            }

            // Build columns.
            for(int i = 1; i <= this.center.Size.Col; i++)
            {
                ColumnDefinition cd = new ColumnDefinition { Width = new GridLength(20) };
                this.mainGrid.ColumnDefinitions.Add(cd);
            }

            // Build side numbers.
            for(int i = 1; i < this.center.Size.Row; i++)
            {

                TextBlock tbSide = new TextBlock
                {
                    FontSize = 15,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Text = i.ToString()
                };
                Grid.SetRow(tbSide, i);
                Grid.SetColumn(tbSide, 0);

                this.mainGrid.Children.Add(tbSide);
            }

            // Build top numbers.
            for(int i = 0; i < this.center.Size.Col; i++)
            {
                TextBlock tbTop = new TextBlock
                {
                    FontSize = 15,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Text = i.ToString()
                };
                Grid.SetRow(tbTop, 0);
                Grid.SetColumn(tbTop, i);

                this.mainGrid.Children.Add(tbTop);
            }

            // Add zones.
            for(int i = 1; i < this.center.Size.Row; i++)
                for(int j = 1; j < this.center.Size.Col; j++)
                {
                    Rectangle rec = new Rectangle { Name = "Z_" + i.ToString() + "_" + j.ToString() };
                    Image img = new Image() // An image indicating an employee on the square.
                    {
                        Visibility = Visibility.Hidden,
                        Name = "I_" + i.ToString() + '_' + j.ToString(),
                        Source = new BitmapImage(new Uri(@"C:\Users\Geco\Documents\Visual Studio 2017\Projects\Sudoku\Sudoku\Img\Employee.png"))
                    };

                    Grid.SetRow(rec, i);
                    Grid.SetColumn(rec, j);
                    Grid.SetRow(img, i);
                    Grid.SetColumn(img, j);
                    Panel.SetZIndex(rec, -1);
                    Panel.SetZIndex(img, 999);

                    rec.MouseEnter += this.ZoneHover;

                    // Add colors.
                    switch(this.center.Layout[i][j].Type)
                    {
                        case ZoneType.Operation:
                            rec.Fill = new SolidColorBrush(Colors.Green);
                            break;
                        case ZoneType.TexiCharge:
                            rec.Fill = new SolidColorBrush(Colors.Red);
                            break;
                        default:
                            rec.Fill = new SolidColorBrush(Colors.AliceBlue);
                            break;
                    }

                    this.mainGrid.Children.Add(rec);
                    this.mainGrid.Children.Add(img);
                }

            // Log
            this.icLog.Items.Add(new { message = "* Main grid created." });
        }


        private void SelectTexi(object sender, SelectedCellsChangedEventArgs e)
        {
            if(this.texiList.SelectedItem != null)
            {
                // Get the texi as an object from the list.
                Texi texi = this.texiList.SelectedItem as Texi;
                int row = texi.Row;
                int col = texi.Col;

                // Color the texiHighlight black.
                if(this.texiHighlight.Stroke == null)
                    this.texiHighlight.Stroke = new SolidColorBrush(Colors.Black);

                // Move the texiHighlight to the selected texi.
                Grid.SetRow(this.texiHighlight, row);
                Grid.SetColumn(this.texiHighlight, col);

                this.center.SelectedTexis = this.texiList.SelectedItems;
            }
        }
        private void AddTexi(object sender, RoutedEventArgs e)
        {
            // Add a random texi.
            this.center.AddTexi();

            // Scroll texi list to bottom.
            this.texiList.ScrollIntoView(this.texiList.Items[this.texiList.Items.Count - 1]);

            // Scroll log view to bottom.
            ScrollViewer svLog = VisualTreeHelper.GetChild(this.icLog, 0) as ScrollViewer;
            svLog.ScrollToBottom();
        }
        private void RemoveTexi(object sender, RoutedEventArgs e)
        {
            try
            {
                if(this.texiHighlight != null)
                {
                    int row = Grid.GetRow(this.texiHighlight);
                    int col = Grid.GetColumn(this.texiHighlight);

                    this.texiHighlight.Stroke = null;

                    this.center.RemoveTexi();
                }
            }
            catch(NullReferenceException)
            {
                // TODO
            }
        }
        private void MoveTexi(object sender, RoutedEventArgs e)
        {
            if(this.texiHighlight.Stroke != null)
            {
                try
                {
                    int moveFromRow = Grid.GetRow(this.texiHighlight);
                    int moveFromCol = Grid.GetColumn(this.texiHighlight);
                    int moveToRow = int.Parse(this.moveToRow.Text);
                    int moveToCol = int.Parse(this.moveToCol.Text);

                    Location destination = new Location(moveToRow, moveToCol);
                    Texi texi = this.center.Layout[moveFromRow][moveFromCol].Texi;

                    this.texiHighlight.Stroke = null;
                    texi.Move(destination);
                }
                catch(ArgumentNullException)
                {
                    this.PrintTexiMessage("No coordinates.");
                }
                catch(FormatException)
                {
                    this.PrintTexiMessage($"Coordinates number range is between 1 and {this.center.Size.Col}.");
                }
                catch(OverflowException)
                {
                    this.PrintTexiMessage("Coordinates number range is between 1 and {this.center.Size.Col}.");
                }
            }
            else
            {
                this.PrintTexiMessage("No texi was selected.");
            }
        }

        private void SelectEmployee(object sender, RoutedEventArgs e)
        {
            if(this.employeeList.SelectedItem != null)
            {
                // Get the employee as an object from the list.
                Employee employee = this.employeeList.SelectedItem as Employee;
                int row = employee.Row;
                int col = employee.Col;

                // Color the employeeHighlight black.
                if(this.employeeHighlight.Stroke == null)
                    this.employeeHighlight.Stroke = new SolidColorBrush(Colors.Blue);

                // Move the employeeHighlight to the selected texi.
                Grid.SetRow(this.employeeHighlight, row);
                Grid.SetColumn(this.employeeHighlight, col);

                this.center.SelectedEmployees = this.employeeList.SelectedItems;
            }
        }
        private void AddEmployee(object sender, RoutedEventArgs e)
        {
            this.center.AddRandomEmployee();

            this.svEmployees.ScrollToBottom();

            // Scroll employee list to bottom.
            this.employeeList.ScrollIntoView(this.employeeList.Items[this.employeeList.Items.Count - 1]);
        }
        private void RemoveEmployee(object sender, RoutedEventArgs e)
        {
            try
            {
                if(this.employeeHighlight != null)
                {
                    int row = Grid.GetRow(this.employeeHighlight);
                    int col = Grid.GetColumn(this.employeeHighlight);

                    this.employeeHighlight.Stroke = null;

                    this.center.RemoveEmployee();
                }
            }
            catch(NullReferenceException)
            {
                this.tbEmployees.Text = "No texis to remove.";
                this.utilityTimer = new Timer(o => this.dispatcher.Invoke(() => this.tbEmployees.Visibility = Visibility.Hidden), null, 2000, Timeout.Infinite);
            }
        }
        private void CallTexi(object sender, RoutedEventArgs e)
        {
            try
            {
                int destRow = int.Parse(this.destToRow.Text);
                int destCol = int.Parse(this.destToCol.Text);
                Location destination = new Location(destRow, destCol);
                Employee employee = this.center.SelectedEmployees[0] as Employee;

                this.center.CallTexi(employee, destination);
            }
            catch(NullReferenceException)
            {
                this.PrintEmployeeMessage("No employee selected.");
            }
            catch(ArgumentNullException)
            {
                this.PrintEmployeeMessage("No coordinates input.");
            }
            catch(FormatException)
            {
                this.PrintEmployeeMessage($"Coordinates range is between 1 and {this.center.Size.Row}.");
            }
            catch(OverflowException)
            {
                this.PrintEmployeeMessage($"Coordinates range is between 1 and {this.center.Size.Row}.");
            }
        }
        private void ZoneHover(object sender, MouseEventArgs e)
        {
            int row = Grid.GetRow((Rectangle)sender);
            int col = Grid.GetColumn((Rectangle)sender);

            Zone zone = this.center.Layout[row][col];

            this.tbZoneLocation.Text = zone.Location.ToString();
            this.tbZoneType.Text = zone.Type.ToString();

            if(zone.HasEmployeeOn || zone.HasTexiOn)
            {
                this.gbInfo.Visibility = Visibility.Visible;

                if(zone.HasEmployeeOn)
                {
                    this.gbInfo.Header = "Employee";

                    this.gridInfoTb00.Text = "Id:";
                    this.gridInfoTb01.Text = "Name:";
                    this.gridInfoTb02.Text = "Destination:";

                    this.gridInfoTb0.Text = zone.Employee.Id.ToString();
                    this.gridInfoTb1.Text = zone.Employee.First + ' ' + zone.Employee.Last;
                    this.gridInfoTb2.Text = zone.Employee.Destination?.ToString() ?? "";
                }
                else
                {
                    this.gbInfo.Header = "Texi";

                    this.gridInfoTb00.Text = "Number:";
                    this.gridInfoTb01.Text = "Location:";
                    this.gridInfoTb02.Text = "Status:";

                    this.gridInfoTb0.Text = zone.Texi.Number.ToString();
                    this.gridInfoTb1.Text = zone.Texi.Location.ToString();
                    this.gridInfoTb2.Text = zone.Texi.Status.ToString();
                }
            }
            else this.gbInfo.Visibility = Visibility.Hidden;
        }
    }
}