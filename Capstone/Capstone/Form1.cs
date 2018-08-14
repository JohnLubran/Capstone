using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace Capstone
{
    public partial class Form1 : Form
    {
        ///////////////////////////////////////////////////////////////////////////////////
        /// Stores multiple sets of data for markers.

        /// Contains multiple points, which use the point class. The number of points
        /// included is specified with an integer when the constructor is called.
        /// This class is used to store marker point data during each frame process step
        /// and the marker locations stored for final output.
        ///////////////////////////////////////////////////////////////////////////////////
        public class markerset
        {
            /// Points included in this set.
            public point[] marker { get; set; }
            /// Initializes the set of points.
            /// <param name="size">The number of points in this set.</param>
            public markerset(int size)
            {
                marker = new point[size];
            }
        }
        ///////////////////////////////////////////////////////////////////////////////////
        /// Stores data for marker points.

        /// Contains three integers which represent the x and y position and the size of a
        /// market point.
        /// This class is used to store marker point data during each frame process step,
        /// the marker locations stored for final output.
        ///////////////////////////////////////////////////////////////////////////////////
        public class point
        {
            /// X position for this point, or set of points.
            public int xValue { get; set; }
            /// Y position for this point, or set of points.
            public int yValue { get; set; }
            /// Number of points in this set, if this is a set.
            public int size { get; set; }
        }

        /// Horizontal resolution of the video.
        public int xResolution = 1920;
        /// Vertical resolution of the video.
        public int yResolution = 1080;
        /// Frame rate of the video.
        public int frameRate = 60;
        /// Start time, from beginning of video, in seconds.
        public decimal startTime = 0;
        /// End time, from beginning of video, in seconds.
        public decimal endTime = 1;

        /// Maximum number of sets of pixels allowed per frame.
        public int upperSetCount = 1000;
        /// Minimum size for a set of pixels to be accepted.
        public int lowerSetSize = 1;
        /// Maximum size for a set of pixels to be accepted.
        public int upperSetSize = 200;
        /// Range from previous points a fast search will scan.
        public int fastSearchRange = 100;

        /// List of pixels with the correct marker color; updates per frame.
        public point[] nMatrix;
        /// Bookmark variable to store where we are in the nMatrix.
        public int nMatrixSize = 0;
        /// Stores whether each pixel is being considered as a marker pixel.
        public bool[,] mxnMatrix;
        /// Stores data for sets of contiguous pixels, within a frame.
        public markerset[] pixelSets;
        /// Stores data for final output
        public markerset[] outputData;

        /// Path of Data folder, used for all file navigation.
        public string folderPath = "";
        /// Name of video file being accessed.
        public string videoFileName = "";
        /// Name of settings file being accessed.
        public string settingsFileName = "";

        /// If RGB is being used to process colors, as opposed to HSB.
        public bool ifRGB = false;
        /// Minimum Red value, lowest is 0.
        public decimal redLow = 0;
        /// Maximum Red value, highest is 255.
        public decimal redHigh = 255;
        /// Minimum Green value, lowest is 0.
        public decimal greenLow = 0;
        /// Maximum Green value, highest is 255.
        public decimal greenHigh = 255;
        /// Minimum Blue value, lowest is 0.
        public decimal blueLow = 0;
        /// Maximum Blue value, highest is 255.
        public decimal blueHigh = 255;
        /// Minimum Hue value, lowest is 0.
        public decimal hueLow = 0;
        /// Maximum Hue value, highest is 255.
        public decimal hueHigh = 255;
        /// Minimum Saturation value, lowest is 0.
        public decimal saturationLow = 0;
        /// Maximum Saturation value, highest is 255.
        public decimal saturationHigh = 255;
        /// Minimum Brightness value, lowest is 0.
        public decimal brightnessLow = 0;
        /// Maximum Brightness value, highest is 255.
        public decimal brightnessHigh = 255;

        /// Left position of bounding box for calibration
        public int leftBounding = 0;
        /// Top position of bounding box for calibration
        public int topBounding = 0;
        /// Width of bounding box for calibration
        public int widthBounding = 1;
        /// Height of bounding box for calibration
        public int heightBounding = 1;

        /// Whether or not we are happy with the red calibration
        public bool rLock = false;
        /// Whether or not we are happy with the green calibration
        public bool gLock = false;
        /// Whether or not we are happy with the blue calibration
        public bool bLock = false;
        /// Whether or not we are happy with the hue calibration
        public bool hLock = false;
        /// Whether or not we are happy with the saturation calibration
        public bool sLock = false;
        /// Whether or not we are happy with the brightness calibration
        public bool lLock = false;

        /// Red Matrix for calibration
        public int[,] redMatrix;
        /// Green Matrix for calibration
        public int[,] greenMatrix;
        /// Blue Matrix for calibration
        public int[,] blueMatrix;
        /// Hue Matrix for calibration
        public int[,] hueMatrix;
        /// Saturation Matrix for calibration
        public int[,] saturationMatrix;
        /// Brightness Matrix for calibration
        public int[,] brightnessMatrix;

        /// Distance from camera to set-up; used for pixel-real space conversion.
        public double range = 84;
        /// Slope used for pixel-real space conversion.
        public double slopeCalibration = 0.0006038;
        /// Intercept used for pixel-real space conversion.
        public double interceptCalibration = -0.0004014;

        /// If there are no points in the frame
        public bool noPoints = false;

        /// Builds GUI for program.
        public Form1()
        {
            InitializeComponent();
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// When you push the Run Search button, this calls the main function.
        ///////////////////////////////////////////////////////////////////////////////////
        private void RunSearchButton_Click(object sender, EventArgs e)
        {
            MainFunction();
        }
        ///////////////////////////////////////////////////////////////////////////////////
        /// Imports settings.

        /// Finds the input file name using TextBoxInputs, calls ImportSettings, then sets
        /// the data in the boxes to match using TextBoxOutputs.
        ///////////////////////////////////////////////////////////////////////////////////
        private void button2_Click(object sender, EventArgs e)
        {
            TextBoxInputs();
            try
            {
                ImportSettings(folderPath + settingsFileName + ".xml");
            }
            catch (IOException)
            {
            }
            TextBoxOutputs();
        }
        ///////////////////////////////////////////////////////////////////////////////////
        /// When you push the Calibrate for Current Settings button, this calls the Main
        /// Calibration function.
        ///////////////////////////////////////////////////////////////////////////////////
        private void button4_Click(object sender, EventArgs e)
        {
            MainCalibration();
        }
        ///////////////////////////////////////////////////////////////////////////////////
        /// Exports settings.

        /// Finds the input file name using TextBoxInputs, then calls ExportSettings.
        /// Does not work if file exists.
        ///////////////////////////////////////////////////////////////////////////////////
        private void button3_Click(object sender, EventArgs e)
        {
            TextBoxInputs();
            try
            {
                ExportSettings(folderPath + settingsFileName + ".xml");
            }
            catch (IOException)
            {
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Evaluates whether a pixel is in the color range for a marker.

        /// Tests either RGB or HSB based on ifRGB.
        /// If ifRGB is true, tests whether the pixel is in range of the redLow, redHigh,
        /// greenLow, greenHigh, blueLow, and blueHigh values, accordingly.
        /// If ifRGB is false, tests whether the pixel is in range of the hueLow, hueHigh,
        /// saturationLow, saturationHigh, brightnessLow, and brightnessHigh values,
        /// accordingly.
        /// Returns true if pixel colors are in range, and false otherwise.
        /// <param name="pixelColor">The color input for the pixel being checked</param>
        ///////////////////////////////////////////////////////////////////////////////////
        public bool PixelCheck(Color pixelColor)
        {
            if (ifRGB)
            {
                int red = pixelColor.R;
                if (red <= redHigh && red >= redLow)
                {
                    int green = pixelColor.G;
                    if (green <= greenHigh && green >= greenLow)
                    {
                        int blue = pixelColor.B;
                        if (blue <= blueHigh && blue >= blueLow)
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                decimal hue = 255 * (decimal)pixelColor.GetHue() / 360;
                if (hue <= hueHigh && hue >= hueLow)
                {
                    decimal saturation = 255 * (decimal)pixelColor.GetSaturation();
                    if (saturation >= saturationLow && saturation <= saturationHigh)
                    {
                        decimal brightness = 255 * (decimal)pixelColor.GetBrightness();
                        if (brightness >= brightnessLow && brightness <= brightnessHigh)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Evaluates whether a set is the correct size.

        /// Given an integer index, tests whether a set's size is correct according to
        /// lowerSetSize and upperSetSize (adjusting index to start at 1).
        /// Returns true if set is within the range, and false otherwise.
        /// <param name="setNumber">The index of the set being evaluated.</param>
        ///////////////////////////////////////////////////////////////////////////////////
        public bool EvaluateSet(int setNumber)
        {
            if (pixelSets[setNumber].marker[upperSetSize - 1].xValue != 0)
            {
                if (pixelSets[setNumber].marker[upperSetSize - 1].yValue != 0)
                {
                    //If the final data points aren't empty, then we are too big.
                    return false;
                }
            }
            if (pixelSets[setNumber].marker[lowerSetSize - 1].xValue == 0)
                if (pixelSets[setNumber].marker[lowerSetSize - 1].yValue == 0)
                {
                    //If the points at the lower limit are empty, then we are too small.
                    return false;
                }
            //Otherwise we are within the size range.
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Evaluates the size of a set.

        /// Given an integer index, finds the last non-empty point of a pixelSet, and
        /// returns that index as the size. Otherwise, returns upperSetSize.
        /// <param name="setNumber">The index of the set being evaluated.</param>
        ///////////////////////////////////////////////////////////////////////////////////
        public int SetSize(int setNumber)
        {
            for (int i = 0; i < upperSetSize; i++)
            {
                if (pixelSets[setNumber].marker[i].xValue == 0)
                    if (pixelSets[setNumber].marker[i].yValue == 0)
                    {
                        return i;
                    }
            }
            return upperSetSize;
        }
        ///////////////////////////////////////////////////////////////////////////////////
        /// Evaluates the centroid of a set.

        /// Given an integer index, evaluates the mean x and y position of a set. Returns
        /// the resulting x and y position as int[2], or 0,0 if the set's size is 0.
        /// <param name="setNumber">The index of the set being evaluated.</param>
        ///////////////////////////////////////////////////////////////////////////////////
        public int[] Centroid(int setNumber)
        {
            int setSize = SetSize(setNumber);
            int xValue = 0;
            int yValue = 0;
            int[] output = new int[2];
            if (setSize > 0)
            {
                for (int i = 0; i < setSize; i++)
                {
                    xValue += pixelSets[setNumber].marker[i].xValue;
                    yValue += pixelSets[setNumber].marker[i].yValue;
                }
                xValue = (int)((decimal)xValue / setSize);
                yValue = (int)((decimal)yValue / setSize);
                output[0] = xValue;
                output[1] = yValue;
            }
            else
            {
                output[0] = 0;
                output[1] = 0;
            }
            return output;
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Finds the name of an image.

        /// The first input(integer) determines the format for the numbering such that it
        /// has three characters, ie "-123". Then determines the remaining format based on
        /// the second input(bool), ie ".../Data/frameA-123.bmp".
        /// Warning. Highest possible frameRate is 10^9-1.
        /// <param name="frameNumber">The frame number of the imaged.</param>
        /// <param name="vidA">The bool for the type of frame naming scheme used.</param>
        ///////////////////////////////////////////////////////////////////////////////////
        public string FindImageName(int frameNumber, bool vidA)
        {
            string frameName = "";
            string output = "";
            if (frameNumber <= 9)
            {
                frameName = "-00000000" + frameNumber + ".bmp";
            }
            if (frameNumber > 9)
            {
                frameName = "-0000000" + frameNumber + ".bmp";
            }
            if (frameNumber > 99)
            {
                frameName = "-000000" + frameNumber + ".bmp";
            }
            if (frameNumber > 999)
            {
                frameName = "-00000" + frameNumber + ".bmp";
            }
            if (frameNumber > 9999)
            {
                frameName = "-0000" + frameNumber + ".bmp";
            }
            if (frameNumber > 99999)
            {
                frameName = "-000" + frameNumber + ".bmp";
            }
            if (frameNumber > 999999)
            {
                frameName = "-00" + frameNumber + ".bmp";
            }
            if (frameNumber > 9999999)
            {
                frameName = "-0" + frameNumber + ".bmp";
            }
            if (frameNumber > 99999999)
            {
                frameName = "-" + frameNumber + ".bmp";
            }
            if (vidA)
            {
                output = folderPath + "frameA" + frameName;
            }
            else
            {
                output = folderPath + "frameB" + frameName;
            }
            return output;
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Finds the first nonexistent file for csv output.

        /// Searches 2,147,483,646 possible names for an output file in the scheme of
        /// "...\Data\output-123.csv". If none exist, use "...\Data\TMarks.csv"
        ///////////////////////////////////////////////////////////////////////////////////
        public string FindCSVName()
        {
            string output;
            for (int i = 0; i < 2147483646; i++)
            {
                output = folderPath + "output-" + i + ".csv";
                if (!File.Exists(output))
                {
                    return output;
                }
            }
            output = folderPath + "TMarks.csv";
            return output;
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Creates matrices used per video.

        /// Allocates space for pixelSets, mxnMatrix, nMatrix, and outputData matrices.
        ///////////////////////////////////////////////////////////////////////////////////
        public void MatrixAllocation()
        {
            int frameCountTotal = (int)((endTime - startTime) * frameRate);
            pixelSets = new markerset[upperSetCount];
            mxnMatrix = new bool[xResolution, yResolution];
            nMatrix = new point[xResolution * yResolution];
            outputData = new markerset[frameCountTotal + 1];
            for (int i = 0; i < nMatrix.Length; i++)
            {
                nMatrix[i] = new point();
            }
            for (int i = 0; i < upperSetCount; i++)
            {
                pixelSets[i] = new markerset(upperSetSize);
                for (int j = 0; j < pixelSets[i].marker.Length; j++)
                {
                    pixelSets[i].marker[j] = new point();
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Cleans matrices for one frame step.

        /// Sets zeros in used values of nMatrix, based on nMatrixSize, then resets
        /// nMatrixSize, then zeros all values in pixelSets.
        ///////////////////////////////////////////////////////////////////////////////////
        public void MatrixCleaning()
        {
            if (nMatrixSize > 0)
            {
                for (int i = 0; i < nMatrixSize; i++)
                {
                    nMatrix[i].xValue = 0;
                    nMatrix[i].yValue = 0;
                }
            }
            nMatrixSize = 0;
            for (int i = 0; i < upperSetCount; i++)
            {
                for (int j = 0; j < pixelSets[i].marker.Length; j++)
                {
                    pixelSets[i].marker[j].xValue = 0;
                    pixelSets[i].marker[j].yValue = 0;
                }
            }
        }
        ///////////////////////////////////////////////////////////////////////////////////
        /// Chooses and calls the full or fast search for an image.

        /// If frameNumber is a whole multiple of frameRate, use the full search, otherwise
        /// use the fast search.
        /// <param name="fileLocation">File location being searched.</param>
        /// <param name="frameNumber">Frame number of the frame being used.</param>
        /// <param name="secondNumber">Second number of the frame being searched.</param>
        ///////////////////////////////////////////////////////////////////////////////////
        public void SearchImage(string fileLocation, int frameNumber, int secondNumber)
        {
            if (frameNumber % frameRate == 1)
            {
                InputBMPFull(fileLocation);
            }
            else
            {
                InputBMPFast(fileLocation, frameNumber, secondNumber);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Forms sets of contiguous pixels.

        /// For each point in the nMatrix runs CheckAt, which combines all the nearby
        /// pixels into a set. Then, checks the size using EvaluateSet. If it's good,
        /// increments the set count. Otherwise, erases the invalid set. Skips points in
        /// nMatrix which aren't in mxnMatrix.
        ///////////////////////////////////////////////////////////////////////////////////
        public void SetFormation()
        {
            int fullSets = 0;
            if (nMatrixSize > 0)
            {
                for (int i = 0; i < nMatrixSize; i++)
                {
                    if (mxnMatrix[nMatrix[i].xValue, nMatrix[i].yValue])
                    {
                        CheckAt(nMatrix[i].xValue, nMatrix[i].yValue, fullSets);
                        if (EvaluateSet(fullSets))
                        {
                            fullSets++;
                        }
                        else
                        {
                            for (int j = 0; j < upperSetSize; j++)
                            {
                                pixelSets[fullSets].marker[j].xValue = 0;
                                pixelSets[fullSets].marker[j].yValue = 0;
                            }
                        }
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Finds how many points are in pixelSets.

        /// Looks for the first empty set in pixelSets and returns the index before that.
        ///////////////////////////////////////////////////////////////////////////////////
        public int PointCount()
        {
            int output = 0;
            for (int i = 0; i < upperSetCount; i++)
            {
                if (pixelSets[i].marker[0].xValue == 0)
                {
                    if (pixelSets[i].marker[0].yValue == 0)
                    {
                        return output;
                    }
                }
                output++;
            }
            return output;
        }
        ///////////////////////////////////////////////////////////////////////////////////
        /// Stores results from a single frame for final output.

        /// Finds number of points in pixelSets using PointCount, allocates space in
        /// outputData for these, calculates the centroid for each set using Centroid,
        /// then stores the centroid and set size (calculated using SetSize) in outputData.
        /// <param name="timeValue">Index for the time value for the final outputs.</param>
        ///////////////////////////////////////////////////////////////////////////////////
        public void CentroidOutput(int timeValue)
        {
            noPoints = false;
            int pointCount = PointCount();
            if (pointCount > 0)
            {
                outputData[timeValue] = new markerset(pointCount);
                for (int j = 0; j < outputData[timeValue].marker.Length; j++)
                {
                    outputData[timeValue].marker[j] = new point();
                }
                int[] coord = new int[3];
                for (int i = 0; i < pointCount; i++)
                {
                    coord = Centroid(i);
                    int x = coord[0];
                    int y = coord[1];
                    int setSize = SetSize(i);
                    outputData[timeValue].marker[i].xValue = x;
                    outputData[timeValue].marker[i].yValue = y;
                    outputData[timeValue].marker[i].size = setSize;
                }
            }
            else
            {
                noPoints = true;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Checks if we should add the pixel and then the nearest pixels.

        /// If the pixel specified by the first and second integer inputs is in the
        /// mxnMatrix, checks the size of the set specified by the third integer input,
        /// then adds the pixels location to that position in that set. If the set is full
        /// this will overwrite the last point. Then calls CheckNear to check the nearest
        /// points.
        /// <param name="x">X position of the pixel being checked.</param>
        /// <param name="y">Y position of the pixel being checked.</param>
        /// <param name="fullSets">Index of the set being processed.</param>
        ///////////////////////////////////////////////////////////////////////////////////
        public void CheckAt(int x, int y, int fullSets)
        {
            if (mxnMatrix[x, y])
            {
                mxnMatrix[x, y] = false;
                int setSize = SetSize(fullSets);
                if (setSize < upperSetSize)
                {
                    pixelSets[fullSets].marker[setSize].xValue = x + 1;
                    pixelSets[fullSets].marker[setSize].yValue = y + 1;
                    CheckNear(x, y, fullSets);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Checks if we should add the nearest pixels.

        /// Without checking out-of-bounds points, calls CheckAt for the pixels below,
        /// above, to the right, and to the left of the location specified with the first
        /// two integer inputs, and maintains the third integer input as the current set.
        /// <param name="x">X position of the pixel being checked.</param>
        /// <param name="y">Y position of the pixel being checked.</param>
        /// <param name="fullSets">Index of the set being processed.</param>
        ///////////////////////////////////////////////////////////////////////////////////
        public void CheckNear(int x, int y, int fullSets)
        {
            if (y != yResolution - 1)
            {
                CheckAt(x, y + 1, fullSets);
            }
            if (y != 0)
            {
                CheckAt(x, y - 1, fullSets);
            }
            if (x != xResolution - 1)
            {
                CheckAt(x + 1, y, fullSets);
            }
            if (x != 0)
            {
                CheckAt(x - 1, y, fullSets);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Adds point to nMatrix and mxnMatrix.

        /// If the point specified with the two integer inputs isn't in the mxnMatrix,
        /// which would imply we have already added it, add it to the mxnMatrix, and the
        /// nMatrix, and increment nMatrixSize.
        /// <param name="x">X position of the pixel being added.</param>
        /// <param name="y">Y position of the pixel being added.</param>
        ///////////////////////////////////////////////////////////////////////////////////
        public void AddPointToSearchMesh(int x, int y)
        {
            if (!mxnMatrix[x, y])
            {
                mxnMatrix[x, y] = true;
                nMatrix[nMatrixSize].xValue = x;
                nMatrix[nMatrixSize].yValue = y;
                nMatrixSize++;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Orchestrate the entire process.

        /// Starts with progressBar1.Value at 0, then calls TextBoxInputs, then
        /// MatrixAllocation, then for every second runs SecondStep and for every frame
        /// within that, runs FrameStep, and evaluates the percentage of frames processed
        /// and sets the progressBar1.Value to this. Finally, runs Outputcsv and, if
        /// successful, sets progressBar1.Value to 0. If it catchs IOException, sets 
        /// progressBar1.Value to 100, indicating the output was not successful.
        ///////////////////////////////////////////////////////////////////////////////////
        public void MainFunction()
        {
            progressBar1.Value = 0;
            TextBoxInputs();
            MatrixAllocation();
            int startFrame = (int)(startTime * frameRate);
            int timeCountTotal = (int)(Math.Ceiling(endTime - startTime));
            bool vidA = false;
            for (int i = 0; i < timeCountTotal; i++)
            {
                vidA = SecondStep(i);
                for (int f = 0; f < frameRate; f++)
                {
                    FrameStep(1+f, i, vidA);
                    decimal progressValue = (decimal)i + (decimal)f / (decimal)frameRate;
                    progressValue = progressValue * 100 / (decimal)timeCountTotal;
                    progressBar1.Value = (int)progressValue;
                }
            }
            try
            {
                Outputcsv();
                progressBar1.Value = 0;
            }
            catch (IOException)
            {
                progressBar1.Value = 100;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Orchestrate the process for one frame.

        /// Uses the first input as the frame number, the second input as the second
        /// number, and the third input as boolean for vidA (used in SearchImage).
        /// Finds the file address, then runs MatrixCleaning, SearchImage, SetFormation,
        /// and CentroidOutput, which scans each frame, sorts the pixels into sets, and
        /// stores those sets into the final output matrix.
        /// <param name="frameNumber">Frame number of the frame being used.</param>
        /// <param name="secondNumber">Second number of the frame being searched.</param>
        /// <param name="vidA">The bool for the type of frame naming scheme used.</param>
        ///////////////////////////////////////////////////////////////////////////////////
        public void FrameStep(int frameNumber, int secondNumber, bool vidA)
        {
            string fileAddress = FindImageName(frameNumber, vidA);
            MatrixCleaning();
            SearchImage(fileAddress, frameNumber, secondNumber);
            SetFormation();
            CentroidOutput(frameNumber + (secondNumber * frameRate));
            ManagePoints(frameNumber, secondNumber);
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Orchestrate the subprocess for one second.

        /// Uses the first input as the second number.
        /// If the second number is odd, uses the "A" naming scheme (vidA = true),
        /// otherwise uses the "B" naming scheme (vidA = false).
        /// Then runs BatFileGeneration. Next, tries to delete all image files to be used
        /// in current cycle, before running the bat file at "\Data\ffmpeg\Command.bat".
        /// If file in use, waits before trying again.
        /// This will pull all the frames within the second and save them according to the
        /// naming scheme from FindImageName.
        /// When it runs Command.bat, it does so without a window.
        /// Returns vidA.
        /// <param name="timeValue">Index for the time value for the final outputs.</param>
        ///////////////////////////////////////////////////////////////////////////////////
        public bool SecondStep(int timeNumber)
        {
            bool vidA = false;
            if ((timeNumber % 2) == 1)
            {
                vidA = true;
            }
            BatFileGeneration(timeNumber, vidA);
            for (int i = 1; i < frameRate + 1; i++)
            {
                bool fileClosed = true;
                string thisFileName = FindImageName(i, vidA);
                while (fileClosed)
                {
                    try
                    {
                        File.Delete(thisFileName);
                        fileClosed = false;
                    }
                    catch (IOException)
                    {
                        System.Threading.Thread.Sleep(10);
                        continue;
                    }
                }
            }
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.FileName = (folderPath + "ffmpeg\\Command.bat");
            using (Process exeProcess = Process.Start(startInfo))
            {
                exeProcess.WaitForExit();
            }
            return vidA;
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Scans the entire image for marker pixels.

        /// Opens the image file using the string input, and scans throughout the range
        /// specified by xResolution and yResolution. Gets the color of each pixel, and
        /// if it is the right color, via PixelColor, adds the pixel to the appropriate
        /// matrices using AddPointToSearchMesh.
        /// <param name="fileLocation">File location being searched.</param>
        ///////////////////////////////////////////////////////////////////////////////////
        public void InputBMPFull(string fileLocation)
        {
            try
            {
                using (Bitmap bmp = (Bitmap)Image.FromFile(fileLocation, true))
                {
                    for (int x = 0; x < xResolution; x++)
                    {
                        for (int y = 0; y < yResolution; y++)
                        {
                            Color pixelColor = bmp.GetPixel(x, y);
                            if (PixelCheck(pixelColor))
                            {
                                AddPointToSearchMesh(x, y);
                            }
                        }
                    }
                }
            }
            catch (IOException)
            {
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Scans the image based on previous marker points for marker pixels.

        /// Opens the image file using the first input (string) and forms a range to scan
        /// for each previous marker pixel. This is a square centered around the pixel with
        /// side-length equal to fastSearchRange + 1. This square is truncated if the range
        /// extends out of bounds, at the edge of the frame.
        /// The color for each pixel in each range is obtained and checked via PixelColor.
        /// If it is a marker color, adds to the appropriate matrices using
        /// AddPointToSearchMesh.
        /// <param name="fileLocation">File location being searched.</param>
        /// <param name="frameNumber">Frame number of the frame being used.</param>
        /// <param name="secondNumber">Second number of the frame being searched.</param>
        ///////////////////////////////////////////////////////////////////////////////////
        public void InputBMPFast(string fileLocation, int frameNumber, int secondNumber)
        {
            int previousFrame = (frameNumber - 1) + secondNumber * frameRate;
            try
            {
                using (Bitmap bmp = (Bitmap)Image.FromFile(fileLocation, true))
                {
                    if (!noPoints)
                    {
                        for (int i = 0; i < outputData[previousFrame].marker.Length; i++)
                        {
                            int xCenter = outputData[previousFrame].marker[i].xValue;
                            int yCenter = outputData[previousFrame].marker[i].yValue;
                            int xLeft = xCenter - fastSearchRange;
                            if (xLeft < 0)
                            {
                                xLeft = 0;
                            }
                            int xRight = xCenter + fastSearchRange;
                            if (xRight > xResolution)
                            {
                                xRight = xResolution;
                            }
                            int yLeft = yCenter - fastSearchRange;
                            if (yLeft < 0)
                            {
                                yLeft = 0;
                            }
                            int yRight = yCenter + fastSearchRange;
                            if (yRight > yResolution)
                            {
                                yRight = yResolution;
                            }
                            for (int x = xLeft; x < xRight; x++)
                            {
                                for (int y = yLeft; y < yRight; y++)
                                {
                                    Color pixelColor = bmp.GetPixel(x, y);
                                    if (PixelCheck(pixelColor))
                                    {
                                        AddPointToSearchMesh(x, y);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (IOException)
            {
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Inputs all the textbox inputs.

        /// Inputs the folderPath, videoFileName and settingsFileName directly from the
        /// appropriate textboxes, and tries to parse the huelow, hueHigh, brightnessLow,
        /// brightnessHigh, saturationLow, saturationHigh, redLow, redHigh, blueLow,
        /// blueHigh, greenLow, greenHigh, startTime, endTime, frameRate, lowerSetSize,
        /// upperSetSize, upperSetCount, xResolution, yResolution, leftBounding,
        /// topBounding, widthBounding, heightBounding, rLock, gLock, bLock, hLock, sLock,
        /// lLock, ifRGB, slopeCalibration, and interceptCalibration variables.
        ///////////////////////////////////////////////////////////////////////////////////
        public void TextBoxInputs()
        {
            folderPath = TextAddress.Text;
            videoFileName = TextVideoFileName.Text;
            settingsFileName = TextSettingsName.Text;
            float j;
            if (Single.TryParse((TextHueLow.Text), out j))
            {
                hueLow = (decimal)(j);
                if (hueLow < 0)
                {
                    hueLow = 0;
                }
                if (hueLow > 255)
                {
                    hueLow = 255;
                }
            }
            if (Single.TryParse((TextHueHigh.Text), out j))
            {
                hueHigh = (decimal)(j);
                if (hueHigh < 0)
                {
                    hueHigh = 0;
                }
                if (hueHigh > 255)
                {
                    hueHigh = 255;
                }
            }
            if (Single.TryParse((TextBrightnessLow.Text), out j))
            {
                brightnessLow = (decimal)(j);
                if (brightnessLow < 0)
                {
                    brightnessLow = 0;
                }
                if (brightnessLow > 255)
                {
                    brightnessLow = 255;
                }
            }
            if (Single.TryParse((TextBrightnessHigh.Text), out j))
            {
                brightnessHigh = (decimal)(j);
                if (brightnessHigh < 0)
                {
                    brightnessHigh = 0;
                }
                if (brightnessHigh > 255)
                {
                    brightnessHigh = 255;
                }
            }
            if (Single.TryParse((TextSaturationLow.Text), out j))
            {
                saturationLow = (decimal)(j);
                if (saturationLow < 0)
                {
                    saturationLow = 0;
                }
                if (saturationLow > 255)
                {
                    saturationLow = 255;
                }
            }
            if (Single.TryParse((TextSaturationHigh.Text), out j))
            {
                saturationHigh = (decimal)(j);
                if (saturationHigh < 0)
                {
                    saturationHigh = 0;
                }
                if (saturationHigh > 255)
                {
                    saturationHigh = 255;
                }
            }
            if (Single.TryParse((TextRedLow.Text), out j))
            {
                redLow = (decimal)(j);
                if (redLow < 0)
                {
                    redLow = 0;
                }
                if (redLow > 255)
                {
                    redLow = 255;
                }
            }
            if (Single.TryParse((TextRedHigh.Text), out j))
            {
                redHigh = (decimal)(j);
                if (redHigh < 0)
                {
                    redHigh = 0;
                }
                if (redHigh > 255)
                {
                    redHigh = 255;
                }
            }
            if (Single.TryParse((TextBlueLow.Text), out j))
            {
                blueLow = (decimal)(j);
                if (blueLow < 0)
                {
                    blueLow = 0;
                }
                if (blueLow > 255)
                {
                    blueLow = 255;
                }
            }
            if (Single.TryParse((TextBlueHigh.Text), out j))
            {
                blueHigh = (decimal)(j);
                if (blueHigh < 0)
                {
                    blueHigh = 0;
                }
                if (blueHigh > 255)
                {
                    blueHigh = 255;
                }
            }
            if (Single.TryParse((TextGreenLow.Text), out j))
            {
                greenLow = (decimal)(j);
                if (greenLow < 0)
                {
                    greenLow = 0;
                }
                if (greenLow > 255)
                {
                    greenLow = 255;
                }
            }
            if (Single.TryParse((TextGreenHigh.Text), out j))
            {
                greenHigh = (decimal)(j);
                if (greenHigh < 0)
                {
                    greenHigh = 0;
                }
                if (greenHigh > 255)
                {
                    greenHigh = 255;
                }
            }
            if (Single.TryParse((TextEndTime.Text), out j))
            {
                endTime = (decimal)(j);
                if (endTime < 0)
                {
                    endTime = 1;
                }
            }
            if (Single.TryParse((TextStartTime.Text), out j))
            {
                startTime = (decimal)(j);
                if (startTime < 0)
                {
                    startTime = 0;
                }
                if (endTime < startTime)
                {
                    startTime = endTime - 1;
                }
            }
            if (Single.TryParse((TextFrameRate.Text), out j))
            {
                frameRate = (int)j;
                if (frameRate < 0)
                {
                    frameRate = 30;
                }
                if (frameRate > 999999999)
                {
                    frameRate = 999999999;
                }
            }
            if (Single.TryParse((TextMinSize.Text), out j))
            {
                lowerSetSize = (int)j;
                if (lowerSetSize < 1)
                {
                    lowerSetSize = 1;
                }
                //if (lowerSetSize > maxSetSize - 1)
                //{
                //    lowerSetSize = maxSetSize - 1;
                //}
                //Find max lower set size before crash [[[]]]
            }
            if (Single.TryParse((TextMaxSize.Text), out j))
            {
                upperSetSize = (int)j;
                if (upperSetSize < 2)
                {
                    upperSetSize = 2;
                }
                //if (upperSetSize > maxSetSize)
                //{
                //    upperSetSize = maxSetSize;
                //}
                //Find max upper set size before crash [[[]]]
            }
            if (Single.TryParse((TextMaxSets.Text), out j))
            {
                upperSetCount = (int)j;
                if (upperSetCount < 1)
                {
                    upperSetCount = 1;
                }
                if (upperSetCount > xResolution*yResolution)
                {
                    upperSetCount = xResolution * yResolution;
                }
            }
            if (Single.TryParse((TextXResolution.Text), out j))
            {
                xResolution = (int)j;
                if (xResolution < 1)
                {
                    xResolution = 1;
                }
            }
            if (Single.TryParse((TextYResolution.Text), out j))
            {
                yResolution = (int)j;
                if (yResolution < 1)
                {
                    yResolution = 1;
                }
            }
            if (Single.TryParse((TextRange.Text), out j))
            {
                range = (double)j;
                if (range < 0)
                {
                    range = 0;
                }
            }
            if (RGBButton.Checked)
            {
                ifRGB = true;
            }
            else
            {
                ifRGB = false;
            }
            if (Single.TryParse((TextLeftPosition.Text), out j))
            {
                leftBounding = (int)j;
                if (leftBounding < 0)
                {
                    leftBounding = 0;
                }
                if (leftBounding > xResolution - 1)
                {
                    leftBounding = xResolution - 1;
                }
            }
            if (Single.TryParse((TextTopPosition.Text), out j))
            {
                topBounding = (int)j;
                if (topBounding < 0)
                {
                    topBounding = 0;
                }
                if (topBounding > yResolution - 1)
                {
                    topBounding = yResolution - 1;
                }
            }
            if (Single.TryParse((TextWidth.Text), out j))
            {
                widthBounding = (int)j;
                if (widthBounding < 1)
                {
                    widthBounding = 1;
                }
                if (widthBounding > xResolution - 1)
                {
                    widthBounding = xResolution - 1;
                }
            }
            if (Single.TryParse((TextHeight.Text), out j))
            {
                heightBounding = (int)j;
                if (heightBounding < 1)
                {
                    heightBounding = 1;
                }
                if (heightBounding > yResolution - 1)
                {
                    heightBounding = yResolution - 1;
                }
            }
            if (Single.TryParse((TextIntercept.Text), out j))
            {
                interceptCalibration = (double)j;
            }
            if (Single.TryParse((TextSlope.Text), out j))
            {
                slopeCalibration = (double)j;
                if (slopeCalibration < 0)
                {
                    slopeCalibration = -1 * slopeCalibration;
                }
            }
            if (CheckBoxR.Checked)
            {
                rLock = true;
            }
            else
            {
                rLock = false;
            }
            if (CheckBoxG.Checked)
            {
                gLock = true;
            }
            else
            {
                gLock = false;
            }
            if (CheckBoxB.Checked)
            {
                bLock = true;
            }
            else
            {
                bLock = false;
            }
            if (CheckBoxH.Checked)
            {
                hLock = true;
            }
            else
            {
                hLock = false;
            }
            if (CheckBoxS.Checked)
            {
                sLock = true;
            }
            else
            {
                sLock = false;
            }
            if (CheckBoxL.Checked)
            {
                lLock = true;
            }
            else
            {
                lLock = false;
            }
            if (Single.TryParse((TextSearchRange.Text), out j))
            {
                fastSearchRange = (int)j;
                if (fastSearchRange < 1)
                {
                    fastSearchRange = 1;
                }
                if (fastSearchRange > xResolution - 1 || fastSearchRange > yResolution - 1)
                {
                    fastSearchRange = Math.Min(xResolution - 1, yResolution - 1);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Outputs all current variables to the textboxes.

        /// Outputs folderPath, videoFileName, settingsFileName, huelow, hueHigh,
        /// brightnessLow, brightnessHigh, saturationLow, saturationHigh, redLow, redHigh,
        /// blueLow, blueHigh, greenLow, greenHigh, startTime, endTime, frameRate,
        /// lowerSetSize, upperSetSize, upperSetCount, xResolution, yResolution,
        /// fastSearchRange, ifRGB, leftBounding, topBounding, widthBounding,
        /// heightBounding, rLock, gLock, bLock, hLock, sLock, lLock, slopeCalibration, and
        /// interceptCalibration to the textboxes or check boxes as appropriate.
        ///////////////////////////////////////////////////////////////////////////////////
        public void TextBoxOutputs()
        {
            TextAddress.Text = folderPath;
            TextVideoFileName.Text = videoFileName;
            TextSettingsName.Text = settingsFileName;

            TextHueLow.Text = hueLow.ToString();
            TextHueHigh.Text = hueHigh.ToString();
            TextSaturationLow.Text = saturationLow.ToString();
            TextSaturationHigh.Text = saturationHigh.ToString();
            TextBrightnessLow.Text = brightnessLow.ToString();
            TextBrightnessHigh.Text = brightnessHigh.ToString();

            TextRedLow.Text = redLow.ToString();
            TextRedHigh.Text = redHigh.ToString();
            TextGreenLow.Text = greenLow.ToString();
            TextGreenHigh.Text = greenHigh.ToString();
            TextBlueLow.Text = blueLow.ToString();
            TextBlueHigh.Text = blueHigh.ToString();

            TextStartTime.Text = startTime.ToString();
            TextEndTime.Text = endTime.ToString();

            TextMinSize.Text = lowerSetSize.ToString();
            TextMaxSize.Text = upperSetSize.ToString();
            TextMaxSets.Text = upperSetCount.ToString();
            TextSearchRange.Text = fastSearchRange.ToString();
            TextRange.Text = range.ToString();

            TextLeftPosition.Text = leftBounding.ToString();
            TextTopPosition.Text = topBounding.ToString();
            TextWidth.Text = widthBounding.ToString();
            TextHeight.Text = heightBounding.ToString();

            TextXResolution.Text = xResolution.ToString();
            TextYResolution.Text = yResolution.ToString();
            TextFrameRate.Text = frameRate.ToString();
            TextSlope.Text = slopeCalibration.ToString();
            TextIntercept.Text = interceptCalibration.ToString();

            CheckBoxR.Checked = rLock;
            CheckBoxG.Checked = gLock;
            CheckBoxB.Checked = bLock;
            CheckBoxH.Checked = hLock;
            CheckBoxS.Checked = sLock;
            CheckBoxL.Checked = lLock;

            if (ifRGB)
            {
                RGBButton.Checked = true;
            }
            else
            {
                RGBButton.Checked = false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Output the stored data to a csv file.

        /// Finds a file name using findCSVName, outputs a header line, and one line per
        /// marker per frame including the time, calculated from the startTime, endTime,
        /// and frameRate, the x position, y position, slopeCalibration,
        /// interceptCalibration, and the size of the marker.
        ///////////////////////////////////////////////////////////////////////////////////
        public void Outputcsv()
        {
            string address = FindCSVName();
            int frameCountTotal = (int)((endTime - startTime) * frameRate);
            using (System.IO.StreamWriter file =
             new System.IO.StreamWriter(address, false))
            {
                double conversionFactor;
                conversionFactor = (slopeCalibration * range + interceptCalibration);
                string toWrite = "";
                int MaxPoints = 0;
                for (int i = 0; i < frameCountTotal; i++)
                {
                    if (outputData[i] != null)
                    {
                        if (MaxPoints < outputData[i].marker.Length)
                        {
                            MaxPoints = outputData[i].marker.Length;
                        }
                    }
                }
                for (int i = 0; i < MaxPoints; i++)
                {
                    toWrite += "time (s),x position (in),y position (in),marker size (pixels),";
                }
                file.WriteLine(toWrite);
                for (int i = 1; i < frameCountTotal; i++)
                {
                    toWrite = "";
                    if (outputData[i] != null)
                    {
                        for (int j = 0; j < outputData[i].marker.Length; j++)
                        {
                            if (outputData[i].marker[j].size > 0)
                            {
                                decimal tcoord = startTime + (i / (decimal)frameRate);
                                decimal xcoord = outputData[i].marker[j].xValue * (decimal)conversionFactor;
                                decimal ycoord = (yResolution - outputData[i].marker[j].yValue) * (decimal)conversionFactor;
                                toWrite += tcoord + ",";
                                toWrite += xcoord + ",";
                                toWrite += ycoord + ",";
                                toWrite += outputData[i].marker[j].size + ",";
                            }
                            else
                            {
                                toWrite += ",,,,";
                            }
                        }
                        file.WriteLine(toWrite);
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Writes a batch file to pull a set of frames from the video input.

        /// The first input (integer) sets the start time for pulling 1 second worth of
        /// frames from the video input. The second input (bool) sets whether to use 
        /// naming scheme A (frameA-) for vidA = true, or B (frameB-) for vidA = false.
        /// Outputs a .bat file that will open ffmpeg, and call the appropriate command.
        /// <param name="time">Time value for current frame.</param>
        /// <param name="vidA">The bool for the type of frame naming scheme used.</param>
        ///////////////////////////////////////////////////////////////////////////////////
        public void BatFileGeneration(int time, bool vidA)
        {
            using (System.IO.StreamWriter file =
             new System.IO.StreamWriter(folderPath + "ffmpeg\\Command.bat", false))
            {
                file.WriteLine("CD " + folderPath + "ffmpeg\\bin");
                file.WriteLine("PROMPT $P$_$G");
                file.WriteLine("SET PATH=%CD%;%PATH%");
                decimal framed = (decimal)startTime + time;
                string frames = framed.ToString();
                decimal framed2 = framed + 1;
                string frames2 = framed2.ToString();
                string frameRate2 = frameRate.ToString();
                string iString = " -i " + folderPath + videoFileName;
                string rString = " -ss " + frames + " -to " + frames2;
                string oString;
                if (vidA)
                {
                    oString = " " + folderPath + "frameA-%%09d.bmp";
                    file.WriteLine("ffmpeg" + iString + " -f image2" + rString + oString);
                }
                else
                {
                    oString = " " + folderPath + "frameB-%%09d.bmp";
                    file.WriteLine("ffmpeg" + iString + " -f image2" + rString + oString);
                }
                file.WriteLine("exit");
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Imports settings from the input file.

        /// Reads the file at the specified input string, using four intuitive categories:
        /// HSB includes variables: hueLow, hueHigh, saturationLow, saturationHigh,
        /// brightnessLow and brightnessHigh.
        /// RGB includes variables: redLow, redHigh, greenLow, greenHigh, blueLow and
        /// blueHigh.
        /// Size includes variables: lowerSetSize, upperSetSize, upperSetCount, and
        /// fastSearchRange.
        /// Time includes variables: startTime and endTime.
        /// Camera includes variables: xResolution, yResolution, frameRate,
        /// slopeCalibration, and interceptCalibration.
        /// Misc includes variables: ifRGB and range.
        /// The attribute name for each variable is identical to the variable name.
        /// <param name="fileName">The location of the settings file being imported.</param>
        ///////////////////////////////////////////////////////////////////////////////////
        public void ImportSettings(string fileName)
        {
            XmlReader reader = XmlReader.Create(fileName);
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "HSB")
                {
                    if (reader.HasAttributes)
                    {
                        hueLow = decimal.Parse(reader.GetAttribute("hueLow"));
                        hueHigh = decimal.Parse(reader.GetAttribute("hueHigh"));
                        saturationLow = decimal.Parse(reader.GetAttribute("saturationLow"));
                        saturationHigh = decimal.Parse(reader.GetAttribute("saturationHigh"));
                        brightnessLow = decimal.Parse(reader.GetAttribute("brightnessLow"));
                        brightnessHigh = decimal.Parse(reader.GetAttribute("brightnessHigh"));
                    }
                }
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "RGB")
                {
                    if (reader.HasAttributes)
                    {
                        redLow = decimal.Parse(reader.GetAttribute("redLow"));
                        redHigh = decimal.Parse(reader.GetAttribute("redHigh"));
                        greenLow = decimal.Parse(reader.GetAttribute("greenLow"));
                        greenHigh = decimal.Parse(reader.GetAttribute("greenHigh"));
                        blueLow = decimal.Parse(reader.GetAttribute("blueLow"));
                        blueHigh = decimal.Parse(reader.GetAttribute("blueHigh"));
                    }
                }
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "Size")
                {
                    if (reader.HasAttributes)
                    {
                        lowerSetSize = int.Parse(reader.GetAttribute("lowerSetSize"));
                        upperSetSize = int.Parse(reader.GetAttribute("upperSetSize"));
                        upperSetCount = int.Parse(reader.GetAttribute("upperSetCount"));
                        fastSearchRange = int.Parse(reader.GetAttribute("fastSearchRange"));
                    }
                }
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "Time")
                {
                    if (reader.HasAttributes)
                    {
                        startTime = decimal.Parse(reader.GetAttribute("startTime"));
                        endTime = decimal.Parse(reader.GetAttribute("endTime"));
                    }
                }
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "Camera")
                {
                    if (reader.HasAttributes)
                    {
                        xResolution = int.Parse(reader.GetAttribute("xResolution"));
                        yResolution = int.Parse(reader.GetAttribute("yResolution"));
                        frameRate = int.Parse(reader.GetAttribute("frameRate"));
                        slopeCalibration = double.Parse(reader.GetAttribute("slopeCalibration"));
                        interceptCalibration = double.Parse(reader.GetAttribute("interceptCalibration"));
                    }
                }
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "Misc")
                {
                    if (reader.HasAttributes)
                    {
                        ifRGB = bool.Parse(reader.GetAttribute("ifRGB"));
                        range = double.Parse(reader.GetAttribute("range"));
                    }
                }
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "Calibration")
                {
                    if (reader.HasAttributes)
                    {
                        leftBounding = int.Parse(reader.GetAttribute("leftBounding"));
                        topBounding = int.Parse(reader.GetAttribute("topBounding"));
                        widthBounding = int.Parse(reader.GetAttribute("widthBounding"));
                        heightBounding = int.Parse(reader.GetAttribute("heightBounding"));
                        rLock = bool.Parse(reader.GetAttribute("rLock"));
                        gLock = bool.Parse(reader.GetAttribute("gLock"));
                        bLock = bool.Parse(reader.GetAttribute("bLock"));
                        hLock = bool.Parse(reader.GetAttribute("hLock"));
                        sLock = bool.Parse(reader.GetAttribute("sLock"));
                        lLock = bool.Parse(reader.GetAttribute("lLock"));
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Exports settings from the input file.

        /// Exports the variables at the file at the specified input string, using four
        /// intuitive categories:
        /// HSB includes variables: hueLow, hueHigh, saturationLow, saturationHigh,
        /// brightnessLow and brightnessHigh.
        /// RGB includes variables: redLow, redHigh, greenLow, greenHigh, blueLow and
        /// blueHigh.
        /// Size includes variables: lowerSetSize, upperSetSize, upperSetCount, and
        /// fastSearchRange.
        /// Time includes variables: startTime and endTime.
        /// Misc includes variables: ifRGB and range.
        /// Camera includes variables: xResolution, yResolution, frameRate,
        /// slopeCalibration, and interceptCalibration.
        /// The attribute name for each variable is identical to the variable name.
        /// <param name="fileName">The location of the settings file being exported.</param>
        ///////////////////////////////////////////////////////////////////////////////////
        public void ExportSettings(string fileName)
        {
            File.Delete(fileName);
            using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(@fileName, false))
            {
                string toWrite = "";
                toWrite += "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>";
                file.WriteLine(toWrite);
                toWrite = "";
                toWrite += "<root>";
                file.WriteLine(toWrite);
                toWrite = "";
                toWrite += "<HSB ";
                toWrite += "hueLow = \"" + hueLow + "\" ";
                toWrite += "hueHigh = \"" + hueHigh + "\" ";
                toWrite += "saturationLow = \"" + saturationLow + "\" ";
                toWrite += "saturationHigh = \"" + saturationHigh + "\" ";
                toWrite += "brightnessLow = \"" + brightnessLow + "\" ";
                toWrite += "brightnessHigh = \"" + brightnessHigh + "\" ";
                toWrite += "/>";
                file.WriteLine(toWrite);
                toWrite = "";
                toWrite += "<RGB ";
                toWrite += "redLow = \"" + redLow + "\" ";
                toWrite += "redHigh = \"" + redHigh + "\" ";
                toWrite += "greenLow = \"" + greenLow + "\" ";
                toWrite += "greenHigh = \"" + greenHigh + "\" ";
                toWrite += "blueLow = \"" + blueLow + "\" ";
                toWrite += "blueHigh = \"" + blueHigh + "\" ";
                toWrite += "/>";
                file.WriteLine(toWrite);
                toWrite = "";
                toWrite += "<Size ";
                toWrite += "lowerSetSize = \"" + lowerSetSize + "\" ";
                toWrite += "upperSetSize = \"" + upperSetSize + "\" ";
                toWrite += "upperSetCount = \"" + upperSetCount + "\" ";
                toWrite += "fastSearchRange = \"" + fastSearchRange + "\" ";
                toWrite += "/>";
                file.WriteLine(toWrite);
                toWrite = "";
                toWrite += "<Time ";
                toWrite += "startTime = \"" + startTime + "\" ";
                toWrite += "endTime = \"" + endTime + "\" ";
                toWrite += "/>";
                file.WriteLine(toWrite);
                toWrite = "";
                toWrite += "<Camera ";
                toWrite += "xResolution = \"" + xResolution + "\" ";
                toWrite += "yResolution = \"" + yResolution + "\" ";
                toWrite += "frameRate = \"" + frameRate + "\" ";
                toWrite += "slopeCalibration = \"" + slopeCalibration + "\" ";
                toWrite += "interceptCalibration = \"" + interceptCalibration + "\" ";
                toWrite += "/>";
                file.WriteLine(toWrite);
                toWrite = "";
                toWrite += "<Misc ";
                toWrite += "ifRGB = \"" + ifRGB + "\" ";
                toWrite += "range = \"" + range + "\" ";
                toWrite += "/>";
                file.WriteLine(toWrite);
                toWrite = "";
                toWrite += "<Calibration ";
                toWrite += "leftBounding = \"" + leftBounding + "\" ";
                toWrite += "topBounding = \"" + topBounding + "\" ";
                toWrite += "widthBounding = \"" + widthBounding + "\" ";
                toWrite += "heightBounding = \"" + heightBounding + "\" ";
                toWrite += "rLock = \"" + rLock + "\" ";
                toWrite += "gLock = \"" + gLock + "\" ";
                toWrite += "bLock = \"" + bLock + "\" ";
                toWrite += "hLock = \"" + hLock + "\" ";
                toWrite += "sLock = \"" + sLock + "\" ";
                toWrite += "lLock = \"" + lLock + "\" ";
                toWrite += "/>";
                file.WriteLine(toWrite);
                file.WriteLine("</root>");
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Runs CombinePoints and SortPoints.
        /// <param name="frameNumber">Frame number of the frame being used.</param>
        /// <param name="secondNumber">Second number of the frame being searched.</param>
        ///////////////////////////////////////////////////////////////////////////////////
        public void ManagePoints(int frameNumber, int secondNumber)
        {
            int currentFrame = frameNumber + secondNumber * frameRate;
            CombinePoints(currentFrame);
            if (currentFrame > 1)
            {
                SortPoints(currentFrame);
            }
        }
        ///////////////////////////////////////////////////////////////////////////////////
        /// Combines points that are near each-other.

        /// Looks at all pairs of points, and combines any that are near to each-other,
        /// setting the other's size to 0.
        /// <param name="frame">Frame being analyzed.</param>
        ///////////////////////////////////////////////////////////////////////////////////
        public void CombinePoints(int frame)
        {
            try
            {
            int numPoints = outputData[frame].marker.Length;
            for (int i = 0; i < numPoints; i++)
            {
                for (int j = 0; j < numPoints; j++)
                {
                    if (i != j && IfNearSet(frame, i, frame, j))
                    {
                        int xOne = outputData[frame].marker[i].xValue;
                        int yOne = outputData[frame].marker[i].yValue;
                        int sizeOne = outputData[frame].marker[i].size;
                        int xTwo = outputData[frame].marker[j].xValue;
                        int yTwo = outputData[frame].marker[j].yValue;
                        int sizeTwo = outputData[frame].marker[j].size;
                        int sizeNew = sizeOne + sizeTwo;
                        int xNew = (int)Math.Round((decimal)(xOne * sizeOne + xTwo * sizeTwo) / sizeNew);
                        int yNew = (int)Math.Round((decimal)(yOne * sizeOne + yTwo * sizeTwo) / sizeNew);
                        outputData[frame].marker[i].xValue = xNew;
                        outputData[frame].marker[i].yValue = yNew;
                        outputData[frame].marker[i].size = sizeNew;
                        outputData[frame].marker[j].size = 0;
                    }
                }
            }
            }
            catch (NullReferenceException)
            {
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Sorts the points in outputData at a specified frame.

        /// Stores the data from outputData into temporary storage, checks every pair of
        /// points across this frame and the previous frame to see if they are close to
        /// each-other. If so, stores the point at that index. If no point is found, stores
        /// 0's in all values. Once all previous points have been checked, adds the
        /// remaining non-empty points to the back of the outputData for the new frame.
        /// <param name="frame">The frame for the set of points</param>
        ///////////////////////////////////////////////////////////////////////////////////
        public void SortPoints(int frame)
        {
            try
            {
            int numPointsOld = outputData[frame - 1].marker.Length;
            int numPointsNow = outputData[frame].marker.Length;
            markerset tempOutput = new markerset(numPointsNow + numPointsOld);
            for (int i = 0; i < numPointsNow + numPointsOld; i++)
            {
                tempOutput.marker[i] = new point();
                tempOutput.marker[i].xValue = 0;
                tempOutput.marker[i].yValue = 0;
                tempOutput.marker[i].size = 0;
            }
            int index = 0;
            for (int i = 0; i < numPointsOld; i++)
            {
                for (int j = 0; j < numPointsNow; j++)
                {
                    if (IfNearSet(frame - 1, i, frame, j))
                    {
                        tempOutput.marker[index].xValue = outputData[frame].marker[j].xValue;
                        tempOutput.marker[index].yValue = outputData[frame].marker[j].yValue;
                        tempOutput.marker[index].size = outputData[frame].marker[j].size;
                        outputData[frame].marker[j].size = 0;
                        break;
                    }
                }
                index++;
            }
            for (int i = 0; i < numPointsNow; i++)
            {
                if (outputData[frame].marker[i].size != 0)
                {
                    tempOutput.marker[index].xValue = outputData[frame].marker[i].xValue;
                    tempOutput.marker[index].yValue = outputData[frame].marker[i].yValue;
                    tempOutput.marker[index].size = outputData[frame].marker[i].size;
                    outputData[frame].marker[i].size = 0;
                    index++;
                }
            }
            outputData[frame] = new markerset(index);
            for (int i = 0; i < index; i++)
            {
                outputData[frame].marker[i] = new point();
                outputData[frame].marker[i].xValue = tempOutput.marker[i].xValue;
                outputData[frame].marker[i].yValue = tempOutput.marker[i].yValue;
                outputData[frame].marker[i].size = tempOutput.marker[i].size;
            }

            }
            catch (NullReferenceException)
            {
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Determines whether two sets of points are near each other.

        /// First determines if either point is of zero size, and if so, returns false.
        /// Then calculates the distance between the two points. If it is less than the
        /// fastSearchRange, returns true, otherwise false.
        /// <param name="frameA">The frame for the first point</param>
        /// <param name="indexA">The index for the first point</param>
        /// <param name="frameB">The frame for the second point</param>
        /// <param name="indexB">The index for the second point</param>
        ///////////////////////////////////////////////////////////////////////////////////
        public bool IfNearSet(int frameA, int indexA, int frameB, int indexB)
        {
            int sizeA = outputData[frameA].marker[indexA].size;
            int sizeB = outputData[frameB].marker[indexB].size;
            if (sizeA == 0 || sizeB == 0)
            {
                return false;
            }
            int xPosA = outputData[frameA].marker[indexA].xValue;
            int yPosA = outputData[frameA].marker[indexA].yValue;
            int xPosB = outputData[frameB].marker[indexB].xValue;
            int yPosB = outputData[frameB].marker[indexB].yValue;
            int posXDif = xPosB - xPosA;
            int posYDif = yPosB - yPosA;
            int posDif = posXDif * posXDif + posYDif * posYDif;
            return (posDif < (fastSearchRange * fastSearchRange));
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Helps calibration by showing RGB and HSB distributions.

        /// Calls TextBoxInputs, calls Input
        ///////////////////////////////////////////////////////////////////////////////////
        public void MainCalibration()
        {
            TextBoxInputs();
            CalibrationAllocation();
            bool vidA = SecondStep(0);
            string fileAddress = FindImageName(1, vidA);
            InputBMPCalibration(fileAddress);
            MxNCalibration();
            OutputRegion();
            OutputStatistics();
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Allocates matrices for calibration.

        /// Creates mxnMatrix, redMatrix, greenMatrix, blueMatrix, hueMatrix,
        /// saturationMatrix, and brightnessMatrix.
        ///////////////////////////////////////////////////////////////////////////////////
        public void CalibrationAllocation()
        {
            mxnMatrix = new bool[xResolution, yResolution];
            redMatrix = new int[xResolution, yResolution];
            greenMatrix = new int[xResolution, yResolution];
            blueMatrix = new int[xResolution, yResolution];
            hueMatrix = new int[xResolution, yResolution];
            saturationMatrix = new int[xResolution, yResolution];
            brightnessMatrix = new int[xResolution, yResolution];
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Records colors from an image into matrices for RGB and HSB.

        /// Stores the red, green, blue, hue, saturation, and brightness values of every
        /// pixel from an image into the respective redMatrix, greenMatrix, blueMatrix,
        /// hueMatrix, saturationMatrix, and brightnessMatrix. Auto-scales all values from
        /// 0 to 255 the appropriate internal bounds. Finally, sets mxnMatrix to true for
        /// all pixels.
        /// <param name="fileLocation">The location of the file being exported.</param>
        ///////////////////////////////////////////////////////////////////////////////////
        public void InputBMPCalibration(string fileLocation)
        {
            using (Bitmap bmp = (Bitmap)Image.FromFile(fileLocation, true))
            {
                for (int x = 0; x < xResolution; x++)
                {
                    for (int y = 0; y < yResolution; y++)
                    {
                        Color pixelColor = bmp.GetPixel(x, y);
                        redMatrix[x, y] = pixelColor.R;
                        greenMatrix[x, y] = pixelColor.G;
                        blueMatrix[x, y] = pixelColor.B;
                        hueMatrix[x, y] = (int)Math.Round(255 * (decimal)pixelColor.GetHue() / 360);
                        saturationMatrix[x, y] = (int)Math.Round(255 * (decimal)pixelColor.GetSaturation());
                        brightnessMatrix[x, y] = (int)Math.Round(255 * (decimal)pixelColor.GetBrightness());
                        mxnMatrix[x, y] = true;
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Filters the mxnMatrix of all pixels that do not match Lock criteria.

        /// For each rLock, gLock, bLock, hLock, sLock, lLock, if true, filters pixels
        /// according to the bounding critera: ie, for redMatrix lower than redLow OR
        /// higher than redHigh set the respective mxnMatrix to false.
        /// Uses redMatrix, greenMatrix, blueMatrix, hueMatrix, saturationMatrix, and
        /// brightnessMatrix for pixel values to be filtered, and redLow, redHigh,
        /// greenLow, greenHigh, blueLow, blueHigh, hueLow, hueHigh, saturationLow,
        /// saturationHigh, brightnessLow, and brightnessHigh for filter settings.
        ///////////////////////////////////////////////////////////////////////////////////
        public void MxNCalibration()
        {
            if (rLock)
            {
                for (int x = 0; x < xResolution; x++)
                {
                    for (int y = 0; y < yResolution; y++)
                    {
                        if (mxnMatrix[x, y])
                        {
                            if (redMatrix[x, y] > redHigh || redMatrix[x, y] < redLow)
                            {
                                mxnMatrix[x, y] = false;
                            }
                        }
                    }
                }
            }
            if (gLock)
            {
                for (int x = 0; x < xResolution; x++)
                {
                    for (int y = 0; y < yResolution; y++)
                    {
                        if (mxnMatrix[x, y])
                        {
                            if (greenMatrix[x, y] > greenHigh || greenMatrix[x, y] < greenLow)
                            {
                                mxnMatrix[x, y] = false;
                            }
                        }
                    }
                }
            }
            if (bLock)
            {
                for (int x = 0; x < xResolution; x++)
                {
                    for (int y = 0; y < yResolution; y++)
                    {
                        if (mxnMatrix[x, y])
                        {
                            if (blueMatrix[x, y] > blueHigh || blueMatrix[x, y] < blueLow)
                            {
                                mxnMatrix[x, y] = false;
                            }
                        }
                    }
                }
            }
            if (hLock)
            {
                for (int x = 0; x < xResolution; x++)
                {
                    for (int y = 0; y < yResolution; y++)
                    {
                        if (mxnMatrix[x, y])
                        {
                            if (hueMatrix[x, y] > hueHigh || hueMatrix[x, y] < hueLow)
                            {
                                mxnMatrix[x, y] = false;
                            }
                        }
                    }
                }
            }
            if (sLock)
            {
                for (int x = 0; x < xResolution; x++)
                {
                    for (int y = 0; y < yResolution; y++)
                    {
                        if (mxnMatrix[x, y])
                        {
                            if (saturationMatrix[x, y] > saturationHigh || saturationMatrix[x, y] < saturationLow)
                            {
                                mxnMatrix[x, y] = false;
                            }
                        }
                    }
                }
            }
            if (lLock)
            {
                for (int x = 0; x < xResolution; x++)
                {
                    for (int y = 0; y < yResolution; y++)
                    {
                        if (mxnMatrix[x, y])
                        {
                            if (brightnessMatrix[x, y] > brightnessHigh || brightnessMatrix[x, y] < brightnessLow)
                            {
                                mxnMatrix[x, y] = false;
                            }
                        }
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Outputs filtered color data for selected region to separate csv files.

        /// Uses leftBounding, topBounding, widthBounding, and heightBounding bounds
        /// modified to prevent exceeding the borders of the image to output the RGB or HSB
        /// color data to csv files title "calibration-red.csv" or "calibration-blue.csv"
        /// etc.
        ///////////////////////////////////////////////////////////////////////////////////
        public void OutputRegion()
        {
            if (leftBounding > xResolution - 1)
            {
                leftBounding = xResolution - 1;
            }
            if (topBounding > yResolution - 1)
            {
                topBounding = yResolution - 1;
            }
            if (leftBounding + widthBounding > xResolution)
            {
                widthBounding = xResolution - leftBounding;
            }
            if (topBounding + heightBounding > yResolution)
            {
                heightBounding = yResolution - topBounding;
            }
            string address = folderPath + "calibration-red.csv";
            if (!rLock)
            {
                try
                {
                    using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(address, false))
                    {
                        for (int y = topBounding; y < topBounding + heightBounding; y++)
                        {
                            string toWrite = "";
                            for (int x = leftBounding; x < leftBounding + widthBounding; x++)
                            {
                                toWrite += redMatrix[x, y].ToString() + ",";
                            }
                            file.WriteLine(toWrite);
                        }
                    }
                }
                catch (IOException)
                {
                }
            }
            address = folderPath + "calibration-green.csv";
            if (!gLock)
            {
                try
                {
                    using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(address, false))
                    {
                        for (int y = topBounding; y < topBounding + heightBounding; y++)
                        {
                            string toWrite = "";
                            for (int x = leftBounding; x < leftBounding + widthBounding; x++)
                            {
                                toWrite += greenMatrix[x, y].ToString() + ",";
                            }
                            file.WriteLine(toWrite);
                        }
                    }
                }
                catch (IOException)
                {
                }
            }
            address = folderPath + "calibration-blue.csv";
            if (!bLock)
            {
                try
                {
                    using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(address, false))
                    {
                        for (int y = topBounding; y < topBounding + heightBounding; y++)
                        {
                            string toWrite = "";
                            for (int x = leftBounding; x < leftBounding + widthBounding; x++)
                            {
                                toWrite += blueMatrix[x, y].ToString() + ",";
                            }
                            file.WriteLine(toWrite);
                        }
                    }
                }
                catch (IOException)
                {
                }
            }
            address = folderPath + "calibration-hue.csv";
            if (!hLock)
            {
                try
                {
                    using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(address, false))
                    {
                        for (int y = topBounding; y < topBounding + heightBounding; y++)
                        {
                            string toWrite = "";
                            for (int x = leftBounding; x < leftBounding + widthBounding; x++)
                            {
                                toWrite += hueMatrix[x, y].ToString() + ",";
                            }
                            file.WriteLine(toWrite);
                        }
                    }
                }
                catch (IOException)
                {
                }
            }
            address = folderPath + "calibration-saturation.csv";
            if (!sLock)
            {
                try
                {
                    using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(address, false))
                    {
                        for (int y = topBounding; y < topBounding + heightBounding; y++)
                        {
                            string toWrite = "";
                            for (int x = leftBounding; x < leftBounding + widthBounding; x++)
                            {
                                toWrite += saturationMatrix[x, y].ToString() + ",";
                            }
                            file.WriteLine(toWrite);
                        }
                    }
                }
                catch (IOException)
                {
                }
            }
            address = folderPath + "calibration-brightness.csv";
            if (!lLock)
            {
                try
                {
                    using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(address, false))
                    {
                        for (int y = topBounding; y < topBounding + heightBounding; y++)
                        {
                            string toWrite = "";
                            for (int x = leftBounding; x < leftBounding + widthBounding; x++)
                            {
                                toWrite += brightnessMatrix[x, y].ToString() + ",";
                            }
                            file.WriteLine(toWrite);
                        }
                    }
                }
                catch (IOException)
                {
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// Outputs statistics about RGB and HSB composition to calbration-statistics.csv

        /// Outputs columns that show the number of points in the total and selected
        /// region that have valid points of various values of a specific color type,
        /// excluding all colored types locked in using rLock, gLock, bLock, hLock, sLock,
        /// and lLock.
        ///////////////////////////////////////////////////////////////////////////////////
        public void OutputStatistics()
        {
            try
            {
            string address = folderPath + "calibration-statistics.csv";
            using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(address, false))
            {
                string toWrite = "Value, ";
                if (!rLock)
                {
                    toWrite += "Total Valid Red Points, Selected Valid Red Points, ";
                }
                if (!gLock)
                {
                    toWrite += "Total Valid Green Points, Selected Valid Green Points, ";
                }
                if (!bLock)
                {
                    toWrite += "Total Valid Blue Points, Selected Valid Blue Points, ";
                }
                if (!hLock)
                {
                    toWrite += "Total Valid Hue Points, Selected Valid Hue Points, ";
                }
                if (!sLock)
                {
                    toWrite += "Total Valid Saturation Points, Selected Valid Saturation Points, ";
                }
                if (!lLock)
                {
                    toWrite += "Total Valid Brightness Points, Selected Valid Brightness Points, ";
                }
                toWrite += "Total Valid Points, Selected Valid Points";
                file.WriteLine(toWrite);
                for (int i = 0; i < 256; i++)
                {
                    toWrite = i.ToString() + ", ";
                    int index;
                    if (!rLock)
                    {
                        index = 0;
                        for (int x = 0; x < xResolution; x++)
                        {
                            for (int y = 0; y < yResolution; y++)
                            {
                                if (mxnMatrix[x,y] && redMatrix[x, y] == i)
                                {
                                    index++;
                                }
                            }
                        }
                        toWrite += index.ToString() + ", ";
                        index = 0;
                        for (int x = leftBounding; x < leftBounding + widthBounding; x++)
                        {
                            for (int y = topBounding; y < topBounding + heightBounding; y++)
                            {
                                if (mxnMatrix[x, y] && redMatrix[x, y] == i)
                                {
                                    index++;
                                }
                            }
                        }
                        toWrite += index.ToString() + ", ";
                    }
                    if (!gLock)
                    {
                        index = 0;
                        for (int x = 0; x < xResolution; x++)
                        {
                            for (int y = 0; y < yResolution; y++)
                            {
                                if (mxnMatrix[x, y] && greenMatrix[x, y] == i)
                                {
                                    index++;
                                }
                            }
                        }
                        toWrite += index.ToString() + ", ";
                        index = 0;
                        for (int x = leftBounding; x < leftBounding + widthBounding; x++)
                        {
                            for (int y = topBounding; y < topBounding + heightBounding; y++)
                            {
                                if (mxnMatrix[x, y] && greenMatrix[x, y] == i)
                                {
                                    index++;
                                }
                            }
                        }
                        toWrite += index.ToString() + ", ";
                    }
                    if (!bLock)
                    {
                        index = 0;
                        for (int x = 0; x < xResolution; x++)
                        {
                            for (int y = 0; y < yResolution; y++)
                            {
                                if (mxnMatrix[x, y] && blueMatrix[x, y] == i)
                                {
                                    index++;
                                }
                            }
                        }
                        toWrite += index.ToString() + ", ";
                        index = 0;
                        for (int x = leftBounding; x < leftBounding + widthBounding; x++)
                        {
                            for (int y = topBounding; y < topBounding + heightBounding; y++)
                            {
                                if (mxnMatrix[x, y] && blueMatrix[x, y] == i)
                                {
                                    index++;
                                }
                            }
                        }
                        toWrite += index.ToString() + ", ";
                    }
                    if (!hLock)
                    {
                        index = 0;
                        for (int x = 0; x < xResolution; x++)
                        {
                            for (int y = 0; y < yResolution; y++)
                            {
                                if (mxnMatrix[x, y] && hueMatrix[x, y] == i)
                                {
                                    index++;
                                }
                            }
                        }
                        toWrite += index.ToString() + ", ";
                        index = 0;
                        for (int x = leftBounding; x < leftBounding + widthBounding; x++)
                        {
                            for (int y = topBounding; y < topBounding + heightBounding; y++)
                            {
                                if (mxnMatrix[x, y] && hueMatrix[x, y] == i)
                                {
                                    index++;
                                }
                            }
                        }
                        toWrite += index.ToString() + ", ";
                    }
                    if (!sLock)
                    {
                        index = 0;
                        for (int x = 0; x < xResolution; x++)
                        {
                            for (int y = 0; y < yResolution; y++)
                            {
                                if (mxnMatrix[x, y] && saturationMatrix[x, y] == i)
                                {
                                    index++;
                                }
                            }
                        }
                        toWrite += index.ToString() + ", ";
                        index = 0;
                        for (int x = leftBounding; x < leftBounding + widthBounding; x++)
                        {
                            for (int y = topBounding; y < topBounding + heightBounding; y++)
                            {
                                if (mxnMatrix[x, y] && saturationMatrix[x, y] == i)
                                {
                                    index++;
                                }
                            }
                        }
                        toWrite += index.ToString() + ", ";
                    }
                    if (!lLock)
                    {
                        index = 0;
                        for (int x = 0; x < xResolution; x++)
                        {
                            for (int y = 0; y < yResolution; y++)
                            {
                                if (mxnMatrix[x, y] && brightnessMatrix[x, y] == i)
                                {
                                    index++;
                                }
                            }
                        }
                        toWrite += index.ToString() + ", ";
                        index = 0;
                        for (int x = leftBounding; x < leftBounding + widthBounding; x++)
                        {
                            for (int y = topBounding; y < topBounding + heightBounding; y++)
                            {
                                if (mxnMatrix[x, y] && brightnessMatrix[x, y] == i)
                                {
                                    index++;
                                }
                            }
                        }
                        toWrite += index.ToString() + ", ";
                    }
                    if (i == 0)
                    {
                        index = 0;
                        for (int x = 0; x < xResolution; x++)
                        {
                            for (int y = 0; y < yResolution; y++)
                            {
                                if (mxnMatrix[x, y])
                                {
                                    index++;
                                }
                            }
                        }
                        toWrite += index.ToString() + ", ";
                        index = 0;
                        for (int x = leftBounding; x < leftBounding + widthBounding; x++)
                        {
                            for (int y = topBounding; y < topBounding + heightBounding; y++)
                            {
                                if (mxnMatrix[x, y])
                                {
                                    index++;
                                }
                            }
                        }
                        toWrite += index.ToString() + ", ";
                    }
                    file.WriteLine(toWrite);
                }
            }

            }
            catch (IOException)
            {
            }
        }

    }
}