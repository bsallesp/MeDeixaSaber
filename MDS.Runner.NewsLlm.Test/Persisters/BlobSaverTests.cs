using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FluentAssertions;
using MDS.Runner.NewsLlm.Persisters;
using Xunit;

namespace MDS.Tests.Persisters
{
    public sealed class BlobSaverTests
    {
        sealed class FakeBlobClient : BlobClient
        {
            readonly Uri _uri;
            readonly Action<string, byte[]> _onUpload;
            readonly string _name;

            public FakeBlobClient(Uri uri, string name, Action<string, byte[]> onUpload)
            {
                _uri = uri;
                _name = name;
                _onUpload = onUpload;
            }

            public override Uri Uri => _uri;

            public override Task<Response<BlobContentInfo>> UploadAsync(
                Stream content,
                BlobUploadOptions options,
                CancellationToken cancellationToken = default)
            {
                using var ms = new MemoryStream();
                content.CopyTo(ms);
                _onUpload(_name, ms.ToArray());

                return Task.FromResult<Response<BlobContentInfo>>(null!);
            }
        }

        sealed class FakeBlobContainerClient : BlobContainerClient
        {
            readonly Uri _uri;
            readonly string _name;
            readonly ConcurrentDictionary<string, byte[]> _store = new();

            public FakeBlobContainerClient(Uri uri, string name)
            {
                _uri = uri;
                _name = name;
            }

            public int EnsureCalls { get; private set; }
            public IReadOnlyDictionary<string, byte[]> Store => _store;
            public override string Name => _name;
            public override Uri Uri => _uri;

            public override Task<Response<BlobContainerInfo>> CreateIfNotExistsAsync(
                PublicAccessType publicAccessType = PublicAccessType.None,
                IDictionary<string, string>? metadata = null,
                BlobContainerEncryptionScopeOptions? encryptionScopeOptions = null,
                CancellationToken cancellationToken = default)
            {
                EnsureCalls++;
                return Task.FromResult<Response<BlobContainerInfo>>(null!);
            }

            public override BlobClient GetBlobClient(string blobName)
            {
                return new FakeBlobClient(new Uri(_uri, blobName), blobName, (n, b) => _store[n] = b);
            }
        }

        [Fact]
        public async Task SaveJsonAsync_SavesToContainer_WithExpectedNamePattern_AndContent()
        {
            var baseUri = new Uri("https://acct.blob.core.windows.net/news-org/");
            var container = new FakeBlobContainerClient(baseUri, "news-org");
            var sut = new BlobSaver(container);

            var json = """{"a":1}""";
            var uri = await sut.SaveJsonAsync(json, "newsapi-everything");

            container.EnsureCalls.Should().Be(1);
            uri.Should().NotBeNull();
            uri.ToString().Should().StartWith(baseUri.ToString());

            var name = uri.ToString().Substring(baseUri.ToString().Length);
            Regex.IsMatch(name, @"^dt=\d{4}-\d{2}-\d{2}/newsapi-everything-\d{9}-[a-f0-9]{32}\.json$").Should().BeTrue();

            container.Store.Should().ContainKey(name);
            Encoding.UTF8.GetString(container.Store[name]).Should().Be(json);
        }

        [Theory]
        [InlineData(null, "p")]
        [InlineData("", "p")]
        [InlineData("   ", "p")]
        [InlineData("{}", null)]
        [InlineData("{}", "")]
        [InlineData("{}", "   ")]
        public async Task SaveJsonAsync_InvalidArgs_Throws(string json, string prefix)
        {
            var container = new FakeBlobContainerClient(new Uri("https://acct.blob.core.windows.net/c/"), "c");
            var sut = new BlobSaver(container);
            var act = () => sut.SaveJsonAsync(json!, prefix!);
            await act.Should().ThrowAsync<ArgumentException>();
        }
    }
}
