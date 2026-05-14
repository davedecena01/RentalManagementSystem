using System.Text.RegularExpressions;

namespace RentalManagementApi.Tests;

public class StoragePathValidationTests
{
    // The same regex used in StorageController
    private static readonly Regex SafePathPattern = new(@"^[a-zA-Z0-9._-]+$", RegexOptions.Compiled);

    [Theory]
    [InlineData("file.jpg", true)]
    [InlineData("maintenance-1234567890.jpg", true)]
    [InlineData("tenant-id-1716000000.pdf", true)]
    [InlineData("payment-proof_abc123.png", true)]
    [InlineData("../../etc/passwd", false)]
    [InlineData("file/path.jpg", false)]
    [InlineData("../secrets.txt", false)]
    [InlineData("file path.jpg", false)]
    [InlineData("file;rm -rf /.jpg", false)]
    [InlineData("", false)]
    public void SafePathPattern_AcceptsAndRejectsCorrectly(string path, bool expected)
    {
        var result = !string.IsNullOrWhiteSpace(path) && SafePathPattern.IsMatch(path);
        Assert.Equal(expected, result);
    }
}
