using System.CommandLine;

namespace FileCrypt;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var fileArgument = new Argument<FileInfo?>(
            name: "file",
            description: "File to be encrypted/decrypted",
            parse: result =>
            {
                string? filePath = result.Tokens.SingleOrDefault()?.Value;
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    result.ErrorMessage = "File does not exist";
                    return null;
                }
                else
                {
                    return new FileInfo(filePath);
                }
            }
        );

        var keyOption = new Option<string?>(
            name: "--key",
            description: "Key to be used when encrypting/decrypting the file",
            isDefault: true,
            parseArgument: result =>
            {
                string? key = result.Tokens.SingleOrDefault()?.Value;
                return string.IsNullOrWhiteSpace(key) ? FileCryptLib.FileCrypt.DEFAULT_KEY : key.Trim();
            }
        );

        var encryptCommand = new Command("enc", "Encrypts a file");
        var decryptCommand = new Command("dec", "Derypts a file");

        encryptCommand.AddArgument(fileArgument);
        decryptCommand.AddArgument(fileArgument);

        encryptCommand.SetHandler(async (file, key) => await EncryptFile(file!, key!, CancellationToken.None), fileArgument, keyOption);
        decryptCommand.SetHandler(async (file, key) => await DecryptFile(file!, key!, CancellationToken.None), fileArgument, keyOption);

        var rootCommand = new RootCommand("FileCrypt: Utility to encrypt and decrypt files.");
        rootCommand.AddGlobalOption(keyOption);

        rootCommand.AddCommand(encryptCommand);
        rootCommand.AddCommand(decryptCommand);

        return await rootCommand.InvokeAsync(args);
    }

    private static async ValueTask EncryptFile(FileInfo file, string key, CancellationToken ct)
    {
        string encryptedName = file.FullName + ".enc";
        using var inputStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var outputStream = new FileStream(encryptedName, FileMode.Create, FileAccess.Write, FileShare.None);
        await FileCryptLib.FileCrypt.Encrypt(inputStream, outputStream, key, ct);
    }

    private static async ValueTask DecryptFile(FileInfo file, string key, CancellationToken ct)
    {
        string decryptedName = null!;
        string encryptedName = file.FullName;
        if (encryptedName.EndsWith(".enc", StringComparison.OrdinalIgnoreCase))
        {
            decryptedName = encryptedName[0..^4];
            if (File.Exists(decryptedName))
                decryptedName = null!;
        }

        decryptedName ??= encryptedName + ".dec";

        using var inputStream = new FileStream(encryptedName, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var outputStream = new FileStream(decryptedName, FileMode.Create, FileAccess.Write, FileShare.None);
        await FileCryptLib.FileCrypt.Decrypt(inputStream, outputStream, key, ct);
    }
}