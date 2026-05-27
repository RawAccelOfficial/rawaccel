using System;
using System.IO;

namespace userspace_backend.IO
{
    public abstract class ReaderWriterBase<T>
    {
        protected abstract string FileType { get; }

        public void Write(string path, T toWrite)
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path))
            {
                throw new ArgumentException($"Not a valid path: [{path}]", nameof(path));
            }

            var parent = Directory.GetParent(path)?.FullName;

            if (parent != null && !Directory.Exists(parent))
            {
                Directory.CreateDirectory(parent);
            }

            var serialized = Serialize(toWrite);

            using (StreamWriter outputFile = new StreamWriter(path))
            {
                outputFile.Write(serialized);
            }
        }

        public T Read(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }

            string fileText;
            using (StreamReader fileToRead = new StreamReader(path))
            {
                fileText = fileToRead.ReadToEnd();
            }

            if (string.IsNullOrWhiteSpace(fileText))
            {
                throw new Exception($"{FileType} file is empty.");
            }

            T? readIn;
            try
            {
                readIn = Deserialize(fileText);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing {FileType} file at path {path}", ex);
            }

            if (readIn is null)
            {
                throw new Exception($"{FileType} file deserialized to null at path {path}.");
            }

            return readIn;
        }

        public abstract string Serialize(T toWrite);

        public abstract T? Deserialize(string toRead);
    }
}
