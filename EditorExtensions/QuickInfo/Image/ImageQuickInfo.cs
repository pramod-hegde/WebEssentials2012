﻿using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace MadsKristensen.EditorExtensions
{
    internal class ImageQuickInfo : IQuickInfoSource
    {
        private readonly ITextBuffer _buffer;
        private CssTree _tree;

        public ImageQuickInfo(ITextBuffer subjectBuffer)
        {
            _buffer = subjectBuffer;
        }

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> qiContent, out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;

            if (!EnsureTreeInitialized() || session == null || qiContent == null)
                return;

            SnapshotPoint? point = session.GetTriggerPoint(_buffer.CurrentSnapshot);
            if (!point.HasValue)
                return;

            ParseItem item = _tree.StyleSheet.ItemBeforePosition(point.Value.Position);
            if (item == null || !item.IsValid)
                return;

            UrlItem urlItem = item.FindType<UrlItem>();

            if (urlItem != null && urlItem.UrlString != null && urlItem.UrlString.IsValid)
            {
                string url = GetFileName(urlItem.UrlString.Text.Trim('\'', '"'));
                if (!string.IsNullOrEmpty(url))
                {
                    applicableToSpan = _buffer.CurrentSnapshot.CreateTrackingSpan(point.Value.Position, 1, SpanTrackingMode.EdgeNegative);
                    var image = CreateImage(url);
                    if (image != null && image.Source != null)
                    {
                        qiContent.Add(image);
                        qiContent.Add(Math.Round(image.Source.Width) + "x" + Math.Round(image.Source.Height));
                    }
                }
            }
        }

        /// <summary>
        /// This must be delayed so that the TextViewConnectionListener
        /// has a chance to initialize the WebEditor host.
        /// </summary>
        public bool EnsureTreeInitialized()
        {
            if (_tree == null)
            {
                try
                {
                    CssEditorDocument document = CssEditorDocument.FromTextBuffer(_buffer);
                    _tree = document.Tree;
                }
                catch (Exception)
                {
                }
            }

            return _tree != null;
        }

        public static string GetFileName(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                if (text.StartsWith("data:", StringComparison.Ordinal))
                    return text;

                string imageUrl = text.Trim(new[] { '\'', '"' });
                
                string filePath = string.Empty;

                if (text.StartsWith("//", StringComparison.Ordinal))
                    text = "http:" + text;

                if (text.StartsWith("http://", StringComparison.Ordinal) || text.Contains(";base64,"))
                {
                    return text;
                }
                else if (imageUrl.StartsWith("/", StringComparison.Ordinal))
                {
                    string root = ProjectHelpers.GetProjectFolder(EditorExtensionsPackage.DTE.ActiveDocument.FullName);
                    if (root.Contains("://"))
                        return root + imageUrl;
                    else if (!string.IsNullOrEmpty(root))
                        filePath = root + imageUrl;// new FileInfo(root).Directory + imageUrl;
                }
                else if (EditorExtensionsPackage.DTE.ActiveDocument != null)
                {
                    FileInfo fi = new FileInfo(EditorExtensionsPackage.DTE.ActiveDocument.FullName);
                    DirectoryInfo dir = fi.Directory;
                    while (imageUrl.Contains("../"))
                    {
                        imageUrl = imageUrl.Remove(imageUrl.IndexOf("../", StringComparison.Ordinal), 3);
                        dir = dir.Parent;
                    }

                    filePath = Path.Combine(dir.FullName, imageUrl.Replace("/", "\\"));
                }

                return File.Exists(filePath) ? filePath : "pack://application:,,,/WebEssentials2012;component/Resources/nopreview.png";
            }

            return null;
        }

        public static Image CreateImage(string file)
        {
            try
            {
                var image = new Image();

                if (file.StartsWith("data:", StringComparison.Ordinal))
                {
                    int index = file.IndexOf("base64,", StringComparison.Ordinal) + 7;
                    byte[] imageBytes = Convert.FromBase64String(file.Substring(index));
                    
                    using (MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
                    {
                        image.Source = BitmapFrame.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                    }
                }
                else if (File.Exists(file))
                {
                    image.Source = BitmapFrame.Create(new Uri(file), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                }

                return image;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
