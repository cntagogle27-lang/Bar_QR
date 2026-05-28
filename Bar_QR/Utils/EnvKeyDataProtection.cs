using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace Bar_QR.Utils;

/// <summary>
/// IDataProtectionProvider con clave fija derivada de OAUTH_SECRET.
/// Garantiza que la cookie de correlación OAuth se pueda descifrar
/// tras reinicios de contenedor en Railway.
/// </summary>
public class EnvKeyDataProtectionProvider : IDataProtectionProvider
{
	private readonly byte[] _masterKey;

	public EnvKeyDataProtectionProvider(string secret)
	{
		// Derivar una clave de 32 bytes reproducible desde el secret
		_masterKey = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
	}

	public IDataProtector CreateProtector(string purpose)
		=> new EnvKeyDataProtector(_masterKey, purpose);
}

public class EnvKeyDataProtector : IDataProtector
{
	private readonly byte[] _key; // 32 bytes

	public EnvKeyDataProtector(byte[] masterKey, string purpose)
	{
		// Derivar una subkey específica para este purpose
		_key = HMACSHA256.HashData(masterKey, Encoding.UTF8.GetBytes(purpose));
	}

	public IDataProtector CreateProtector(string purpose)
		=> new EnvKeyDataProtector(_key, purpose);

	public byte[] Protect(byte[] plaintext)
	{
		using var aes = Aes.Create();
		aes.Key = _key;
		aes.GenerateIV(); // IV aleatorio de 16 bytes
		aes.Mode = CipherMode.CBC;
		aes.Padding = PaddingMode.PKCS7;

		using var ms = new MemoryStream();
		ms.Write(aes.IV, 0, 16); // Escribir IV al inicio
		using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
			cs.Write(plaintext, 0, plaintext.Length);

		// Añadir HMAC para integridad
		var ciphertext = ms.ToArray();
		var mac = HMACSHA256.HashData(_key, ciphertext);
		var result = new byte[ciphertext.Length + 32];
		ciphertext.CopyTo(result, 0);
		mac.CopyTo(result, ciphertext.Length);
		return result;
	}

	public byte[] Unprotect(byte[] protectedData)
	{
		if (protectedData.Length < 48) // 16 IV + al menos 1 bloque + 32 HMAC
			throw new CryptographicException("Datos protegidos inválidos.");

		// Verificar HMAC
		var ciphertext = protectedData[..^32];
		var mac = protectedData[^32..];
		var expectedMac = HMACSHA256.HashData(_key, ciphertext);
		if (!CryptographicOperations.FixedTimeEquals(mac, expectedMac))
			throw new CryptographicException("Firma inválida.");

		// Descifrar
		var iv = ciphertext[..16];
		var encrypted = ciphertext[16..];
		using var aes = Aes.Create();
		aes.Key = _key;
		aes.IV = iv;
		aes.Mode = CipherMode.CBC;
		aes.Padding = PaddingMode.PKCS7;

		using var ms = new MemoryStream();
		using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
			cs.Write(encrypted, 0, encrypted.Length);
		return ms.ToArray();
	}
}
