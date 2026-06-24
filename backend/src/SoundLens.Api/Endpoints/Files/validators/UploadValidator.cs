using FluentValidation;
using SoundLens.Api.Endpoints.Files.command;
using System;

namespace SoundLens.Api.Endpoints.Files.validators;

public sealed class UploadValidator : AbstractValidator<UploadCommand>
{
    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50MB

    public UploadValidator()
    {
        RuleFor(x => x.File)
            .NotNull()
            .WithMessage("No file provided")
            .Must(file => file.Length > 0)
            .WithMessage("File is empty")
            .Must(file => file.Length <= MaxFileSizeBytes)
            .WithMessage($"File size exceeds maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)}MB")
            .Must(file => !string.IsNullOrEmpty(file.FileName))
            .WithMessage("File name is missing")
            .Must(file =>
            {
                try
                {
                    return Path.GetExtension(file.FileName).Equals(".wav", StringComparison.OrdinalIgnoreCase);
                }
                catch (ArgumentException)
                {
                    return false;
                }
            })
            .WithMessage("Only WAV files are currently supported");
    }

    public async Task<bool> ValidateFileContentAsync(IFormFile file, CancellationToken ct)
    {
        const int headerSize = 12; // RIFF header size
        var buffer = new byte[headerSize];
        
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(buffer, 0, headerSize, ct);
        
        if (bytesRead < headerSize)
        {
            return false;
        }

        // Check for RIFF header
        if (buffer[0] != 'R' || buffer[1] != 'I' || buffer[2] != 'F' || buffer[3] != 'F')
        {
            return false;
        }

        // Check for WAVE format
        if (buffer[8] != 'W' || buffer[9] != 'A' || buffer[10] != 'V' || buffer[11] != 'E')
        {
            return false;
        }

        return true;
    }

}
