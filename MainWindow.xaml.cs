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
        private Rectangle firstObject = null;
        private Rectangle secondObject = null;
        private bool isMeasuringDistance = false;
        private Line distanceLine = null;
        private Polygon arrowHead1 = null;
        private Polygon arrowHead2 = null;
        private TextBlock distanceLabel = null;
        private Rectangle firstAlignedObject = null;
        private Rectangle secondAlignedObject = null;



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
            // Create walls
            CreateWall(0, 0, RoomCanvas.Width, 2, true);  // Top wall (Red)
            CreateWall(0, RoomCanvas.Height - 2, RoomCanvas.Width, 2, false);  // Bottom wall (Black)
            CreateWall(0, 0, 2, RoomCanvas.Height, true);  // Left wall (Red)
            CreateWall(RoomCanvas.Width - 2, 0, 2, RoomCanvas.Height, false);  // Right wall (Black)
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
            Rectangle clickedRectangle = sender as Rectangle;
            if (clickedRectangle == null) return;

            if (isMeasuringDistance)
            {
                // Handle distance measurement selection (Ctrl Key)
                if (firstObject == null)
                {
                    firstObject = clickedRectangle;
                }
                else if (secondObject == null && clickedRectangle != firstObject)
                {
                    secondObject = clickedRectangle;
                    ShowDistanceBetweenObjects();
                    firstObject = null;
                    secondObject = null;
                    isMeasuringDistance = false;
                    measureDistanceButton.Content = "Measure Distance";
                }
                return;
            }

            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                // Handle alignment selection (Shift Key)
                if (firstAlignedObject == null)
                {
                    firstAlignedObject = clickedRectangle;
                    alignButton.IsEnabled = false;
                }
                else if (secondAlignedObject == null && clickedRectangle != firstAlignedObject)
                {
                    secondAlignedObject = clickedRectangle;

                    if (firstAlignedObject.Width == secondAlignedObject.Width && firstAlignedObject.Height == secondAlignedObject.Height)
                    {
                        alignButton.IsEnabled = true;
                    }
                    else
                    {
                        MessageBox.Show("Selected objects must be the same size to align them.");
                        firstAlignedObject = null;
                        secondAlignedObject = null;
                    }
                }
            }
            else
            {
                // Handle normal dragging
                selectedRectangle = clickedRectangle;
                selectedLabel = clickedRectangle.Tag as TextBlock;

                if (selectedRectangle != null)
                {
                    isDragging = true;
                    lastMousePosition = e.GetPosition(RoomCanvas);
                    selectedRectangle.CaptureMouse();
                }
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

            selectedRectangle = sender as Rectangle;
            if (selectedRectangle != null)
            {
                selectedLabel = selectedRectangle.Tag as TextBlock;

                ContextMenu contextMenu = new ContextMenu();

                MenuItem resizeItem = new MenuItem { Header = "Resize" };
                resizeItem.Click += (s, ev) => ShowResizeDialog();

                MenuItem renameItem = new MenuItem { Header = "Rename" };
                renameItem.Click += (s, ev) => RenameObject();

                MenuItem flipHorizontal = new MenuItem { Header = "Flip Horizontal" };
                flipHorizontal.Click += (s, ev) => FlipObject(true);

                MenuItem flipVertical = new MenuItem { Header = "Flip Vertical" };
                flipVertical.Click += (s, ev) => FlipObject(false);

                contextMenu.Items.Add(resizeItem);
                contextMenu.Items.Add(renameItem);
                contextMenu.Items.Add(flipHorizontal);
                contextMenu.Items.Add(flipVertical);

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

        private void MeasureDistanceButton_Click(object sender, RoutedEventArgs e)
        {
            isMeasuringDistance = !isMeasuringDistance;
            measureDistanceButton.Content = isMeasuringDistance ? "Select 2 Objects" : "Measure Distance";

            // Clear previous selections
            firstObject = null;
            secondObject = null;

            // Disable alignment selection mode
            firstAlignedObject = null;
            secondAlignedObject = null;
            alignButton.IsEnabled = false;
        }


        private void ShowDistanceBetweenObjects()
        {
            RemoveDistanceVisuals();

            if (firstObject == null || secondObject == null) return;

            double x1 = Canvas.GetLeft(firstObject);
            double y1 = Canvas.GetTop(firstObject);
            double w1 = firstObject.Width;
            double h1 = firstObject.Height;

            double x2 = Canvas.GetLeft(secondObject);
            double y2 = Canvas.GetTop(secondObject);
            double w2 = secondObject.Width;
            double h2 = secondObject.Height;

            // Find nearest edges
            double leftDist = Math.Abs(x1 - (x2 + w2));
            double rightDist = Math.Abs((x1 + w1) - x2);
            double topDist = Math.Abs(y1 - (y2 + h2));
            double bottomDist = Math.Abs((y1 + h1) - y2);

            double minHorizontalDist = Math.Min(leftDist, rightDist);
            double minVerticalDist = Math.Min(topDist, bottomDist);

            double lineX1 = 0, lineY1 = 0, lineX2 = 0, lineY2 = 0;

            if (minHorizontalDist <= minVerticalDist)
            {
                if (leftDist < rightDist)
                {
                    lineX1 = x1;
                    lineY1 = y1 + h1 / 2;
                    lineX2 = x2 + w2;
                    lineY2 = y2 + h2 / 2;
                }
                else
                {
                    lineX1 = x1 + w1;
                    lineY1 = y1 + h1 / 2;
                    lineX2 = x2;
                    lineY2 = y2 + h2 / 2;
                }
            }
            else
            {
                if (topDist < bottomDist)
                {
                    lineX1 = x1 + w1 / 2;
                    lineY1 = y1;
                    lineX2 = x2 + w2 / 2;
                    lineY2 = y2 + h2;
                }
                else
                {
                    lineX1 = x1 + w1 / 2;
                    lineY1 = y1 + h1;
                    lineX2 = x2 + w2 / 2;
                    lineY2 = y2;
                }
            }

            distanceLine = new Line
            {
                X1 = lineX1,
                Y1 = lineY1,
                X2 = lineX2,
                Y2 = lineY2,
                Stroke = Brushes.DarkRed,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };
            RoomCanvas.Children.Add(distanceLine);

            arrowHead1 = CreateArrow(lineX2, lineY2, lineX1, lineY1);
            RoomCanvas.Children.Add(arrowHead1);

            double distanceMeters = Math.Sqrt(Math.Pow(lineX2 - lineX1, 2) + Math.Pow(lineY2 - lineY1, 2)) / PixelsPerMeter;
            distanceLabel = new TextBlock
            {
                Text = $"{distanceMeters:F2} m",
                Foreground = Brushes.DarkRed,
                Background = Brushes.White,
                FontWeight = FontWeights.Bold
            };
            Canvas.SetLeft(distanceLabel, (lineX1 + lineX2) / 2 + 5);
            Canvas.SetTop(distanceLabel, (lineY1 + lineY2) / 2 - 10);
            RoomCanvas.Children.Add(distanceLabel);
        }



        private Polygon CreateArrow(double fromX, double fromY, double toX, double toY)
        {
            double arrowLength = 10;
            double arrowWidth = 6;

            Vector direction = new Vector(fromX - toX, fromY - toY);
            direction.Normalize();
            Vector normal = new Vector(-direction.Y, direction.X);

            Point tip = new Point(fromX, fromY);
            Point base1 = tip - direction * arrowLength + normal * arrowWidth;
            Point base2 = tip - direction * arrowLength - normal * arrowWidth;

            return new Polygon
            {
                Points = new PointCollection { tip, base1, base2 },
                Fill = Brushes.DarkRed
            };
        }

        private void RemoveDistanceVisuals()
        {
            if (distanceLine != null) RoomCanvas.Children.Remove(distanceLine);
            if (arrowHead1 != null) RoomCanvas.Children.Remove(arrowHead1);
            if (arrowHead2 != null) RoomCanvas.Children.Remove(arrowHead2);
            if (distanceLabel != null) RoomCanvas.Children.Remove(distanceLabel);

            distanceLine = null;
            arrowHead1 = null;
            arrowHead2 = null;
            distanceLabel = null;
        }

        private void FlipObject(bool horizontal)
        {
            if (selectedRectangle == null || selectedLabel == null) return;

            double originalWidth = selectedRectangle.Width / PixelsPerMeter;
            double originalHeight = selectedRectangle.Height / PixelsPerMeter;

            selectedRectangle.Width = originalHeight * PixelsPerMeter;
            selectedRectangle.Height = originalWidth * PixelsPerMeter;

            string orientation = horizontal ? "Horizontal" : "Vertical";
            string[] labelParts = selectedLabel.Text.Split('(');
            string namePart = labelParts[0].Trim();
            selectedLabel.Text = $"{namePart} ({orientation}) ({originalHeight}m x {originalWidth}m)";
        }

        private void CreateWall(double x, double y, double width, double height, bool isRed)
        {
            Rectangle wall = new Rectangle
            {
                Width = width,
                Height = height,
                Stroke = isRed ? Brushes.Red : Brushes.Black,
                StrokeThickness = 2,
                Fill = isRed ? Brushes.Red : Brushes.Black
            };

            Canvas.SetLeft(wall, x);
            Canvas.SetTop(wall, y);

            RoomCanvas.Children.Add(wall);

            wall.MouseLeftButtonDown += Wall_MouseLeftButtonDown;
        }

        private void Wall_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Rectangle clickedWall = sender as Rectangle;
            if (clickedWall == null || RoomCanvas == null) return;

            RemoveDistanceVisuals();

            double wallX = Canvas.GetLeft(clickedWall);
            double wallY = Canvas.GetTop(clickedWall);
            double wallWidth = clickedWall.Width;
            double wallHeight = clickedWall.Height;

            Rectangle nearestObject = null;
            double minDistance = double.MaxValue;
            Point startPoint = new Point();
            Point endPoint = new Point();

            foreach (UIElement element in RoomCanvas.Children)
            {
                if (element is Rectangle obj && obj != clickedWall && obj.Tag is TextBlock)
                {
                    double objX = Canvas.GetLeft(obj);
                    double objY = Canvas.GetTop(obj);
                    double objW = obj.Width;
                    double objH = obj.Height;

                    if (wallWidth == 2) // Vertical wall: measure horizontal distance
                    {
                        double distance = Math.Min(Math.Abs(objX - wallX), Math.Abs((objX + objW) - wallX));
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            nearestObject = obj;

                            double centerY = objY + objH / 2;
                            double fromX = (objX > wallX) ? objX : objX + objW;
                            startPoint = new Point(fromX, centerY);
                            endPoint = new Point(wallX, centerY);
                        }
                    }
                    else if (wallHeight == 2) // Horizontal wall: measure vertical distance
                    {
                        double distance = Math.Min(Math.Abs(objY - wallY), Math.Abs((objY + objH) - wallY));
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            nearestObject = obj;

                            double centerX = objX + objW / 2;
                            double fromY = (objY > wallY) ? objY : objY + objH;
                            startPoint = new Point(centerX, fromY);
                            endPoint = new Point(centerX, wallY);
                        }
                    }
                }
            }

            if (nearestObject != null)
            {
                DrawDistanceLine(startPoint, endPoint, minDistance / PixelsPerMeter);
            }
        }

        private void AlignButton_Click(object sender, RoutedEventArgs e)
        {
            if (firstAlignedObject == null || secondAlignedObject == null) return;

            // Ask user for alignment type
            var result = MessageBox.Show("Align Horizontally? (Click Yes)\nAlign Vertically? (Click No)", "Align Objects", MessageBoxButton.YesNoCancel);

            if (result == MessageBoxResult.Cancel) return;

            double x1 = Canvas.GetLeft(firstAlignedObject);
            double y1 = Canvas.GetTop(firstAlignedObject);
            double x2 = Canvas.GetLeft(secondAlignedObject);
            double y2 = Canvas.GetTop(secondAlignedObject);

            if (result == MessageBoxResult.Yes)
            {
                // Align Horizontally (Side-by-side)
                if (x1 < x2)
                {
                    Canvas.SetLeft(secondAlignedObject, x1 + firstAlignedObject.Width);
                    Canvas.SetTop(secondAlignedObject, y1);
                }
                else
                {
                    Canvas.SetLeft(firstAlignedObject, x2 + secondAlignedObject.Width);
                    Canvas.SetTop(firstAlignedObject, y2);
                }
            }
            else if (result == MessageBoxResult.No)
            {
                // Align Vertically (One above the other)
                if (y1 < y2)
                {
                    Canvas.SetTop(secondAlignedObject, y1 + firstAlignedObject.Height);
                    Canvas.SetLeft(secondAlignedObject, x1);
                }
                else
                {
                    Canvas.SetTop(firstAlignedObject, y2 + secondAlignedObject.Height);
                    Canvas.SetLeft(firstAlignedObject, x2);
                }
            }

            // Reset selection
            firstAlignedObject = null;
            secondAlignedObject = null;
            alignButton.IsEnabled = false;
        }


        private void ShowWallDistance(Rectangle obj, Rectangle wall)
        {
            RemoveDistanceVisuals();

            double objLeft = Canvas.GetLeft(obj);
            double objTop = Canvas.GetTop(obj);
            double objRight = objLeft + obj.Width;
            double objBottom = objTop + obj.Height;

            double wallLeft = Canvas.GetLeft(wall);
            double wallTop = Canvas.GetTop(wall);
            double wallRight = wallLeft + wall.Width;
            double wallBottom = wallTop + wall.Height;

            double lineX1 = 0, lineY1 = 0, lineX2 = 0, lineY2 = 0;

            if (wall.Width == 2) // Vertical wall
            {
                lineX1 = wallLeft;
                lineY1 = objTop + obj.Height / 2;
                lineX2 = (objLeft > wallLeft) ? objLeft : objRight;
                lineY2 = lineY1;
            }
            else if (wall.Height == 2) // Horizontal wall
            {
                lineX1 = objLeft + obj.Width / 2;
                lineY1 = wallTop;
                lineX2 = lineX1;
                lineY2 = (objTop > wallTop) ? objTop : objBottom;
            }

            // Draw the line
            distanceLine = new Line
            {
                X1 = lineX1,
                Y1 = lineY1,
                X2 = lineX2,
                Y2 = lineY2,
                Stroke = Brushes.DarkRed,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };
            RoomCanvas.Children.Add(distanceLine);

            // Draw the arrowhead
            arrowHead1 = CreateArrow(lineX2, lineY2, lineX1, lineY1);
            RoomCanvas.Children.Add(arrowHead1);

            // Show the distance label
            double distanceMeters = Math.Sqrt(Math.Pow(lineX2 - lineX1, 2) + Math.Pow(lineY2 - lineY1, 2)) / PixelsPerMeter;
            distanceLabel = new TextBlock
            {
                Text = $"{distanceMeters:F2} m",
                Foreground = Brushes.DarkRed,
                Background = Brushes.White,
                FontWeight = FontWeights.Bold
            };
            Canvas.SetLeft(distanceLabel, (lineX1 + lineX2) / 2 + 5);
            Canvas.SetTop(distanceLabel, (lineY1 + lineY2) / 2 - 10);
            RoomCanvas.Children.Add(distanceLabel);
        }


       
        private void DrawDistanceLine(Point start, Point end, double distanceMeters)
        {
            distanceLine = new Line
            {
                X1 = start.X,
                Y1 = start.Y,
                X2 = end.X,
                Y2 = end.Y,
                Stroke = Brushes.DarkRed,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };
            RoomCanvas.Children.Add(distanceLine);

            arrowHead1 = CreateArrow(end.X, end.Y, start.X, start.Y);
            RoomCanvas.Children.Add(arrowHead1);

            distanceLabel = new TextBlock
            {
                Text = $"{distanceMeters:F2} m",
                Foreground = Brushes.DarkRed,
                Background = Brushes.White,
                FontWeight = FontWeights.Bold
            };

            Canvas.SetLeft(distanceLabel, (start.X + end.X) / 2 + 5);
            Canvas.SetTop(distanceLabel, (start.Y + end.Y) / 2 - 10);
            RoomCanvas.Children.Add(distanceLabel);
        }


        private Brush GetNextRainbowColor()
        {
            Brush color = rainbowColors[currentColorIndex];
            currentColorIndex = (currentColorIndex + 1) % rainbowColors.Length; // Cycle to next color
            return color;
        }

    }
}
