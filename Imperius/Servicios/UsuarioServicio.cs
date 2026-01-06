using Imperius.Modelo;
using SQLite;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace Imperius.Servicios
{
    public static class UsuarioServicio
    {
        static SQLiteAsyncConnection? _db;
        const string DbFileName = "Imperius.db";
        const string SecureKeyName = "imperius_db_key_v1";

        private const string CurrentUserEmailHashKey = "current_user_email_hash";
        private const string CurrentUserBoletaHashKey = "current_user_boleta_hash";
        private const string CurrentUserIdKey = "current_user_id";

        private static bool _isInitialized = false;
        private static readonly SemaphoreSlim _initSemaphore = new(1, 1);

        public static async Task InitAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            await _initSemaphore.WaitAsync();
            try
            {
                if (_isInitialized)
                {
                    return;
                }

                var localDbPath = Path.Combine(FileSystem.AppDataDirectory, DbFileName);

                if (!File.Exists(localDbPath))
                {
                    try
                    {
                        using var stream = await FileSystem.OpenAppPackageFileAsync(DbFileName);
                        using var outStream = File.Create(localDbPath);
                        await stream.CopyToAsync(outStream);
                    }
                    catch (FileNotFoundException)
                    {
                        Console.WriteLine("Advertencia: No se encontró el archivo de base de datos pre-empaquetado. Se creará una base de datos vacía.");
                        Directory.CreateDirectory(Path.GetDirectoryName(localDbPath) ?? FileSystem.AppDataDirectory);
                        using var fs = File.Create(localDbPath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error al copiar la base de datos: {ex.Message}");
                        Directory.CreateDirectory(Path.GetDirectoryName(localDbPath) ?? FileSystem.AppDataDirectory);
                        using var fs = File.Create(localDbPath);
                    }
                }

                _db = new SQLiteAsyncConnection(localDbPath);
                await _db.CreateTableAsync<Usuario>();

                await EnsureCorreoHashColumnAsync();
                await EnsureBoletaHashColumnAsync();

                _isInitialized = true;
            }
            finally
            {
                _initSemaphore.Release();
            }
        }

        static async Task EnsureCorreoHashColumnAsync()
        {
            if (_db == null) throw new InvalidOperationException("DB no inicializada.");

            var cols = await _db.QueryAsync<TableInfo>("PRAGMA table_info('tbUsuarios');");
            var hasCorreoHash = cols.Any(c => c.name.Equals("correoHash", StringComparison.OrdinalIgnoreCase));
            if (!hasCorreoHash)
            {
                await _db.ExecuteAsync("ALTER TABLE tbUsuarios ADD COLUMN correoHash TEXT;");
                var rows = await _db.QueryAsync<IdCorreo>("SELECT idUsuario, correo FROM tbUsuarios;");
                var key = await GetOrCreateEncryptionKeyAsync();
                foreach (var r in rows)
                {
                    if (!string.IsNullOrWhiteSpace(r.correo))
                    {
                        var decryptedCorreo = DecryptString(r.correo, key);
                        var hash = ComputeCorreoHash(decryptedCorreo);
                        await _db.ExecuteAsync("UPDATE tbUsuarios SET correoHash = ? WHERE idUsuario = ?;", hash, r.idUsuario);
                    }
                }
            }
        }

        static async Task EnsureBoletaHashColumnAsync()
        {
            if (_db == null) throw new InvalidOperationException("DB no inicializada.");

            var cols = await _db.QueryAsync<TableInfo>("PRAGMA table_info('tbUsuarios');");
            var hasBoletaHash = cols.Any(c => c.name.Equals("boletaHash", StringComparison.OrdinalIgnoreCase));
            if (!hasBoletaHash)
            {
                await _db.ExecuteAsync("ALTER TABLE tbUsuarios ADD COLUMN boletaHash TEXT;");
                var rows = await _db.QueryAsync<IdBoleta>("SELECT idUsuario, boleta FROM tbUsuarios;");
                var key = await GetOrCreateEncryptionKeyAsync();
                foreach (var r in rows)
                {
                    if (!string.IsNullOrWhiteSpace(r.boleta))
                    {
                        var decryptedBoleta = DecryptString(r.boleta, key);
                        var hash = ComputeBoletaHash(decryptedBoleta);
                        await _db.ExecuteAsync("UPDATE tbUsuarios SET boletaHash = ? WHERE idUsuario = ?;", hash, r.idUsuario);
                    }
                }
            }
        }

        public static async Task<bool> CrearUsuarioAsync(Usuario usuario)
        {
            if (usuario is null) throw new ArgumentNullException(nameof(usuario));
            await InitAsync();

            var key = await GetOrCreateEncryptionKeyAsync();

            var correoNormalized = (usuario.Correo ?? string.Empty).Trim().ToLowerInvariant();
            var correoHash = ComputeCorreoHash(correoNormalized);

            var boletaNormalized = (usuario.Boleta ?? string.Empty).Trim().ToLowerInvariant();
            var boletaHash = ComputeBoletaHash(boletaNormalized);

            var passwordHash = HashPassword(usuario.Contrasena);

            var nombreEnc = EncryptString(usuario.Nombre ?? string.Empty, key);
            var apellidoEnc = EncryptString(usuario.Apellido ?? string.Empty, key);
            var boletaEnc = EncryptString(boletaNormalized, key);
            var correoEnc = EncryptString(correoNormalized, key);

            var toInsert = new Usuario
            {
                Correo = correoEnc,
                CorreoHash = correoHash,
                Contrasena = passwordHash,
                Nombre = nombreEnc,
                Apellido = apellidoEnc,
                Boleta = boletaEnc,
                BoletaHash = boletaHash
            };

            var rows = await _db!.InsertAsync(toInsert);
            return rows > 0;
        }

        public static async Task<Usuario?> ObtenerPorCorreoYContrasenaAsync(string correo, string contrasena)
        {
            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contrasena))
                return null;

            await InitAsync();
            var correoNormalized = correo.Trim().ToLowerInvariant();
            var correoHash = ComputeCorreoHash(correoNormalized);

            var found = await _db!.Table<Usuario>().Where(u => u.CorreoHash == correoHash).FirstOrDefaultAsync();
            if (found == null) return null;

            if (!VerifyHashedPassword(found.Contrasena, contrasena))
                return null;

            var key = await GetOrCreateEncryptionKeyAsync();
            var usuario = new Usuario
            {
                IdUsuario = found.IdUsuario,
                Correo = DecryptString(found.Correo, key),
                CorreoHash = found.CorreoHash,
                Contrasena = string.Empty,
                Nombre = DecryptString(found.Nombre, key),
                Apellido = DecryptString(found.Apellido, key),
                Boleta = DecryptString(found.Boleta, key),
                BoletaHash = found.BoletaHash
            };

            return usuario;
        }

        public static async Task<Usuario?> ObtenerPorBoletaYContrasenaAsync(string boleta, string contrasena)
        {
            if (string.IsNullOrWhiteSpace(boleta) || string.IsNullOrWhiteSpace(contrasena))
                return null;

            await InitAsync();
            var boletaNormalized = boleta.Trim().ToLowerInvariant();
            var boletaHash = ComputeBoletaHash(boletaNormalized);

            var found = await _db!.Table<Usuario>().Where(u => u.BoletaHash == boletaHash).FirstOrDefaultAsync();
            if (found == null) return null;

            if (!VerifyHashedPassword(found.Contrasena, contrasena))
                return null;

            var key = await GetOrCreateEncryptionKeyAsync();
            var usuario = new Usuario
            {
                IdUsuario = found.IdUsuario,
                Correo = DecryptString(found.Correo, key),
                CorreoHash = found.CorreoHash,
                Contrasena = string.Empty,
                Nombre = DecryptString(found.Nombre, key),
                Apellido = DecryptString(found.Apellido, key),
                Boleta = DecryptString(found.Boleta, key),
                BoletaHash = found.BoletaHash
            };

            return usuario;
        }

        public static async Task<Usuario?> ObtenerUsuarioPorCorreoHashAsync(string correoHash)
        {
            if (string.IsNullOrWhiteSpace(correoHash)) return null;

            await InitAsync();

            var found = await _db!.Table<Usuario>().Where(u => u.CorreoHash == correoHash).FirstOrDefaultAsync();
            if (found == null) return null;

            var key = await GetOrCreateEncryptionKeyAsync();
            var usuario = new Usuario
            {
                IdUsuario = found.IdUsuario,
                Correo = DecryptString(found.Correo, key),
                CorreoHash = found.CorreoHash,
                Contrasena = string.Empty,
                Nombre = DecryptString(found.Nombre, key),
                Apellido = DecryptString(found.Apellido, key),
                Boleta = DecryptString(found.Boleta, key),
                BoletaHash = found.BoletaHash
            };

            return usuario;
        }

        public static async Task<int?> GetCurrentUserIdAsync()
        {
            var userIdString = await SecureStorage.Default.GetAsync(CurrentUserIdKey);
            if (int.TryParse(userIdString, out int userId))
            {
                return userId;
            }
            return null;
        }

        public static async Task<string?> GetCurrentUserNameAsync()
        {
            // 1. Obtener el ID del usuario actual
            var currentUserId = await GetCurrentUserIdAsync();
            if (currentUserId == null)
            {
                return null; // No hay usuario logeado
            }

            await InitAsync();
            if (_db == null)
            {
                // Esto no debería pasar si InitAsync() se ejecutó correctamente
                throw new InvalidOperationException("La base de datos no está inicializada.");
            }

            // 2. Consultar la base de datos para obtener el registro del usuario
            // Usamos FindAsync con la clave primaria.
            var found = await _db.FindAsync<Usuario>(currentUserId.Value);
            if (found == null)
            {
                // El ID encontrado en SecureStorage no corresponde a un usuario en la DB
                ClearCurrentUser(); // Limpiamos la sesión inconsistente
                return null;
            }

            // 3. Obtener la clave de encriptación
            var key = await GetOrCreateEncryptionKeyAsync();

            // 4. Desencriptar el campo Nombre
            var decryptedNombre = DecryptString(found.Nombre, key);

            return decryptedNombre;
        }

        public static async Task<string?> GetCurrentUserBoletaHashAsync()
        {
            return await SecureStorage.Default.GetAsync(CurrentUserBoletaHashKey);
        }

        public static async Task<int?> GetUserIdByBoletaHashAsync(string boletaHash)
        {
            if (string.IsNullOrWhiteSpace(boletaHash)) return null;

            await InitAsync();

            var found = await _db!.Table<Usuario>().Where(u => u.BoletaHash == boletaHash).FirstOrDefaultAsync();
            return found?.IdUsuario;
        }

        public static async Task SetCurrentUserAsync(int userId, string emailHash, string boletaHash)
        {
            await SecureStorage.Default.SetAsync(CurrentUserIdKey, userId.ToString());
            await SecureStorage.Default.SetAsync(CurrentUserEmailHashKey, emailHash);
            await SecureStorage.Default.SetAsync(CurrentUserBoletaHashKey, boletaHash);
        }

        public static void ClearCurrentUser()
        {
            SecureStorage.Default.Remove(CurrentUserIdKey);
            SecureStorage.Default.Remove(CurrentUserEmailHashKey);
            SecureStorage.Default.Remove(CurrentUserBoletaHashKey);
        }

        static string ComputeCorreoHash(string correo)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(correo);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
        
        static string ComputeBoletaHash(string boleta)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(boleta);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        static string HashPassword(string password)
        {
            const int iter = 100_000;
            var salt = RandomNumberGenerator.GetBytes(16);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iter, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);
            return $"{iter}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        static bool VerifyHashedPassword(string stored, string providedPassword)
        {
            try
            {
                var parts = stored.Split('.');
                if (parts.Length != 3) return false;
                var iter = int.Parse(parts[0]);
                var salt = Convert.FromBase64String(parts[1]);
                var hash = Convert.FromBase64String(parts[2]);

                using var pbkdf2 = new Rfc2898DeriveBytes(providedPassword, salt, iter, HashAlgorithmName.SHA256);
                var candidate = pbkdf2.GetBytes(hash.Length);
                return CryptographicOperations.FixedTimeEquals(candidate, hash);
            }
            catch
            {
                return false;
            }
        }

        static async Task<byte[]> GetOrCreateEncryptionKeyAsync()
        {
            try
            {
                var existing = await SecureStorage.Default.GetAsync(SecureKeyName);
                if (!string.IsNullOrEmpty(existing))
                {
                    return Convert.FromBase64String(existing);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al acceder a SecureStorage para la llave de encriptación: {ex.Message}");
            }

            var key = RandomNumberGenerator.GetBytes(32);
            try
            {
                await SecureStorage.Default.SetAsync(SecureKeyName, Convert.ToBase64String(key));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar la llave de encriptación en SecureStorage: {ex.Message}");
            }
            return key;
        }

        static string EncryptString(string plainText, byte[] key)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;
            var plaintextBytes = Encoding.UTF8.GetBytes(plainText);

            var nonce = RandomNumberGenerator.GetBytes(12);
            var cipher = new byte[plaintextBytes.Length];

            const int TagSize = 16;
            var tag = new byte[TagSize];

            using (var aesGcm = new AesGcm(key, TagSize))
            {
                aesGcm.Encrypt(nonce, plaintextBytes, cipher, tag);
            }
            var combinedLength = nonce.Length + cipher.Length + tag.Length;
            var combined = new byte[combinedLength];

            Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
            Buffer.BlockCopy(cipher, 0, combined, nonce.Length, cipher.Length);
            Buffer.BlockCopy(tag, 0, combined, nonce.Length + cipher.Length, tag.Length);
            return Convert.ToBase64String(combined);
        }

        static string DecryptString(string base64, byte[] key)
        {
            if (string.IsNullOrEmpty(base64)) return string.Empty;
            try
            {
                var combined = Convert.FromBase64String(base64);
                const int NonceSize = 12;
                const int TagSize = 16;

                if (combined.Length < NonceSize + TagSize)
                {
                    Console.WriteLine("Error de desencriptación: la longitud combinada es demasiado corta.");
                    return string.Empty;
                }

                var nonce = combined.Take(NonceSize).ToArray();
                var tag = combined.Skip(combined.Length - TagSize).ToArray();
                var cipherLength = combined.Length - NonceSize - TagSize;
                if (cipherLength < 0)
                {
                    Console.WriteLine("Error de desencriptación: la longitud del cifrado es inválida.");
                    return string.Empty;
                }
                var cipher = combined.Skip(NonceSize).Take(cipherLength).ToArray();
                var plaintext = new byte[cipher.Length];
                using (var aesGcm = new AesGcm(key, TagSize))
                {
                    aesGcm.Decrypt(nonce, cipher, tag, plaintext);
                }

                return Encoding.UTF8.GetString(plaintext);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al desencriptar: {ex.Message}");
                return string.Empty;
            }
        }

        class TableInfo
        {
            public int cid { get; set; }
            public string name { get; set; } = string.Empty;
            public string type { get; set; } = string.Empty;
            public int notnull { get; set; }
            public string dflt_value { get; set; } = string.Empty;
            public int pk { get; set; }
        }

        class IdCorreo
        {
            public int idUsuario { get; set; }
            public string correo { get; set; } = string.Empty;
        }

        class IdBoleta
        {
            public int idUsuario { get; set; }
            public string boleta { get; set; } = string.Empty;
        }
    }
}