/*
 * FileFormat.HEIC 
 * Copyright (c) 2024 Openize Pty Ltd. 
 *
 * This file is part of FileFormat.HEIC.
 *
 * FileFormat.HEIC is available under Openize license, which is
 * available along with FileFormat.HEIC sources.
 */

namespace FileFormat.Heic.Tests
{
    using NUnit.Framework;
    using FileFormat.Heic.Decoder;
    using System;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Runtime.Intrinsics;

    public class Tests
    {
        /// <summary>
        /// Samples path.
        /// </summary>
        private string SamplesPath { get; set; }

        /// <summary>
        /// Ethalons path.
        /// </summary>
        private string EthalonsPath { get; set; }

        /// <summary>
        /// Startup setup.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            SamplesPath = GetSamplesPath();
            EthalonsPath = GetEthalonsPath();
        }

        /// <summary>
        /// Test decoding of the images generated by iphone.
        /// </summary>
        [Test]
        [TestCase("iphone_photo.heic")]
        [TestCase("iphone_portrait_photo.heic")]
        public void TestIphoneImages(string filename)
        {
            using (var fs = new FileStream(Path.Combine(SamplesPath, filename), FileMode.Open))
            {
                var image = HeicImage.Load(fs);
                var pixels = image.GetByteArray(PixelFormat.Argb32);
                CompareWithEthalon(filename, pixels);
            }
        }

        /// <summary>
        /// Test decoding of the derived image.
        /// Image sourse: Nokia.
        /// </summary>
        [Test]
        [TestCase("nokia/grid_960x640.heic")]
        [TestCase("nokia/overlay_1000x680.heic")]
        public void TestDerivedImages(string filename)
        {
            using (var fs = new FileStream(Path.Combine(SamplesPath, filename), FileMode.Open))
            {
                var image = HeicImage.Load(fs);
                var pixels = image.GetByteArray(PixelFormat.Argb32);
                CompareWithEthalon(filename, pixels);
            }
        }

        /// <summary>
        /// Test decoding of image collection.
        /// Image sourse: Nokia.
        /// </summary>
        [Test]
        [TestCase("nokia/random_collection_1440x960.heic")]
        public void TestCollection(string filename)
        {
            using (var fs = new FileStream(Path.Combine(SamplesPath, filename), FileMode.Open))
            {
                var image = HeicImage.Load(fs);

                foreach (var frame in image.Frames)
                {
                    var pixels = frame.Value.GetByteArray(PixelFormat.Argb32);
                    CompareWithEthalon(filename + "_" + frame.Key, pixels);
                }
            }
        }

        /// <summary>
        /// Test decoding of image with alpha data.
        /// Image is generated in Gimp.
        /// </summary>
        [Test]
        [TestCase("gimp_rgb_420_with_alpha.heic")]
        public void TestAlphaLayer(string filename)
        {
            using (var fs = new FileStream(Path.Combine(SamplesPath, filename), FileMode.Open))
            {
                var image = HeicImage.Load(fs);
                var pixels = image.GetByteArray(PixelFormat.Argb32);
                CompareWithEthalon(filename, pixels);
            }
        }

        /// <summary>
        /// Create ethalon file.
        /// </summary>
        /// <param name="filename">File name.</param>
        /// <param name="data">Color data.</param>
        private void CreateEthalon(string filename, byte[] data)
        {
            var outputFilename = filename + ".bin";

            using (var stream = new FileStream(Path.Combine(EthalonsPath, outputFilename), FileMode.OpenOrCreate, FileAccess.Write))
            {
                stream.Write(data, 0, data.Length);
            }
        }

        /// <summary>
        /// Compare color data with ethalon file.
        /// </summary>
        /// <param name="filename">File name.</param>
        /// <param name="data">Color data.</param>
        private void CompareWithEthalon(string filename, byte[] data)
        {
            var outputFilename = filename + ".bin";

            using (var stream = new FileStream(Path.Combine(EthalonsPath, outputFilename), FileMode.Open))
            {
                const int bytesToRead = 32;
                int index = 0;

                if (stream.Length != data.Length)
                {
                    Assert.Fail($"Ethalon length do not match. Ethalon length equals {stream.Length}, read data length equals {data.Length}");
                }

                var one = new byte[bytesToRead];
                var two = new byte[bytesToRead];

                var canRead = true;
                while (canRead)
                {
                    var read_bytes_from_stream = stream.Read(one, 0, bytesToRead);

                    if (index + bytesToRead <= data.Length)
                        Array.Copy(data, index, two, 0, bytesToRead);
                    else
                        Array.Copy(data, index, two, 0, data.Length - index);

                    index += bytesToRead;

                    var vOne = MemoryMarshal.Cast<byte, Vector256<byte>>(one);
                    var vTwo = MemoryMarshal.Cast<byte, Vector256<byte>>(two);

                    if (!vTwo.SequenceEqual(vOne))
                    {
                        Assert.Fail("Data does not match ethalon");
                        return;
                    }

                    canRead = read_bytes_from_stream == bytesToRead && index < data.Length;
                }

                Assert.Pass();
            }
        }

        /// <summary>
        /// Get project path.
        /// </summary>
        private static string GetProjectPath()
        {
            var path = new Uri(Assembly.GetExecutingAssembly().GetName().CodeBase).LocalPath;
            return path.Remove(path.IndexOf("\\bin"));
        }

        /// <summary>
        /// Get test samples path.
        /// </summary>
        private static string GetSamplesPath()
        {
            return Path.Combine(GetProjectPath(), "TestsData", "samples");
        }

        /// <summary>
        /// Get test ethalons path.
        /// </summary>
        private static string GetEthalonsPath()
        {
            return Path.Combine(GetProjectPath(), "TestsData", "ethalons");
        }

    }
}