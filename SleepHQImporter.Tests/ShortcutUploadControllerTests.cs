using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Uplink.Applications.Websites.CorporateSites.UplinkBg.Controllers;

namespace SleepHQImporter.Tests;

[TestClass]
public class ShortcutUploadControllerTests
{
    [TestMethod]
    public void CalculateContentHash_ReturnsExpectedHash_ForSimpleContent()
    {
        // Arrange
        var content = "Hello, World!";
        var fileName = "test.txt";
        using var stream = new MemoryStream(Encoding.Latin1.GetBytes(content));

        // Act
        var hash = ShortcutUploadController.CalculateContentHash(stream, fileName);

        // Assert
        var expectedInput = content + fileName.ToLowerInvariant();
        var expectedHash = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(expectedInput))).ToLowerInvariant();
        Assert.AreEqual(expectedHash, hash);
    }

    [TestMethod]
    public void CalculateContentHash_ReturnsLowercaseHexString()
    {
        // Arrange
        var content = "test content";
        var fileName = "file.txt";
        using var stream = new MemoryStream(Encoding.Latin1.GetBytes(content));

        // Act
        var hash = ShortcutUploadController.CalculateContentHash(stream, fileName);

        // Assert
        Assert.AreEqual(hash, hash.ToLowerInvariant());
        Assert.IsTrue(Regex.IsMatch(hash, "^[0-9a-f]{32}$"));
    }

    [TestMethod]
    public void CalculateContentHash_ConvertsFileNameToLowercase()
    {
        // Arrange
        var content = "sample data";
        var upperFileName = "TEST.TXT";
        var lowerFileName = "test.txt";
        using var stream1 = new MemoryStream(Encoding.Latin1.GetBytes(content));
        using var stream2 = new MemoryStream(Encoding.Latin1.GetBytes(content));

        // Act
        var hashUpper = ShortcutUploadController.CalculateContentHash(stream1, upperFileName);
        var hashLower = ShortcutUploadController.CalculateContentHash(stream2, lowerFileName);

        // Assert
        Assert.AreEqual(hashLower, hashUpper);
    }

    [TestMethod]
    public void CalculateContentHash_ResetsStreamPosition()
    {
        // Arrange
        var content = "stream position test";
        var fileName = "position.txt";
        using var stream = new MemoryStream(Encoding.Latin1.GetBytes(content));

        // Act
        _ = ShortcutUploadController.CalculateContentHash(stream, fileName);

        // Assert
        Assert.AreEqual(0L, stream.Position);
    }

    [TestMethod]
    public void CalculateContentHash_HandlesEmptyContent()
    {
        // Arrange
        var content = "";
        var fileName = "empty.txt";
        using var stream = new MemoryStream(Encoding.Latin1.GetBytes(content));

        // Act
        var hash = ShortcutUploadController.CalculateContentHash(stream, fileName);

        // Assert
        var expectedInput = content + fileName.ToLowerInvariant();
        var expectedHash = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(expectedInput))).ToLowerInvariant();
        Assert.AreEqual(expectedHash, hash);
    }

    [TestMethod]
    public void CalculateContentHash_HandlesSpecialCharactersInFileName()
    {
        // Arrange
        var content = "data";
        var fileName = "file-with_special.chars (1).txt";
        using var stream = new MemoryStream(Encoding.Latin1.GetBytes(content));

        // Act
        var hash = ShortcutUploadController.CalculateContentHash(stream, fileName);

        // Assert
        var expectedInput = content + fileName.ToLowerInvariant();
        var expectedHash = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(expectedInput))).ToLowerInvariant();
        Assert.AreEqual(expectedHash, hash);
    }

    [TestMethod]
    public void CalculateContentHash_HandlesBinaryContent()
    {
        // Arrange
        var binaryContent = new byte[] { 0x00, 0x01, 0x02, 0xFF, 0xFE, 0x80 };
        var fileName = "binary.dat";
        using var stream = new MemoryStream(binaryContent);

        // Act
        var hash = ShortcutUploadController.CalculateContentHash(stream, fileName);

        // Assert
        Assert.IsTrue(Regex.IsMatch(hash, "^[0-9a-f]{32}$"));
    }

    [TestMethod]
    public void CalculateContentHash_DifferentContentProducesDifferentHash()
    {
        // Arrange
        var fileName = "same.txt";
        using var stream1 = new MemoryStream(Encoding.Latin1.GetBytes("content1"));
        using var stream2 = new MemoryStream(Encoding.Latin1.GetBytes("content2"));

        // Act
        var hash1 = ShortcutUploadController.CalculateContentHash(stream1, fileName);
        var hash2 = ShortcutUploadController.CalculateContentHash(stream2, fileName);

        // Assert
        Assert.AreNotEqual(hash1, hash2);
    }

    [TestMethod]
    public void CalculateContentHash_DifferentFileNameProducesDifferentHash()
    {
        // Arrange
        var content = "same content";
        using var stream1 = new MemoryStream(Encoding.Latin1.GetBytes(content));
        using var stream2 = new MemoryStream(Encoding.Latin1.GetBytes(content));

        // Act
        var hash1 = ShortcutUploadController.CalculateContentHash(stream1, "file1.txt");
        var hash2 = ShortcutUploadController.CalculateContentHash(stream2, "file2.txt");

        // Assert
        Assert.AreNotEqual(hash1, hash2);
    }

    [TestMethod]
    public void CalculateContentHash_SameInputProducesSameHash()
    {
        // Arrange
        var content = "reproducible content";
        var fileName = "reproducible.txt";
        using var stream1 = new MemoryStream(Encoding.Latin1.GetBytes(content));
        using var stream2 = new MemoryStream(Encoding.Latin1.GetBytes(content));

        // Act
        var hash1 = ShortcutUploadController.CalculateContentHash(stream1, fileName);
        var hash2 = ShortcutUploadController.CalculateContentHash(stream2, fileName);

        // Assert
        Assert.AreEqual(hash1, hash2);
    }
}
