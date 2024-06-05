using System;

namespace Triband.Validation.Editor.Data
{
    public readonly struct SourceInfo
    {
        public bool Equals(SourceInfo other)
        {
            return filePath == other.filePath && sourceLine == other.sourceLine;
        }

        public override bool Equals(object obj)
        {
            return obj is SourceInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(filePath, sourceLine);
        }

        public readonly string filePath;
        public readonly int sourceLine;

        public SourceInfo(string filePath, int sourceLine)
        {
            this.filePath = filePath;
            this.sourceLine = sourceLine;
        }
    }
}