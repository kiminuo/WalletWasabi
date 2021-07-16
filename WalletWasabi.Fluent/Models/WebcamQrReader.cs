using Avalonia.Media.Imaging;
using System;
using System.Threading.Tasks;
using OpenCvSharp;
using Avalonia;
using Avalonia.Media;
using System.Runtime.InteropServices;
using WalletWasabi.Logging;
using WalletWasabi.Userfacing;
using NBitcoin;
using Nito.AsyncEx;
using Avalonia.Platform;
using System.Buffers;

namespace WalletWasabi.Fluent.Models
{
	public class WebcamQrReader
	{
		private const byte DefaultCameraId = 0;
		private AsyncLock ScanningTaskLock { get; set; }
		public bool RequestEnd { get; set; }
		public Network Network { get; }
		public Task? ScanningTask { get; set; }
		public bool IsRunning => ScanningTask is not null;

		public WebcamQrReader(Network network)
		{
			ScanningTaskLock = new();
			Network = network;
		}

		public async Task StartScanningAsync()
		{
			using (await ScanningTaskLock.LockAsync().ConfigureAwait(false))
			{
				if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					throw new NotImplementedException("This operating system is not supported.");
				}
				ScanningTask = Task.Run(() =>
				{
					using VideoCapture camera = new();

					try
					{
						if (!camera.Open(DefaultCameraId))
						{
							throw new InvalidOperationException("Could not open webcam.");
						}				

						RequestEnd = false;
						KeepScanning(camera);
					}
					catch (Exception exc)
					{
						Logger.LogError("QR scanning stopped. Reason:", exc);
						ErrorOccured?.Invoke(this, exc);
					}
					finally
					{
						camera.Release();
					}
				});
			}
		}

		public async Task StopScanningAsync()
		{
			using (await ScanningTaskLock.LockAsync().ConfigureAwait(false))
			{
				if (ScanningTask is { } task)
				{
					RequestEnd = true;
					await task;

					ScanningTask = null;
				}
			}
		}

		private void KeepScanning(VideoCapture camera)
		{
			WriteableBitmap? lastBitmap = null;
			WriteableBitmap? currentBitmap = null;
			using QRCodeDetector qRCodeDetector = new();
			while (!RequestEnd)
			{
				try
				{
					using Mat frame = new();
					bool gotBackFrame = camera.Read(frame);
					if (!gotBackFrame || frame.Width == 0 || frame.Height == 0)
					{
						continue;
					}
					currentBitmap = ConvertMatToWriteableBitmap(frame);

					NewImageArrived?.Invoke(this, currentBitmap);
					lastBitmap?.Dispose();
					lastBitmap = currentBitmap;

					if (qRCodeDetector.Detect(frame, out Point2f[] points))
					{
						using Mat tmpMat = new();
						string qrCode = qRCodeDetector.Decode(frame, points, tmpMat);
						if (string.IsNullOrWhiteSpace(qrCode))
						{
							continue;
						}
						if (AddressStringParser.TryParse(qrCode, Network, out _))
						{
							CorrectAddressFound?.Invoke(this, qrCode);
							break;
						}
						else
						{
							InvalidAddressFound?.Invoke(this, qrCode);
						}
					}
				}
				catch (OpenCVException exc)
				{
					Logger.LogWarning(exc);
					currentBitmap?.Dispose();
				}
			}
			lastBitmap?.Dispose();
			currentBitmap?.Dispose();
		}

		private WriteableBitmap ConvertMatToWriteableBitmap(Mat frame)
		{
			PixelSize pixelSize = new(frame.Width, frame.Height);
			Vector dpi = new(96, 96);
			WriteableBitmap writeableBitmap = new(pixelSize, dpi, PixelFormat.Rgba8888, AlphaFormat.Unpremul);
			ArrayPool<int> pool = ArrayPool<int>.Shared;

			using (ILockedFramebuffer fb = writeableBitmap.Lock())
			{
				Mat.Indexer<Vec3b> indexer = frame.GetGenericIndexer<Vec3b>();
				int dataSize = fb.Size.Width * fb.Size.Height;
				int[] data = pool.Rent(dataSize);

				try
				{
					for (int y = 0; y < frame.Height; y++)
					{
						int rowIndex = y * fb.Size.Width;

						for (int x = 0; x < frame.Width; x++)
						{
							Vec3b pixel = indexer[y, x];
							byte r = pixel.Item0;
							byte g = pixel.Item1;
							byte b = pixel.Item2;
							Color color = new(255, r, g, b);
							
							data[rowIndex + x] = (int)color.ToUint32();
						}
					}

					Marshal.Copy(data, 0, fb.Address, dataSize);
				}
				finally
				{
					pool.Return(data);
				}
			}

			return writeableBitmap;
		}

		public event EventHandler<WriteableBitmap>? NewImageArrived;

		public event EventHandler<string>? CorrectAddressFound;

		public event EventHandler<string>? InvalidAddressFound;

		public event EventHandler<Exception>? ErrorOccured;
	}
}
