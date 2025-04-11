/*
    Source: https://github.com/Tyrrrz/GitHubActionsTestLogger/blob/924bc77df7530d447a2c13c6727d7e61e41ff06d/GitHubActionsTestLogger/Utils/ContentionTolerantWriteFileStream.cs
    License: https://github.com/Tyrrrz/GitHubActionsTestLogger/blob/924bc77df7530d447a2c13c6727d7e61e41ff06d/License.txt
    MIT License

    Copyright (c) 2020-2024 Oleksii Holub

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.

*/
using System.Diagnostics.CodeAnalysis;
namespace WingetIndexGenerator.Util;

internal class GithubFileStream(string filePath, FileMode fileMode) : Stream
{
    private readonly List<byte> _buffer = new(1024);
    private readonly Random _random = new();

    [ExcludeFromCodeCoverage]
    public override bool CanRead => false;

    [ExcludeFromCodeCoverage]
    public override bool CanSeek => false;

    [ExcludeFromCodeCoverage]
    public override bool CanWrite => true;

    [ExcludeFromCodeCoverage]
    public override long Length => _buffer.Count;

    [ExcludeFromCodeCoverage]
    public override long Position { get; set; }

    // Backoff and retry if the file is locked
    private FileStream CreateInnerStream()
    {
        for (var retriesRemaining = 10; ; retriesRemaining--)
        {
            try
            {
                return new FileStream(filePath, fileMode);
            }
            catch (IOException) when (retriesRemaining > 0)
            {
                // Variance in delay to avoid overlapping back-offs
                Thread.Sleep(_random.Next(200, 1000));
            }
        }
    }

    public override void Write(byte[] buffer, int offset, int count) =>
        _buffer.AddRange(buffer.Skip(offset).Take(count));

    public override void Flush()
    {
        using var stream = CreateInnerStream();
        stream.Write(_buffer.ToArray(), 0, _buffer.Count);
        _buffer.Clear();
    }

    [ExcludeFromCodeCoverage]
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _buffer.Clear();
    }

    [ExcludeFromCodeCoverage]
    public override int Read(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();

    [ExcludeFromCodeCoverage]
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    [ExcludeFromCodeCoverage]
    public override void SetLength(long value) => throw new NotSupportedException();
}