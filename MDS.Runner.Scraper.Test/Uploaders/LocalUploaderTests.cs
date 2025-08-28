using FluentAssertions;
using MDS.Runner.Scraper.Services;

namespace MDS.Runner.Scraper.Test.Uploaders;

public sealed class LocalUploaderTests
{
    string NewTempDir()
    {
        var d = Path.Combine(Path.GetTempPath(), "mds-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(d);
        return d;
    }

    string NewTempFile(string contents)
    {
        var f = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".csv");
        File.WriteAllText(f, contents);
        return f;
    }

    [Fact]
    public async Task SaveAsync_Creates_Directory_And_Copies_File()
    {
        var baseDir = NewTempDir();
        var src = NewTempFile("a,b,c\n1,2,3");
        var uploader = new LocalUploader(baseDir);

        await uploader.SaveAsync("acheiusa", src);

        var expectedName = Path.GetFileName(src);
        var destDir = Path.Combine(baseDir, "acheiusa");
        var destPath = Path.Combine(destDir, expectedName);

        File.Exists(destPath).Should().BeTrue();
        File.ReadAllText(destPath).Should().Be("a,b,c\n1,2,3");
    }

    [Fact]
    public async Task SaveAsync_Overwrites_Existing_File()
    {
        var baseDir = NewTempDir();
        var fileName = "data.csv";
        var src1 = NewTempFile("v1");
        var src2 = NewTempFile("v2");

        var fixedSrc1 = Path.Combine(Path.GetDirectoryName(src1)!, fileName);
        var fixedSrc2 = Path.Combine(Path.GetDirectoryName(src2)!, fileName);
        File.Copy(src1, fixedSrc1, overwrite: true);
        File.Copy(src2, fixedSrc2, overwrite: true);

        var uploader = new LocalUploader(baseDir);

        await uploader.SaveAsync("opajuda", fixedSrc1);
        await uploader.SaveAsync("opajuda", fixedSrc2);

        var destPath = Path.Combine(baseDir, "opajuda", fileName);
        File.ReadAllText(destPath).Should().Be("v2");
    }
}