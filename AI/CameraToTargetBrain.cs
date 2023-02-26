using SinkingFeeling.Utils;
using SinkingFeelingPOC.DroneAttacking.CameraView;
using SinkingFeelingPOC.ShipTarget.UI;
using SinkingFeelingPOC.World;
using System.Diagnostics;
using System.Drawing.Imaging;

namespace SinkingFeeling.AI
{
    /// <summary>
    /// Neural network to determine where to hit the ship.
    /// </summary>
    internal class CameraToTargetBrain
    {
        /// <summary>
        /// Where it loads/saves the AI model from/to.
        /// </summary>
        const string c_AIFilePath = @"c:\temp\sinking-feeling.ai";

        /// <summary>
        /// Neural network to map the camera image to the centre of ship.
        /// </summary>
        readonly NeuralNetwork neuralNetworkMappingPixelsToCentreOfShip;

        /// <summary>
        /// Training data to associate ship and target relative to it.
        /// </summary>
        readonly List<TrainingDataItemImageToTarget> training = new();

        /// <summary>
        /// Controls everythin.
        /// </summary>
        readonly WorldController worldController;

        /// <summary>
        /// Constructor.
        /// </summary>
        internal CameraToTargetBrain(WorldController controller)
        {
            worldController = controller;

            // INPUT: camera (200x80)
            // HIDDEN: some random number I chose.
            // OUTPUT: x-position of target within camera.
            neuralNetworkMappingPixelsToCentreOfShip = new(new int[] { Camera.CameraWidthPX * 80, 5, 1 });

            // load rather than compute.
            if (1 == 0 && neuralNetworkMappingPixelsToCentreOfShip.Load(c_AIFilePath))
            {
                worldController.Telemetry(">> LOADED AI MODEL");
                return;
            }

            // don't want to wait for this??? Then copy the /Model/sinking-feeling.ai to c:\temp

            worldController.ShowMessageOnDroneCam("TRAINING");

            worldController.Telemetry(">> CREATING TRAINING DATA");
            worldController.Telemetry(">> PLEASE WAIT...");
            Application.DoEvents();

            // we don't do this, if we load it.
            training = CreateTrainingData();

            worldController.Telemetry(">> TRAINING AI MODEL");
            worldController.Telemetry(">> PLEASE WAIT...");
            Application.DoEvents();

            // teach the neural network to associate camera with target position.
            TraingNeuralNetworkUsingTrainingData(training);

            // save the neural network for next time.
            neuralNetworkMappingPixelsToCentreOfShip.Save(c_AIFilePath);

            worldController.Telemetry(">> AI MODEL SAVED");
            worldController.ShowMessageOnDroneCam("TRAINED");
            training.Clear();
        }

        /// <summary>
        /// Debug. Outputs the boat images, labelled with bullseye.
        /// </summary>
        internal void DebugShow()
        {
            foreach (var image in training)
            {
                int target = (int)Math.Round(AIMapCameraImageIntoTargetX(image.pixelsInAIinputForm));

                using Bitmap s = new($@"c:\temp\sink\boat_{image.Size}_{image.X}.png");

                using Graphics g = Graphics.FromImage(s);

                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                // draw bullseye               
                g.FillEllipse(Brushes.Red, target - 10, 70, 20, 20);
                g.FillEllipse(Brushes.White, target - 6, 74, 12, 12);
                g.FillEllipse(Brushes.Red, target - 3, 77, 6, 6);

                g.DrawString(target.ToString(), new Font("Arial", 8), Brushes.Yellow, 5, 5);

                s.Save($@"c:\temp\sink\result_{image.X}.png", ImageFormat.Png);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xCentreOfBoat"></param>
        /// <param name="sizeOfBoat"></param>
        /// <returns></returns>
        internal Bitmap? GetImage(int xCentreOfBoat, int sizeOfBoat)
        {
            foreach (var image in training)
            {
                if (image.X == xCentreOfBoat && image.Size == sizeOfBoat)
                {
                    return new Bitmap($@"c:\temp\sink\boat_{sizeOfBoat}_{xCentreOfBoat}.png");
                }
            }

            return null;
        }

        /// <summary>
        /// Create training data mapping image to position.
        /// </summary>
        /// <returns></returns>
        private static List<TrainingDataItemImageToTarget> CreateTrainingData()
        {
            Debug.WriteLine("CREATING TRAINING DATA");

            List<TrainingDataItemImageToTarget> trainingData = new();

            // for each size of boat, generate image
            for (float sizeOfBoat = 25; sizeOfBoat < 175; sizeOfBoat += 5)
            {
                for (float xCentreOfBoat = 0; xCentreOfBoat < Camera.CameraWidthPX; xCentreOfBoat++)
                {
                    Bitmap boatImage = new(Camera.CameraWidthPX, 80);

                    using Graphics graphics = Graphics.FromImage(boatImage);
                    graphics.Clear(Color.Black);

                    ShipRenderer.EdgesOfRealisticRenderer(graphics, xCentreOfBoat, sizeOfBoat);

                    // uncomment this if you wish to see the images
                    // boatImage.Save($@"c:\temp\sink\images\boat_{sizeOfBoat}_{xCentreOfBoat}.png",ImageFormat.Png);

                    trainingData.Add(new(CameraPixelsToDoubleArray(boatImage), xCentreOfBoat, (int)sizeOfBoat));
                }
            }

            Debug.WriteLine("CREATING TRAINING DATA COMPLETE");
            return trainingData;
        }

        /// <summary>
        /// Training the AI.
        /// </summary>
        /// <param name="trainingData"></param>
        private void TraingNeuralNetworkUsingTrainingData(List<TrainingDataItemImageToTarget> trainingData)
        {
            Debug.WriteLine("TRAINING STARTED");

            int epoch = 0;

            bool trained;

            do
            {
                ++epoch;

                // push ALL the training images through back propagation
                foreach (TrainingDataItemImageToTarget trainingDataItem in trainingData)
                {
                    neuralNetworkMappingPixelsToCentreOfShip.BackPropagate(trainingDataItem.pixelsInAIinputForm, new double[] { trainingDataItem.X / Camera.CameraWidthPX });
                }

                trained = true;
          
                // check every image returns an accurate enough response (within 5 pixels for large, 2 for small)
                foreach (TrainingDataItemImageToTarget trainingDataItem in trainingData)
                {
                    double aiTargetPosition = AIMapCameraImageIntoTargetX(trainingDataItem.pixelsInAIinputForm);

                    double deviationXofAItoExpected = Math.Abs(aiTargetPosition - trainingDataItem.X);

                    if (deviationXofAItoExpected > (trainingDataItem.Size < 40 ? 2 : 5))
                    {
                        trained = false;
                        break;
                    }
                }

                worldController.Telemetry($">> EPOCH {epoch}");
                Application.DoEvents();

                Debug.WriteLine(epoch);
            } while (!trained);

            Debug.WriteLine($"TRAINING COMPLETE. EPOCH {epoch}");
        }

        /// <summary>
        /// Converts a camera image into an "x" position of the ship within the image.
        /// </summary>
        /// <param name="cameraInput"></param>
        /// <returns></returns>
        internal double AIMapCameraImageIntoTargetX(double[] cameraInput)
        {
            double[] aiTargetPosition = neuralNetworkMappingPixelsToCentreOfShip.FeedForward(cameraInput);

            return Math.Round(aiTargetPosition[0] * (float)Camera.CameraWidthPX);
        }

        /// <summary>
        /// Returns ALL the pixels as a "double" array. We are using 1 channel only.
        /// Pixels are deemed "white" or not "white".
        /// </summary>
        /// <returns>pixels as array of 1/0.</returns>
        internal static double[] CameraPixelsToDoubleArray(Bitmap frameImage)
        {
            ByteAccessibleBitmap accessibleBitmap = new(frameImage);

            int width = frameImage.Width;
            int height = frameImage.Height;

            double[] vertPixels = new double[Camera.CameraWidthPX * 80];

            int whitePixelCount = 0;

            // visit all pixels in image, and put a "1" in our output if pixel is found.
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // white is red+green+blue, we only need one of them to detect presence of pixel
                    if (accessibleBitmap.GetRedChannelPixel(x, y) != 0)
                    {
                        vertPixels[y * 200 + x] = 1; // 1 = white pixel

                        ++whitePixelCount;
                    }
                }
            }

            if (whitePixelCount == 0)
            {
                //Debugger.Break(); // this can happen under normal circumstances, but useful for debug
            }

            return vertPixels;
        }
    }
}