using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MultiPlayerLib;
using System.ServiceModel;


namespace MultiplayerGame
{

    /*
    NOTES:
    -We should probably handle several things asynchronously otherwise the drawing will be far too choppy and inconsistent
    -Updates to the service might have to be in an async method

    -Having a list of connected users somewhere in the menu to show that this really is a multiplayer "game"
    -Do we need some type of name selection to start?

    -Clearing the screen would be removing all children that are points or lines, but how would that work if there are multiple users,
    can 1 person clear the screen for all the other users as well?
    -Potential if easy enough to save a copy of the image currently drawn to screen

    -Change the drawing to use Windows.Media using an actual image? Or stay with controls for simplicity. However using controls to draw is extremely inefficient and may cause lag when we communicate over a network

        */

    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant, UseSynchronizationContext = false)]
    public partial class MainWindow : Window, IDrawCallback
    {
        IDrawInfo CurrentlyDrawn = null;

        //Variables to hold previous mouse state
        double dPreviousX;
        double dPreviousY;
        bool bMousePressed;

        //Variable limits
        int MinBrushThickness = 2;
        int MaxBrushThickness = 30;
        List<string> lColours = new List<string> {
            "Black",
            "Blue",
            "Red",
            "Green",
            "Yellow",
            "Purple",
            "Gold"
        };

        //Class to pass between service and clients
        //IDrawInfo CurrentlyDrawn;
        //ChannelFactory<IDrawInfo> channel;
        

        //Draw Variables
        int BrushThickness = 10;
        SolidColorBrush BrushColour = new SolidColorBrush(Colors.Black);

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
        }

        /*==========================================================================================================================================
        *
        *EVENTS
        *
        ==========================================================================================================================================*/
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Width = (Double)FindResource("WIDTH");
            this.Height = (Double)FindResource("HEIGHT");

            cboColour.ItemsSource = lColours;
            cboColour.SelectedItem = "Black";

            for (int i = MinBrushThickness; i < MaxBrushThickness; i+=2)
            {
                cboSize.Items.Add(i);
            }
            cboSize.SelectedItem = BrushThickness;

        }

        private void Canvas_MouseMoved(object sender, MouseEventArgs e)
        {
            //We will handle the mouse pressed ouselves because the order of events when gaining and losing focus can draw erratic lines otherwise
            if (bMousePressed)
            {
                DrawLine(e.MouseDevice.GetPosition(Canvas).X, e.MouseDevice.GetPosition(Canvas).Y);
            }
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            bMousePressed = true;
            DrawPoint(e.MouseDevice.GetPosition(Canvas).X, e.MouseDevice.GetPosition(Canvas).Y);
        }
        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            bMousePressed = false;
        }

        private void cboSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BrushThickness = (int)cboSize.SelectedValue;
        }

        private void cboColour_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //We will create a new colour from the string of named colours
            BrushColour = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(
                    (string)cboColour.SelectedValue
                )
            );
        }

        private void MenuItemNew_Click(object sender, RoutedEventArgs e)
        {
            Canvas.Children.Clear();
            TextBlock tb = new TextBlock();
            tb.Width = Canvas.Width;
            tb.Height = Canvas.Height;
            Canvas.Children.Add(tb);
        }
        private void MenuItemSave_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveDialog = new Microsoft.Win32.SaveFileDialog();
            saveDialog.Filter = "Image Files(*.BMP;*.JPG;*.GIF;*.PNG)|*.BMP;*.JPG;*.GIF;*.PNG";
            if (saveDialog.ShowDialog().Value == true)
            {
                RenderTargetBitmap targetBitmap =
        new RenderTargetBitmap((int)Canvas.ActualWidth,
                                (int)Canvas.ActualHeight,
                                96d, 96d,
                                PixelFormats.Default);
                //Need to render the whole window because if we render canvas the controls on the canvas wont be shown. Our drawing is not true media drawing
                targetBitmap.Render(this);

                BitmapEncoder encoder = new BmpBitmapEncoder();
                string extension = saveDialog.FileName.Substring(saveDialog.FileName.LastIndexOf('.'));
                switch (extension.ToLower())
                {
                    case ".jpg":
                        encoder = new JpegBitmapEncoder();
                        break;
                    case ".bmp":
                        encoder = new BmpBitmapEncoder();
                        break;
                    case ".gif":
                        encoder = new GifBitmapEncoder();
                        break;
                    case ".png":
                        encoder = new PngBitmapEncoder();
                        break;
                }
                encoder.Frames.Add(BitmapFrame.Create(targetBitmap));
                using (FileStream fs = File.Open(saveDialog.FileName, FileMode.OpenOrCreate))
                {
                    encoder.Save(fs);
                }
            }

        }
        /*==========================================================================================================================================
        *
        *WCF
        *
        ==========================================================================================================================================*/
        private delegate void GuiUpdateDelegate(double x, double y, double prevx, double prevy);


        //This should be called anywhere that we are drawing something
        private void UpdateCurrentlyDrawn(double x, double y, double prevx, double prevy)
        {
            if (this.Dispatcher.Thread == System.Threading.Thread.CurrentThread)
            {
                try
                {
                    if (CurrentlyDrawn != null)
                    {
                        if (CurrentlyDrawn.dd == null)
                        {
                            CurrentlyDrawn.dd = new DrawData(x, y, prevx, prevy, (string)cboColour.SelectedValue, (int)cboSize.SelectedValue);
                        }
                        else
                        {

                            CurrentlyDrawn.dd.brushThickness = (int)cboSize.SelectedValue;
                            CurrentlyDrawn.dd.colour = (string)cboColour.SelectedValue;
                            CurrentlyDrawn.dd.X = x;
                            CurrentlyDrawn.dd.Y = y;
                            CurrentlyDrawn.dd.prevX = prevx;
                            CurrentlyDrawn.dd.prevY = prevy;
                            CurrentlyDrawn.updateAllUsers();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
                this.Dispatcher.BeginInvoke(new GuiUpdateDelegate(UpdateCurrentlyDrawn), new object[] { x, y, prevx, prevy });
        }
        private void UpdateService()
        {
            //This will send the current class info to the service to update other clients

        }

        /*==========================================================================================================================================
        *
        *DRAW
        *
        ==========================================================================================================================================*/
        private void DrawPoint(double x, double y)
        {
            UpdateCurrentlyDrawn(x, y, x, y);

            Ellipse circle = new Ellipse();
            circle.Stroke = BrushColour;
            circle.StrokeThickness = BrushThickness/2;
            circle.Width = BrushThickness; circle.Height = BrushThickness;

            circle.Margin = new Thickness(x - BrushThickness/2, y - BrushThickness/2, 0, 0);

            dPreviousX = x;
            dPreviousY = y;

            Canvas.Children.Add(circle);
        }

        private void DrawLine(double x, double y)
        {
            UpdateCurrentlyDrawn(x, y, dPreviousX, dPreviousY);
            //Should eventually become a series of points to make the line look smooth
            //Draw a point at each end to make it smooth?
            Line line = new Line();
            line.Stroke = BrushColour;
            line.StrokeThickness = BrushThickness;
            
            line.X1 = dPreviousX;
            line.Y1 = dPreviousY;
            DrawPoint(dPreviousX, dPreviousY);

            line.X2 = dPreviousX = x;
            line.Y2 = dPreviousY = y;
            DrawPoint(x, y);

            Canvas.Children.Add(line);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (CurrentlyDrawn != null)
                CurrentlyDrawn.Leave(tb_userName.Text);
        }

        private void btn_join_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                connectToMessageBoard();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //this func connects to the service via the User endpoint 
        private void connectToMessageBoard()
        {
            
            try
            {
               // Configure the ABCs of using the MessageBoard service
               DuplexChannelFactory<IDrawInfo> channel
                        = new DuplexChannelFactory<IDrawInfo>(this, "DrawInfo");

                // Activate a DrawInfo object
                CurrentlyDrawn = channel.CreateChannel();

                if (CurrentlyDrawn.Join(tb_userName.Text))
                {
                    // Alias accepted by the service so update GUI
                    tbCanvas.IsEnabled = true;
                }
                else
                {
                    // Alias rejected by the service so nullify service proxies
                    CurrentlyDrawn = null;
                    MessageBox.Show("ERROR: User Name in use. Please try again.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        void IDrawCallback.DrawLine(double x, double y, double prevx, double prevy)
        {
            if (x != prevx && y != prevy)
            {
                Line line = new Line();
                line.Stroke = BrushColour;
                line.StrokeThickness = BrushThickness;

                line.X1 = prevx;
                line.Y1 = prevy;
                DrawPoint(prevx, prevy);

                line.X2 = x;
                line.Y2 = y;
                DrawPoint(x, y);

                Canvas.Children.Add(line);
            }
            else
            {
                DrawPoint(x, y);
            }
        }
    }
}
