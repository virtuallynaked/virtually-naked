using SharpDX;
using SharpDX.Direct3D11;
using Format = SharpDX.DXGI.Format;
using System;
using System.IO;
using System.Runtime.InteropServices;
using SharpDX.Win32;
using System.Diagnostics;
using SharpDX.Direct3D;

static class DdsLoader {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct DdsPixelFormat {
		public static readonly int SIZE = Marshal.SizeOf<DdsPixelFormat>();

		public uint size;
		public uint flags;
		public uint fourCC;
		public uint RGBBitCount;
		public uint RBitMask;
		public uint GBitMask;
		public uint BBitMask;
		public uint ABitMask;

		public bool IsBitMask(uint r, uint g, uint b, uint a) {
			return (RBitMask == r && GBitMask == g && BBitMask == b && ABitMask == a);
		}
	};

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct DdsHeader {
		public static readonly int SIZE = Marshal.SizeOf<DdsHeader>();

		public uint size;
		public uint flags;
		public uint height;
		public uint width;
		public uint pitchOrLinearSize;
		public uint depth; // only if DDS_HEADER_FLAGS_VOLUME is set in flags
		public uint mipMapCount;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)] uint[] reserved1;
		public DdsPixelFormat ddspf;
		public uint caps;
		public uint caps2;
		public uint caps3;
		public uint caps4;
		public uint reserved2;
	};

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct DdsHeaderDxt10 {
		public static readonly int SIZE = Marshal.SizeOf<DdsHeaderDxt10>();

		public Format dxgiFormat;
		public ResourceDimension resourceDimension;
		public ResourceOptionFlags miscFlag; // see D3D11_RESOURCE_MISC_FLAG
		public uint arraySize;
		public uint miscFlags2;
	};

	const uint DDS_MAGIC = 0x20534444; // "DDS "

	const uint DDS_FOURCC = 0x00000004; // DDPF_FOURCC
	const uint DDS_RGB = 0x00000040; // DDPF_RGB
	const uint DDS_LUMINANCE = 0x00020000; // DDPF_LUMINANCE
	const uint DDS_ALPHA = 0x00000002; // DDPF_ALPHA
	const uint DDS_BUMPDUDV = 0x00080000; // DDPF_BUMPDUDV

	const uint DDS_HEADER_FLAGS_VOLUME = 0x00800000;  // DDSD_DEPTH

	const uint DDS_HEIGHT = 0x00000002; // DDSD_HEIGHT
	const uint DDS_WIDTH = 0x00000004; // DDSD_WIDTH

	const uint DDS_CUBEMAP_POSITIVEX = 0x00000600; // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEX
	const uint DDS_CUBEMAP_NEGATIVEX = 0x00000a00; // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEX
	const uint DDS_CUBEMAP_POSITIVEY = 0x00001200; // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEY
	const uint DDS_CUBEMAP_NEGATIVEY = 0x00002200; // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEY
	const uint DDS_CUBEMAP_POSITIVEZ = 0x00004200; // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEZ
	const uint DDS_CUBEMAP_NEGATIVEZ = 0x00008200; // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEZ

	const uint DDS_CUBEMAP_ALLFACES = (DDS_CUBEMAP_POSITIVEX | DDS_CUBEMAP_NEGATIVEX |
								   DDS_CUBEMAP_POSITIVEY | DDS_CUBEMAP_NEGATIVEY |
								   DDS_CUBEMAP_POSITIVEZ | DDS_CUBEMAP_NEGATIVEZ);

	const uint DDS_CUBEMAP = 0x00000200; // DDSCAPS2_CUBEMAP

	private static int MakeFourCC(int ch0, int ch1, int ch2, int ch3) {
		return ((int)(byte)(ch0) | ((int)(byte)(ch1) << 8) | ((int)(byte)(ch2) << 16) | ((int)(byte)(ch3) << 24));
	}
	
	private static int BitsPerPixel(Format fmt) {
		switch (fmt) {
			case Format.R32G32B32A32_Typeless:
			case Format.R32G32B32A32_Float:
			case Format.R32G32B32A32_UInt:
			case Format.R32G32B32A32_SInt:
				return 128;

			case Format.R32G32B32_Typeless:
			case Format.R32G32B32_Float:
			case Format.R32G32B32_UInt:
			case Format.R32G32B32_SInt:
				return 96;

			case Format.R16G16B16A16_Typeless:
			case Format.R16G16B16A16_Float:
			case Format.R16G16B16A16_UNorm:
			case Format.R16G16B16A16_UInt:
			case Format.R16G16B16A16_SNorm:
			case Format.R16G16B16A16_SInt:
			case Format.R32G32_Typeless:
			case Format.R32G32_Float:
			case Format.R32G32_UInt:
			case Format.R32G32_SInt:
			case Format.R32G8X24_Typeless:
			case Format.D32_Float_S8X24_UInt:
			case Format.R32_Float_X8X24_Typeless:
			case Format.X32_Typeless_G8X24_UInt:
			case Format.Y416:
			case Format.Y210:
			case Format.Y216:
				return 64;

			case Format.R10G10B10A2_Typeless:
			case Format.R10G10B10A2_UNorm:
			case Format.R10G10B10A2_UInt:
			case Format.R11G11B10_Float:
			case Format.R8G8B8A8_Typeless:
			case Format.R8G8B8A8_UNorm:
			case Format.R8G8B8A8_UNorm_SRgb:
			case Format.R8G8B8A8_UInt:
			case Format.R8G8B8A8_SNorm:
			case Format.R8G8B8A8_SInt:
			case Format.R16G16_Typeless:
			case Format.R16G16_Float:
			case Format.R16G16_UNorm:
			case Format.R16G16_UInt:
			case Format.R16G16_SNorm:
			case Format.R16G16_SInt:
			case Format.R32_Typeless:
			case Format.D32_Float:
			case Format.R32_Float:
			case Format.R32_UInt:
			case Format.R32_SInt:
			case Format.R24G8_Typeless:
			case Format.D24_UNorm_S8_UInt:
			case Format.R24_UNorm_X8_Typeless:
			case Format.X24_Typeless_G8_UInt:
			case Format.R9G9B9E5_Sharedexp:
			case Format.R8G8_B8G8_UNorm:
			case Format.G8R8_G8B8_UNorm:
			case Format.B8G8R8A8_UNorm:
			case Format.B8G8R8X8_UNorm:
			case Format.R10G10B10_Xr_Bias_A2_UNorm:
			case Format.B8G8R8A8_Typeless:
			case Format.B8G8R8A8_UNorm_SRgb:
			case Format.B8G8R8X8_Typeless:
			case Format.B8G8R8X8_UNorm_SRgb:
			case Format.AYUV:
			case Format.Y410:
			case Format.YUY2:
				return 32;

			case Format.P010:
			case Format.P016:
				return 24;

			case Format.R8G8_Typeless:
			case Format.R8G8_UNorm:
			case Format.R8G8_UInt:
			case Format.R8G8_SNorm:
			case Format.R8G8_SInt:
			case Format.R16_Typeless:
			case Format.R16_Float:
			case Format.D16_UNorm:
			case Format.R16_UNorm:
			case Format.R16_UInt:
			case Format.R16_SNorm:
			case Format.R16_SInt:
			case Format.B5G6R5_UNorm:
			case Format.B5G5R5A1_UNorm:
			case Format.A8P8:
			case Format.B4G4R4A4_UNorm:
				return 16;

			case Format.NV12:
			case Format.Opaque420:
			case Format.NV11:
				return 12;

			case Format.R8_Typeless:
			case Format.R8_UNorm:
			case Format.R8_UInt:
			case Format.R8_SNorm:
			case Format.R8_SInt:
			case Format.A8_UNorm:
			case Format.AI44:
			case Format.IA44:
			case Format.P8:
				return 8;

			case Format.R1_UNorm:
				return 1;

			case Format.BC1_Typeless:
			case Format.BC1_UNorm:
			case Format.BC1_UNorm_SRgb:
			case Format.BC4_Typeless:
			case Format.BC4_UNorm:
			case Format.BC4_SNorm:
				return 4;

			case Format.BC2_Typeless:
			case Format.BC2_UNorm:
			case Format.BC2_UNorm_SRgb:
			case Format.BC3_Typeless:
			case Format.BC3_UNorm:
			case Format.BC3_UNorm_SRgb:
			case Format.BC5_Typeless:
			case Format.BC5_UNorm:
			case Format.BC5_SNorm:
			case Format.BC6H_Typeless:
			case Format.BC6H_Uf16:
			case Format.BC6H_Sf16:
			case Format.BC7_Typeless:
			case Format.BC7_UNorm:
			case Format.BC7_UNorm_SRgb:
				return 8;

			default:
				return 0;
		}
	}

	private static Format GetDXGIFormat(DdsPixelFormat ddpf) {
		if ((ddpf.flags & DDS_RGB) != 0) {
			// Note that sRGB formats are written using the "DX10" extended header

			switch (ddpf.RGBBitCount) {
				case 32:
					if (ddpf.IsBitMask(0x000000ff, 0x0000ff00, 0x00ff0000, 0xff000000)) {
						return Format.R8G8B8A8_UNorm;
					}

					if (ddpf.IsBitMask(0x00ff0000, 0x0000ff00, 0x000000ff, 0xff000000)) {
						return Format.B8G8R8A8_UNorm;
					}

					if (ddpf.IsBitMask(0x00ff0000, 0x0000ff00, 0x000000ff, 0x00000000)) {
						return Format.B8G8R8X8_UNorm;
					}

					// No DXGI format maps to ddpf.ISBITMASK(0x000000ff,0x0000ff00,0x00ff0000,0x00000000) aka D3DFMT_X8B8G8R8

					// Note that many common DDS reader/writers (including D3DX) swap the
					// the RED/BLUE masks for 10:10:10:2 formats. We assume
					// below that the 'backwards' header mask is being used since it is most
					// likely written by D3DX. The more robust solution is to use the 'DX10'
					// header extension and specify the Format.R10G10B10A2_UNorm format directly

					// For 'correct' writers, this should be 0x000003ff,0x000ffc00,0x3ff00000 for RGB data
					if (ddpf.IsBitMask(0x3ff00000, 0x000ffc00, 0x000003ff, 0xc0000000)) {
						return Format.R10G10B10A2_UNorm;
					}

					// No DXGI format maps to ddpf.ISBITMASK(0x000003ff,0x000ffc00,0x3ff00000,0xc0000000) aka D3DFMT_A2R10G10B10

					if (ddpf.IsBitMask(0x0000ffff, 0xffff0000, 0x00000000, 0x00000000)) {
						return Format.R16G16_UNorm;
					}

					if (ddpf.IsBitMask(0xffffffff, 0x00000000, 0x00000000, 0x00000000)) {
						// Only 32-bit color channel format in D3D9 was R32F
						return Format.R32_Float; // D3DX writes this out as a FourCC of 114
					}
					break;

				case 24:
					// No 24bpp DXGI formats aka D3DFMT_R8G8B8
					break;

				case 16:
					if (ddpf.IsBitMask(0x7c00, 0x03e0, 0x001f, 0x8000)) {
						return Format.B5G5R5A1_UNorm;
					}
					if (ddpf.IsBitMask(0xf800, 0x07e0, 0x001f, 0x0000)) {
						return Format.B5G6R5_UNorm;
					}

					// No DXGI format maps to ddpf.ISBITMASK(0x7c00,0x03e0,0x001f,0x0000) aka D3DFMT_X1R5G5B5

					if (ddpf.IsBitMask(0x0f00, 0x00f0, 0x000f, 0xf000)) {
						return Format.B4G4R4A4_UNorm;
					}

					// No DXGI format maps to ddpf.ISBITMASK(0x0f00,0x00f0,0x000f,0x0000) aka D3DFMT_X4R4G4B4

					// No 3:3:2, 3:3:2:8, or paletted DXGI formats aka D3DFMT_A8R3G3B2, D3DFMT_R3G3B2, D3DFMT_P8, D3DFMT_A8P8, etc.
					break;
			}
		} else if ((ddpf.flags & DDS_LUMINANCE) != 0) {
			if (8 == ddpf.RGBBitCount) {
				if (ddpf.IsBitMask(0x000000ff, 0x00000000, 0x00000000, 0x00000000)) {
					return Format.R8_UNorm; // D3DX10/11 writes this out as DX10 extension
				}

				// No DXGI format maps to ddpf.ISBITMASK(0x0f,0x00,0x00,0xf0) aka D3DFMT_A4L4

				if (ddpf.IsBitMask(0x000000ff, 0x00000000, 0x00000000, 0x0000ff00)) {
					return Format.R8G8_UNorm; // Some DDS writers assume the bitcount should be 8 instead of 16
				}
			}

			if (16 == ddpf.RGBBitCount) {
				if (ddpf.IsBitMask(0x0000ffff, 0x00000000, 0x00000000, 0x00000000)) {
					return Format.R16_UNorm; // D3DX10/11 writes this out as DX10 extension
				}
				if (ddpf.IsBitMask(0x000000ff, 0x00000000, 0x00000000, 0x0000ff00)) {
					return Format.R8G8_UNorm; // D3DX10/11 writes this out as DX10 extension
				}
			}
		} else if ((ddpf.flags & DDS_ALPHA) != 0) {
			if (8 == ddpf.RGBBitCount) {
				return Format.A8_UNorm;
			}
		} else if ((ddpf.flags & DDS_BUMPDUDV) != 0) {
			if (16 == ddpf.RGBBitCount) {
				if (ddpf.IsBitMask(0x00ff, 0xff00, 0x0000, 0x0000)) {
					return Format.R8G8_SNorm; // D3DX10/11 writes this out as DX10 extension
				}
			}

			if (32 == ddpf.RGBBitCount) {
				if (ddpf.IsBitMask(0x000000ff, 0x0000ff00, 0x00ff0000, 0xff000000)) {
					return Format.R8G8B8A8_SNorm; // D3DX10/11 writes this out as DX10 extension
				}
				if (ddpf.IsBitMask(0x0000ffff, 0xffff0000, 0x00000000, 0x00000000)) {
					return Format.R16G16_SNorm; // D3DX10/11 writes this out as DX10 extension
				}

				// No DXGI format maps to ddpf.ISBITMASK(0x3ff00000, 0x000ffc00, 0x000003ff, 0xc0000000) aka D3DFMT_A2W10V10U10
			}
		} else if ((ddpf.flags & DDS_FOURCC) != 0) {
			if (MakeFourCC('D', 'X', 'T', '1') == ddpf.fourCC) {
				return Format.BC1_UNorm;
			}
			if (MakeFourCC('D', 'X', 'T', '3') == ddpf.fourCC) {
				return Format.BC2_UNorm;
			}
			if (MakeFourCC('D', 'X', 'T', '5') == ddpf.fourCC) {
				return Format.BC3_UNorm;
			}

			// While pre-multiplied alpha isn't directly supported by the DXGI formats,
			// they are basically the same as these BC formats so they can be mapped
			if (MakeFourCC('D', 'X', 'T', '2') == ddpf.fourCC) {
				return Format.BC2_UNorm;
			}
			if (MakeFourCC('D', 'X', 'T', '4') == ddpf.fourCC) {
				return Format.BC3_UNorm;
			}

			if (MakeFourCC('A', 'T', 'I', '1') == ddpf.fourCC) {
				return Format.BC4_UNorm;
			}
			if (MakeFourCC('B', 'C', '4', 'U') == ddpf.fourCC) {
				return Format.BC4_UNorm;
			}
			if (MakeFourCC('B', 'C', '4', 'S') == ddpf.fourCC) {
				return Format.BC4_SNorm;
			}

			if (MakeFourCC('A', 'T', 'I', '2') == ddpf.fourCC) {
				return Format.BC5_UNorm;
			}
			if (MakeFourCC('B', 'C', '5', 'U') == ddpf.fourCC) {
				return Format.BC5_UNorm;
			}
			if (MakeFourCC('B', 'C', '5', 'S') == ddpf.fourCC) {
				return Format.BC5_SNorm;
			}

			// BC6H and BC7 are written using the "DX10" extended header

			if (MakeFourCC('R', 'G', 'B', 'G') == ddpf.fourCC) {
				return Format.R8G8_B8G8_UNorm;
			}
			if (MakeFourCC('G', 'R', 'G', 'B') == ddpf.fourCC) {
				return Format.G8R8_G8B8_UNorm;
			}

			if (MakeFourCC('Y', 'U', 'Y', '2') == ddpf.fourCC) {
				return Format.YUY2;
			}

			// Check for D3DFORMAT enums being set here
			switch (ddpf.fourCC) {
				case 36: // D3DFMT_A16B16G16R16
					return Format.R16G16B16A16_UNorm;

				case 110: // D3DFMT_Q16W16V16U16
					return Format.R16G16B16A16_SNorm;

				case 111: // D3DFMT_R16F
					return Format.R16_Float;

				case 112: // D3DFMT_G16R16F
					return Format.R16G16_Float;

				case 113: // D3DFMT_A16B16G16R16F
					return Format.R16G16B16A16_Float;

				case 114: // D3DFMT_R32F
					return Format.R32_Float;

				case 115: // D3DFMT_G32R32F
					return Format.R32G32_Float;

				case 116: // D3DFMT_A32B32G32R32F
					return Format.R32G32B32A32_Float;
			}
		}

		return Format.Unknown;
	}

	private static void CreateTextureFromDDS(
		Device d3dDevice,
		DdsHeader header,
		DdsHeaderDxt10? headerDxt10,
		IntPtr bitData,
		int bitSize,
		int maxsize,
		ResourceUsage usage,
		BindFlags bindFlags,
		CpuAccessFlags cpuAccessFlags,
		ResourceOptionFlags miscFlags,
		bool forceSRGB,
		out Resource texture,
		out ShaderResourceView textureView) {
		Result hr = Result.Ok;

		int width = (int)header.width;
		int height = (int)header.height;
		int depth = (int)header.depth;

		ResourceDimension resDim = ResourceDimension.Unknown;
		uint arraySize = 1;
		SharpDX.DXGI.Format format = SharpDX.DXGI.Format.Unknown;
		bool isCubeMap = false;

		int mipCount = (int)header.mipMapCount;
		if (0 == mipCount) {
			mipCount = 1;
		}

		if (headerDxt10.HasValue) {
			DdsHeaderDxt10 d3d10ext = headerDxt10.Value;

			arraySize = d3d10ext.arraySize;
			if (arraySize == 0) {
				throw new SharpDXException(ErrorCodeHelper.ToResult(ErrorCode.InvalidData));
			}

			switch (d3d10ext.dxgiFormat) {
				case Format.AI44:
				case Format.IA44:
				case Format.P8:
				case Format.A8P8:
					throw new SharpDXException(ErrorCodeHelper.ToResult(ErrorCode.NotSupported));

				default:
					if (BitsPerPixel(d3d10ext.dxgiFormat) == 0) {
						throw new SharpDXException(ErrorCodeHelper.ToResult(ErrorCode.NotSupported));
					}
					break;
			}

			format = d3d10ext.dxgiFormat;

			switch (d3d10ext.resourceDimension) {
				case ResourceDimension.Texture1D:
					// D3DX writes 1D textures with a fixed Height of 1
					if ((header.flags & DDS_HEIGHT) != 0 && height != 1) {
						throw new SharpDXException(ErrorCodeHelper.ToResult(ErrorCode.InvalidData));
					}
					height = depth = 1;
					break;

				case ResourceDimension.Texture2D:
					if ((d3d10ext.miscFlag & ResourceOptionFlags.TextureCube) != 0) {
						arraySize *= 6;
						isCubeMap = true;
					}
					depth = 1;
					break;

				case ResourceDimension.Texture3D:
					if ((header.flags & DDS_HEADER_FLAGS_VOLUME) == 0) {
						throw new SharpDXException(ErrorCodeHelper.ToResult(ErrorCode.InvalidData));
					}

					if (arraySize > 1) {
						throw new SharpDXException(ErrorCodeHelper.ToResult(ErrorCode.InvalidData));
					}
					break;

				default:
					throw new SharpDXException(ErrorCodeHelper.ToResult(ErrorCode.InvalidData));
			}

			resDim = d3d10ext.resourceDimension;
		} else {
			format = GetDXGIFormat(header.ddspf);

			if (format == Format.Unknown) {
				throw new SharpDXException(ErrorCodeHelper.ToResult(ErrorCode.NotSupported));
			}

			if ((header.flags & DDS_HEADER_FLAGS_VOLUME) != 0) {
				resDim = ResourceDimension.Texture3D;
			} else {
				if ((header.caps2 & DDS_CUBEMAP) != 0) {
					// We require all six faces to be defined
					if ((header.caps2 & DDS_CUBEMAP_ALLFACES) != DDS_CUBEMAP_ALLFACES) {
						throw new SharpDXException(ErrorCodeHelper.ToResult(ErrorCode.NotSupported));
					}

					arraySize = 6;
					isCubeMap = true;
				}

				depth = 1;
				resDim = ResourceDimension.Texture2D;

				// Note there's no way for a legacy Direct3D 9 DDS to express a '1D' texture
			}

			Debug.Assert(BitsPerPixel(format) != 0);
		}

		// Bound sizes (for security purposes we don't trust DDS file metadata larger than the D3D 11.x hardware requirements)
		if (mipCount > Resource.MaximumMipLevels) {
			throw new SharpDXException(ErrorCodeHelper.ToResult(ErrorCode.NotSupported));
		}

		switch (resDim) {
			case ResourceDimension.Texture1D:
				if ((arraySize > Resource.MaximumTexture1DArraySize) ||
					(width > Resource.MaximumTexture1DSize)) {
					throw new SharpDXException(ErrorCodeHelper.ToResult(ErrorCode.NotSupported));
				}
				break;

			case ResourceDimension.Texture2D:
				if (isCubeMap) {
					// This is the right bound because we set arraySize to (NumCubes*6) above
					if ((arraySize > Resource.MaximumTexture2DArraySize) ||
						(width > Resource.MaximumTextureCubeSize) ||
						(height > Resource.MaximumTextureCubeSize)) {
						throw new SharpDXException(ErrorCodeHelper.ToResult(ErrorCode.NotSupported));
					}
				} else if ((arraySize > Resource.MaximumTexture2DArraySize) ||
					  (width > Resource.MaximumTexture2DSize) ||
					  (height > Resource.MaximumTexture2DSize)) {
					throw new SharpDXException(ErrorCodeHelper.ToResult(ErrorCode.NotSupported));
				}
				break;

			case ResourceDimension.Texture3D:
				if ((arraySize > 1) ||
					(width > Resource.MaximumTexture3DSize) ||
					(height > Resource.MaximumTexture3DSize) ||
					(depth > Resource.MaximumTexture3DSize)) {
					throw new SharpDXException(ErrorCodeHelper.ToResult(ErrorCode.NotSupported));
				}
				break;

			default:
				throw new SharpDXException(ErrorCodeHelper.ToResult(ErrorCode.NotSupported));
		}

		// Create the texture
		DataBox[] initData = new DataBox[mipCount * arraySize];

		FillInitData(width, height, depth, mipCount, (int)arraySize, format, maxsize, bitSize, bitData,
			out int twidth, out int theight, out int tdepth, out int skipMip, initData);

		CreateD3DResources(d3dDevice, resDim, twidth, theight, tdepth, mipCount - skipMip, (int)arraySize,
			format, usage, bindFlags, cpuAccessFlags, miscFlags, forceSRGB,
			isCubeMap, initData, out texture, out textureView);
	}

	private static void GetSurfaceInfo(
		int width,
		int height,
		Format fmt,
		out int outNumBytes,
		out int outRowBytes,
		out int outNumRows) {
		int numBytes = 0;
		int rowBytes = 0;
		int numRows = 0;

		bool bc = false;
		bool packed = false;
		bool planar = false;
		int bpe = 0;
		switch (fmt) {
			case Format.BC1_Typeless:
			case Format.BC1_UNorm:
			case Format.BC1_UNorm_SRgb:
			case Format.BC4_Typeless:
			case Format.BC4_UNorm:
			case Format.BC4_SNorm:
				bc = true;
				bpe = 8;
				break;

			case Format.BC2_Typeless:
			case Format.BC2_UNorm:
			case Format.BC2_UNorm_SRgb:
			case Format.BC3_Typeless:
			case Format.BC3_UNorm:
			case Format.BC3_UNorm_SRgb:
			case Format.BC5_Typeless:
			case Format.BC5_UNorm:
			case Format.BC5_SNorm:
			case Format.BC6H_Typeless:
			case Format.BC6H_Uf16:
			case Format.BC6H_Sf16:
			case Format.BC7_Typeless:
			case Format.BC7_UNorm:
			case Format.BC7_UNorm_SRgb:
				bc = true;
				bpe = 16;
				break;

			case Format.R8G8_B8G8_UNorm:
			case Format.G8R8_G8B8_UNorm:
			case Format.YUY2:
				packed = true;
				bpe = 4;
				break;

			case Format.Y210:
			case Format.Y216:
				packed = true;
				bpe = 8;
				break;

			case Format.NV12:
			case Format.Opaque420:
				planar = true;
				bpe = 2;
				break;

			case Format.P010:
			case Format.P016:
				planar = true;
				bpe = 4;
				break;
		}

		if (bc) {
			int numBlocksWide = 0;
			if (width > 0) {
				numBlocksWide = Math.Max(1, (width + 3) / 4);
			}
			int numBlocksHigh = 0;
			if (height > 0) {
				numBlocksHigh = Math.Max(1, (height + 3) / 4);
			}
			rowBytes = numBlocksWide * bpe;
			numRows = numBlocksHigh;
			numBytes = rowBytes * numBlocksHigh;
		} else if (packed) {
			rowBytes = ((width + 1) >> 1) * bpe;
			numRows = height;
			numBytes = rowBytes * height;
		} else if (fmt == Format.NV11) {
			rowBytes = ((width + 3) >> 2) * 4;
			numRows = height * 2; // Direct3D makes this simplifying assumption, although it is larger than the 4:1:1 data
			numBytes = rowBytes * numRows;
		} else if (planar) {
			rowBytes = ((width + 1) >> 1) * bpe;
			numBytes = (rowBytes * height) + ((rowBytes * height + 1) >> 1);
			numRows = height + ((height + 1) >> 1);
		} else {
			int bpp = BitsPerPixel(fmt);
			rowBytes = (width * bpp + 7) / 8; // round up to nearest byte
			numRows = height;
			numBytes = rowBytes * height;
		}

		outNumBytes = numBytes;
		outRowBytes = rowBytes;
		outNumRows = numRows;
	}

	private static void FillInitData(
		int width,
		int height,
		int depth,
		int mipCount,
		int arraySize,
		Format format,
		int maxsize,
		int bitSize,
		IntPtr bitData,
		out int twidth,
		out int theight,
		out int tdepth,
		out int skipMip,
		DataBox[] initData) {
		if (bitData == IntPtr.Zero || initData == null) {
			throw new SharpDXException(Result.InvalidPointer);
		}

		skipMip = 0;
		twidth = 0;
		theight = 0;
		tdepth = 0;
		
		IntPtr pSrcBits = bitData;
		IntPtr pEndBits = bitData + bitSize;

		int index = 0;
		for (int j = 0; j < arraySize; j++) {
			int w = width;
			int h = height;
			int d = depth;
			for (int i = 0; i < mipCount; i++) {
				GetSurfaceInfo(w,
					h,
					format,
					out int NumBytes,
					out int RowBytes,
					out int NumRows
				);

				if ((mipCount <= 1) || (maxsize == 0) || (w <= maxsize && h <= maxsize && d <= maxsize)) {
					if (twidth == 0) {
						twidth = w;
						theight = h;
						tdepth = d;
					}

					Debug.Assert(index < mipCount * arraySize);
					initData[index].DataPointer = pSrcBits;
					initData[index].RowPitch = RowBytes;
					initData[index].SlicePitch = NumBytes;
					++index;
				} else if (j == 0) {
					// Count number of skipped mipmaps (first item only)
					++skipMip;
				}

				if ((long)pSrcBits + (NumBytes * d) > (long)pEndBits) {
					throw new SharpDXException(ErrorCodeHelper.ToResult(ErrorCode.HandleEof));
				}

				pSrcBits += NumBytes * d;

				w = w >> 1;
				h = h >> 1;
				d = d >> 1;
				if (w == 0) {
					w = 1;
				}
				if (h == 0) {
					h = 1;
				}
				if (d == 0) {
					d = 1;
				}
			}
		}

		if (index > 0) {
			return;
		} else {
			throw new SharpDXException(Result.Fail);
		}
	}

	private static Format MakeSRGB(Format format) {
		switch (format) {
			case Format.R8G8B8A8_UNorm:
				return Format.R8G8B8A8_UNorm_SRgb;

			case Format.BC1_UNorm:
				return Format.BC1_UNorm_SRgb;

			case Format.BC2_UNorm:
				return Format.BC2_UNorm_SRgb;

			case Format.BC3_UNorm:
				return Format.BC3_UNorm_SRgb;

			case Format.B8G8R8A8_UNorm:
				return Format.B8G8R8A8_UNorm_SRgb;

			case Format.B8G8R8X8_UNorm:
				return Format.B8G8R8X8_UNorm_SRgb;

			case Format.BC7_UNorm:
				return Format.BC7_UNorm_SRgb;

			default:
				return format;
		}
	}

	private static void CreateD3DResources(
		Device d3dDevice,
		ResourceDimension resDim,
		int width,
		int height,
		int depth,
		int mipCount,
		int arraySize,
		Format format,
		ResourceUsage usage,
		BindFlags bindFlags,
		CpuAccessFlags cpuAccessFlags,
		ResourceOptionFlags miscFlags,
		bool forceSRGB,
		bool isCubeMap,
		DataBox[] initData,
		out Resource texture,
		out ShaderResourceView textureView) {
		if (d3dDevice == null)
			throw new SharpDXException(Result.InvalidPointer);

		if (forceSRGB) {
			format = MakeSRGB(format);
		}

		switch (resDim) {
			case ResourceDimension.Texture1D: {
					Texture1DDescription desc;
					desc.Width = width;
					desc.MipLevels = mipCount;
					desc.ArraySize = arraySize;
					desc.Format = format;
					desc.Usage = usage;
					desc.BindFlags = bindFlags;
					desc.CpuAccessFlags = cpuAccessFlags;
					desc.OptionFlags = miscFlags & ~ResourceOptionFlags.TextureCube;

					using (Texture1D tex = new Texture1D(d3dDevice, desc, initData)) {
						ShaderResourceViewDescription SRVDesc = default(ShaderResourceViewDescription);
						SRVDesc.Format = format;

						if (arraySize > 1) {
							SRVDesc.Dimension = ShaderResourceViewDimension.Texture1DArray;
							SRVDesc.Texture1DArray.MipLevels = (mipCount == 0) ? -1 : desc.MipLevels;
							SRVDesc.Texture1DArray.ArraySize = arraySize;
						} else {
							SRVDesc.Dimension = ShaderResourceViewDimension.Texture1D;
							SRVDesc.Texture1D.MipLevels = (mipCount == 0) ? -1 : desc.MipLevels;
						}

						textureView = new ShaderResourceView(d3dDevice, tex, SRVDesc);
						texture = tex.QueryInterface<Texture1D>();
					}
				}
				break;

			case ResourceDimension.Texture2D: {
					Texture2DDescription desc;
					desc.Width = width;
					desc.Height = height;
					desc.MipLevels = mipCount;
					desc.ArraySize = arraySize;
					desc.Format = format;
					desc.SampleDescription.Count = 1;
					desc.SampleDescription.Quality = 0;
					desc.Usage = usage;
					desc.BindFlags = bindFlags;
					desc.CpuAccessFlags = cpuAccessFlags;
					if (isCubeMap) {
						desc.OptionFlags = miscFlags | ResourceOptionFlags.TextureCube;
					} else {
						desc.OptionFlags = miscFlags & ~ResourceOptionFlags.TextureCube;
					}

					using (Texture2D tex = new Texture2D(d3dDevice, desc, initData)) {
						ShaderResourceViewDescription SRVDesc = default(ShaderResourceViewDescription);
						SRVDesc.Format = format;

						if (isCubeMap) {
							if (arraySize > 6) {
								SRVDesc.Dimension = ShaderResourceViewDimension.TextureCubeArray;
								SRVDesc.TextureCubeArray.MipLevels = (mipCount == 0) ? -1 : desc.MipLevels;

								// Earlier we set arraySize to (NumCubes * 6)
								SRVDesc.TextureCubeArray.CubeCount = arraySize / 6;
							} else {
								SRVDesc.Dimension = ShaderResourceViewDimension.TextureCube;
								SRVDesc.TextureCube.MipLevels = (mipCount == 0) ? -1 : desc.MipLevels;
							}
						} else if (arraySize > 1) {
							SRVDesc.Dimension = ShaderResourceViewDimension.Texture2DArray;
							SRVDesc.Texture2DArray.MipLevels = (mipCount == 0) ? -1 : desc.MipLevels;
							SRVDesc.Texture2DArray.ArraySize = arraySize;
						} else {
							SRVDesc.Dimension = ShaderResourceViewDimension.Texture2D;
							SRVDesc.Texture2D.MipLevels = (mipCount == 0) ? -1 : desc.MipLevels;
						}

						textureView = new ShaderResourceView(d3dDevice, tex, SRVDesc);
						texture = tex.QueryInterface<Texture2D>();
					}
				}
				break;

			case ResourceDimension.Texture3D: {
					Texture3DDescription desc;
					desc.Width = width;
					desc.Height = height;
					desc.Depth = depth;
					desc.MipLevels = mipCount;
					desc.Format = format;
					desc.Usage = usage;
					desc.BindFlags = bindFlags;
					desc.CpuAccessFlags = cpuAccessFlags;
					desc.OptionFlags = miscFlags & ~ResourceOptionFlags.TextureCube;

					using (Texture3D tex = new Texture3D(d3dDevice, desc, initData)) {
						ShaderResourceViewDescription SRVDesc = default(ShaderResourceViewDescription);
						SRVDesc.Format = format;

						SRVDesc.Dimension = ShaderResourceViewDimension.Texture3D;
						SRVDesc.Texture3D.MipLevels = (mipCount == 0) ? -1 : desc.MipLevels;

						textureView = new ShaderResourceView(d3dDevice, tex, SRVDesc);
						texture = tex.QueryInterface<Texture3D>();
					}
				}
				break;

			default:
				throw new SharpDXException(Result.Fail);
		}
	}

	public static void CreateDDSTextureFromMemory(Device d3dDevice,
		DataPointer dataPointer,
		out Resource texture,
		out ShaderResourceView textureView,
		int maxsize = 0,
		ResourceUsage usage = ResourceUsage.Default,
		BindFlags bindFlags = BindFlags.ShaderResource,
		CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None,
		ResourceOptionFlags miscFlags = ResourceOptionFlags.None,
		bool forceSRGB = false
	) {
		texture = null;
		textureView = null;

		if (d3dDevice == null) {
			throw new SharpDXException(Result.InvalidArg);
		}

		// Validate DDS file in memory
		if (dataPointer.Size < (sizeof(uint) + DdsHeader.SIZE)) {
			throw new SharpDXException(Result.Fail);
		}

		uint magicNumber = Marshal.PtrToStructure<uint>(dataPointer.Pointer);
		if (magicNumber != DDS_MAGIC) {
			throw new SharpDXException(Result.Fail);
		}

		DdsHeader header = Marshal.PtrToStructure<DdsHeader>(dataPointer.Pointer + sizeof(uint));

		// Verify header to validate DDS file
		if (header.size != DdsHeader.SIZE ||
			header.ddspf.size != DdsPixelFormat.SIZE) {
			throw new SharpDXException(Result.Fail);
		}

		// Check for DX10 extension
		DdsHeaderDxt10? headerDxt10;

		bool bDXT10Header = false;
		if ((header.ddspf.flags & DDS_FOURCC) != 0 &&
			(MakeFourCC('D', 'X', '1', '0') == header.ddspf.fourCC)) {
			// Must be long enough for both headers and magic value
			if (dataPointer.Size < (DdsHeader.SIZE + sizeof(uint) + DdsHeaderDxt10.SIZE)) {
				throw new SharpDXException(Result.Fail);
			}

			bDXT10Header = true;
			headerDxt10 = Marshal.PtrToStructure<DdsHeaderDxt10>(dataPointer.Pointer + sizeof(uint) + DdsHeader.SIZE);
		} else {
			headerDxt10 = null;
		}

		int offset = sizeof(uint)
			+ DdsHeader.SIZE
			+ (bDXT10Header ? DdsHeaderDxt10.SIZE : 0);

		CreateTextureFromDDS(d3dDevice, header, headerDxt10,
			dataPointer.Pointer + offset, dataPointer.Size - offset, maxsize,
			usage, bindFlags, cpuAccessFlags, miscFlags, forceSRGB,
			out texture, out textureView);
	}

	public static void CreateDDSTextureFromMemory(Device d3dDevice,
		byte[] ddsData,
		out Resource texture,
		out ShaderResourceView textureView,
		int maxsize = 0,
		ResourceUsage usage = ResourceUsage.Default,
		BindFlags bindFlags = BindFlags.ShaderResource,
		CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None,
		ResourceOptionFlags miscFlags = ResourceOptionFlags.None,
		bool forceSRGB = false
	) {
		GCHandle ddsDataHandle = GCHandle.Alloc(ddsData, GCHandleType.Pinned);
		try {
			CreateDDSTextureFromMemory(
				d3dDevice,
				new DataPointer(ddsDataHandle.AddrOfPinnedObject(), ddsData.Length),
				out texture,
				out textureView,
				maxsize,
				usage,
				bindFlags,
				cpuAccessFlags,
				miscFlags,
				forceSRGB);
		} finally {
			ddsDataHandle.Free();
		}
	}
}
