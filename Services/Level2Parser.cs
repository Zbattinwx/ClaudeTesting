using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using SharpCompress.Compressors.BZip2;

namespace OhioNewsWeather.WeatherApp.Services
{
    /// <summary>
    /// NEXRAD Level 2 parser implementing NOAA ICD for Build RDA 18.0
    /// Correctly parses Archive II format with Message 31 digital radar data
    /// </summary>
    public class Level2Parser
    {
        private const int ARCHIVE2_HEADER_SIZE = 24;
        private const int CTM_HEADER_SIZE = 12;
        private const int MESSAGE_SIZE = 2432;

        public Level2Data ParseLevel2File(byte[] fileData)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== Starting Level 2 Parse: {fileData.Length} bytes ===");

                // Decompress if needed
                byte[] data = DecompressIfNeeded(fileData);
                System.Diagnostics.Debug.WriteLine($"After decompression: {data.Length} bytes");

                return ParseArchive2Data(data);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FATAL: Level 2 parse error: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        private byte[] DecompressIfNeeded(byte[] data)
        {
            if (data.Length < 2) return data;

            // Check for gzip (0x1f 0x8b)
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

            // Check for bzip2 (0x42 0x5A - "BZ")
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

        private Level2Data ParseArchive2Data(byte[] data)
        {
            using var stream = new MemoryStream(data);
            using var reader = new BigEndianBinaryReader(stream);

            var level2Data = new Level2Data { Sweeps = new List<Level2Sweep>() };

            try
            {
                // Read Archive II header (24 bytes)
                byte[] headerBytes = reader.ReadBytes(9);
                string header = System.Text.Encoding.ASCII.GetString(headerBytes).TrimEnd('\0');
                System.Diagnostics.Debug.WriteLine($"Archive II Header: '{header}'");

                // Skip rest of header
                stream.Position = ARCHIVE2_HEADER_SIZE;

                // Track sweeps by elevation
                var sweepsByElevation = new Dictionary<float, Level2Sweep>();

                int messageCount = 0;
                int validRadials = 0;

                // Parse messages
                while (stream.Position + MESSAGE_SIZE <= stream.Length)
                {
                    long messageStart = stream.Position;

                    try
                    {
                        // Read CTM header (12 bytes)
                        // Bytes 0-3: Size (negative means metadata)
                        stream.Position = messageStart;
                        byte[] ctmSizeBytes = reader.ReadBytes(4);
                        Array.Reverse(ctmSizeBytes); // Big endian
                        int ctmSize = BitConverter.ToInt32(ctmSizeBytes, 0);

                        // Skip CTM header
                        stream.Position = messageStart + CTM_HEADER_SIZE;

                        // Read message header (starts at byte 12 from record start)
                        // Bytes 0-11: RDA status, etc.
                        // Byte 12-13: Message size in halfwords
                        // Byte 14: RDA channel
                        // Byte 15: Message type
                        stream.Position = messageStart + CTM_HEADER_SIZE;

                        // Read first 16 bytes of message
                        byte[] msgHeaderBytes = reader.ReadBytes(16);

                        byte messageType = msgHeaderBytes[15];

                        if (messageType == 31) // Digital Radar Data
                        {
                            messageCount++;

                            // Parse Message 31 starting from byte 12 of the record
                            stream.Position = messageStart + CTM_HEADER_SIZE;
                            var radial = ParseMessage31Radial(reader);

                            if (radial != null && radial.ReflectivityGates != null && radial.ReflectivityGates.Count > 0)
                            {
                                validRadials++;

                                // Group by elevation angle (rounded to 0.1 degree)
                                float elevKey = (float)Math.Round(radial.Elevation, 1);

                                if (!sweepsByElevation.ContainsKey(elevKey))
                                {
                                    sweepsByElevation[elevKey] = new Level2Sweep
                                    {
                                        ElevationAngle = elevKey,
                                        Radials = new List<Level2Radial>()
                                    };
                                }

                                sweepsByElevation[elevKey].Radials.Add(radial);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error parsing message at {messageStart}: {ex.Message}");
                    }

                    // Move to next message (2432 bytes each)
                    stream.Position = messageStart + MESSAGE_SIZE;
                }

                // Convert to list and sort by elevation
                foreach (var kvp in sweepsByElevation)
                {
                    level2Data.Sweeps.Add(kvp.Value);
                    System.Diagnostics.Debug.WriteLine($"Sweep at {kvp.Key:F1}° has {kvp.Value.Radials.Count} radials");
                }

                level2Data.Sweeps.Sort((a, b) => a.ElevationAngle.CompareTo(b.ElevationAngle));

                System.Diagnostics.Debug.WriteLine($"=== Parse Complete: {messageCount} messages, {validRadials} valid radials, {level2Data.Sweeps.Count} sweeps ===");

                return level2Data.Sweeps.Count > 0 ? level2Data : null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ParseArchive2Data: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        private Level2Radial ParseMessage31Radial(BigEndianBinaryReader reader)
        {
            long msgStart = reader.BaseStream.Position;

            try
            {
                // Message 31 Header (100 bytes total)
                // Bytes 0-11: RDA status header
                reader.ReadBytes(12); // Skip RDA status

                // Bytes 12-13: Message date (days since 1/1/1970)
                ushort dateJulian = reader.ReadUInt16();

                // Bytes 14-17: Message time (ms since midnight)
                uint timeMs = reader.ReadUInt32();

                // Bytes 18-19: Number of message segments
                ushort numSegments = reader.ReadUInt16();

                // Bytes 20-21: Message segment number
                ushort segmentNum = reader.ReadUInt16();

                // === RADIAL HEADER (starts at byte 28) ===
                reader.BaseStream.Position = msgStart + 28;

                // Bytes 28-31: Collection time (ms past midnight)
                uint collectionTime = reader.ReadUInt32();

                // Bytes 32-33: Modified Julian date
                ushort julianDate = reader.ReadUInt16();

                // Bytes 34-35: Unambiguous range (tenths of km)
                ushort unambigRange = reader.ReadUInt16();

                // Bytes 36-37: Azimuth angle (hundredths of degrees)
                ushort azimuthRaw = reader.ReadUInt16();
                float azimuth = azimuthRaw * 0.01f;

                // Bytes 38: Azimuth number
                byte azimuthNumber = reader.ReadByte();

                // Bytes 39: Radial status
                byte radialStatus = reader.ReadByte();

                // Bytes 40-41: Elevation angle (hundredths of degrees)
                ushort elevationRaw = reader.ReadUInt16();
                float elevation = elevationRaw * 0.01f;

                // Bytes 42: Elevation number
                byte elevationNumber = reader.ReadByte();

                // Bytes 43-44: Surveillance range (tenths of km)
                reader.ReadUInt16();

                // Bytes 45-46: Doppler range
                reader.ReadUInt16();

                // Bytes 47-48: Surveillance range sample interval
                reader.ReadUInt16();

                // Bytes 49-50: Doppler range sample interval
                reader.ReadUInt16();

                // Bytes 51: Number of surveillance bins
                byte numSurveillanceBins = reader.ReadByte();

                // Bytes 52: Number of Doppler bins
                byte numDopplerBins = reader.ReadByte();

                // Bytes 53: Cut sector number
                reader.ReadByte();

                // Bytes 54-57: Calibration constant
                reader.ReadSingle();

                // Bytes 58-61: Surveillance pointer (offset to REF data block)
                uint refPointer = reader.ReadUInt32();

                // Bytes 62-65: Velocity pointer
                uint velPointer = reader.ReadUInt32();

                // Bytes 66-69: Spectrum width pointer
                uint swPointer = reader.ReadUInt32();

                // Bytes 70-73: Doppler resolution
                reader.ReadUInt32();

                // Bytes 74-77: VCP
                reader.ReadUInt32();

                // Skip to byte 100 (rest of header)
                reader.BaseStream.Position = msgStart + 100;

                var radial = new Level2Radial
                {
                    Azimuth = azimuth,
                    Elevation = elevation,
                    ReflectivityGates = new List<float>(),
                    VelocityGates = new List<float>(),
                    SpectrumWidthGates = new List<float>()
                };

                // Parse data blocks
                if (refPointer > 0 && refPointer < MESSAGE_SIZE)
                {
                    reader.BaseStream.Position = msgStart + refPointer;
                    ParseDataBlock(reader, radial.ReflectivityGates);
                }

                if (velPointer > 0 && velPointer < MESSAGE_SIZE)
                {
                    reader.BaseStream.Position = msgStart + velPointer;
                    ParseDataBlock(reader, radial.VelocityGates);
                }

                return radial;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing Message 31 radial: {ex.Message}");
                return null;
            }
        }

        private void ParseDataBlock(BigEndianBinaryReader reader, List<float> gates)
        {
            try
            {
                long blockStart = reader.BaseStream.Position;

                // Data block header
                // Bytes 0-3: Block type (e.g., "DREF", "DVEL")
                string blockType = new string(reader.ReadChars(1));
                reader.ReadBytes(3); // Rest of block ID

                // Bytes 4-7: Reserved
                reader.ReadUInt32();

                // Bytes 8-9: Number of gates
                ushort numGates = reader.ReadUInt16();

                // Bytes 10-11: First gate range (meters)
                ushort firstGate = reader.ReadUInt16();

                // Bytes 12-13: Gate size (meters)
                ushort gateSize = reader.ReadUInt16();

                // Bytes 14-15: RF threshold
                ushort rfThreshold = reader.ReadUInt16();

                // Bytes 16-17: SNR threshold
                ushort snrThreshold = reader.ReadUInt16();

                // Bytes 18: Control flags
                byte controlFlags = reader.ReadByte();

                // Bytes 19: Data word size (bits)
                byte wordSize = reader.ReadByte();

                // Bytes 20-23: Scale
                float scale = reader.ReadSingle();

                // Bytes 24-27: Offset
                float offset = reader.ReadSingle();

                // Read gate data (starts at byte 28 of data block)
                for (int i = 0; i < numGates; i++)
                {
                    byte rawValue = reader.ReadByte();

                    if (rawValue == 0 || rawValue == 1) // Below threshold or range folded
                    {
                        gates.Add(float.NaN);
                    }
                    else
                    {
                        // Apply scaling formula from ICD
                        float value = (rawValue - offset) / scale;
                        gates.Add(value);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing data block: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Big-endian binary reader for NEXRAD data
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

    public class Level2Data
    {
        public List<Level2Sweep> Sweeps { get; set; }
    }

    public class Level2Sweep
    {
        public float ElevationAngle { get; set; }
        public List<Level2Radial> Radials { get; set; }
    }

    public class Level2Radial
    {
        public float Azimuth { get; set; }
        public float Elevation { get; set; }
        public List<float> ReflectivityGates { get; set; }
        public List<float> VelocityGates { get; set; }
        public List<float> SpectrumWidthGates { get; set; }
    }
}
