using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Windows.Media;


namespace SpaceEfficiencyAnalyzer
{
    public partial class MainWindow : Window
    {
        private const double PixelsPerMeter = 50;
        private Rectangle selectedRectangle = null;
        private TextBlock selectedLabel = null;
        private bool isSelectingObject = false;
        private bool isDragging = false;
        private Point lastMousePosition;

        private readonly Brush[] rainbowColors = new Brush[]
        {
            Brushes.Red,
            Brushes.Orange,
            Brushes.Yellow,
            Brushes.Green,
            Brushes.Blue,
            Brushes.Indigo,
            Brushes.Violet
        };
        private int currentColorIndex = 0; // Tracks which rainbow color to use next
        public MainWindow()
        {
            InitializeComponent();
            //   this.DataContext = new YourViewModel();
        }

        private void AddObject_Click(object sender, RoutedEventArgs e)
        {
            if (RoomCanvas == null) return;

            double x = RoomCanvas.Children.Count * 60;
            double y = 50;

            // Get the next rainbow color
            Brush currentColor = GetNextRainbowColor();

            Rectangle rect = new Rectangle
            {
                Width = 2 * PixelsPerMeter,
                Height = 1 * PixelsPerMeter,
                Fill = currentColor,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };

            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);

            // Prompt the user for the object's name
            string objectName = PromptForObjectName();
            if (string.IsNullOrWhiteSpace(objectName)) objectName = "Unnamed Object";

            TextBlock label = new TextBlock
            {
                Text = $"{objectName} (2m x 1m)",
                Foreground = Brushes.Black,
                Background = Brushes.White,
                FontSize = 12,
                Margin = new Thickness(5)
            };

            Canvas.SetLeft(label, x);
            Canvas.SetTop(label, y - 20);

            RoomCanvas.Children.Add(rect);
            RoomCanvas.Children.Add(label);

            // Store the label as part of the rectangle's "Tag" for easy access
            rect.Tag = label;


            rect.MouseLeftButtonDown += Object_MouseLeftButtonDown;
            rect.MouseMove += Object_MouseMove;
            rect.MouseLeftButtonUp += Object_MouseLeftButtonUp;
            rect.MouseRightButtonDown += Object_MouseRightButtonDown; // Right-click to resize
        }

        private string PromptForObjectName()
        {
            // Create a dialog window to get the name from the user
            Window inputWindow = new Window
            {
                Title = "Enter Object Name",
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(10) };
            TextBox nameInput = new TextBox { Width = 200 };
            Button submitButton = new Button { Content = "Submit", Margin = new Thickness(5) };
            string objectName = null;

            submitButton.Click += (s, e) =>
            {
                objectName = nameInput.Text;
                inputWindow.Close();
            };

            panel.Children.Add(new TextBlock { Text = "Enter the name of the object:" });
            panel.Children.Add(nameInput);
            panel.Children.Add(submitButton);

            inputWindow.Content = panel;
            inputWindow.ShowDialog();

            return objectName;
        }

        private void Object_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
       
            selectedRectangle = sender as Rectangle;
            if (selectedRectangle != null)
            {
                // Find the label associated with this rectangle
                selectedLabel = selectedRectangle.Tag as TextBlock;

                isDragging = true;
                lastMousePosition = e.GetPosition(RoomCanvas);
                selectedRectangle.CaptureMouse();
            }
        }

        private void Object_MouseMove(object sender, MouseEventArgs e)
        {
           
            if (isDragging && selectedRectangle != null)
            {
                Point newMousePosition = e.GetPosition(RoomCanvas);
                double offsetX = newMousePosition.X - lastMousePosition.X;
                double offsetY = newMousePosition.Y - lastMousePosition.Y;

                double newX = Canvas.GetLeft(selectedRectangle) + offsetX;
                double newY = Canvas.GetTop(selectedRectangle) + offsetY;

                Canvas.SetLeft(selectedRectangle, newX);
                Canvas.SetTop(selectedRectangle, newY);

                // Move the label along with the object
                if (selectedRectangle.Tag is TextBlock label)
                {
                    Canvas.SetLeft(label, newX);
                    Canvas.SetTop(label, newY - 20); // Slightly above the object
                }

                lastMousePosition = newMousePosition;
            }
        }


        private void Object_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                selectedRectangle?.ReleaseMouseCapture();
            }
        }
        private void Object_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            /*selectedRectangle = sender as Rectangle;
            if (selectedRectangle != null)
            {
                selectedLabel = selectedRectangle.Tag as TextBlock;
                ShowResizeDialog();
            }*/
            selectedRectangle = sender as Rectangle;
            if (selectedRectangle != null)
            {
                selectedLabel = selectedRectangle.Tag as TextBlock;

                ContextMenu contextMenu = new ContextMenu();

                MenuItem resizeItem = new MenuItem { Header = "Resize" };
                resizeItem.Click += (s, ev) => ShowResizeDialog();

                MenuItem renameItem = new MenuItem { Header = "Rename" };
                renameItem.Click += (s, ev) => RenameObject();

                contextMenu.Items.Add(resizeItem);
                contextMenu.Items.Add(renameItem);

                contextMenu.IsOpen = true;
            }
        }

        private void RenameObject()
        {
            if (selectedLabel == null) return;

            string newName = PromptForObjectName();
            if (!string.IsNullOrWhiteSpace(newName))
            {
                string[] labelParts = selectedLabel.Text.Split('(');
                string sizePart = labelParts.Length > 1 ? $"({labelParts[1]}" : "";
                selectedLabel.Text = $"{newName} {sizePart}";
            }
        }


        private void ShowResizeDialog()
        {
            if (selectedRectangle == null) return;

            Window resizeWindow = new Window
            {
                Title = "Resize Object",
                Width = 300,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(10) };
            TextBox widthInput = new TextBox { Text = (selectedRectangle.Width / PixelsPerMeter).ToString(), Width = 200 };
            TextBox heightInput = new TextBox { Text = (selectedRectangle.Height / PixelsPerMeter).ToString(), Width = 200 };
            Button submitButton = new Button { Content = "Apply", Margin = new Thickness(5) };

            submitButton.Click += (s, e) =>
            {
                if (double.TryParse(widthInput.Text, out double newWidth) &&
                    double.TryParse(heightInput.Text, out double newHeight))
                {
                    selectedRectangle.Width = newWidth * PixelsPerMeter;
                    selectedRectangle.Height = newHeight * PixelsPerMeter;

                    if (selectedLabel != null)
                    {
                        selectedLabel.Text = $"{selectedLabel.Text.Split(' ')[0]} ({newWidth}m x {newHeight}m)";
                    }
                    resizeWindow.Close();
                }
            };

            panel.Children.Add(new TextBlock { Text = "Width (m):" });
            panel.Children.Add(widthInput);
            panel.Children.Add(new TextBlock { Text = "Height (m):" });
            panel.Children.Add(heightInput);
            panel.Children.Add(submitButton);

            resizeWindow.Content = panel;
            resizeWindow.ShowDialog();
        }


        private void SelectObject(Rectangle rect)
        {
            if (selectedRectangle != null)
                selectedRectangle.Stroke = Brushes.Black;

            selectedRectangle = rect;
            selectedRectangle.Stroke = Brushes.Red;
            deleteObjectButton.IsEnabled = true;
        }

        private void SelectObjectButton_Click(object sender, RoutedEventArgs e)
        {
            isSelectingObject = !isSelectingObject;
            selectObjectButton.Content = isSelectingObject ? "Click an Object" : "Select Object";
        }

        private void DeleteObjectButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedRectangle != null)
            {
                RoomCanvas.Children.Remove(selectedRectangle);
                selectedRectangle = null;
                deleteObjectButton.IsEnabled = false;
            }
        }
        private void UpdateRoomSize_Click(object sender, RoutedEventArgs e)
        {
            // Your logic for handling the click event
            try
            {
                // Assuming you have TextBoxes for user input, like:
                // RoomWidthInput and RoomHeightInput (both of type TextBox)

                double roomWidth = double.Parse(roomWidthInput.Text);
                double roomHeight = double.Parse(roomHeightInput.Text);

                // Assuming you have a method to update the room size, like:
                UpdateRoomSize(roomWidth, roomHeight);

                // Provide feedback to the user (Optional)
                MessageBox.Show($"Room size updated to: {roomWidth}m x {roomHeight}m", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (FormatException)
            {
                MessageBox.Show("Please enter valid numbers for width and height.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void UpdateRoomSize(double width, double height)
        {
            // Example: Updating a Canvas element representing the room
            RoomCanvas.Width = width * 100;  // Convert meters to pixels if needed
            RoomCanvas.Height = height * 100;
        }
        private Brush GetNextRainbowColor()
        {
            Brush color = rainbowColors[currentColorIndex];
            currentColorIndex = (currentColorIndex + 1) % rainbowColors.Length; // Cycle to next color
            return color;
        }

    }
}
