using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SinkingFeelingPOC.Utils
{
    /// <summary>
    /// Utilities for manipulating the image.
    /// </summary>
    public static class ImageUtils
    {
        /// <summary>
        /// The first kernel matrix to apply.
        /// </summary>
        private static double[,]? kernel1;

        /// <summary>
        /// The second optional kernel matrix to apply.
        /// </summary>
        private static double[,]? kernel2;

        /// <summary>
        /// Greyscale changes how we apply the kernels, so we set a flag to adapt behaviour.
        /// </summary>
        private static bool greyScaleMode;

        /// <summary>
        /// Indicates where the centre of the kernel. e.g., in 3x3 matrix, 0/2 are the outer edge array elements, 1 is the centre element.
        /// </summary>
        private static int KernelMiddlePoint;

        /// <summary>
        /// How many kernels to apply.
        /// </summary>
        private static int numkernels;

        /// <summary>
        /// The bitmap being processed.
        /// </summary>
        private static Bitmap? bitmap;

        /// <summary>
        /// The byte array containing the image.
        /// </summary>
        private static byte[]? rgbValuesOrig;

        /// <summary>
        /// Stores the bitmap for reading/writing.
        /// </summary>
        /// <param name="bitmap"></param>
        public static void LoadCurrentBitmap(Bitmap bitmap)
        {
            ImageUtils.bitmap = (Bitmap)bitmap.Clone();
            rgbValuesOrig = GetByteDataFromBitmap(bitmap);
            greyScaleMode = false;
        }

        /// <summary>
        /// Load the kernel for Roberts Cross edge detection.
        /// https://en.wikipedia.org/wiki/Roberts_cross
        /// https://homepages.inf.ed.ac.uk/rbf/HIPR2/roberts.htm
        /// </summary>
        public static void LoadRobertsCrossKernels_3x3()
        {
            kernel1 = new double[,]
            {
                { 0, 0, 0 },
                { 0, 1, 0 },
                { 0, 0, -1 }
            };

            kernel2 = new double[,]
            {
                { 0, 0, 0 },
                { 0, 1, 0 },
                { -1, 0, 0 }
            };

            numkernels = 2;
            KernelMiddlePoint = 1;
        }

        /// <summary>
        /// Locks the bitmap and copies it to a byte[] for editing.
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static byte[] GetByteDataFromBitmap(Bitmap bitmap)
        {
            if (bitmap is null) throw new ArgumentNullException(nameof(bitmap), "did you forget to call LoadCurrentBitmap()?");

            BitmapData bmData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            byte[] rgbValues;

            try
            {
                // we need to calculate how many bytes the image occupies.
                int stride = bmData.Stride;
                IntPtr ptr = bmData.Scan0;

                int bytes = Math.Abs(stride) * bitmap.Height;
                rgbValues = new byte[bytes];

                System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);
            }

            finally
            {
                bitmap.UnlockBits(bmData);
            }

            return rgbValues;
        }

        /// <summary>
        /// Converts the loaded image into greyscale.
        /// </summary>
        public static void ConvertBitmapToGreyScale()
        {
            if (bitmap is null) throw new ArgumentNullException(nameof(bitmap), "did you forget to call LoadCurrentBitmap()?");

            bitmap = ImageUtils.ToGrayScale(bitmap);
            rgbValuesOrig = GetByteDataFromBitmap(bitmap);
            greyScaleMode = true;
        }

        /// <summary>
        /// Apply edge detection.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static Bitmap ApplyRobertsFilter()
        {
            if (bitmap is null) throw new ArgumentNullException(nameof(bitmap), "did you forget to call LoadCurrentBitmap()?");

            LoadRobertsCrossKernels_3x3();

            LoadedMatrixBitmapConvolution();

            return bitmap;
        }

        /// <summary>
        /// Applies the "kernel" matrix (provided prior to calling this) to the image.
        /// </summary>
        public static void LoadedMatrixBitmapConvolution()
        {
            if (bitmap is null) throw new ArgumentNullException(nameof(bitmap), "did you forget to call LoadCurrentBitmap()?");
            if (rgbValuesOrig is null) throw new ArgumentNullException(nameof(rgbValuesOrig), "did you forget to call LoadCurrentBitmap()?");

            BitmapData bmData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

            try
            {
                int height = bitmap.Height;
                int width = bitmap.Width;
                int stride = bmData.Stride;
                int bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
                int totalLength = Math.Abs(stride) * bitmap.Height;

                IntPtr ptr = bmData.Scan0;

                byte[] rgbValues = new byte[totalLength];

                System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, totalLength);

                // walk over all the pixels, apply the kernel matrix/matrices
                for (int y = KernelMiddlePoint; y < height - KernelMiddlePoint; y++)
                {
                    for (int x = KernelMiddlePoint; x < width - KernelMiddlePoint; x++)
                    {
                        if (greyScaleMode)
                        {
                            ApplyKernelMatrixToPixel(rgbValuesOrig, rgbValues, x, y, width, bytesPerPixel);
                        }
                        else
                        {
                            ApplyMatrixToPixel_AllChannels(rgbValuesOrig, rgbValues, x, y, width, bytesPerPixel);
                        }
                    }
                }

                // Copy the RGB values back to the bitmap
                System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, totalLength);
            }

            finally
            {
                bitmap.UnlockBits(bmData);
            }
        }

        /// <summary>
        /// Apply the kernel(s) to the pixel. That requires looking at neighbouring pixels. 
        /// With a 3x3, it's evaluating 8 neighbour pixels for every pixel.
        /// This method acts on a single colour channel.
        /// Note: we have source and dest. We are applying the matrix output to the destination, not the source.
        /// If we modified the source, the kernel would not work.
        /// </summary>
        /// <param name="sourceArrayOfPixels"></param>
        /// <param name="destinationArrayOfPixels"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="bytesPerPixel"></param>
        private static void ApplyKernelMatrixToPixel(byte[] sourceArrayOfPixels, byte[] destinationArrayOfPixels, int x, int y, int width, int bytesPerPixel)
        {
            if (kernel1 is null) throw new ArgumentNullException(nameof(kernel1), "did you forget to initialise the kernels?");
            if (numkernels > 1 && kernel2 is null) throw new ArgumentNullException(nameof(kernel2), "did you forget to initialise the kernels?");

            double kernel1Output = 0, kernel2Output = 0;

            int pos;

            for (int i = 0; i < kernel1.GetLength(0); i++)
            {
                for (int j = 0; j < kernel1.GetLength(1); j++)
                {
                    int I = y + i - KernelMiddlePoint;
                    int J = x + j - KernelMiddlePoint;

                    pos = (I * width + J) * bytesPerPixel;  // find the offset of the pixel we need to sample

                    kernel1Output += kernel1[i, j] * sourceArrayOfPixels[pos];

                    if (numkernels > 1) // apply a 2nd matrix
                    {
                        kernel2Output += kernel2[i, j] * sourceArrayOfPixels[pos];
                    }
                }
            }

            pos = (y * width + x) * bytesPerPixel; // find the offset of the pixel we need to change

            if (numkernels == 2)
            {
                // use Pythagoras. We have two values, but we need one. Imagine them as width and height of the triangle, we compute the hypotenuse.
                destinationArrayOfPixels[pos] = destinationArrayOfPixels[pos + 1] = destinationArrayOfPixels[pos + 2] = (byte)MathUtils.Clamp(Math.Sqrt(kernel1Output * kernel1Output + kernel2Output * kernel2Output), 0, 255.0);
            }
            else if (numkernels == 1)
            {
                destinationArrayOfPixels[pos] = destinationArrayOfPixels[pos + 1] = destinationArrayOfPixels[pos + 2] = (byte)MathUtils.Clamp(Math.Abs(kernel1Output), 0, 255.0);
            }
        }

        /// <summary>
        /// Apply the kernel(s) to the pixel. That requires looking at neighbouring pixels. 
        /// With a 3x3, it's evaluating 8 neighbour pixels for every pixel.
        /// This method acts on all colour channels.
        /// Note: we have source and dest. We are applying the matrix output to the destination, not the source.
        /// If we modified the source, the kernel would not work.
        /// </summary>
        /// <param name="sourceArrayOfPixels"></param>
        /// <param name="destinationArrayOfPixels"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="bytesPerPixel"></param>
        private static void ApplyMatrixToPixel_AllChannels(byte[] sourceArrayOfPixels, byte[] destinationArrayOfPixels, int x, int y, int width, int bytesPerPixel)
        {
            if (kernel1 is null) throw new ArgumentNullException(nameof(kernel1), "did you forget to initialise the kernels?");
            if (numkernels > 1 && kernel2 is null) throw new ArgumentNullException(nameof(kernel2), "did you forget to initialise the kernels?");

            double kernelOutput1R, kernelOutput1G, kernelOutput1B;
            double kernelOutput2R, kernelOutput2G, kernelOutput2B;

            kernelOutput1R = kernelOutput1G = kernelOutput1B = kernelOutput2R = kernelOutput2G = kernelOutput2B = 0;

            int pos;

            for (int i = 0; i < kernel1.GetLength(0); i++)
            {
                for (int j = 0; j < kernel1.GetLength(1); j++)
                {
                    int I = y + i - KernelMiddlePoint;
                    int J = x + j - KernelMiddlePoint;

                    pos = (I * width + J) * bytesPerPixel;  // find the offset of the pixel we need to sample

                    // process red, green and blue.
                    kernelOutput1R += kernel1[i, j] * sourceArrayOfPixels[pos];
                    kernelOutput1G += kernel1[i, j] * sourceArrayOfPixels[pos + 1];
                    kernelOutput1B += kernel1[i, j] * sourceArrayOfPixels[pos + 2];

                    if (numkernels > 1)
                    {
                        // do the same for the 2nd matrix
                        kernelOutput2R += kernel2[i, j] * sourceArrayOfPixels[pos];
                        kernelOutput2G += kernel2[i, j] * sourceArrayOfPixels[pos + 1];
                        kernelOutput2B += kernel2[i, j] * sourceArrayOfPixels[pos + 2];
                    }
                }
            }

            pos = (y * width + x) * bytesPerPixel;  // find the offset of the pixel we need to change

            if (numkernels == 2)
            {
                // use Pythagoras. We have two values per color channel, but we need one. Imagine them as width and height of the triangle, we compute the hypotenuse.
                destinationArrayOfPixels[pos] = (byte)MathUtils.Clamp(Math.Sqrt(kernelOutput1R * kernelOutput1R + kernelOutput2R * kernelOutput2R), 0, 255.0);
                destinationArrayOfPixels[pos + 1] = (byte)MathUtils.Clamp(Math.Sqrt(kernelOutput1G * kernelOutput1G + kernelOutput2G * kernelOutput2G), 0, 255.0);
                destinationArrayOfPixels[pos + 2] = (byte)MathUtils.Clamp(Math.Sqrt(kernelOutput1B * kernelOutput1B + kernelOutput2B * kernelOutput2B), 0, 255.0);
            }

            else if (numkernels == 1)
            {
                destinationArrayOfPixels[pos] = (byte)MathUtils.Clamp(Math.Abs(kernelOutput1R), 0, 255.0);
                destinationArrayOfPixels[pos + 1] = (byte)MathUtils.Clamp(Math.Abs(kernelOutput1G), 0, 255.0);
                destinationArrayOfPixels[pos + 2] = (byte)MathUtils.Clamp(Math.Abs(kernelOutput1B), 0, 255.0);
            }
        }

        /// <summary>
        /// Resize the image, disposing of the original and assigning a resized image.
        /// </summary>
        /// </summary>
        /// <param name="image">IN: Image to scale, OUT: scaled image </param>
        /// <param name="canvasWidth"></param>
        /// <param name="canvasHeight"></param>
        public static void ResizeImage(ref Bitmap imageToScale, int desiredWidth, int desiredHeight)
        {
            if (imageToScale is null) throw new ArgumentNullException(nameof(imageToScale), "input: image to scale, output: image after scaling");

            if (desiredWidth <= 0 || desiredHeight <= 0) throw new Exception("insufficient to resize.");
            int originalWidth = imageToScale.Width;
            int originalHeight = imageToScale.Height;

            if (desiredWidth > 20000) desiredWidth = 20000;
            Bitmap newResizeImage = new(desiredWidth, desiredHeight);

            using Graphics graphic = Graphics.FromImage(newResizeImage);

            // figure out the ratio
            double ratioX = desiredWidth / (double)originalWidth;
            double ratioY = desiredHeight / (double)originalHeight;

            // use whichever multiplier is smaller
            double ratio = ratioX < ratioY ? ratioX : ratioY;

            // now we can get the new height and width
            int newHeight = Convert.ToInt32(originalHeight * ratio);
            int newWidth = Convert.ToInt32(originalWidth * ratio);

            // now calculate the X,Y position of the upper-left corner 
            int posX = Convert.ToInt32((desiredWidth - originalWidth * ratio) / 2);
            int posY = Convert.ToInt32((desiredHeight - originalHeight * ratio) / 2);

            graphic.Clear(Color.White); // white padding: only matters if the new width/height are different ratios, as there is a region that will have no pixels copied.
            graphic.DrawImage(imageToScale, posX, posY, newWidth, newHeight);

            imageToScale.Dispose(); // the original image

            imageToScale = newResizeImage; // the one we created.
        }

        /// <summary>
        /// Turns an image into its negative (inverting the colour: 255-rgb value).
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static Bitmap CreateNegativeFromImage(Bitmap bitmap)
        {
            if (bitmap is null) throw new ArgumentNullException(nameof(bitmap));

            Bitmap res = new(bitmap.Width, bitmap.Height);
            BitmapData bmData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            BitmapData dstBmData = res.LockBits(new Rectangle(0, 0, res.Width, res.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

            try
            {
                int height = bitmap.Height;
                int width = bitmap.Width;
                int stride = bmData.Stride;
                int bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
                int offset = stride - width * bytesPerPixel;
                int totalLength = Math.Abs(stride) * bitmap.Height;

                IntPtr ptr = bmData.Scan0;
                IntPtr dstPtr = dstBmData.Scan0;

                byte[] rgbValues = new byte[totalLength];

                Marshal.Copy(ptr, rgbValues, 0, totalLength);
                int i = 0;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++, i += bytesPerPixel)
                    {
                        // invert
                        rgbValues[i] = (byte)(255 - rgbValues[i]);
                        rgbValues[i + 1] = (byte)(255 - rgbValues[i + 1]);
                        rgbValues[i + 2] = (byte)(255 - rgbValues[i + 2]);
                    }

                    i += offset;
                }

                Marshal.Copy(rgbValues, 0, dstPtr, totalLength);
            }

            finally
            {
                bitmap.UnlockBits(bmData);
                res.UnlockBits(dstBmData);
            }

            return res;
        }

        /// <summary>
        /// Converts the image into grey scale.
        /// </summary>
        /// <param name="sourceBitmap"></param>
        /// <returns></returns>
        public static Bitmap ToGrayScale(Bitmap sourceBitmap)
        {
            if (sourceBitmap is null) throw new ArgumentNullException(nameof(sourceBitmap));

            Bitmap newBitMapImage = new(sourceBitmap.Width, sourceBitmap.Height);
            BitmapData sourceBitmapDataData = sourceBitmap.LockBits(new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height), ImageLockMode.ReadWrite, sourceBitmap.PixelFormat);
            BitmapData destinationBitmapData = newBitMapImage.LockBits(new Rectangle(0, 0, newBitMapImage.Width, newBitMapImage.Height), ImageLockMode.ReadWrite, sourceBitmap.PixelFormat);

            try
            {
                int height = sourceBitmap.Height;
                int width = sourceBitmap.Width;
                int stride = sourceBitmapDataData.Stride;
                int bytesPerPixel = Image.GetPixelFormatSize(sourceBitmap.PixelFormat) / 8;
                int offset = stride - width * bytesPerPixel;
                int totalLength = Math.Abs(stride) * sourceBitmap.Height;

                IntPtr ptr = sourceBitmapDataData.Scan0;
                IntPtr dstPtr = destinationBitmapData.Scan0;

                byte[] rgbValuesForNewBitmap = new byte[totalLength];

                Marshal.Copy(ptr, rgbValuesForNewBitmap, 0, totalLength);
                int i = 0;

                // walk down all pixels in the source image, converting them to a flattened single value (thus grey scale)
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++, i += bytesPerPixel)  // r g b Alpha
                    {
                        // https://en.wikipedia.org/wiki/Grayscale
                        // Colorimetric (perceptual luminance-preserving) conversion to grayscale
                        // The sRGB color space is defined in terms of the CIE 1931 linear luminance Ylinear, which is given by
                        // Y = 0.2126R + 0.7152G + 0.0722B
                        rgbValuesForNewBitmap[i] = rgbValuesForNewBitmap[i + 1] = rgbValuesForNewBitmap[i + 2] = (byte)(0.21f * rgbValuesForNewBitmap[i] + 0.72f * rgbValuesForNewBitmap[i + 1] + 0.07f * rgbValuesForNewBitmap[i + 2]);
                    }

                    i += offset;
                }

                Marshal.Copy(rgbValuesForNewBitmap, 0, dstPtr, totalLength);
            }
            finally
            {
                sourceBitmap.UnlockBits(sourceBitmapDataData);
                newBitMapImage.UnlockBits(destinationBitmapData);
            }

            return newBitMapImage;
        }

        /// <summary>
        /// Returns a Gaussian blur matrix to the desired size.
        /// </summary>
        /// <param name="length"></param>
        /// <param name="weight"></param>
        /// <returns></returns>
        public static double[,] GetGaussianBlurMatrix(int length, double weight)
        {
            double[,] kernel = new double[length, length];
            double kernelSum = 0;
            int foff = (length - 1) / 2;
            double constant = 1d / (2 * Math.PI * weight * weight);

            for (int y = -foff; y <= foff; y++)
            {
                for (int x = -foff; x <= foff; x++)
                {
                    double distance = (y * y + x * x) / (2 * weight * weight);
                    kernel[y + foff, x + foff] = constant * Math.Exp(-distance);
                    kernelSum += kernel[y + foff, x + foff];
                }
            }

            for (int y = 0; y < length; y++)
            {
                for (int x = 0; x < length; x++)
                {
                    kernel[y, x] = kernel[y, x] * 1d / kernelSum;
                }
            }

            return kernel;
        }

        /// <summary>
        /// Apply kernel to an image.
        /// https://en.wikipedia.org/wiki/Convolution
        /// </summary>
        /// <param name="srcImage"></param>
        /// <param name="kernel"></param>
        /// <returns></returns>
        public static Bitmap Convolve(Bitmap srcImage, double[,] kernel)
        {
            int width = srcImage.Width;
            int height = srcImage.Height;
            int bytesPerPixel = Image.GetPixelFormatSize(srcImage.PixelFormat) / 8;

            // before copying, we must lock the image
            BitmapData srcData = srcImage.LockBits(new Rectangle(0, 0, width, height),
            ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            int bytes = srcData.Stride * srcData.Height;

            // this will contain the pixels in the source
            byte[] sourcePixelsArray = new byte[bytes];

            // this will contain the convolved result.
            byte[] convolvedPixelsArray = new byte[bytes];

            // copy source image into an array of pixels            
            Marshal.Copy(srcData.Scan0, sourcePixelsArray, 0, bytes);
            srcImage.UnlockBits(srcData);

            int colorChannels = 3; // R, G, B

            double[] rgb = new double[colorChannels];
            int foff = (kernel.GetLength(0) - 1) / 2;

            for (int y = foff; y < height - foff; y++)
            {
                for (int x = foff; x < width - foff; x++)
                {
                    // initialise the output for all channels
                    for (int c = 0; c < colorChannels; c++)
                    {
                        rgb[c] = 0.0;
                    }

                    int kcenter = y * srcData.Stride + x * bytesPerPixel;

                    for (int fy = -foff; fy <= foff; fy++)
                    {
                        for (int fx = -foff; fx <= foff; fx++)
                        {
                            int kpixel = kcenter + fy * srcData.Stride + fx * bytesPerPixel;

                            for (int c = 0; c < colorChannels; c++)
                            {
                                rgb[c] += sourcePixelsArray[kpixel + c] * kernel[fy + foff, fx + foff];
                            }
                        }
                    }

                    // apply R,G,B output to result 
                    for (int c = 0; c < colorChannels; c++)
                    {
                        convolvedPixelsArray[kcenter + c] = (byte)rgb[c].Clamp(0, 255);
                    }

                    convolvedPixelsArray[kcenter + 3] = 255; // alpha channel
                }
            }

            Bitmap outputImage = new(width, height);
            BitmapData outputImageData = outputImage.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            // copy our convolved output pixels into our new image
            Marshal.Copy(convolvedPixelsArray, 0, outputImageData.Scan0, bytes);
            outputImage.UnlockBits(outputImageData);

            return outputImage;
        }
    }
}