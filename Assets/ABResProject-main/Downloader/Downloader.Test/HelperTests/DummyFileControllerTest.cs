//using System;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Net;
//using System.Threading.Tasks;

//namespace Downloader.Test.HelperTests
//{
//    public class DummyFileControllerTest
//    {
//        private readonly string contentType = "application/octet-stream";
//        private WebHeaderCollection headers;


//        private void ReadAndGetHeaders(string url, byte[] bytes, bool justFirst512Bytes = false)
//        {
//            try
//            {
//                HttpWebRequest request = WebRequest.CreateHttp(url);
//                request.Timeout = 10000; // 10sec
//                if (justFirst512Bytes)
//                    request.AddRange(0, 511);

//                HttpWebResponse downloadResponse = request.GetResponse() as HttpWebResponse;
//                var respStream = downloadResponse.GetResponseStream();

//                // keep response headers
//                downloadResponse.Headers.Add(nameof(WebResponse.ResponseUri), downloadResponse.ResponseUri.ToString());
//                headers = downloadResponse.Headers;

//                // read stream data
//                var readCount = 1;
//                var offset = 0;
//                while (readCount > 0)
//                {
//                    var count = bytes.Length - offset;
//                    if (count <= 0)
//                        break;

//                    readCount = respStream.Read(bytes, offset, count);
//                    offset += readCount;
//                }
//            }
//            catch(Exception exp)
//            {
//                Console.Error.WriteLine(exp.Message);
//                Debugger.Break();
//                throw;
//            }
//        }
//    }
//}
