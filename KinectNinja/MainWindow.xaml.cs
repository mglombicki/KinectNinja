using Microsoft.Kinect;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KinectNinja
{
    /// <summary>Interaction logic for MainWindow.xaml</summary>
    public partial class MainWindow : Window
    {
        KinectSensor sensor;
        ColorFrameSource colorFrameSource;
        BodyFrameSource bodyFrameSource;
        ColorFrameReader colorFrameReader;
        BodyFrameReader bodyFrameReader;
        DrawingGroup drawingGroup;
        Point fruitPoint;
        Vector fruitVelocity;
        int fruitSize;
        int score;
        private Random randomGenerator;

        public MainWindow()
        {
            // Get a reference to the Kinect sensor and turn it on
            sensor = KinectSensor.GetDefault();
            sensor.Open();

            // Get a reference to the color and body sources
            colorFrameSource = sensor.ColorFrameSource;
            bodyFrameSource = sensor.BodyFrameSource;

            // Open the readers for each of the sources
            colorFrameReader = sensor.ColorFrameSource.OpenReader();
            bodyFrameReader = sensor.BodyFrameSource.OpenReader();

            // Create event handlers for each of the readers
            colorFrameReader.FrameArrived += colorFrameReader_FrameArrived;
            bodyFrameReader.FrameArrived += bodyFrameReader_FrameArrived;

            // Get ready to draw graphics
            drawingGroup = new DrawingGroup();

            // Initialize fruit location, velocity, and size
            fruitPoint = new Point(0, colorFrameSource.FrameDescription.Height);
            fruitVelocity = new Vector(15, -30);
            fruitSize = 70;

            // Initialize a random generator
            randomGenerator = new Random();

            // Tell the UI to get ready to be controlled
            InitializeComponent();
        }

        void bodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            using (var canvas = drawingGroup.Open())
            {
                // Defensive programming: Just in case the sensor skips a frame, exit the function
                if (bodyFrame == null)
                {
                    return;
                }

                // Get the updated body states for all of the bodies in the scene
                var bodies = new Body[bodyFrame.BodyCount];
                bodyFrame.GetAndRefreshBodyData(bodies);

                // Set the dimensions
                canvas.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0,
                    colorFrameSource.FrameDescription.Width, colorFrameSource.FrameDescription.Height));

                // For each body
                foreach (var body in bodies)
                {
                    // That is currently being tracked
                    if (body.IsTracked)
                    {
                        // Uncomment this line to draw a green dot on each tracked joint:
                        //drawJoints(body, Brushes.Green, canvas);

                        // Draw dots on the hand joints
                        var leftHandPoint = body.Joints[JointType.HandLeft].Position;
                        var rightHandPoint = body.Joints[JointType.HandRight].Position;
                        drawCameraPoint(leftHandPoint, Brushes.Blue, 15, canvas);
                        drawCameraPoint(rightHandPoint, Brushes.Red, 15, canvas);

                        // Check if either hand is hitting a fruit
                        if(checkFruitCollision(leftHandPoint, fruitPoint, fruitSize) ||
                        checkFruitCollision(rightHandPoint, fruitPoint, fruitSize))
                        {
                            // Put the fruit off screen so it will be reset
                            fruitPoint = new Point(-100, -100);

                            // Increase the score
                            score++;
                            ScoreLabel.Content = score;
                        }
                    }
                }

                // Move the fruit
                fruitPoint.X += fruitVelocity.X;
                fruitPoint.Y += fruitVelocity.Y;

                // Apply gravity to the fruit
                fruitVelocity.Y += 0.6;

                // Check if the fruit is off the screen
                if (fruitPoint.X > 0 && fruitPoint.X < colorFrameSource.FrameDescription.Width
                && fruitPoint.Y > 0 && fruitPoint.Y < colorFrameSource.FrameDescription.Height)
                {
                    // Draw the fruit
                    canvas.DrawEllipse(Brushes.Yellow, null, fruitPoint, fruitSize, fruitSize);
                }
                else
                {
                    // Reset the fruit location and velocity
                    if (randomGenerator.Next(2) == 1) // Pick a random side to start from
                    {
                        fruitPoint = new Point(colorFrameSource.FrameDescription.Width, colorFrameSource.FrameDescription.Height);
                        fruitVelocity = new Vector(-15, -30);
                    }
                    else
                    {
                        fruitPoint = new Point(0, colorFrameSource.FrameDescription.Height);
                        fruitVelocity = new Vector(15, -30);
                    }
                }

                // Show the drawing on the screen
                DrawingImage.Source = new DrawingImage(drawingGroup);
            }
        }

        private bool checkFruitCollision(CameraSpacePoint cameraPoint, Point fruitPoint, int fruitSize)
        {
            // Convert the CameraSpacePoint to a 2D point
            var colorPoint = sensor.CoordinateMapper.MapCameraPointToColorSpace(cameraPoint);
            var canvasPoint = new Point(colorPoint.X, colorPoint.Y);

            // Get the pythagorean distance between the hand and the fruit
            var dist = Math.Sqrt(Math.Pow(canvasPoint.X - fruitPoint.X, 2) + 
                Math.Pow(canvasPoint.Y - fruitPoint.Y, 2));

            // If the distance is less than the radius, then we have a collision
            if (dist < fruitSize)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void drawJoints(Body body, Brush brushColor, DrawingContext canvas)
        {
            foreach (var jointType in body.Joints.Keys)
            {
                // Get the point (in 3D space) for the joint
                var cameraPoint = body.Joints[jointType].Position;

                // Draw it using the helper method below
                drawCameraPoint(cameraPoint, brushColor, 15, canvas);
            }
        }

        private void drawCameraPoint(CameraSpacePoint cameraPoint, Brush brushColor, int radius, DrawingContext canvas)
        {
            // Convert the point into 2D so we can use it on the screen
            var colorPoint = sensor.CoordinateMapper.MapCameraPointToColorSpace(cameraPoint);
            var canvasPoint = new Point(colorPoint.X, colorPoint.Y);

            // Check if it's safe to draw at that point
            if (canvasPoint.X > 0 && canvasPoint.X < colorFrameSource.FrameDescription.Width
                && canvasPoint.Y > 0 && canvasPoint.Y < colorFrameSource.FrameDescription.Height)
            {
                // Draw a circle
                canvas.DrawEllipse(brushColor, null, canvasPoint, radius, radius);
            }
        }

        private void colorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                // Defensive programming: Just in case the sensor skips a frame, exit the function
                if (colorFrame == null)
                {
                    return;
                }

                // Setup an array that can hold all of the bytes of the image
                var colorFrameDescription = colorFrame.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
                var frameSize = colorFrameDescription.Width * colorFrameDescription.Height * colorFrameDescription.BytesPerPixel;
                var colorData = new byte[frameSize];

                // Fill in the array with the data from the camera
                colorFrame.CopyConvertedFrameDataToArray(colorData, ColorImageFormat.Bgra);

                // Use the byte array to make an image and put it on the screen
                CameraImage.Source = BitmapSource.Create(
                    colorFrame.ColorFrameSource.FrameDescription.Width,
                    colorFrame.ColorFrameSource.FrameDescription.Height,
                    96, 96, PixelFormats.Bgr32, null, colorData, colorFrameDescription.Width * 4);
            }
        }
    }
}