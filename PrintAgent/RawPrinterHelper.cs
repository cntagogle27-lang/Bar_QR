using System.Runtime.InteropServices;

namespace PrintAgent;

/// <summary>
/// Envío de bytes ESC/POS crudos a una impresora Windows mediante P/Invoke (Win32 Spooler API).
/// Permite bypassar GDI+ y enviar los bytes tal cual a la impresora.
/// </summary>
public static class RawPrinterHelper
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	private class DOCINFOA
	{
		[MarshalAs(UnmanagedType.LPStr)] public string pDocName  = "";
		[MarshalAs(UnmanagedType.LPStr)] public string? pOutputFile;
		[MarshalAs(UnmanagedType.LPStr)] public string pDataType = "RAW";
	}

	[DllImport("winspool.drv", EntryPoint = "OpenPrinterA",   SetLastError = true, CharSet = CharSet.Ansi)] static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);
	[DllImport("winspool.drv", EntryPoint = "ClosePrinter",   SetLastError = true)] static extern bool ClosePrinter(IntPtr hPrinter);
	[DllImport("winspool.drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi)] static extern int StartDocPrinter(IntPtr hPrinter, int Level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);
	[DllImport("winspool.drv", EntryPoint = "EndDocPrinter",  SetLastError = true)] static extern bool EndDocPrinter(IntPtr hPrinter);
	[DllImport("winspool.drv", EntryPoint = "StartPagePrinter", SetLastError = true)] static extern bool StartPagePrinter(IntPtr hPrinter);
	[DllImport("winspool.drv", EntryPoint = "EndPagePrinter", SetLastError = true)] static extern bool EndPagePrinter(IntPtr hPrinter);
	[DllImport("winspool.drv", EntryPoint = "WritePrinter",   SetLastError = true)] static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

	public static void EnviarBytesRaw(string printerName, string docName, byte[] bytes)
	{
		if (!OpenPrinter(printerName, out var hPrinter, IntPtr.Zero))
			throw new InvalidOperationException($"No se pudo abrir impresora '{printerName}'. Error: {Marshal.GetLastWin32Error()}");
		try
		{
			var di = new DOCINFOA { pDocName = docName };
			if (StartDocPrinter(hPrinter, 1, di) == 0)
				throw new InvalidOperationException($"StartDocPrinter falló. Error: {Marshal.GetLastWin32Error()}");
			StartPagePrinter(hPrinter);
			var ptr = Marshal.AllocCoTaskMem(bytes.Length);
			try
			{
				Marshal.Copy(bytes, 0, ptr, bytes.Length);
				WritePrinter(hPrinter, ptr, bytes.Length, out _);
			}
			finally { Marshal.FreeCoTaskMem(ptr); }
			EndPagePrinter(hPrinter);
			EndDocPrinter(hPrinter);
		}
		finally { ClosePrinter(hPrinter); }
	}
}
