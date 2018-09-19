using System;
using System.Collections.Generic;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using ScreenShooter.Gun.Pdf.Contracts;

namespace ScreenShooter.Gun.Pdf.ITextSharp
{
    public class PdfCreator : IPdfCreator
    {
        public Byte[] CreateDocument(Int32 width, Int32 height, IEnumerable<Byte[]> images)
        {
            var document = new Document(new Rectangle(width, height), 0, 0, 0, 0);
            Byte[] documentBytes;
            
            using (var documentStream = new MemoryStream())
            {
                var pdf = new PdfCopy(document, documentStream);
                document.Open();

                foreach (var pageImagePart in images)
                {
                    document.NewPage();
                    var imageDocument = new Document(new Rectangle(width, height), 0, 0, 0, 0);

                    using (var imageStream = new MemoryStream())
                    {
                        var imageDocumentWriter = PdfWriter.GetInstance(imageDocument, imageStream);
                        imageDocument.Open();
                        if (!imageDocument.NewPage())
                        {
                            throw new Exception("Unable add page");
                        }

                        var image = Image.GetInstance(pageImagePart);
                        image.Alignment = Element.ALIGN_TOP;
                        image.ScaleToFitHeight = true;
                        image.ScaleToFit(width, height);

                        if (!imageDocument.Add(image))
                        {
                            throw new Exception("Unable add image");
                        }

                        imageDocument.Close();
                        imageDocumentWriter.Close();

                        var imageDocumentReader = new PdfReader(imageStream.ToArray());
                        var page = pdf.GetImportedPage(imageDocumentReader, 1);
                        pdf.AddPage(page);
                        imageDocumentReader.Close();
                    }
                }

                if (document.IsOpen())
                {
                    document.Close();
                }
                documentBytes = documentStream.ToArray();
            }
            
            return documentBytes;
        }
    }
}