using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PDFtoBook
{
    [Serializable]
    public sealed class BookRecord
    {
        public string id;
        public string title;
        public int pageCount;
        public int page;
        public long lastRead;
        public List<int> bookmarks = new List<int>();
        public string Progress => $"Página {page + 1} de {pageCount}  ·  {Mathf.RoundToInt(100f * (page + 1) / Math.Max(1, pageCount))}%";
    }

    [Serializable]
    public sealed class LibraryState
    {
        public int version = 1;
        public bool dark = true;
        public List<BookRecord> books = new List<BookRecord>();
    }

    /// <summary>Versioned JSON with a flushed temporary file and previous-version backup.</summary>
    public sealed class LibraryStore
    {
        public string Root { get; }
        public LibraryState State { get; private set; }
        public string RecoveryNotice { get; private set; }
        private bool preserveBackupOnSave;
        private string IndexPath => Path.Combine(Root, "library.json");
        public LibraryStore(string root)
        {
            Root = root;
            Directory.CreateDirectory(root);
            if (!File.Exists(IndexPath) && !File.Exists(IndexPath + ".bak")) { State = new LibraryState(); return; }
            try { State = Read(IndexPath); }
            catch (Exception) {
                State = Read(IndexPath + ".bak");
                preserveBackupOnSave = true;
                RecoveryNotice = "A biblioteca foi recuperada da última cópia local.";
            }
        }
        private static LibraryState Read(string path)
        {
            var state = JsonUtility.FromJson<LibraryState>(File.ReadAllText(path));
            if (state == null || state.version != 1 || state.books == null) throw new InvalidDataException("Biblioteca inválida.");
            foreach (var book in state.books) {
                if (book == null || !Guid.TryParse(book.id, out _) || book.pageCount < 1) throw new InvalidDataException("Livro inválido.");
                book.page = Mathf.Clamp(book.page, 0, book.pageCount - 1);
                book.bookmarks ??= new List<int>();
                book.bookmarks.RemoveAll(page => page < 0 || page >= book.pageCount);
            }
            return state;
        }
        public string PdfPath(BookRecord book) => Path.Combine(Root, book.id + ".pdf");
        public IEnumerable<BookRecord> SortedBooks => State.books.OrderByDescending(book => book.lastRead);
        public void Save()
        {
            string temp = IndexPath + ".tmp";
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(State, true));
            using (var output = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None)) {
                output.Write(bytes, 0, bytes.Length);
                output.Flush(true);
            }
            if (File.Exists(IndexPath)) File.Replace(temp, IndexPath, preserveBackupOnSave ? null : IndexPath + ".bak");
            else File.Move(temp, IndexPath);
            preserveBackupOnSave = false;
        }
    }
}