﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using OpenCvSharp.Tracking;
using Xunit;

namespace OpenCvSharp.Tests.Tracking
{
    public abstract class TrackerTestBase : TestBase
    {
        protected static void InitBase(Tracker tracker)
        {
            using (var vc = Image("lenna.png"))
            {
                var ret = tracker.Init(vc, new Rect2d(220, 60, 200, 220));
                Assert.True(ret);
            }
        }

        protected static async Task UpdateBaseAsync(Tracker tracker)
        {
            // ETHZ dataset
            // ETHZ is Eidgenössische Technische Hochschule Zürich, in Deutsch
            // https://data.vision.ee.ethz.ch/cvl/aess/cvpr2008/seq03-img-left.tar.gz
            // This video could be research data and it may not allow to use commercial use. 
            // This test can not track person perfectly but it is enough to test whether unit test works.

            // This rect indicates person who be captured in first frame
            var bb = new Rect2d(286, 146, 70, 180);

            // If you want to save markers image, you must change the following values.
            var path = Path.GetFullPath("TrackerTest_Update_Images");

            if (!Directory.Exists(path) || !Directory.EnumerateFiles(path, "*.png").Any())
            {
                Directory.CreateDirectory(path);

                using var stream =
                    await DownloadStreamAsync("https://data.vision.ee.ethz.ch/cvl/aess/cvpr2008/seq03-img-left.tar.gz")
                        .ConfigureAwait(false);
                using var gzStream = new GZipInputStream(stream);
                using var tarArchive = TarArchive.CreateInputTarArchive(gzStream, TarBuffer.DefaultBlockFactor);

                //tarArchive.AsciiTranslate = false;
                //tarArchive.SetUserInfo(0, "", 0, "None");
                tarArchive.ExtractContents(path);
            }

            foreach (var i in Enumerable.Range(0, 21))
            {
                var file = $"image_{i:D8}_0.png";
                
                using var mat = Image(Path.Combine(path, file));
                if (i == 0)
                {
                    tracker.Init(mat, bb);
                }
                else
                {
                    tracker.Update(mat, ref bb);
                }

                if (Debugger.IsAttached)
                {
                    Directory.CreateDirectory(path);
                    mat.Rectangle(
                        new Point((int) bb.X, (int) bb.Y),
                        new Point((int) (bb.X + bb.Width), (int) (bb.Y + bb.Height)),
                        new Scalar(0, 0, 255));
                    Cv2.ImWrite(Path.Combine(path, file), mat);
                }
            }
        }
    }
}
