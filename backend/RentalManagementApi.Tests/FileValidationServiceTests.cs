using RentalManagementApi.Application.Services;

namespace RentalManagementApi.Tests;

public class FileValidationServiceTests
{
    [Fact]
    public void HasValidMagicBytes_AcceptsJpeg()
    {
        var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        Assert.True(FileValidationService.HasValidMagicBytes(bytes));
    }

    [Fact]
    public void HasValidMagicBytes_AcceptsPng()
    {
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x00 };
        Assert.True(FileValidationService.HasValidMagicBytes(bytes));
    }

    [Fact]
    public void HasValidMagicBytes_AcceptsWebP()
    {
        // RIFF....WEBP
        var bytes = new byte[] { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50 };
        Assert.True(FileValidationService.HasValidMagicBytes(bytes));
    }

    [Fact]
    public void HasValidMagicBytes_AcceptsPdf()
    {
        // %PDF
        var bytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        Assert.True(FileValidationService.HasValidMagicBytes(bytes));
    }

    [Fact]
    public void HasValidMagicBytes_RejectsExe()
    {
        // MZ header — Windows executable
        var bytes = new byte[] { 0x4D, 0x5A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        Assert.False(FileValidationService.HasValidMagicBytes(bytes));
    }

    [Fact]
    public void HasValidMagicBytes_RejectsShortBuffer()
    {
        var bytes = new byte[] { 0xFF, 0xD8 }; // too short
        Assert.False(FileValidationService.HasValidMagicBytes(bytes));
    }
}
