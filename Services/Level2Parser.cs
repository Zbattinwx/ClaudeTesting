using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using SharpCompress.Compressors.BZip2;

namespace OhioNewsWeather.WeatherApp.Services
{
    /// <summary>
    /// Robust parser for NEXRAD Level 2 radar data (Message 31 format)
    /// Implements NOAA ICD specification for digital radar data
    /// </summary>
    public class Level2Parser
    {
        private const int VOLUME_HEADER_SIZE = 24;
        private const int LDM_RECORD_SIZE = 2432;
        private const int MESSAGE_HEADER_SIZE = 16;

        /// <summary>
        /// Parse a Level 2 radar file and extract all elevation sweeps
        /// </summary>
        public Level2Data ParseLevel2File(byte[] fileData)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Parsing Level 2 file: {fileData.Length} bytes");

                // Check for compression and decompress if needed
                byte[] data = DecompressIfNeeded(fileData);
                System.Diagnostics.Debug.WriteLine($"Working with {data.Length} bytes after decompression check");

                return ParseUncompressedData(data);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing Level 2 file: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        private byte[] DecompressIfNeeded(byte[] data)
        {
            if (data.Length < 2) return data;

            // Check for gzip (1f 8b)
            if (data[0] == 0x1f && data[1] == 0x8b)
            {
                System.Diagnostics.Debug.WriteLine("Detected gzip compression");
                try
                {
                    using var input = new MemoryStream(data);
                    using var gzip = new GZipStream(input, CompressionMode.Decompress);
                    using var output = new MemoryStream();
                    gzip.CopyTo(output);
                    return output.ToArray();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Gzip decompression failed: {ex.Message}");
                    return data;
                }
            }

            // Check for bzip2 (42 5A - "BZ")
            if (data[0] == 0x42 && data[1] == 0x5A)
            {
                System.Diagnostics.Debug.WriteLine("Detected bzip2 compression");
                try
                {
                    using var input = new MemoryStream(data);
                    using var bzip2 = new BZip2Stream(input, SharpCompress.Compressors.CompressionMode.Decompress, false);
                    using var output = new MemoryStream();
                    bzip2.CopyTo(output);
                    var decompressed = output.ToArray();
                    System.Diagnostics.Debug.WriteLine($"Bzip2 decompressed: {data.Length} -> {decompressed.Length} bytes");
                    return decompressed;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Bzip2 decompression failed: {ex.Message}");
                    return data;
                }
            }

            return data;
        }

        private Level2Data ParseUncompressedData(byte[] data)
        {
            using var stream = new MemoryStream(data);
            using var reader = new BigEndianBinaryReader(stream);

            var level2Data = new Level2Data
            {
                Sweeps = new List<Level2Sweep>()
            };

            try
            {
                // Read Archive II volume header (24 bytes)
                string volumeHeader = new string(reader.ReadChars(9));
                System.Diagnostics.Debug.WriteLine($"Volume header: {volumeHeader}");

                // Skip rest of volume header
                stream.Position = VOLUME_HEADER_SIZE;

                // Read LDM compressed records
                var currentSweep = new Level2Sweep { Radials = new List<Level2Radial>() };
                float currentElevation = -999f;
                int radialCount = 0;

                while (stream.Position + LDM_RECORD_SIZE <= stream.Length)
                {
                    long recordStart = stream.Position;

                    try
                    {
                        // Skip LDM header if present (12 bytes with -1 size means control word)
                        short size = reader.ReadInt16();

                        if (size == -1)
                        {
                            // LDM control word, skip
                            stream.Position = recordStart + 12;
                            size = reader.ReadInt16();
                        }
                        else
                        {
                            stream.Position = recordStart;
                        }

                        // Read message header
                        stream.Position = recordStart + 12; // Skip to message start
                        if (stream.Position + MESSAGE_HEADER_SIZE > stream.Length) break;

                        byte[] messageHeader = reader.ReadBytes(MESSAGE_HEADER_SIZE);

                        // Message type is at byte 15 (0-indexed)
                        byte messageType = messageHeader[15];

                        if (messageType == 31) // Digital Radar Data (Message 31)
                        {
                            // Parse Message 31
                            stream.Position = recordStart + 12; // Reset to message start
                            var radial = ParseMessage31(reader);

                            if (radial != null)
                            {
                                // Check if we're starting a new elevation sweep
                                if (Math.Abs(radial.Elevation - currentElevation) > 0.5f)
                                {
                                    // Save previous sweep if it has data
                                    if (currentSweep.Radials.Count > 0)
                                    {
                                        currentSweep.ElevationAngle = currentElevation;
                                        level2Data.Sweeps.Add(currentSweep);
                                        System.Diagnostics.Debug.WriteLine($"Completed sweep at {currentElevation:F1}° with {currentSweep.Radials.Count} radials");
                                    }

                                    // Start new sweep
                                    currentSweep = new Level2Sweep { Radials = new List<Level2Radial>() };
                                    currentElevation = radial.Elevation;
                                    radialCount = 0;
                                }

                                currentSweep.Radials.Add(radial);
                                radialCount++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error reading record at position {recordStart}: {ex.Message}");
                    }

                    // Move to next LDM record
                    stream.Position = recordStart + LDM_RECORD_SIZE;
                }

                // Add final sweep
                if (currentSweep.Radials.Count > 0)
                {
                    currentSweep.ElevationAngle = currentElevation;
                    level2Data.Sweeps.Add(currentSweep);
                    System.Diagnostics.Debug.WriteLine($"Completed final sweep at {currentElevation:F1}° with {currentSweep.Radials.Count} radials");
                }

                System.Diagnostics.Debug.WriteLine($"Parsed {level2Data.Sweeps.Count} total sweeps");
                return level2Data.Sweeps.Count > 0 ? level2Data : null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ParseUncompressedData: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        private Level2Radial ParseMessage31(BigEndianBinaryReader reader)
        {
            try
            {
                long messageStart = reader.BaseStream.Position;

                // Skip to radial header (starts at byte 28 from message start)
                reader.BaseStream.Position = messageStart + 28;

                // Read azimuth angle (2 bytes, 0.01 degree resolution)
                float azimuth = reader.ReadUInt16() * 0.01f;

                // Read azimuth resolution (2 bytes)
                ushort azimuthRes = reader.ReadUInt16();

                // Read radial status (1 byte)
                byte radialStatus = reader.ReadByte();

                // Read elevation angle (1 byte, 0.01 degree resolution, offset by -127)
                reader.BaseStream.Position = messageStart + 33;
                float elevation = reader.ReadByte() * 0.01f - 127f;

                // Skip ahead to data moment pointers (byte 44)
                reader.BaseStream.Position = messageStart + 44;

                // Read data block pointers
                uint refPointer = reader.ReadUInt32();
                uint velPointer = reader.ReadUInt32();
                uint swPointer = reader.ReadUInt32();

                var radial = new Level2Radial
                {
                    Azimuth = azimuth,
                    Elevation = elevation,
                    ReflectivityGates = new List<float>(),
                    VelocityGates = new List<float>(),
                    SpectrumWidthGates = new List<float>()
                };

                // Parse reflectivity data block
                if (refPointer > 0)
                {
                    reader.BaseStream.Position = messageStart + refPointer;
                    ParseDataMoment(reader, radial.ReflectivityGates, "REF");
                }

                // Parse velocity data block
                if (velPointer > 0)
                {
                    reader.BaseStream.Position = messageStart + velPointer;
                    ParseDataMoment(reader, radial.VelocityGates, "VEL");
                }

                return radial;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing Message 31: {ex.Message}");
                return null;
            }
        }

        private void ParseDataMoment(BigEndianBinaryReader reader, List<float> gates, string momentType)
        {
            try
            {
                // Read data moment header
                uint blockType = reader.ReadUInt32();
                string blockName = new string(reader.ReadChars(3));
                reader.ReadByte(); // Reserved

                ushort numGates = reader.ReadUInt16();
                float firstGateRange = reader.ReadUInt16() * 0.001f; // meters to km
                float gateSpacing = reader.ReadUInt16() * 0.001f; // meters to km
                ushort rfThreshold = reader.ReadUInt16();
                ushort snrThreshold = reader.ReadUInt16();
                byte controlFlags = reader.ReadByte();
                byte wordSize = reader.ReadByte();
                float scale = reader.ReadSingle();
                float offset = reader.ReadSingle();

                // Read gate data
                for (int i = 0; i < numGates; i++)
                {
                    byte value = reader.ReadByte();

                    if (value == 0 || value == 1) // No data or range folded
                    {
                        gates.Add(float.NaN);
                    }
                    else
                    {
                        // Apply scaling: (value - offset) / scale
                        float scaledValue = (value - offset) / scale;
                        gates.Add(scaledValue);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing data moment {momentType}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Helper class for reading big-endian binary data (NEXRAD format)
    /// </summary>
    public class BigEndianBinaryReader : BinaryReader
    {
        public BigEndianBinaryReader(Stream input) : base(input) { }

        public override short ReadInt16()
        {
            byte[] bytes = base.ReadBytes(2);
            Array.Reverse(bytes);
            return BitConverter.ToInt16(bytes, 0);
        }

        public override ushort ReadUInt16()
        {
            byte[] bytes = base.ReadBytes(2);
            Array.Reverse(bytes);
            return BitConverter.ToUInt16(bytes, 0);
        }

        public override int ReadInt32()
        {
            byte[] bytes = base.ReadBytes(4);
            Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        public override uint ReadUInt32()
        {
            byte[] bytes = base.ReadBytes(4);
            Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        public override float ReadSingle()
        {
            byte[] bytes = base.ReadBytes(4);
            Array.Reverse(bytes);
            return BitConverter.ToSingle(bytes, 0);
        }
    }

    /// <summary>
    /// Represents parsed Level 2 radar data
    /// </summary>
    public class Level2Data
    {
        public List<Level2Sweep> Sweeps { get; set; }
    }

    /// <summary>
    /// Represents one elevation sweep
    /// </summary>
    public class Level2Sweep
    {
        public float ElevationAngle { get; set; }
        public List<Level2Radial> Radials { get; set; }
    }

    /// <summary>
    /// Represents one radial beam with moment data
    /// </summary>
    public class Level2Radial
    {
        public float Azimuth { get; set; } // Degrees
        public float Elevation { get; set; } // Degrees
        public List<float> ReflectivityGates { get; set; } // dBZ values
        public List<float> VelocityGates { get; set; } // m/s values
        public List<float> SpectrumWidthGates { get; set; } // m/s values
    }
}
