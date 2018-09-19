using System;
using System.Collections.Generic;

namespace ScreenShooter.Gun.Pdf.Contracts
{
    public interface IPdfCreator
    {
        Byte[] CreateDocument(Int32 width, Int32 height, IEnumerable<Byte[]> images);
    }
}