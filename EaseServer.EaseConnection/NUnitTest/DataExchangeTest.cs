#if UnitTest
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Gwsoft.Ease.Proxy.Service;

namespace EaseServer.EaseConnection.NUnitTest
{
    public class DataExchangeTest
    {

        public void ErrorDocTest()
        {
            //string easeCode = Encoding.UTF8.GetString(File.ReadAllBytes(@"error-ease.xml"));
            //TagAnalyzer analyzer = new TagAnalyzer("http://118.123.205.185:8081/images/chuhan/", "file:///");
            //ProxyResponse resp = new ProxyResponse();
            //string[] linkedResList = analyzer.GetResourceFromCode(easeCode);
            //resp.EaseCode = analyzer.ProcessEaseTag(easeCode, linkedResList, ref resp.ResourceMappingDict);
            //Console.WriteLine(resp.EaseCode);

            Console.WriteLine(Int16.MaxValue);
        }

    }
}
#endif