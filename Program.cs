using Serilog;
using System.Diagnostics;
using System.IO.Compression;
using System.Timers;
using ZstdSharp;
using Timer = System.Timers.Timer;

namespace ObsidianWorldConverter {

	internal class Program {

		private static void Main(string InputFormat, string OutputFormat, string WorkDirectory) {
			Log.Logger = new LoggerConfiguration()
				.WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss.ff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
				.CreateLogger();

			if (!Enum.TryParse(InputFormat, out Format FormatFrom)) {
				Log.Fatal($"The Option 'InputFormat' was not one of [{string.Join(", ", Enum.GetValues<Format>())}]");
				Environment.Exit(1);
			}

			if (!Enum.TryParse(OutputFormat, out Format FormatTo)) {
				Log.Fatal($"The Option 'OutputFormat' was not one of [{string.Join(", ", Enum.GetValues<Format>())}]");
				Environment.Exit(1);
			}

			if (!Directory.Exists(WorkDirectory)) {
				Log.Fatal($"The Directory '{WorkDirectory}' does not exist");
				Environment.Exit(1);
			}

			Stopwatch Watch = new();
			ulong FilesConverted = 0;
			ulong FilesTotal = Convert.ToUInt64(Directory.EnumerateFiles(WorkDirectory).Count());

			int ProcessorCount = (int) (Environment.ProcessorCount * 0.75f);
			Log.Information($"Using {ProcessorCount} Threads to convert {ProcessorCount} Files at once");

			Watch.Start();
			Timer UpdateTimer = new() {
				AutoReset = true,
				Interval = 1000,
				Enabled = true
			};

			UpdateTimer.Elapsed += (object? Sender, ElapsedEventArgs Event) => Log.Information($"{FilesConverted:0000} Files done. ({((double) FilesConverted) / FilesTotal * 100f:000.0}%)");

			Directory.EnumerateFiles(WorkDirectory).AsParallel().WithDegreeOfParallelism(ProcessorCount).ForAll(FilePath => {
				FileStream RegionFileStream = File.OpenRead(FilePath);
				MemoryStream UncompressedFileData = new();

				switch (FormatFrom) {
					case Format.None:
						RegionFileStream.CopyTo(UncompressedFileData);
						break;
					case Format.GZip: {
							using GZipStream StreamDecompressor = new(RegionFileStream, CompressionMode.Decompress);
							StreamDecompressor.CopyTo(UncompressedFileData);
							break;
						}
					case Format.ZLib: {
							using ZLibStream StreamDecompressor = new(RegionFileStream, CompressionMode.Decompress);
							StreamDecompressor.CopyTo(UncompressedFileData);
							break;
						}
					case Format.ZStd: {
							using DecompressionStream StreamDecompressor = new(RegionFileStream);
							StreamDecompressor.CopyTo(UncompressedFileData);
							break;
						}
					case Format.Brotli: {
							using BrotliStream StreamDecompressor = new(RegionFileStream, CompressionMode.Decompress);
							StreamDecompressor.CopyTo(UncompressedFileData);
							break;
						}
					default:
						throw new ArgumentOutOfRangeException("How did you get here?");
				}

				RegionFileStream.Close();
				RegionFileStream.Dispose();
				RegionFileStream = File.OpenWrite(FilePath);
				UncompressedFileData.Seek(0, SeekOrigin.Begin);

				switch (FormatTo) {
					case Format.None:
						UncompressedFileData.CopyTo(RegionFileStream);
						break;
					case Format.GZip: {
							using GZipStream StreamCompressor = new(RegionFileStream, CompressionLevel.SmallestSize, false);
							UncompressedFileData.CopyTo(StreamCompressor);
							break;
						}
					case Format.ZLib: {
							using ZLibStream StreamCompressor = new(RegionFileStream, CompressionLevel.SmallestSize, false);
							UncompressedFileData.CopyTo(StreamCompressor);
							break;
						}
					case Format.ZStd: {
							//As far as i know, ZStd only supports -7 to 19 Normally. 20-22 Get unlocked with a special Flag/Commandline Option
							using CompressionStream StreamCompressor = new(RegionFileStream, level: 19);
							UncompressedFileData.CopyTo(StreamCompressor);
							break;
						}
					case Format.Brotli: {
							using BrotliStream StreamCompressor = new(RegionFileStream, CompressionLevel.SmallestSize, false);
							UncompressedFileData.CopyTo(StreamCompressor);
							break;
						}
					default:
						throw new ArgumentOutOfRangeException("How did you get here?");
				}

				RegionFileStream.Close();
				RegionFileStream.Dispose();

				FilesConverted++;
			});

			UpdateTimer.Stop();
			Watch.Stop();
			Log.Information($"All Done! Converted {FilesConverted} Files in '{WorkDirectory}' in {Watch.ElapsedMilliseconds / 1000f / 60f:N2} Minutes");
		}
	}

	internal enum Format {
		None,
		GZip,
		ZLib,
		ZStd,
		Brotli
	}
}