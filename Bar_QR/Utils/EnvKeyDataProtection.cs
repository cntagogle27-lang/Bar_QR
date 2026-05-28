using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace Bar_QR.Utils;

/// <summary>
/// IDataProtectionProvider con clave fija derivada de variable de entorno.
/// Permite que el estado OAuth sobreviva reinicios de contenedor en Railway.
/// </summary>
public class EnvKeyDataProtectionProvider : IDataProtectionProvider
{
	private readonly byte[] _masterKey;

	public EnvKeyDataProtectionProvider(string secret)
	{
		_masterKey = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
	}

	public IDataProtector CreateProtector(string purpose)
		=> new EnvKeyDataProtector(_masterKey, purpose);
}

public class EnvKeyDataProtector : IDataProtector
{
	private readonly byte[] _key;

	public EnvKeyDataProtector(byte[] masterKey, string purpose)
	{
		_key = HMACSHA256.HashData(masterKey, Encoding.UTF8.GetBytes(purpose));
	}

	public IDataProtector CreateProtector(string purpose)
		=> new EnvKeyDataProtector(_key, purpose);

	public byte[] Protect(byte[] plaintext)
	{
		var nonce = new byte[12];
		RandomNumberGenerator.Fill(nonce);
		var ciphertext = new byte[plaintext.Length];
		var tag = new byte[16];
		using var aes = new AesGcm(_key, 16);
		aes.Encrypt(nonce, plaintext, ciphertext, tag);
		// Formato: [12 nonce][16 tag][ciphertext]
		var result = new byte[28 + ciphertext.Length];
		nonce.CopyTo(result, 0);
		tag.CopyTo(result, 12);
		ciphertext.CopyTo(result, 28);
		return result;
	}

	public byte[] Unprotect(byte[] protectedData)
	{
		if (protectedData.Length < 28)
			throw new CryptographicException("Datos protegidos inválidos.");
		var nonce = protectedData[..12];
		var tag = protectedData[12..28];
		var ciphertext = protectedData[28..];
		var plaintext = new byte[ciphertext.Length];
		using var aes = new AesGcm(_key, 16);
		aes.Decrypt(nonce, ciphertext, tag, plaintext);
		return plaintext;
	}
}
